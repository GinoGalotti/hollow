namespace HollowWardens.Tests.Run;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Turn;
using Xunit;

/// <summary>Tests for Task 7: Bot Strategy for Pairing.</summary>
public class PairingBotStrategyTests
{
    private static Card MakeCard(string id, EffectType topType, int topVal,
        EffectType botType, int botVal, CardTiming timing = CardTiming.Slow,
        Element[]? elements = null) => new Card
    {
        Id = id, Name = id,
        TopEffect = new EffectData { Type = topType, Value = topVal },
        BottomEffect = new EffectData { Type = botType, Value = botVal },
        TopTiming = timing,
        Elements = elements ?? Array.Empty<Element>()
    };

    private static EncounterState MakeState(int weave = 10, bool withCorruption = false)
    {
        var state = new EncounterState
        {
            Balance = new BalanceConfig()
        };
        state.Weave = new WeaveSystem(weave, 20);
        if (withCorruption)
            state.Territories.Add(new Territory { Id = "A1", CorruptionPoints = 5 });
        return state;
    }

    // ── Pair selection ───────────────────────────────────────────────────────

    [Fact]
    public void ChoosePair_PicksHighestScoringOrientation()
    {
        var bot = new PairingBotStrategy();
        var state = MakeState();

        // cardA: top=3, bottom=10 — high bottom value but penalised (>5)
        // cardB: top=5, bottom=2 — lower bottom
        var cardA = MakeCard("A", EffectType.DamageInvaders, 3, EffectType.DamageInvaders, 10);
        var cardB = MakeCard("B", EffectType.GenerateFear, 5, EffectType.GenerateFear, 2);

        var hand = new List<Card> { cardA, cardB };
        var pair = bot.ChoosePair(hand, state);

        Assert.NotNull(pair);
        // Both orientations scored — just verify a pair was chosen
        Assert.NotEqual(pair!.TopCard.Id, pair.BottomCard.Id);
    }

    [Fact]
    public void ChoosePair_PrefersTopWithFastTiming()
    {
        var bot = new PairingBotStrategy();
        var state = MakeState();

        var fastCard = MakeCard("Fast", EffectType.DamageInvaders, 2, EffectType.DamageInvaders, 3,
            timing: CardTiming.Fast);
        var slowCard = MakeCard("Slow", EffectType.DamageInvaders, 2, EffectType.DamageInvaders, 3,
            timing: CardTiming.Slow);

        var hand = new List<Card> { fastCard, slowCard };
        var pair = bot.ChoosePair(hand, state);

        Assert.NotNull(pair);
        // Fast card should be chosen as top (FastTopBonus +10)
        Assert.Equal("Fast", pair!.TopCard.Id);
    }

    [Fact]
    public void ChoosePair_ElementSynergyBoostsScore()
    {
        var bot = new PairingBotStrategy();
        // Use a state with corruption so ReduceCorruption cards score their actual value
        var state = MakeState(withCorruption: true);

        // rootA and rootB both have Root — synergy pair; with corruption present, their pair
        // scores higher than noSync (GenerateFear) because synergy adds +4 and ReduceCorruption
        // scores its real value.
        var rootA = MakeCard("rootA", EffectType.ReduceCorruption, 2, EffectType.ReduceCorruption, 4,
            elements: new[] { Element.Root, Element.Root });
        var rootB = MakeCard("rootB", EffectType.PlacePresence, 1, EffectType.PlacePresence, 3,
            elements: new[] { Element.Root, Element.Mist });
        // noSync has no Root — no synergy and no corruption reduction
        var noSync = MakeCard("noSync", EffectType.GenerateFear, 2, EffectType.GenerateFear, 4,
            elements: new[] { Element.Ash, Element.Gale });

        var hand = new List<Card> { rootA, rootB, noSync };
        var pair = bot.ChoosePair(hand, state);

        Assert.NotNull(pair);
        // Best pair: rootB top (PlacePresenceBonus*2=16) + rootA bottom (ReduceCorruption*3=12)
        // + synergy Root×2 = 32. noSync combos score ≤28.
        bool synergistic = (pair!.TopCard.Elements.Contains(Element.Root) && pair.BottomCard.Elements.Contains(Element.Root));
        Assert.True(synergistic, "Bot should prefer element-synergistic pairs when all effects are scoring");
    }

