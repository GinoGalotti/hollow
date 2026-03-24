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

    // ── Assimilation — base spawn (B6) ───────────────────────────────────────

    [Fact]
    public void Assimilation_Base_SpawnsNative_WhenPresenceAtThreshold()
    {
        // Default threshold=3: ≥3 presence → 1 native spawned at Resolution
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        var m1 = state.GetTerritory("M1")!;
        m1.PresenceCount = 3;
        int initialNatives = m1.Natives.Count;

        warden.OnResolution(state);

        Assert.Equal(initialNatives + 1, m1.Natives.Count);
    }

    [Fact]
    public void Assimilation_Base_NoSpawn_BelowThreshold()
    {
        // presence=2 < threshold=3 → no spawn, no conversion
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        var m1 = state.GetTerritory("M1")!;
        m1.PresenceCount = 2;
        m1.Natives.Add(new Native { Hp = 2, MaxHp = 2, Damage = 1, TerritoryId = "M1" });
        m1.Natives.Add(new Native { Hp = 2, MaxHp = 2, Damage = 1, TerritoryId = "M1" });
        m1.Invaders.Add(new Invader { Id = "i1", Hp = 3, MaxHp = 3, UnitType = UnitType.Marcher, TerritoryId = "M1" });

        warden.OnResolution(state);

        Assert.Single(m1.Invaders);        // invader untouched (no conversion)
        Assert.Equal(2, m1.Natives.Count); // no spawn
    }

    [Fact]
    public void Assimilation_Base_SpawnDoesNotRequireInvaders()
    {
        // Base spawn fires at Resolution regardless of whether invaders are present
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        var m1 = state.GetTerritory("M1")!;
        m1.PresenceCount = 3;
        // no invaders — spawn should still fire

        warden.OnResolution(state);

        Assert.Equal(1, m1.Natives.Count); // spawned despite no invaders
        Assert.Empty(m1.Invaders);
    }

    [Fact]
    public void Assimilation_Base_SpawnThreshold_ConfigurableViaBalanceConfig()
    {
        // threshold=2: ≥2 presence is enough to spawn 1 native
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var config   = new BalanceConfig { AssimilationSpawnThreshold = 2 };
        var presence = new PresenceSystem(() => territories);
        var warden   = new RootAbility(presence, config);
        var state    = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        var m1 = state.GetTerritory("M1")!;
        m1.PresenceCount = 2; // below default threshold=3, but meets threshold=2

        warden.OnResolution(state);

        Assert.Equal(1, m1.Natives.Count);
    }

    [Fact]
    public void Assimilation_Base_SpawnsOnlyInQualifyingTerritories()
    {
        // Only territories with presence >= threshold spawn; others untouched
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility();
        var state  = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        var m1 = state.GetTerritory("M1")!;
        var m2 = state.GetTerritory("M2")!;
        m1.PresenceCount = 3; // qualifies (default threshold=3)
        m2.PresenceCount = 2; // below threshold — no spawn

        warden.OnResolution(state);

        Assert.Equal(1, m1.Natives.Count); // spawned
        Assert.Equal(0, m2.Natives.Count); // not spawned
    }

    [Fact]
    public void Assimilation_Base_NoConversion_WithoutUpgrade()
    {
        // Base-only (no upgrade): invaders are never converted even with ≥2 presence + ≥2 natives.
        // Spawn fires because presence=3 ≥ threshold=3.
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var warden = new RootAbility(); // no gating = no upgrade
        var state  = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        var m1 = state.GetTerritory("M1")!;
        m1.PresenceCount = 3;
        m1.Natives.Add(new Native { Hp = 2, MaxHp = 2, Damage = 1, TerritoryId = "M1" });
        m1.Natives.Add(new Native { Hp = 2, MaxHp = 2, Damage = 1, TerritoryId = "M1" });
        m1.Invaders.Add(new Invader { Id = "i1", Hp = 4, MaxHp = 4, UnitType = UnitType.Marcher, TerritoryId = "M1" });

        warden.OnResolution(state);

        Assert.Single(m1.Invaders);        // invader NOT converted (no upgrade)
        Assert.Equal(3, m1.Natives.Count); // 2 original + 1 spawned
    }

    // ── Assimilation — upgraded conversion (assimilation_u1) ─────────────────

    [Fact]
    public void Assimilation_Upgraded_2Presence_2Natives_1Invader_ConvertsOne()
    {
        // presence=2 < spawn threshold=3, so no spawn.
        // Upgrade conversion: floor(min(2,2)/2) = 1 invader converted.
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
        Assert.Equal(3, m1.Natives.Count); // 2 original + 1 converted (no spawn: presence=2 < threshold=3)
    }

    [Fact]
    public void Assimilation_Upgraded_4Presence_4Natives_SpawnsThenConverts2()
    {
        // presence=4 ≥ spawn threshold=3 → base spawn fires first (+1 native → 5 alive).
        // Upgrade conversion: floor(min(4, 5)/2) = 2, weakest converted; HP5 Ironclad remains.
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
        Assert.Equal(7, m1.Natives.Count); // 4 original + 1 spawned + 2 converted
    }

    [Fact]
    public void Assimilation_Upgraded_ConvertedInvader_BecomesNativeWithHalfHp()
    {
        // Converted native HP = max(1, invader.MaxHp / 2). Presence=2 < threshold → no spawn first.
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
        // presence=4 ≥ threshold=3: spawn first (+1 → 5 alive), then floor(min(4,5)/2)=2 conversions
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
