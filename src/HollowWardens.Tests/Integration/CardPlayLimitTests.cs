namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Cards;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Turn;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Card play limits enforced by TurnManager:
/// - Max 2 top plays per Vigil
/// - Max 1 bottom play per Dusk
/// - Counters reset at the start of each phase
/// </summary>
public class CardPlayLimitTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private static (EncounterState state, TurnManager tm) BuildTurnManager(int cardCount = 5)
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 6);
        var warden = new RootAbility();
        // Default handLimit=5 ensures the draw pile isn't exhausted after 2 top plays
        var (state, _, _, _, _) = IntegrationHelpers.Build(
            IntegrationHelpers.MakeCards(cardCount), warden, config);
        var tm = new TurnManager(state, new EffectResolver());
        return (state, tm);
    }

    [Fact]
    public void PlayTop_ThirdPlay_IsRejected_InVigil()
    {
        var (state, tm) = BuildTurnManager();
        tm.StartVigil();

        var hand = state.Deck!.Hand.ToList();
        Assert.True(tm.PlayTop(hand[0]));  // play 1 — accepted
        Assert.True(tm.PlayTop(hand[1]));  // play 2 — accepted
        Assert.False(tm.PlayTop(hand[2])); // play 3 — rejected (limit is 2)
    }

    [Fact]
    public void PlayBottom_SecondPlay_IsRejected_InDusk()
    {
        var (state, tm) = BuildTurnManager();
        tm.StartVigil();
        tm.EndVigil();
        tm.StartDusk();

        var hand = state.Deck!.Hand.ToList();
        Assert.True(tm.PlayBottom(hand[0]));  // play 1 — accepted
        Assert.False(tm.PlayBottom(hand[1])); // play 2 — rejected (limit is 1)
    }

    [Fact]
    public void PlayTop_CounterResetsOnNewTurn()
    {
        var (state, tm) = BuildTurnManager(cardCount: 10);
        tm.StartVigil();

        var hand1 = state.Deck!.Hand.ToList();
        tm.PlayTop(hand1[0]);
        tm.PlayTop(hand1[1]);
        Assert.Equal(2, tm.VigilPlaysThisTurn);

        // Simulate end of turn → start next Vigil
        tm.EndVigil();
        tm.StartDusk();
        tm.EndTurn();
        tm.StartVigil(); // second turn Vigil — counter resets

        Assert.Equal(0, tm.VigilPlaysThisTurn);

        var hand2 = state.Deck!.Hand.ToList();
        Assert.True(tm.PlayTop(hand2[0])); // should be accepted again
        Assert.Equal(1, tm.VigilPlaysThisTurn);
    }
}
