namespace HollowWardens.Tests.Systems;

using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>Tests for Task 3: tier-scaled element decay + Root Elemental Offering passive.</summary>
public class TierDecayTests
{
    private static BalanceConfig MakeTierConfig() => new BalanceConfig
    {
        TierScaledDecay = true,
        ElementTier1Threshold = 4,
        ElementTier2Threshold = 7,
        ElementTier3Threshold = 11,
        ElementDecayBelowT1 = 1,
        ElementDecayAtT1 = 2,
        ElementDecayAtT2 = 3,
        ElementDecayAtT3 = 4,
        RootRestExtraDecay = 2,
    };

    // ── Tier-scaled decay ─────────────────────────────────────────────────────

    [Fact]
    public void Decay_BelowT1_UsesDecayBelowT1()
    {
        var config = MakeTierConfig();
        var es = new ElementSystem(config);

        es.AddElements(new[] { Element.Root }, 3); // 3 Root — below T1 (4)
        es.Decay();

        Assert.Equal(2, es.Get(Element.Root)); // 3 - 1 = 2
    }

    [Fact]
    public void Decay_AtT1_UsesDecayAtT1()
    {
        var config = MakeTierConfig();
        var es = new ElementSystem(config);

        es.AddElements(new[] { Element.Root }, 4); // 4 Root — at T1
        es.Decay();

        Assert.Equal(2, es.Get(Element.Root)); // 4 - 2 = 2
    }

    [Fact]
    public void Decay_AtT2_UsesDecayAtT2()
    {
        var config = MakeTierConfig();
        var es = new ElementSystem(config);

        es.AddElements(new[] { Element.Root }, 7); // 7 Root — at T2
        es.Decay();

        Assert.Equal(4, es.Get(Element.Root)); // 7 - 3 = 4
    }

    [Fact]
    public void Decay_AtT3_UsesDecayAtT3()
    {
        var config = MakeTierConfig();
        var es = new ElementSystem(config);

        es.AddElements(new[] { Element.Root }, 11); // 11 Root — at T3
        es.Decay();

        Assert.Equal(7, es.Get(Element.Root)); // 11 - 4 = 7
    }

    [Fact]
    public void Decay_CrossingTierBoundary_AppliesCorrectDecay()
    {
        var config = MakeTierConfig();
        var es = new ElementSystem(config);

        // Start at 5 (T1), decay removes 2 → 3 (below T1), next decay removes 1 → 2
        es.AddElements(new[] { Element.Mist }, 5);
        es.Decay(); // at T1 → decay 2 → 3
        Assert.Equal(3, es.Get(Element.Mist));
        es.Decay(); // below T1 → decay 1 → 2
        Assert.Equal(2, es.Get(Element.Mist));
    }

    [Fact]
    public void Decay_FlatModePreserved_WhenTierScaledDisabled()
    {
        var config = new BalanceConfig { TierScaledDecay = false, ElementDecayPerTurn = 1 };
        var es = new ElementSystem(config);

        es.AddElements(new[] { Element.Ash }, 10); // well above T1, but flat mode
        es.Decay();

        Assert.Equal(9, es.Get(Element.Ash)); // 10 - 1 = 9 (flat decay)
    }

    // ── ApplyExtraDecay ───────────────────────────────────────────────────────

    [Fact]
    public void ApplyExtraDecay_ReducesAllElements()
    {
        var config = MakeTierConfig();
        var es = new ElementSystem(config);

        es.AddElements(new[] { Element.Root, Element.Mist }, 5);
        es.ApplyExtraDecay(2);

        Assert.Equal(3, es.Get(Element.Root));
        Assert.Equal(3, es.Get(Element.Mist));
    }

    [Fact]
    public void ApplyExtraDecay_ClampsAtZero()
    {
        var config = MakeTierConfig();
        var es = new ElementSystem(config);

        es.AddElements(new[] { Element.Ash }, 1);
        es.ApplyExtraDecay(5); // more than available

        Assert.Equal(0, es.Get(Element.Ash));
    }

    // ── Root Elemental Offering ───────────────────────────────────────────────

