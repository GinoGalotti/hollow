namespace HollowWardens.Tests.Encounter;

using HollowWardens.Core;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using Xunit;

public class ActionDeckTests
{
    private static ActionCard MakePainful(string id) =>
        new() { Id = id, Name = id, Pool = ActionPool.Painful, AdvanceModifier = 1 };

    private static ActionCard MakeEasy(string id) =>
        new() { Id = id, Name = id, Pool = ActionPool.Easy, AdvanceModifier = 1 };

    [Fact]
    public void Draw_ReturnsCardFromCorrectPool()
    {
        var deck = new ActionDeck(
            new[] { MakePainful("p1"), MakePainful("p2") },
            new[] { MakeEasy("e1"),    MakeEasy("e2") },
            shuffle: false);

        var painful = deck.Draw(ActionPool.Painful);
        var easy    = deck.Draw(ActionPool.Easy);

        Assert.Equal(ActionPool.Painful, painful.Pool);
        Assert.Equal(ActionPool.Easy,    easy.Pool);
    }

    [Fact]
    public void Draw_ReshufflesWhenPainfulPoolExhausted()
    {
        var deck = new ActionDeck(
            new[] { MakePainful("p1") },
            new[] { MakeEasy("e1") },
            rng: GameRandom.FromSeed(42),
            shuffle: false);

        deck.Draw(ActionPool.Painful);  // exhausts the draw pile → p1 in discard

        // Next draw must reshuffle discard back and return a card
        var card = deck.Draw(ActionPool.Painful);

        Assert.Equal("p1", card.Id);
    }

    [Fact]
    public void Draw_ReshufflesWhenEasyPoolExhausted()
    {
        var deck = new ActionDeck(
            new[] { MakePainful("p1") },
            new[] { MakeEasy("e1") },
            rng: GameRandom.FromSeed(42),
            shuffle: false);

        deck.Draw(ActionPool.Easy);  // exhausts easy draw pile

        var card = deck.Draw(ActionPool.Easy);

        Assert.Equal("e1", card.Id);
    }

    [Fact]
    public void AddEscalationCard_IncreasesPainfulPool()
    {
        var deck = new ActionDeck(
            new[] { MakePainful("p1") },
            new[] { MakeEasy("e1") },
            shuffle: false);

        var escalation = new ActionCard
        {
            Id = "pm_corrupt", Name = "Corrupt",
            Pool = ActionPool.Painful, IsEscalation = true
        };
        deck.AddEscalationCard(escalation);

        // Two painful cards now available — both draws must succeed
        deck.Draw(ActionPool.Painful);
        deck.Draw(ActionPool.Painful);

        Assert.Equal(2, deck.PainfulCount);  // both in discard, total unchanged
    }

    [Fact]
    public void AddEscalationCard_RoutesToCorrectPool()
    {
        var deck = new ActionDeck(
            new[] { MakePainful("p1") },
            new[] { MakeEasy("e1") },
            shuffle: false);

        var easyEscalation = new ActionCard
        {
            Id = "easy_esc", Name = "Easy Escalation",
            Pool = ActionPool.Easy, IsEscalation = true
        };
        deck.AddEscalationCard(easyEscalation);

        deck.Draw(ActionPool.Easy);
        deck.Draw(ActionPool.Easy);  // two easy cards available

        Assert.Equal(2, deck.EasyCount);
    }
}
