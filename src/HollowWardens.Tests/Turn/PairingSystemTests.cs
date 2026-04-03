namespace HollowWardens.Tests.Turn;

using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Turn;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>Tests for Task 2: pairing system turn structure.</summary>
public class PairingSystemTests
{
    private class GenericWarden : IWardenAbility
    {
        public string WardenId => "generic";
        public BottomResult OnBottomPlayed(Card card, EncounterTier tier) => BottomResult.Dissolved;
        public BottomResult OnRestDissolve(Card card) => BottomResult.Dissolved;
        public void OnResolution(EncounterState state) { }
        public int CalculatePassiveFear(EncounterState state) => 0;
    }

    private static Card MakeFastCard(string id, Element[]? elements = null) => new Card
    {
        Id = id, Name = id, TopTiming = CardTiming.Fast,
        Elements = elements ?? Array.Empty<Element>(),
        TopEffect = new EffectData { Type = EffectType.GenerateFear, Value = 1 },
        BottomEffect = new EffectData { Type = EffectType.GenerateFear, Value = 2 }
    };

    private static Card MakeSlowCard(string id, Element[]? elements = null) => new Card
    {
        Id = id, Name = id, TopTiming = CardTiming.Slow,
        Elements = elements ?? Array.Empty<Element>(),
        TopEffect = new EffectData { Type = EffectType.GenerateFear, Value = 1 },
        BottomEffect = new EffectData { Type = EffectType.GenerateFear, Value = 2 }
    };

    private (EncounterState state, TurnManager tm, DeckManager deck) Build(
        List<Card> cards, int handLimit = 5)
    {
        var warden = new GenericWarden();
        var dm = new DeckManager(warden, cards,
            rng: GameRandom.FromSeed(42), handLimit: handLimit, shuffle: false);
        var balance = new BalanceConfig();
        var elements = new ElementSystem(balance);
        var dread = new DreadSystem(balance);
        var weave = new WeaveSystem(balance.StartingWeave, balance.MaxWeave);

        var state = new EncounterState
        {
            Config = new EncounterConfig { Id = "test", Tier = EncounterTier.Standard, TideCount = 8 },
            Deck = dm,
            Elements = elements,
            Dread = dread,
            Weave = weave,
            Balance = balance,
            Territories = new List<Territory>(),
        };

        var resolver = new EffectResolver();
        var tm = new TurnManager(state, resolver);
        return (state, tm, dm);
    }

    // ── CardPair ──────────────────────────────────────────────────────────────

    [Fact]
    public void CardPair_FastTop_IsFast()
    {
        var fast = MakeFastCard("fast");
        var slow = MakeSlowCard("slow");
        var pair = new CardPair(fast, slow);

        Assert.True(pair.TopIsFast);
        Assert.False(pair.TopIsSlow);
    }

    [Fact]
    public void CardPair_SlowTop_IsSlow()
    {
        var fast = MakeFastCard("fast");
        var slow = MakeSlowCard("slow");
        var pair = new CardPair(slow, fast);

        Assert.False(pair.TopIsFast);
        Assert.True(pair.TopIsSlow);
    }

    // ── SubmitPair validation ─────────────────────────────────────────────────

    [Fact]
    public void SubmitPair_WithSameCard_ReturnsFalse()
    {
        var cards = new List<Card> { MakeFastCard("c1"), MakeSlowCard("c2") };
        var (state, tm, deck) = Build(cards);
        deck.RefillHand();

        var card = deck.Hand[0];
        var result = tm.SubmitPair(new CardPair(card, card));

        Assert.False(result);
    }

    [Fact]
    public void SubmitPair_WithValidCards_ReturnsTrue()
    {
        var cards = new List<Card> { MakeFastCard("c1"), MakeSlowCard("c2") };
        var (state, tm, deck) = Build(cards);
        deck.RefillHand();

        var result = tm.SubmitPair(new CardPair(deck.Hand[0], deck.Hand[1]));

        Assert.True(result);
        Assert.NotNull(state.CurrentPair);
    }

    [Fact]
    public void SubmitPair_SetsPhase_ToPlan()
    {
        var cards = new List<Card> { MakeFastCard("c1"), MakeSlowCard("c2") };
        var (state, tm, deck) = Build(cards);
        deck.RefillHand();

        tm.SubmitPair(new CardPair(deck.Hand[0], deck.Hand[1]));

        Assert.Equal(TurnPhase.Plan, tm.CurrentPhase);
    }

    // ── Fast top resolves before Tide ─────────────────────────────────────────

    [Fact]
    public void FastTop_ExecutesFastPhase_BeforeTide()
    {
        var fast = MakeFastCard("fast");
        var slow = MakeSlowCard("slow");
        var (state, tm, deck) = Build(new List<Card> { fast, slow });
        deck.RefillHand();

        bool topResolved = false;
        GameEvents.CardPlayed += (c, p) => { if (c.Id == "fast") topResolved = true; };
        try
        {
            tm.SubmitPair(new CardPair(fast, slow));
            tm.ExecuteFastPhase();

            // Top card resolved in Fast phase
            Assert.True(topResolved);
            // Card is in TopDiscard now
            Assert.Equal(1, deck.TopDiscardCount);
        }
        finally { GameEvents.ClearAll(); }
    }

