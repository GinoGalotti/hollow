namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Cards;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Turn;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Play 1 top + 1 bottom per turn for 3 turns; verify Rest is forced on turn 4 (not 5).
/// Also verifies deck is thinner post-Rest (draw pile shrunk by rest-dissolve).
/// </summary>
public class BottomBudgetTest : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private class GenericWarden : IWardenAbility
    {
        public string WardenId => "generic";
        public BottomResult OnBottomPlayed(Card card, EncounterTier tier) =>
            tier == EncounterTier.Boss ? BottomResult.PermanentlyRemoved : BottomResult.Dissolved;
        public BottomResult OnRestDissolve(Card card) => BottomResult.Dissolved;
        public void OnResolution(HollowWardens.Core.Encounter.EncounterState state) { }
        public int CalculatePassiveFear() => 0;
    }

    [Fact]
    public void RestForcedOnTurn4_After3BottomPlays()
    {
        // 10 cards, hand limit 5, generic warden (bottoms dissolve)
        // Play 1 top + 1 bottom per turn → deck depletes such that Rest is needed on turn 4
        var config = IntegrationHelpers.MakeConfig(tideCount: 7);
        var deck = IntegrationHelpers.MakeCards(10);
        var warden = new GenericWarden();
        var (state, _, _, _, _) = IntegrationHelpers.Build(deck, warden, config);

        var resolver = new EffectResolver();
        var turnManager = new TurnManager(state, resolver);

        int restDetectedOnTurn = -1;

        for (int turn = 1; turn <= 6; turn++)
        {
            turnManager.StartVigil();

            if (turnManager.IsRestTurn)
            {
                restDetectedOnTurn = turn;
                break;
            }

            // Play 1 top
            var topCard = state.Deck!.Hand.FirstOrDefault(c => !c.IsDormant);
            if (topCard != null) turnManager.PlayTop(topCard);

            turnManager.EndVigil();
            turnManager.StartDusk();

            // Play 1 bottom
            var bottomCard = state.Deck!.Hand.FirstOrDefault(c => !c.IsDormant);
            if (bottomCard != null) turnManager.PlayBottom(bottomCard);

            turnManager.EndTurn();
        }

        Assert.Equal(4, restDetectedOnTurn);
    }

    [Fact]
    public void DeckThinner_AfterRest()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 7);
        var deck = IntegrationHelpers.MakeCards(10);
        var (state, _, _, _, _) = IntegrationHelpers.Build(deck, new GenericWarden(), config);

        var resolver = new EffectResolver();
        var turnManager = new TurnManager(state, resolver);

        // Play 3 turns of 1 top + 1 bottom
        for (int turn = 1; turn <= 3; turn++)
        {
            turnManager.StartVigil();
            var top = state.Deck!.Hand.FirstOrDefault(c => !c.IsDormant);
            if (top != null) turnManager.PlayTop(top);
            turnManager.EndVigil();
            turnManager.StartDusk();
            var bottom = state.Deck!.Hand.FirstOrDefault(c => !c.IsDormant);
            if (bottom != null) turnManager.PlayBottom(bottom);
            turnManager.EndTurn();
        }

        // Turn 4 should be Rest
        turnManager.StartVigil();
        Assert.True(turnManager.IsRestTurn);

        int drawBeforeRest = state.Deck!.DrawPileCount;
        int discardBeforeRest = state.Deck.DiscardCount;

        turnManager.Rest();

        // After Rest: discards shuffled back, minus 1 rest-dissolve
        int drawAfterRest = state.Deck.DrawPileCount;
        // Draw pile = discardBeforeRest - 1 (rest-dissolve)
        Assert.Equal(discardBeforeRest - 1, drawAfterRest);
    }
}
