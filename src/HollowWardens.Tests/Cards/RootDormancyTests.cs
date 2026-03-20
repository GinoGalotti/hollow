namespace HollowWardens.Tests.Cards;

using HollowWardens.Core.Cards;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;
using Xunit;

public class RootDormancyTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Card MakeCard(string id) => new Card { Id = id, Name = id };

    private static List<Card> MakeCards(int count)
        => Enumerable.Range(1, count).Select(i => MakeCard($"card{i}")).ToList();

    private static DeckManager MakeRootDeck(int cardCount = 10, int handLimit = 5)
        => new DeckManager(new RootAbility(), MakeCards(cardCount),
            rng: new Random(42), handLimit: handLimit, shuffle: false);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void BottomPlayMakesCardDormant()
    {
        var dm = MakeRootDeck();
        dm.RefillHand();
        var card = dm.Hand[0];

        dm.PlayBottom(card, EncounterTier.Standard);

        Assert.True(card.IsDormant);
        Assert.Equal(1, dm.DormantCount);
    }

    [Fact]
    public void DormantCardStaysInDeck()
    {
        var dm = MakeRootDeck();
        dm.RefillHand(); // hand=5, draw=5
        var card = dm.Hand[0];

        dm.PlayBottom(card, EncounterTier.Standard); // card → dormant, shuffled back into draw

        Assert.Equal(4, dm.Hand.Count);
        Assert.Equal(6, dm.DrawPileCount); // 5 remaining + 1 dormant shuffled back
        Assert.Equal(0, dm.DissolvedCount);
        Assert.True(card.IsDormant);
    }

    [Fact]
    public void DormantCardIsNotPlayable()
    {
        var dormantCard = new Card { Id = "dormant1", IsDormant = true };
        var normalCard = MakeCard("normal1");

        var dm = new DeckManager(new RootAbility(),
            new[] { dormantCard, normalCard },
            rng: new Random(0), handLimit: 2, shuffle: false);

        dm.RefillHand();

        Assert.False(dm.IsPlayable(dormantCard));
        Assert.True(dm.IsPlayable(normalCard));
    }

    [Fact]
    public void RestDissolveGosDormantForRoot()
    {
        var dm = MakeRootDeck(10, handLimit: 5);
        dm.RefillHand();                            // hand=5, draw=5
        foreach (var c in dm.Hand.ToList())
            dm.PlayTop(c);                          // discard=5, hand=0
        dm.RefillHand();                            // draws 5 more; hand=5, draw=0
        // discard=5, draw=0 → NeedsRest

        Assert.True(dm.NeedsRest);
        dm.Rest();

        // Root: rest-dissolved card goes dormant, NOT dissolved
        Assert.Equal(0, dm.DissolvedCount);
        Assert.Equal(1, dm.DormantCount);
    }

    [Fact]
    public void AwakeDormantReactivatesCard()
    {
        var dormantCard = new Card { Id = "d1", IsDormant = true };

        var dm = new DeckManager(new RootAbility(),
            new[] { dormantCard },
            rng: new Random(0), handLimit: 1, shuffle: false);

        dm.RefillHand();
        Assert.False(dm.IsPlayable(dm.Hand[0]));

        dm.AwakenDormant(dm.Hand[0]);

        Assert.False(dm.Hand[0].IsDormant);
        Assert.True(dm.IsPlayable(dm.Hand[0]));
        Assert.Equal(0, dm.DormantCount);
    }

    [Fact]
    public void AwakeAllReactivatesAllDormant()
    {
        var cards = new[]
        {
            new Card { Id = "d1", IsDormant = true },
            new Card { Id = "d2", IsDormant = true },
            new Card { Id = "d3", IsDormant = true },
        };

        var dm = new DeckManager(new RootAbility(), cards,
            rng: new Random(0), handLimit: 3, shuffle: false);

        dm.RefillHand();
        Assert.Equal(3, dm.DormantCount);

        dm.AwakenAllDormant();

        Assert.Equal(0, dm.DormantCount);
        Assert.All(dm.Hand, c => Assert.False(c.IsDormant));
    }

    [Fact]
    public void BossDoubleDissolveRemovesPermanently()
    {
        // Root: playing bottom of an already-dormant card on Boss = permanently removed
        var dormantCard = new Card { Id = "dormant-boss", IsDormant = true };

        var dm = new DeckManager(new RootAbility(),
            new[] { dormantCard },
            rng: new Random(0), handLimit: 1, shuffle: false);

        dm.RefillHand();
        dm.PlayBottom(dormantCard, EncounterTier.Boss);

        Assert.Equal(1, dm.PermanentlyRemovedCards.Count);
        Assert.Equal(0, dm.DrawPileCount);
        Assert.Equal(0, dm.DormantCount);
        Assert.Equal(0, dm.DissolvedCount);
    }

    [Fact]
    public void DormantCountTracksCorrectly()
    {
        // Mix of pre-dormant and active cards; verify count across piles
        var dormant1 = new Card { Id = "d1", IsDormant = true };
        var dormant2 = new Card { Id = "d2", IsDormant = true };
        var active1 = MakeCard("a1");
        var active2 = MakeCard("a2");
        var active3 = MakeCard("a3");

        // shuffle=false, draws from end: active3, active2, active1 drawn first
        var dm = new DeckManager(new RootAbility(),
            new[] { dormant1, dormant2, active1, active2, active3 },
            rng: new Random(0), handLimit: 3, shuffle: false);

        dm.RefillHand(); // hand=[active3, active2, active1], draw=[dormant1, dormant2]

        Assert.Equal(2, dm.DormantCount); // both in draw pile

        // Play bottom of active1 → goes dormant, shuffled into draw
        dm.PlayBottom(active1, EncounterTier.Standard);

        Assert.Equal(3, dm.DormantCount); // dormant1, dormant2 in draw + active1(dormant) in draw
    }
}
