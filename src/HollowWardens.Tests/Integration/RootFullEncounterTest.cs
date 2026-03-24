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

    // ── Network Fear ──────────────────────────────────────────────────────────

    [Fact]
    public void NetworkFear_InvaderWith2PresenceNeighbors_GeneratesNoFear()
    {
        // New mechanic: need ≥3 presence-neighbor territories to generate fear
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var presence = new PresenceSystem(() => territories);
        var warden = new RootAbility(presence);

        // M1 has invader; only 2 neighbors have presence → no fear
        territories.First(t => t.Id == "M1").Invaders.Add(
            new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 3, MaxHp = 3, TerritoryId = "M1" });
        territories.First(t => t.Id == "I1").PresenceCount = 1;
        territories.First(t => t.Id == "A1").PresenceCount = 1;

        var state = new EncounterState { Territories = territories };

        Assert.Equal(0, warden.CalculatePassiveFear(state));
    }

    [Fact]
    public void NetworkFear_InvaderSurroundedBy3PresenceNeighbors_Generates1Fear()
    {
        // M1 has invader; all 3 neighbors have presence → 1 fear
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var presence = new PresenceSystem(() => territories);
        var warden = new RootAbility(presence);

        territories.First(t => t.Id == "M1").Invaders.Add(
            new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 3, MaxHp = 3, TerritoryId = "M1" });

        // Give all 3 neighbors of M1 presence (I1, A1, A2)
        foreach (var neighborId in HollowWardens.Core.Map.TerritoryGraph.Standard.GetNeighbors("M1"))
        {
            var neighbor = territories.FirstOrDefault(t => t.Id == neighborId);
            if (neighbor != null) neighbor.PresenceCount = 1;
        }

        var state = new EncounterState { Territories = territories };

        Assert.Equal(1, warden.CalculatePassiveFear(state));
    }

    // ── Assimilation — base spawn (B6 tide-start, three formula modes) ──────────

    [Fact]
    public void Assimilation_TideStart_Scaled_PresenceOne_SpawnsOne()
    {
        // scaled (default): 1 + floor(1/2) = 1
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        state.GetTerritory("M1")!.PresenceCount = 1;
        warden.OnTideStart(state);

        Assert.Equal(1, state.GetTerritory("M1")!.Natives.Count);
    }

    [Fact]
    public void Assimilation_TideStart_Scaled_PresenceTwo_SpawnsTwo()
    {
        // scaled: 1 + floor(2/2) = 2
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        state.GetTerritory("M1")!.PresenceCount = 2;
        warden.OnTideStart(state);

        Assert.Equal(2, state.GetTerritory("M1")!.Natives.Count);
    }

    [Fact]
    public void Assimilation_TideStart_Scaled_PresenceThree_SpawnsTwo()
    {
        // scaled: 1 + floor(3/2) = 2
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        state.GetTerritory("M1")!.PresenceCount = 3;
        warden.OnTideStart(state);

        Assert.Equal(2, state.GetTerritory("M1")!.Natives.Count);
    }

    [Fact]
    public void Assimilation_TideStart_Linear_PresenceThree_SpawnsThree()
    {
        // linear: count = presence = 3
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var config   = new BalanceConfig { AssimilationSpawnMode = "linear" };
        var presence = new PresenceSystem(() => territories);
        var warden   = new RootAbility(presence, config);
        var state    = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        state.GetTerritory("M1")!.PresenceCount = 3;
        warden.OnTideStart(state);

        Assert.Equal(3, state.GetTerritory("M1")!.Natives.Count);
    }

    [Fact]
    public void Assimilation_TideStart_Half_PresenceOne_SpawnsOne()
    {
        // half: ceil(1/2) = 1
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var config   = new BalanceConfig { AssimilationSpawnMode = "half" };
        var presence = new PresenceSystem(() => territories);
        var warden   = new RootAbility(presence, config);
        var state    = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        state.GetTerritory("M1")!.PresenceCount = 1;
        warden.OnTideStart(state);

        Assert.Equal(1, state.GetTerritory("M1")!.Natives.Count);
    }

    [Fact]
    public void Assimilation_TideStart_Half_PresenceThree_SpawnsTwo()
    {
        // half: ceil(3/2) = 2
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var config   = new BalanceConfig { AssimilationSpawnMode = "half" };
        var presence = new PresenceSystem(() => territories);
        var warden   = new RootAbility(presence, config);
        var state    = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        state.GetTerritory("M1")!.PresenceCount = 3;
        warden.OnTideStart(state);

        Assert.Equal(2, state.GetTerritory("M1")!.Natives.Count);
    }

    [Fact]
    public void Assimilation_TideStart_SkipsWhenNoPresence()
    {
        // No territory has presence — nothing spawns
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        warden.OnTideStart(state);

        Assert.True(territories.All(t => t.Natives.Count == 0));
    }

    [Fact]
    public void Assimilation_TideStart_SpawnDoesNotRequireInvaders()
    {
        // Spawn fires regardless of whether invaders are present
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        state.GetTerritory("M1")!.PresenceCount = 2;
        warden.OnTideStart(state);

        Assert.Equal(2, state.GetTerritory("M1")!.Natives.Count); // scaled: 1 + floor(2/2) = 2
        Assert.Empty(state.GetTerritory("M1")!.Invaders);
    }

    // ── Assimilation — upgraded conversion at Resolution (assimilation_u1) ───────

    [Fact]
    public void Assimilation_Upgraded_2Presence_2Natives_1Invader_ConvertsOne()
    {
        // Upgrade conversion at Resolution: floor(min(2,2)/2) = 1 invader converted.
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var gating = new PassiveGating("root");
        gating.UpgradePassive("assimilation_u1");
        warden.Gating = gating;
        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        var m1 = state.GetTerritory("M1")!;
        m1.PresenceCount = 2;
        m1.Natives.Add(new Native { Hp = 2, MaxHp = 2, Damage = 1, TerritoryId = "M1" });
        m1.Natives.Add(new Native { Hp = 2, MaxHp = 2, Damage = 1, TerritoryId = "M1" });
        m1.Invaders.Add(new Invader { Id = "i1", Hp = 4, MaxHp = 4, UnitType = UnitType.Marcher, TerritoryId = "M1" });

        warden.OnResolution(state);

        Assert.Empty(m1.Invaders);         // 1 invader converted
        Assert.Equal(3, m1.Natives.Count); // 2 original + 1 converted
    }

    [Fact]
    public void Assimilation_Upgraded_4Presence_4Natives_Converts2()
    {
        // 4 presence + 4 natives at Resolution: floor(min(4,4)/2) = 2 conversions.
        // Weakest 2 converted; HP5 Ironclad remains. (Spawn happens at tide start, not here.)
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var gating = new PassiveGating("root");
        gating.UpgradePassive("assimilation_u1");
        warden.Gating = gating;
        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        var m1 = state.GetTerritory("M1")!;
        m1.PresenceCount = 4;
        for (int i = 0; i < 4; i++)
            m1.Natives.Add(new Native { Hp = 2, MaxHp = 2, Damage = 1, TerritoryId = "M1" });
        m1.Invaders.Add(new Invader { Id = "i1", Hp = 2, MaxHp = 2, UnitType = UnitType.Marcher,  TerritoryId = "M1" });
        m1.Invaders.Add(new Invader { Id = "i2", Hp = 3, MaxHp = 3, UnitType = UnitType.Marcher,  TerritoryId = "M1" });
        m1.Invaders.Add(new Invader { Id = "i3", Hp = 5, MaxHp = 5, UnitType = UnitType.Ironclad, TerritoryId = "M1" });

        warden.OnResolution(state);

        Assert.Single(m1.Invaders);
        Assert.Equal(5, m1.Invaders[0].Hp);
        Assert.Equal(6, m1.Natives.Count); // 4 original + 2 converted
    }

    [Fact]
    public void Assimilation_Upgraded_ConvertedInvader_BecomesNativeWithHalfHp()
    {
        // Converted native HP = max(1, invader.MaxHp / 2).
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var gating = new PassiveGating("root");
        gating.UpgradePassive("assimilation_u1");
        warden.Gating = gating;
        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        var m1 = state.GetTerritory("M1")!;
        m1.PresenceCount = 2;
        m1.Natives.Add(new Native { Hp = 2, MaxHp = 2, Damage = 1, TerritoryId = "M1" });
        m1.Natives.Add(new Native { Hp = 2, MaxHp = 2, Damage = 1, TerritoryId = "M1" });
        m1.Invaders.Add(new Invader { Id = "i1", Hp = 6, MaxHp = 6, UnitType = UnitType.Ironclad, TerritoryId = "M1" });

        warden.OnResolution(state);

        Assert.Empty(m1.Invaders);
        var newNative = m1.Natives.Last();
        Assert.Equal(3, newNative.Hp);    // max(1, 6/2) = 3
        Assert.Equal(3, newNative.MaxHp);
        Assert.Equal(1, newNative.Damage);
    }

    [Fact]
    public void Assimilation_Upgraded_FiresInvaderDefeatedEvent_PerConversion()
    {
        // 4 presence + 4 natives: floor(min(4,4)/2) = 2 conversions → 2 InvaderDefeated events.
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var gating = new PassiveGating("root");
        gating.UpgradePassive("assimilation_u1");
        warden.Gating = gating;
        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        var m1 = state.GetTerritory("M1")!;
        m1.PresenceCount = 4;
        for (int i = 0; i < 4; i++)
            m1.Natives.Add(new Native { Hp = 2, MaxHp = 2, Damage = 1, TerritoryId = "M1" });
        m1.Invaders.Add(new Invader { Id = "i1", Hp = 3, MaxHp = 3, UnitType = UnitType.Marcher, TerritoryId = "M1" });
        m1.Invaders.Add(new Invader { Id = "i2", Hp = 3, MaxHp = 3, UnitType = UnitType.Marcher, TerritoryId = "M1" });

        int defeatedCount = 0;
        GameEvents.InvaderDefeated += _ => defeatedCount++;

        warden.OnResolution(state);

        Assert.Equal(2, defeatedCount);
    }
}
