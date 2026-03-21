namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Turn;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Verifies that PlayBottom:
/// - Resolves BottomEffect (not TopEffect)
/// - Adds elements with multiplier 2 (not 1)
/// - Also resolves BottomSecondary when present
/// </summary>
public class BottomPlayTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private static (EncounterState state, TurnManager tm) Setup(List<Card> hand)
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 6);
        var warden = new RootAbility();
        var (state, _, _, _, _) = IntegrationHelpers.Build(hand, warden, config, handLimit: hand.Count);
        var tm = new TurnManager(state, new EffectResolver());
        tm.StartVigil();
        tm.EndVigil();
        tm.StartDusk();
        return (state, tm);
    }

    [Fact]
    public void BottomPlay_UsesBottomEffect_NotTopEffect()
    {
        // Top generates 1 fear; Bottom generates 5 fear.
        // Playing bottom should generate exactly 5 fear (via Dread direct call).
        var card = new Card
        {
            Id = "test", Name = "test",
            Elements = Array.Empty<Element>(),
            TopEffect    = new EffectData { Type = EffectType.GenerateFear, Value = 1 },
            BottomEffect = new EffectData { Type = EffectType.GenerateFear, Value = 5 }
        };

        var (state, tm) = Setup(new List<Card> { card });

        int fearBefore = state.Dread!.TotalFearGenerated;
        tm.PlayBottom(card);

        Assert.Equal(fearBefore + 5, state.Dread.TotalFearGenerated); // bottom effect (5), not top (1)
    }

    [Fact]
    public void BottomPlay_AddsElements_WithMultiplierTwo()
    {
        // Card has [Root]. Playing bottom should add Root×2 to the element pool.
        var card = new Card
        {
            Id = "test", Name = "test",
            Elements     = new[] { Element.Root },
            TopEffect    = new EffectData { Type = EffectType.PlacePresence },
            BottomEffect = new EffectData { Type = EffectType.PlacePresence }
        };

        var (state, tm) = Setup(new List<Card> { card });

        int rootBefore = state.Elements!.Get(Element.Root);
        tm.PlayBottom(card);

        Assert.Equal(rootBefore + 2, state.Elements.Get(Element.Root)); // multiplier=2
    }

    [Fact]
    public void BottomPlay_ResolvesBottomSecondary_WhenPresent()
    {
        // Bottom generates 0 fear; BottomSecondary generates 3 fear.
        // Both should be resolved, total fear = 3.
        var card = new Card
        {
            Id = "test", Name = "test",
            Elements        = Array.Empty<Element>(),
            TopEffect       = new EffectData { Type = EffectType.GenerateFear, Value = 0 },
            BottomEffect    = new EffectData { Type = EffectType.GenerateFear, Value = 0 },
            BottomSecondary = new EffectData { Type = EffectType.GenerateFear, Value = 3 }
        };

        var (state, tm) = Setup(new List<Card> { card });

        int fearBefore = state.Dread!.TotalFearGenerated;
        tm.PlayBottom(card);

        Assert.Equal(fearBefore + 3, state.Dread.TotalFearGenerated); // secondary resolved
    }
}
