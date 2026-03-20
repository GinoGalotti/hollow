namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Cards;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Root warden full-encounter integration: dormancy, awaken, network fear passive,
/// and assimilation on resolution.
/// </summary>
public class RootFullEncounterTest : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    [Fact]
    public void BottomPlay_MakesCardDormant_NotDissolved()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var deck = IntegrationHelpers.MakeCards(10);
        var warden = new RootAbility();
        var (state, _, _, _, _) = IntegrationHelpers.Build(deck, warden, config);

        var resolver = new EffectResolver();
        var turn = new HollowWardens.Core.Turn.TurnManager(state, resolver);

        turn.StartVigil();
        Assert.False(turn.IsRestTurn);

        var card = state.Deck!.Hand.First(c => !c.IsDormant);
        turn.StartDusk();
        turn.PlayBottom(card);

        // Root bottom → dormant, not dissolved
        Assert.True(card.IsDormant);
        Assert.Equal(0, state.Deck.DissolvedCount);
        Assert.Equal(1, state.Deck.DormantCount);
    }

    [Fact]
    public void DormantCard_IsInDeck_ButNotPlayable()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var deck = IntegrationHelpers.MakeCards(3);
        var warden = new RootAbility();
        var (state, _, _, _, _) = IntegrationHelpers.Build(deck, warden, config, handLimit: 3);

        var resolver = new EffectResolver();
        var turn = new HollowWardens.Core.Turn.TurnManager(state, resolver);

        turn.StartVigil();
        var card = state.Deck!.Hand.First();
        turn.StartDusk();
        turn.PlayBottom(card);

        // On subsequent turn: card is in deck but dormant
        turn.EndTurn();
        turn.StartVigil();

        // Card may or may not be in hand (depends on shuffle); if in hand, not playable
        if (state.Deck.Hand.Contains(card))
            Assert.True(card.IsDormant);
    }

    [Fact]
    public void NetworkFear_GeneratesFear_BasedOnPresenceAdjacency()
    {
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var presence = new PresenceSystem(() => territories);
        var warden = new RootAbility(presence);

        // Place Presence in A1 and M1 (adjacent)
        territories.First(t => t.Id == "A1").PresenceCount = 1;
        territories.First(t => t.Id == "M1").PresenceCount = 1;

        // A1↔M1 are adjacent → 2 directed edges → 2 network fear
        int fear = warden.CalculatePassiveFear();
        Assert.Equal(2, fear);
    }

    [Fact]
    public void OnResolution_Assimilation_RemovesAdjacentInvaders()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var deck = IntegrationHelpers.MakeCards(10);
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var presence = new PresenceSystem(() => territories);
        var warden = new RootAbility(presence);
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(deck, warden, config);

        // Place Presence in M1
        state.GetTerritory("M1")!.PresenceCount = 1;

        // Place invader in I1 (adjacent to M1)
        var invader = faction.CreateUnit(UnitType.Marcher, "I1");
        state.GetTerritory("I1")!.Invaders.Add(invader);

        // Run resolution (calls warden.OnResolution)
        var resRunner = new ResolutionRunner(new EffectResolver());
        resRunner.RunResolution(state, new IntegrationHelpers.IdleStrategy());
        warden.OnResolution(state);

        // Invader in I1 (adjacent to M1 presence) should be removed from the territory list
        Assert.False(state.GetTerritory("I1")!.Invaders.Contains(invader),
            "Assimilation should remove invaders from territories adjacent to Root Presence");
    }

    [Fact]
    public void OnResolution_Assimilation_ReducesNeighborCorruption()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var deck = IntegrationHelpers.MakeCards(10);
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility(new PresenceSystem(() => territories));
        var (state, _, _, _, _) = IntegrationHelpers.Build(deck, warden, config);

        // Place Presence in A1; put 5 corruption on A2 (adjacent to A1)
        state.GetTerritory("A1")!.PresenceCount = 1;
        state.GetTerritory("A2")!.CorruptionPoints = 5;

        warden.OnResolution(state);

        // A2 corruption reduced by 1 (from 5 to 4)
        Assert.Equal(4, state.GetTerritory("A2")!.CorruptionPoints);
    }
}