    // ── Slow top does NOT resolve in Fast phase ───────────────────────────────

    [Fact]
    public void SlowTop_DoesNotResolveInFastPhase()
    {
        var fast = MakeFastCard("fast");
        var slow = MakeSlowCard("slow");
        var (state, tm, deck) = Build(new List<Card> { slow, fast });
        deck.RefillHand();

        bool topResolved = false;
        GameEvents.CardPlayed += (c, p) => { if (c.Id == "slow") topResolved = true; };
        try
        {
            tm.SubmitPair(new CardPair(slow, fast)); // slow on top
            tm.ExecuteFastPhase();

            Assert.False(topResolved);      // not resolved yet
            Assert.Equal(0, deck.TopDiscardCount);

            tm.ExecuteSlowPhase();
            Assert.True(topResolved);       // now resolved
            Assert.Equal(1, deck.TopDiscardCount);
        }
        finally { GameEvents.ClearAll(); }
    }

    // ── Elements: top ×1, bottom ×2 ──────────────────────────────────────────

    [Fact]
    public void ExecuteElements_AddsTopOnce_BottomTwice()
    {
        var topCard = new Card
        {
            Id = "top", TopTiming = CardTiming.Fast,
            Elements = new[] { Element.Root },
            TopEffect = new EffectData { Type = EffectType.GenerateFear },
            BottomEffect = new EffectData { Type = EffectType.GenerateFear }
        };
        var bottomCard = new Card
        {
            Id = "bot", TopTiming = CardTiming.Slow,
            Elements = new[] { Element.Mist },
            TopEffect = new EffectData { Type = EffectType.GenerateFear },
            BottomEffect = new EffectData { Type = EffectType.GenerateFear }
        };
        var (state, tm, deck) = Build(new List<Card> { topCard, bottomCard });
        deck.RefillHand();

        tm.SubmitPair(new CardPair(topCard, bottomCard));
        tm.ExecuteFastPhase();  // top goes to TopDiscard
        tm.ExecutePairingDusk(); // bottom goes to BottomDiscard
        tm.ExecuteElements();

        // TopElementMultiplier=1, BottomElementMultiplier=2
        Assert.Equal(1, state.Elements!.Get(Element.Root));  // top contributes 1×1=1
        Assert.Equal(2, state.Elements!.Get(Element.Mist));  // bottom contributes 1×2=2
    }

    // ── Cards go to correct discard piles ────────────────────────────────────

    [Fact]
    public void Cleanup_TopInTopDiscard_BottomInBottomDiscard()
    {
        var fast = MakeFastCard("fast");
        var slow = MakeSlowCard("slow");
        var (state, tm, deck) = Build(new List<Card> { fast, slow });
        deck.RefillHand();

        tm.SubmitPair(new CardPair(fast, slow));
        tm.ExecuteFastPhase();  // fast top resolved → TopDiscard
        tm.ExecuteSlowPhase();  // slow top not resolved (fast was top)
        tm.ExecutePairingDusk(); // bottom resolved → BottomDiscard
        tm.ExecuteElements();
        tm.ExecuteCleanup();

        Assert.Equal(1, deck.TopDiscardCount);   // fast card in top-discard
        Assert.Equal(1, deck.BottomDiscardCount); // slow card in bottom-discard
        Assert.Null(state.CurrentPair);
    }

    // ── CanSubmitPair: hand empty forces rest ─────────────────────────────────

    [Fact]
    public void CanSubmitPair_WithFewerThanTwoCards_ReturnsFalse()
    {
        var (state, tm, deck) = Build(new List<Card> { MakeFastCard("c1") });
        deck.RefillHand();

        Assert.False(tm.CanSubmitPair());
    }

    [Fact]
    public void CanSubmitPair_WithTwoCards_ReturnsTrue()
    {
        var cards = new List<Card> { MakeFastCard("c1"), MakeSlowCard("c2") };
        var (state, tm, deck) = Build(cards);
        deck.RefillHand();

        Assert.True(tm.CanSubmitPair());
    }

    // ── Rest turn: Tide still acts ────────────────────────────────────────────

    [Fact]
    public void BeginRestTurn_SetsIsRestTurnTrue()
    {
        var cards = new List<Card> { MakeFastCard("c1"), MakeSlowCard("c2") };
        var (state, tm, deck) = Build(cards);

        tm.BeginRestTurn();

        Assert.True(state.IsRestTurn);
        Assert.Equal(TurnPhase.Rest, tm.CurrentPhase);
    }

    // ── EncounterState pairing fields ─────────────────────────────────────────

    [Fact]
    public void EncounterState_HasPairingFields()
    {
        var state = new EncounterState();
        Assert.Null(state.CurrentPair);
        Assert.Equal(0, state.RestCycleCount);
        Assert.False(state.IsRestTurn);
        Assert.False(state.RootOfferingUsedThisCycle);
    }
}
