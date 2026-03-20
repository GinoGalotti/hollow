namespace HollowWardens.Tests.Cards;

using HollowWardens.Core.Cards;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;
using Xunit;

public class DeckManagerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Card MakeCard(string id) => new Card { Id = id, Name = id };

    private static List<Card> MakeCards(int count)
        => Enumerable.Range(1, count).Select(i => MakeCard($"card{i}")).ToList();

    // Generic warden: bottoms dissolve for encounter; permanently removed on Boss
    private class GenericWarden : IWardenAbility
    {
        public string WardenId => "generic";
        public BottomResult OnBottomPlayed(Card card, EncounterTier tier)
            => tier == EncounterTier.Boss ? BottomResult.PermanentlyRemoved : BottomResult.Dissolved;
        public BottomResult OnRestDissolve(Card card) => BottomResult.Dissolved;
        public void OnResolution(EncounterState state) { }
        public int CalculatePassiveFear() => 0;
    }

    private static DeckManager MakeDeck(int cardCount = 10, int handLimit = 5)
        => new DeckManager(new GenericWarden(), MakeCards(cardCount),
            rng: new Random(42), handLimit: handLimit, shuffle: false);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void RefillDrawsToHandLimit()
    {
        var dm = MakeDeck(10, handLimit: 5);
        dm.RefillHand();

        Assert.Equal(5, dm.Hand.Count);
        Assert.Equal(5, dm.DrawPileCount);
    }

    [Fact]
    public void RefillStopsWhenDeckEmpty()
    {
        var dm = MakeDeck(cardCount: 3, handLimit: 5);
        dm.RefillHand();

        Assert.Equal(3, dm.Hand.Count);
        Assert.Equal(0, dm.DrawPileCount);
    }

    [Fact]
    public void PlayTopMovesToDiscard()
    {
        var dm = MakeDeck();
        dm.RefillHand();
        var card = dm.Hand[0];

        dm.PlayTop(card);

        Assert.DoesNotContain(card, dm.Hand);
        Assert.Equal(1, dm.DiscardCount);
        Assert.Equal(0, dm.DissolvedCount);
    }

    [Fact]
    public void PlayBottomDissolves()
    {
        var dm = MakeDeck();
        dm.RefillHand();
        var card = dm.Hand[0];

        dm.PlayBottom(card, EncounterTier.Standard);

        Assert.DoesNotContain(card, dm.Hand);
        Assert.Equal(1, dm.DissolvedCount);
        Assert.Equal(0, dm.DiscardCount);
    }

    [Fact]
    public void RestShufflesDiscardIntoDeck()
    {
        var dm = MakeDeck(10);
        dm.RefillHand();                      // hand=5, draw=5
        foreach (var c in dm.Hand.ToList())   // play all as tops
            dm.PlayTop(c);                    // discard=5, hand=0

        dm.Rest();

        // 5 (original draw) + 5 (discards shuffled in) - 1 (rest-dissolve) = 9
        Assert.Equal(9, dm.DrawPileCount);
        Assert.Equal(0, dm.DiscardCount);
        Assert.Equal(1, dm.DissolvedCount);
    }

    [Fact]
    public void RestDissolveRemovesOneCard()
    {
        var dm = MakeDeck(10);
        dm.RefillHand();
        foreach (var c in dm.Hand.ToList()) dm.PlayTop(c);

        int totalBeforeRest = dm.DrawPileCount + dm.DiscardCount; // 5 + 5 = 10

        dm.Rest();

        Assert.Equal(1, dm.DissolvedCount);
        Assert.Equal(totalBeforeRest - 1, dm.DrawPileCount);
    }

    [Fact]
    public void FourPlayTurnsThenRestWith10Cards()
    {
        // Follows the design reference deck math exactly
        var dm = MakeDeck(10, handLimit: 5);

        // Turn 1: refill→5, play 2 tops. hand=3, draw=5, discard=2
        dm.RefillHand();
        dm.PlayTop(dm.Hand[0]);
        dm.PlayTop(dm.Hand[0]);
        Assert.Equal(3, dm.Hand.Count);
        Assert.Equal(5, dm.DrawPileCount);
        Assert.Equal(2, dm.DiscardCount);

        // Turn 2: refill→5 (draw 2). hand=5, draw=3. Play 2. hand=3, draw=3, discard=4
        dm.RefillHand();
        Assert.Equal(5, dm.Hand.Count);
        Assert.Equal(3, dm.DrawPileCount);
        dm.PlayTop(dm.Hand[0]);
        dm.PlayTop(dm.Hand[0]);

        // Turn 3: refill→5 (draw 2). draw=1. Play 2. hand=3, draw=1, discard=6
        dm.RefillHand();
        Assert.Equal(5, dm.Hand.Count);
        Assert.Equal(1, dm.DrawPileCount);
        dm.PlayTop(dm.Hand[0]);
        dm.PlayTop(dm.Hand[0]);

        // Turn 4: refill draws 1 → hand=4. Play 2. hand=2, draw=0, discard=8
        dm.RefillHand();
        Assert.Equal(4, dm.Hand.Count);
        Assert.Equal(0, dm.DrawPileCount);
        dm.PlayTop(dm.Hand[0]);
        dm.PlayTop(dm.Hand[0]);

        Assert.True(dm.NeedsRest);
        Assert.Equal(2, dm.Hand.Count);

        // Turn 5: Rest — shuffle 8 discards, dissolve 1 → draw=7
        dm.Rest();

        Assert.Equal(7, dm.DrawPileCount);
        Assert.Equal(0, dm.DiscardCount);
        Assert.Equal(1, dm.DissolvedCount);
        Assert.Equal(2, dm.Hand.Count); // hand unchanged by rest
    }

    [Fact]
    public void AggressiveBottomsForceEarlierRest()
    {
        // Playing bottoms (dissolved) permanently thins the deck each cycle.
        // Unlike tops, dissolved cards do NOT return on Rest — the pool shrinks.
        var dm = MakeDeck(10, handLimit: 5);

        dm.RefillHand(); // hand=5, draw=5
        foreach (var c in dm.Hand.ToList())
            dm.PlayBottom(c, EncounterTier.Standard); // all 5 dissolved

        // hand=0, draw=5, dissolved=5, discard=0
        Assert.Equal(5, dm.DissolvedCount);
        Assert.Equal(5, dm.DrawPileCount);
        Assert.Equal(0, dm.DiscardCount);
        Assert.False(dm.NeedsRest); // still draw cards available

        dm.RefillHand(); // draws 5 from draw. hand=5, draw=0
        dm.PlayBottom(dm.Hand[0], EncounterTier.Standard);
        dm.PlayBottom(dm.Hand[0], EncounterTier.Standard);

        // hand=3, draw=0, dissolved=7, discard=0
        // NeedsRest is false — no discards to recycle, deck just exhausted
        Assert.Equal(0, dm.DrawPileCount);
        Assert.Equal(0, dm.DiscardCount);
        Assert.Equal(7, dm.DissolvedCount);
        Assert.False(dm.NeedsRest); // nothing in discard to shuffle back
    }

    [Fact]
    public void SecondCycleIsThinnerAfterBottoms()
    {
        // Mix: 1 top + 1 bottom per turn over 4 turns, then rest.
        // After rest, draw pile should be much smaller than the original 10.
        var dm = MakeDeck(10, handLimit: 5);

        for (int turn = 0; turn < 4; turn++)
        {
            dm.RefillHand();
            dm.PlayTop(dm.Hand[0]);               // → discard
            dm.PlayBottom(dm.Hand[0], EncounterTier.Standard); // → dissolved
        }
        // After 4 turns: discard=4, dissolved=4, draw=0
        Assert.True(dm.NeedsRest);

        dm.Rest();
        // Shuffle discard(4) → draw(4), dissolve 1 → draw=3, dissolved=5

        Assert.Equal(3, dm.DrawPileCount);
        Assert.Equal(0, dm.DiscardCount);
        Assert.Equal(5, dm.DissolvedCount);

        // Second cycle is thinner: only 3 cards in draw (vs 9 with all-tops first cycle)
        Assert.True(dm.DrawPileCount < 9);
    }
}
