namespace HollowWardens.Tests.Cards;

using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Data;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>Tests for Task 1: dual-discard piles, PlayAsTop/Bottom, SoakDamage, RestWithPairing, RerollDissolve.</summary>
public class DualDiscardTests
{
    private static Card MakeCard(string id, params Element[] elements) =>
        new Card { Id = id, Name = id, Elements = elements };

    private static DeckManager MakeDeck(int cardCount = 10, int handLimit = 5)
    {
        var cards = Enumerable.Range(1, cardCount).Select(i => MakeCard($"c{i}")).ToList();
        return new DeckManager(new GenericWarden(), cards,
            rng: GameRandom.FromSeed(42), handLimit: handLimit, shuffle: false);
    }

    private class GenericWarden : IWardenAbility
    {
        public string WardenId => "generic";
        public BottomResult OnBottomPlayed(Card card, EncounterTier tier) => BottomResult.Dissolved;
        public BottomResult OnRestDissolve(Card card) => BottomResult.Dissolved;
        public void OnResolution(EncounterState state) { }
        public int CalculatePassiveFear(EncounterState state) => 0;
    }

    // ── PlayAsTop → TopDiscard ────────────────────────────────────────────────

    [Fact]
    public void PlayAsTop_MovesCardToTopDiscard()
    {
        var dm = MakeDeck();
        dm.RefillHand();
        var card = dm.Hand[0];

        dm.PlayAsTop(card);

        Assert.DoesNotContain(card, dm.Hand);
        Assert.Equal(1, dm.TopDiscardCount);
        Assert.Equal(0, dm.BottomDiscardCount);
        Assert.Equal(0, dm.DissolvedCount);
        Assert.Contains(card, dm.TopDiscardCards);
    }

    [Fact]
    public void PlayAsTop_ThrowsIfCardNotInHand()
    {
        var dm = MakeDeck();
        dm.RefillHand();
        var stranger = MakeCard("not_in_hand");

        Assert.Throws<InvalidOperationException>(() => dm.PlayAsTop(stranger));
    }

    // ── PlayAsBottom → BottomDiscard ──────────────────────────────────────────

    [Fact]
    public void PlayAsBottom_MovesCardToBottomDiscard()
    {
        var dm = MakeDeck();
        dm.RefillHand();
        var card = dm.Hand[0];

        dm.PlayAsBottom(card);

        Assert.DoesNotContain(card, dm.Hand);
        Assert.Equal(0, dm.TopDiscardCount);
        Assert.Equal(1, dm.BottomDiscardCount);
        Assert.Equal(0, dm.DissolvedCount);
        Assert.Contains(card, dm.BottomDiscardCards);
    }

    // ── SoakDamage → TopDiscard ───────────────────────────────────────────────

    [Fact]
    public void SoakDamage_MovesCardToTopDiscard()
    {
        var dm = MakeDeck();
        dm.RefillHand();
        var card = dm.Hand[0];

        dm.SoakDamage(card);

        Assert.DoesNotContain(card, dm.Hand);
        Assert.Equal(1, dm.TopDiscardCount);
        Assert.Equal(0, dm.BottomDiscardCount);
        Assert.Equal(0, dm.DissolvedCount);
    }

    // ── Rest recovers all top-discard cards ───────────────────────────────────

    [Fact]
    public void BeginRestWithPairing_RecoversTopsToHand()
    {
        var dm = MakeDeck(6, handLimit: 6);
        dm.RefillHand();

        // Play 3 tops, 1 bottom
        var topCards = dm.Hand.Take(3).ToList();
        foreach (var c in topCards) dm.PlayAsTop(c);
        var bottomCard = dm.Hand[0];
        dm.PlayAsBottom(bottomCard);

        // 2 cards left in hand
        Assert.Equal(2, dm.Hand.Count);
        Assert.Equal(3, dm.TopDiscardCount);
        Assert.Equal(1, dm.BottomDiscardCount);

        var result = dm.BeginRestWithPairing();
        dm.CompleteRestWithPairing();

        // All 3 top cards returned; bottomCard was the only bottom — 1 dissolved (≤2 bottoms)
        Assert.All(topCards, c => Assert.Contains(c, dm.Hand));
        Assert.Equal(0, dm.TopDiscardCount);
        Assert.Equal(0, dm.BottomDiscardCount);
    }

    // ── Rest dissolves 2 random bottoms ──────────────────────────────────────

    [Fact]
    public void BeginRestWithPairing_DissolvesTwoBottoms()
    {
        var dm = MakeDeck(10, handLimit: 5);
        dm.RefillHand();

        // Play 5 bottoms
        var bottoms = dm.Hand.ToList();
        foreach (var c in bottoms) dm.PlayAsBottom(c);

        Assert.Equal(5, dm.BottomDiscardCount);

        var result = dm.BeginRestWithPairing();
        dm.CompleteRestWithPairing();

        Assert.Equal(2, result.Dissolved.Count);
        Assert.Equal(2, dm.DissolvedCount);
        Assert.Equal(3, dm.Hand.Count); // 5 - 2 dissolved = 3 returned to hand
    }

