namespace HollowWardens.Tests.Systems;

using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using Xunit;

public class ElementSystemTests : IDisposable
{
    private readonly ElementSystem _sut = new();

    public void Dispose() => GameEvents.ClearAll();

    [Fact]
    public void AddElementsIncreasesPool()
    {
        _sut.AddElements(new[] { Element.Root });
        Assert.Equal(1, _sut.Get(Element.Root));
    }

    [Fact]
    public void DecayReducesByOne()
    {
        _sut.AddElements(new[] { Element.Root, Element.Root });
        _sut.Decay();
        Assert.Equal(1, _sut.Get(Element.Root));
    }

    [Fact]
    public void DecayDoesNotGoBelowZero()
    {
        _sut.Decay();
        Assert.Equal(0, _sut.Get(Element.Root));
    }

    [Fact]
    public void Tier1FiresAt4()
    {
        int firedTier = 0;
        Element firedElement = Element.Mist;
        GameEvents.ThresholdTriggered += (e, t) => { firedElement = e; firedTier = t; };

        _sut.AddElements(new[] { Element.Root, Element.Root, Element.Root, Element.Root });

        Assert.Equal(1, firedTier);
        Assert.Equal(Element.Root, firedElement);
    }

    [Fact]
    public void Tier1DoesNotFireAt3()
    {
        bool fired = false;
        GameEvents.ThresholdTriggered += (_, _) => fired = true;

        _sut.AddElements(new[] { Element.Root, Element.Root, Element.Root });

        Assert.False(fired);
    }

    [Fact]
    public void Tier2FiresAt7()
    {
        var firedTiers = new List<int>();
        GameEvents.ThresholdTriggered += (_, t) => firedTiers.Add(t);

        _sut.AddElements(Enumerable.Repeat(Element.Root, 7).ToArray());

        Assert.Contains(2, firedTiers);
    }

    [Fact]
    public void Tier3FiresAt11()
    {
        var firedTiers = new List<int>();
        GameEvents.ThresholdTriggered += (_, t) => firedTiers.Add(t);

        _sut.AddElements(Enumerable.Repeat(Element.Root, 11).ToArray());

        Assert.Contains(3, firedTiers);
    }

    [Fact]
    public void ThresholdFiresOncePerTurnPerTier()
    {
        int tier1Count = 0;
        GameEvents.ThresholdTriggered += (_, t) => { if (t == 1) tier1Count++; };

        _sut.AddElements(Enumerable.Repeat(Element.Root, 4).ToArray()); // pool = 4, T1 fires
        _sut.AddElements(new[] { Element.Root });                        // pool = 5, T1 already fired

        Assert.Equal(1, tier1Count);
    }

    [Fact]
    public void Tier1InVigilAndTier2InDuskSameTurn()
    {
        var firedTiers = new List<int>();
        GameEvents.ThresholdTriggered += (_, t) => firedTiers.Add(t);

        // Vigil: add 4 Root → T1 fires
        _sut.AddElements(Enumerable.Repeat(Element.Root, 4).ToArray());
        // Dusk: add 3 more Root → pool = 7, T2 fires; T1 already tracked so skipped
        _sut.AddElements(Enumerable.Repeat(Element.Root, 3).ToArray());

        Assert.Contains(1, firedTiers);
        Assert.Contains(2, firedTiers);
        Assert.Equal(2, firedTiers.Count); // each tier fires exactly once
    }

    [Fact]
    public void BottomDoubleMultiplier()
    {
        _sut.AddElements(new[] { Element.Root }, multiplier: 2);
        Assert.Equal(2, _sut.Get(Element.Root));
    }

    [Fact]
    public void RestTurnCarryoverCheckFires()
    {
        // Build pool to 5 (above T1=4), then decay to 4, then rest-turn check should re-fire T1
        _sut.AddElements(Enumerable.Repeat(Element.Root, 5).ToArray()); // pool = 5, T1 fires
        _sut.Decay(); // pool = 4

        bool fired = false;
        GameEvents.ThresholdTriggered += (_, _) => fired = true;

        _sut.OnRestTurn(); // resets fired-tracking, then checks: pool = 4 ≥ 4 → T1 fires

        Assert.True(fired);
    }

    [Fact]
    public void EngineBuilding5TurnsMatchesMathTable()
    {
        // Root×2 per turn (bottom card). Pool after add: 2,3,4,5,6. After decay: 1,2,3,4,5.
        // T1 (threshold=4) first fires on turn 3.
        var t1FiredOnTurns = new List<int>();

        for (int turn = 1; turn <= 5; turn++)
        {
            _sut.OnNewTurn();

            int capturedTurn = turn;
            void OnThreshold(Element e, int t)
            {
                if (e == Element.Root && t == 1) t1FiredOnTurns.Add(capturedTurn);
            }

            GameEvents.ThresholdTriggered += OnThreshold;
            _sut.AddElements(new[] { Element.Root }, multiplier: 2);
            GameEvents.ThresholdTriggered -= OnThreshold;

            _sut.Decay();
        }

        Assert.DoesNotContain(1, t1FiredOnTurns);
        Assert.DoesNotContain(2, t1FiredOnTurns);
        Assert.Contains(3, t1FiredOnTurns);
        Assert.Contains(4, t1FiredOnTurns);
        Assert.Contains(5, t1FiredOnTurns);
    }

    [Fact]
    public void BankedEffectLostOnDecay()
    {
        _sut.AddElements(Enumerable.Repeat(Element.Root, 4).ToArray()); // T1 fires → banked

        Assert.NotEmpty(_sut.GetBankedEffects());

        _sut.Decay(); // ClearBanked called internally

        Assert.Empty(_sut.GetBankedEffects());
    }
}