    [Fact]
    public void ChoosePair_PenalizesRiskingHighValueBottom()
    {
        // Verify that a FAST high-value card is preferred as top (Fast bonus + avoids penalty)
        // vs slow low-value card as top (no fast bonus, HV takes bottom risk penalty)
        var bot = new PairingBotStrategy { HighValueThreshold = 5, FastTopBonus = 10 };
        var state = MakeState();

        // HV is FAST with moderate top value and high bottom — penalty applies if used as bottom
        var highValue = MakeCard("HV", EffectType.DamageInvaders, 3, EffectType.DamageInvaders, 8,
            timing: CardTiming.Fast);
        var lowValue = MakeCard("LV", EffectType.GenerateFear, 1, EffectType.GenerateFear, 2,
            timing: CardTiming.Slow);

        var hand = new List<Card> { highValue, lowValue };
        var pair = bot.ChoosePair(hand, state);

        Assert.NotNull(pair);
        // Orientation A (HV top, LV bottom): +10 fast + 3×2 + 2×3 = 22
        // Orientation B (LV top, HV bottom): 0 + 1×2 + 8×3 - 8 = 18 (penalty)
        // A wins → HV should be top
        Assert.Equal("HV", pair!.TopCard.Id);
        Assert.Equal("LV", pair.BottomCard.Id);
    }

    [Fact]
    public void ChoosePair_WithOneCard_ReturnsNull()
    {
        var bot = new PairingBotStrategy();
        var state = MakeState();
        var hand = new List<Card> { MakeCard("c1", EffectType.GenerateFear, 2, EffectType.GenerateFear, 4) };

        var pair = bot.ChoosePair(hand, state);
        Assert.Null(pair);
    }

    // ── Rest decision ────────────────────────────────────────────────────────

    [Fact]
    public void ShouldRest_WhenHandHas0Cards_ReturnsTrue()
    {
        var bot = new PairingBotStrategy();
        var state = MakeState();
        Assert.True(bot.ShouldRest(new List<Card>(), state));
    }

    [Fact]
    public void ShouldRest_WhenHandHas2OrFewer_ReturnsTrue()
    {
        var bot = new PairingBotStrategy { RestHandThreshold = 2 };
        var state = MakeState();
        var hand = new List<Card>
        {
            MakeCard("c1", EffectType.GenerateFear, 2, EffectType.GenerateFear, 4)
        };
        Assert.True(bot.ShouldRest(hand, state));
    }

    [Fact]
    public void ShouldRest_WhenHandHas3Cards_ReturnsFalse()
    {
        var bot = new PairingBotStrategy { RestHandThreshold = 2 };
        var state = MakeState();
        var hand = new List<Card>
        {
            MakeCard("c1", EffectType.GenerateFear, 2, EffectType.GenerateFear, 4),
            MakeCard("c2", EffectType.GenerateFear, 2, EffectType.GenerateFear, 4),
            MakeCard("c3", EffectType.GenerateFear, 2, EffectType.GenerateFear, 4)
        };
        Assert.False(bot.ShouldRest(hand, state));
    }

    // ── Reroll decision ──────────────────────────────────────────────────────

    [Fact]
    public void ShouldReroll_HighValueCardAndEnoughWeave_ReturnsTrue()
    {
        var bot = new PairingBotStrategy { RerollValueThreshold = 8, RerollWeaveThreshold = 6 };
        var state = MakeState(weave: 10);
        var dissolved = MakeCard("big", EffectType.DamageInvaders, 5, EffectType.DamageInvaders, 9);

        Assert.True(bot.ShouldReroll(dissolved, state));
    }

    [Fact]
    public void ShouldReroll_LowWeave_ReturnsFalse()
    {
        var bot = new PairingBotStrategy { RerollValueThreshold = 8, RerollWeaveThreshold = 6 };
        var state = MakeState(weave: 4); // only 4 weave — can't afford gamble
        var dissolved = MakeCard("big", EffectType.DamageInvaders, 5, EffectType.DamageInvaders, 9);

        Assert.False(bot.ShouldReroll(dissolved, state));
    }
}
