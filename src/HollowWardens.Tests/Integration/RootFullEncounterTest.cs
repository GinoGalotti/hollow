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

        // A1↔M1 are adjacent → 1 undirected edge → 1 network fear
        int fear = warden.CalculatePassiveFear();
        Assert.Equal(1, fear);
    }

    [Fact]
    public void NetworkFear_TwoAdjacentPresencePairs_Returns2NotFour()
    {
        // A1↔M1 and M1↔I1 form a chain of 2 undirected edges.
        // Directed counting would give 4 (each edge counted twice); undirected gives 2.
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var presence = new PresenceSystem(() => territories);
        var warden = new RootAbility(presence);

        territories.First(t => t.Id == "A1").PresenceCount = 1;
        territories.First(t => t.Id == "M1").PresenceCount = 1;
        territories.First(t => t.Id == "I1").PresenceCount = 1;

        int fear = warden.CalculatePassiveFear();
        Assert.Equal(2, fear); // 2 undirected edges, not 4 directed
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
    public void Assimilation_RemovesUpToPresenceCount_WeakestFirst()
    {
        // A1 presence=2, adjacent A2 has 3 invaders HP 2,3,5 → removes 2 weakest, HP5 survives
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new HollowWardens.Core.Encounter.EncounterState
        {
            Territories = territories,
            Corruption = new HollowWardens.Core.Systems.CorruptionSystem(),
        };

        state.GetTerritory("A1")!.PresenceCount = 2;
        var a2 = state.GetTerritory("A2")!;
        a2.Invaders.Add(new Invader { Id = "i1", Hp = 2, MaxHp = 2, UnitType = UnitType.Marcher, TerritoryId = "A2" });
        a2.Invaders.Add(new Invader { Id = "i2", Hp = 3, MaxHp = 3, UnitType = UnitType.Marcher, TerritoryId = "A2" });
        a2.Invaders.Add(new Invader { Id = "i3", Hp = 5, MaxHp = 5, UnitType = UnitType.Ironclad, TerritoryId = "A2" });

        warden.OnResolution(state);

        Assert.Single(a2.Invaders); // only HP5 survives
        Assert.Equal(5, a2.Invaders[0].Hp);
    }

    [Fact]
    public void Assimilation_MoreInvadersThanPresence_SomeRemain()
    {
        // A1 presence=1, A2 has 3 invaders → only 1 removed
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new HollowWardens.Core.Encounter.EncounterState
        {
            Territories = territories,
            Corruption = new HollowWardens.Core.Systems.CorruptionSystem(),
        };

        state.GetTerritory("A1")!.PresenceCount = 1;
        var a2 = state.GetTerritory("A2")!;
        for (int i = 0; i < 3; i++)
            a2.Invaders.Add(new Invader { Id = $"i{i}", Hp = 3, MaxHp = 3, UnitType = UnitType.Marcher, TerritoryId = "A2" });

        warden.OnResolution(state);

        Assert.Equal(2, a2.Invaders.Count); // 3 - 1 = 2 remain
    }

    [Fact]
    public void Assimilation_MaxPresence_RemovesThree()
    {
        // A1 presence=3, A2 has 5 invaders → removes 3, 2 remain
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new HollowWardens.Core.Encounter.EncounterState
        {
            Territories = territories,
            Corruption = new HollowWardens.Core.Systems.CorruptionSystem(),
        };

        state.GetTerritory("A1")!.PresenceCount = 3;
        var a2 = state.GetTerritory("A2")!;
        for (int i = 0; i < 5; i++)
            a2.Invaders.Add(new Invader { Id = $"i{i}", Hp = 3, MaxHp = 3, UnitType = UnitType.Marcher, TerritoryId = "A2" });

        warden.OnResolution(state);

        Assert.Equal(2, a2.Invaders.Count);
    }

    [Fact]
    public void Assimilation_ReducesCorruptionByRemoveCount()
    {
        // A1 presence=2, A2 has 3 invaders + 5 corruption → removes 2 → corruption becomes 3
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new HollowWardens.Core.Encounter.EncounterState
        {
            Territories = territories,
            Corruption = new HollowWardens.Core.Systems.CorruptionSystem(),
        };

        state.GetTerritory("A1")!.PresenceCount = 2;
        var a2 = state.GetTerritory("A2")!;
        a2.CorruptionPoints = 5;
        for (int i = 0; i < 3; i++)
            a2.Invaders.Add(new Invader { Id = $"i{i}", Hp = 3, MaxHp = 3, UnitType = UnitType.Marcher, TerritoryId = "A2" });

        warden.OnResolution(state);

        Assert.Equal(3, a2.CorruptionPoints); // 5 - 2 = 3
    }

    [Fact]
    public void Assimilation_NoInvaders_NoCorruptionChange()
    {
        // A1 presence=2, A2 has 0 invaders + 4 corruption → no change
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new HollowWardens.Core.Encounter.EncounterState
        {
            Territories = territories,
            Corruption = new HollowWardens.Core.Systems.CorruptionSystem(),
        };

        state.GetTerritory("A1")!.PresenceCount = 2;
        state.GetTerritory("A2")!.CorruptionPoints = 4;

        warden.OnResolution(state);

        Assert.Equal(4, state.GetTerritory("A2")!.CorruptionPoints);
    }

    [Fact]
    public void OnResolution_Assimilation_ReducesNeighborCorruption()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var deck = IntegrationHelpers.MakeCards(10);
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility(new PresenceSystem(() => territories));
        var (state, _, _, _, faction) = IntegrationHelpers.Build(deck, warden, config);

        // Place Presence in A1; put 1 invader + 5 corruption on A2 (adjacent to A1)
        state.GetTerritory("A1")!.PresenceCount = 1;
        state.GetTerritory("A2")!.CorruptionPoints = 5;
        // D30: corruption reduction tied to invader removal — need an invader to remove
        var invader = faction.CreateUnit(UnitType.Marcher, "A2");
        state.GetTerritory("A2")!.Invaders.Add(invader);

        warden.OnResolution(state);

        // Invader removed (PresenceCount=1 removes 1 weakest), corruption -1 per removed
        Assert.Equal(4, state.GetTerritory("A2")!.CorruptionPoints);
    }
}
