namespace HollowWardens.Tests.Cards;

using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Verifies that playing a bottom card with a Dormant result routes the card
/// to the discard pile (not the draw pile). The card moves to the draw pile
/// only after a Rest cycle, matching player expectations.
/// </summary>
public class DormantToDiscardTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private static Card MakeCard(string id) => new() { Id = id, Name = id };

    private static DeckManager MakeRootDeck(int count = 6, int handLimit = 5)
        => new DeckManager(new RootAbility(), Enumerable.Range(1, count).Select(i => MakeCard($"c{i}")).ToList(),
            rng: GameRandom.FromSeed(0), handLimit: handLimit, shuffle: false);

    [Fact]
    public void PlayBottom_Dormant_CardInDiscard()
    {
        var dm = MakeRootDeck();
        dm.RefillHand(); // hand=5, draw=1
        var card = dm.Hand[0];

        dm.PlayBottom(card, EncounterTier.Standard);

        Assert.Equal(1, dm.DiscardCount);
    }

    [Fact]
    public void PlayBottom_Dormant_DrawPileUnchanged()
    {
        var dm = MakeRootDeck();
        dm.RefillHand(); // hand=5, draw=1
        int drawBefore = dm.DrawPileCount;
        var card = dm.Hand[0];

        dm.PlayBottom(card, EncounterTier.Standard);

        Assert.Equal(drawBefore, dm.DrawPileCount);
    }

    [Fact]
    public void PlayBottom_Dormant_CardIsDormant()
    {
        var dm = MakeRootDeck();
        dm.RefillHand();
        var card = dm.Hand[0];

        dm.PlayBottom(card, EncounterTier.Standard);

        Assert.True(card.IsDormant);
        Assert.Equal(1, dm.DormantCount);
    }

    [Fact]
    public void PlayBottom_Dormant_RestCyclesCardToDrawPile()
    {
        // 1-card deck: play bottom → discard; then Rest → discard moves to draw,
        // rest-dissolve picks it (Root: stays dormant via InsertRandom).
        var dm = new DeckManager(new RootAbility(), new[] { MakeCard("solo") },
            rng: GameRandom.FromSeed(0), handLimit: 1, shuffle: false);
        dm.RefillHand();
        var card = dm.Hand[0];

        dm.PlayBottom(card, EncounterTier.Standard); // → dormant in discard
        Assert.Equal(1, dm.DiscardCount);
        Assert.Equal(0, dm.DrawPileCount);

        dm.Rest(); // discard → draw, then rest-dissolve re-inserts it

        Assert.Equal(1, dm.DrawPileCount);
        Assert.True(card.IsDormant);
    }
}