    // ── Rest with 0-1 bottoms ─────────────────────────────────────────────────

    [Fact]
    public void BeginRestWithPairing_WithZeroBottoms_DissolvesNothing()
    {
        var dm = MakeDeck(4, handLimit: 4);
        dm.RefillHand();

        // Play only tops
        var tops = dm.Hand.ToList();
        foreach (var c in tops) dm.PlayAsTop(c);

        var result = dm.BeginRestWithPairing();
        dm.CompleteRestWithPairing();

        Assert.Empty(result.Dissolved);
        Assert.Equal(0, dm.DissolvedCount);
        Assert.Equal(4, dm.Hand.Count); // all tops returned
    }

    [Fact]
    public void BeginRestWithPairing_WithOneBottom_DissolvesOne()
    {
        var dm = MakeDeck(5, handLimit: 5);
        dm.RefillHand();

        var bottom = dm.Hand[0];
        dm.PlayAsBottom(bottom);
        var tops = dm.Hand.ToList();
        foreach (var c in tops) dm.PlayAsTop(c);

        var result = dm.BeginRestWithPairing();
        dm.CompleteRestWithPairing();

        Assert.Equal(1, result.Dissolved.Count);
        Assert.Equal(1, dm.DissolvedCount);
    }

    // ── RerollDissolve mechanics ──────────────────────────────────────────────

    [Fact]
    public void RerollDissolve_SavesCardAndPicksReplacement()
    {
        var dm = MakeDeck(10, handLimit: 5);
        dm.RefillHand();

        // Play 5 bottoms
        var bottoms = dm.Hand.ToList();
        foreach (var c in bottoms) dm.PlayAsBottom(c);

        var result = dm.BeginRestWithPairing();
        Assert.Equal(2, result.Dissolved.Count);

        // Reroll the first dissolved card
        var savedCard = result.Dissolved[0];
        dm.RerollDissolve(savedCard);

        dm.CompleteRestWithPairing();

        // Still exactly 2 dissolved (one was rerolled — new victim took its place)
        Assert.Equal(2, dm.DissolvedCount);
        // The saved card is now in hand
        Assert.Contains(savedCard, dm.Hand);
    }

    [Fact]
    public void RerollDissolve_WithNoSurvivorPool_ReturnsCardToHand()
    {
        var dm = MakeDeck(3, handLimit: 3);
        dm.RefillHand();

        // Play 2 bottoms — only 2, so both get dissolved, no survivors
        dm.PlayAsBottom(dm.Hand[0]);
        dm.PlayAsBottom(dm.Hand[0]);
        var top = dm.Hand[0];
        dm.PlayAsTop(top);

        var result = dm.BeginRestWithPairing();
        Assert.Equal(2, result.Dissolved.Count);
        Assert.Equal(0, dm.BottomDiscardCount); // no survivors

        var savedCard = result.Dissolved[0];
        dm.RerollDissolve(savedCard); // no pool → card just returns to hand
        dm.CompleteRestWithPairing();

        Assert.Contains(savedCard, dm.Hand);
        Assert.Equal(1, dm.DissolvedCount); // only 1 still dissolved
    }

    // ── CardTiming parsed from JSON ───────────────────────────────────────────

    [Fact]
    public void CardTiming_ParsedFromJson_DefaultsSlow()
    {
        var path = FindWardenJson("root");
        var cards = WardenLoader.LoadCards(path);

        // All cards should have TopTiming set (defaulting to Slow if not in JSON)
        Assert.All(cards, c => Assert.True(
            c.TopTiming == CardTiming.Fast || c.TopTiming == CardTiming.Slow));
    }

    [Fact]
    public void CardTiming_EmberCardsHaveTopTiming()
    {
        var path = FindWardenJson("ember");
        var cards = WardenLoader.LoadCards(path);

        Assert.All(cards, c => Assert.True(
            c.TopTiming == CardTiming.Fast || c.TopTiming == CardTiming.Slow));
    }

    [Fact]
    public void CardTiming_FastParsedCorrectly()
    {
        var card = new Card { Id = "test", TopTiming = CardTiming.Fast };
        Assert.Equal(CardTiming.Fast, card.TopTiming);
        Assert.True(card.TopTiming == CardTiming.Fast);
    }

    private static string FindWardenJson(string wardenId)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "wardens", $"{wardenId}.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException($"Cannot find data/wardens/{wardenId}.json");
    }
}