    private static Card MakeCard(string id, params Element[] elements) => new Card
    {
        Id = id, Name = id, Elements = elements,
        TopEffect = new HollowWardens.Core.Effects.EffectData { Type = HollowWardens.Core.Effects.EffectType.GenerateFear },
        BottomEffect = new HollowWardens.Core.Effects.EffectData { Type = HollowWardens.Core.Effects.EffectType.GenerateFear }
    };

    private static (EncounterState state, RootAbility root, DeckManager deck) BuildState(params Card[] cards)
    {
        var balance = new BalanceConfig { TierScaledDecay = true, RootRestExtraDecay = 2 };
        var root = new RootAbility();
        var dm = new DeckManager(root, cards.ToList(), GameRandom.FromSeed(42), handLimit: 10, shuffle: false);
        dm.RefillHand();

        var elements = new ElementSystem(balance);
        var state = new EncounterState
        {
            Config = new EncounterConfig { Id = "test", Tier = EncounterTier.Standard },
            Deck = dm,
            Elements = elements,
            Warden = root,
            Balance = balance,
            Territories = new List<Territory>(),
        };
        return (state, root, dm);
    }

    [Fact]
    public void RootOffering_AddsCardElementsToPool()
    {
        var card = MakeCard("c1", Element.Root, Element.Root);
        var (state, root, deck) = BuildState(card);

        root.UseElementalOffering(card, state);

        Assert.Equal(2, state.Elements!.Get(Element.Root)); // 2 Root elements added ×1
    }

    [Fact]
    public void RootOffering_MovesCardToTopDiscard()
    {
        var card = MakeCard("c1", Element.Root);
        var (state, root, deck) = BuildState(card);

        root.UseElementalOffering(card, state);

        Assert.DoesNotContain(card, deck.Hand);
        Assert.Equal(1, deck.TopDiscardCount);
        Assert.Equal(0, deck.BottomDiscardCount);
    }

    [Fact]
    public void RootOffering_OnlyOncePerCycle()
    {
        var c1 = MakeCard("c1", Element.Root);
        var c2 = MakeCard("c2", Element.Mist);
        var (state, root, deck) = BuildState(c1, c2);

        bool first = root.UseElementalOffering(c1, state);
        bool second = root.UseElementalOffering(c2, state);

        Assert.True(first);
        Assert.False(second); // already used
        Assert.True(state.RootOfferingUsedThisCycle);
    }

    [Fact]
    public void RootOffering_ResetsOnRest()
    {
        var c1 = MakeCard("c1", Element.Root);
        var (state, root, deck) = BuildState(c1);

        root.UseElementalOffering(c1, state);
        Assert.True(state.RootOfferingUsedThisCycle);

        root.OnRest(state, null); // rest resets the flag
        Assert.False(state.RootOfferingUsedThisCycle);
    }

    // ── Root extra decay on rest ──────────────────────────────────────────────

    [Fact]
    public void Root_OnRest_AppliesExtraElementDecay()
    {
        var c1 = MakeCard("c1", Element.Root);
        var (state, root, deck) = BuildState(c1);

        state.Elements!.AddElements(new[] { Element.Root }, 5); // 5 Root
        root.OnRest(state, null);

        // Extra decay = 2 → 5 - 2 = 3
        Assert.Equal(3, state.Elements!.Get(Element.Root));
    }

    [Fact]
    public void Ember_OnRest_DoesNotTakeExtraDecay()
    {
        // EmberAbility.OnRest does nothing by default (no extra decay)
        var balance = new BalanceConfig { TierScaledDecay = true };
        var ember = new EmberAbility();
        var elements = new ElementSystem(balance);
        var state = new EncounterState
        {
            Config = new EncounterConfig { Tier = EncounterTier.Standard },
            Elements = elements,
            Warden = ember,
            Balance = balance,
            Territories = new List<Territory>(),
        };

        elements.AddElements(new[] { Element.Ash }, 5);
        ember.OnRest(state, null); // EmberAbility.OnRest is a no-op by default

        Assert.Equal(5, elements.Get(Element.Ash)); // unchanged
    }
}
