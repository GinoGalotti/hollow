namespace HollowWardens.Tests;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Tests for the passive gating system: Root starts with 3 base passives active;
/// pool passives (rest_growth, presence_provocation, network_slow) start locked
/// and unlock via ForceUnlock (end-of-encounter rewards in play; sim/test use ForceUnlock).
/// Also covers: Network Fear cap at 3, new invader-surrounding mechanic.
/// </summary>
public class PassiveGatingTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Root_StartsWithThreeActivePassives()
    {
        // B6 redesign: presence_provocation is now base; assimilation moved to pool
        var gating = new PassiveGating("root");
        Assert.Equal(3, gating.ActivePassives.Count);
        Assert.True(gating.IsActive("network_fear"));
        Assert.True(gating.IsActive("dormancy"));
        Assert.True(gating.IsActive("presence_provocation"));
    }

    [Fact]
    public void Root_NetworkSlowInactive_AtStart()
    {
        var gating = new PassiveGating("root");
        Assert.False(gating.IsActive("network_slow"));
    }

    [Fact]
    public void Root_ProvocationActive_AtStart()
    {
        // B6 redesign: presence_provocation is now a base passive (always active)
        var gating = new PassiveGating("root");
        Assert.True(gating.IsActive("presence_provocation"));
    }

    [Fact]
    public void Root_AssimilationInactive_AtStart()
    {
        // B6 redesign: assimilation moved from base to pool — starts locked
        var gating = new PassiveGating("root");
        Assert.False(gating.IsActive("assimilation"));
    }

    [Fact]
    public void Root_RestGrowthInactive_AtStart()
    {
        var gating = new PassiveGating("root");
        Assert.False(gating.IsActive("rest_growth"));
    }

    // ── Unlock via ForceUnlock (reward-based) ─────────────────────────────────

    [Fact]
    public void Root_RestGrowth_UnlocksViaForceUnlock()
    {
        var gating = new PassiveGating("root");
        gating.ForceUnlock("rest_growth");
        Assert.True(gating.IsActive("rest_growth"));
    }

    [Fact]
    public void Root_Provocation_UnlocksViaForceUnlock()
    {
        var gating = new PassiveGating("root");
        gating.ForceUnlock("presence_provocation");
        Assert.True(gating.IsActive("presence_provocation"));
    }

    [Fact]
    public void Root_NetworkSlow_UnlocksViaForceUnlock()
    {
        var gating = new PassiveGating("root");
        gating.ForceUnlock("network_slow");
        Assert.True(gating.IsActive("network_slow"));
    }

    [Fact]
    public void Root_ForceUnlock_FiresPassiveUnlockedEvent()
    {
        var gating = new PassiveGating("root");
        string? unlockedId = null;
        gating.PassiveUnlocked += (id, _) => unlockedId = id;

        gating.ForceUnlock("network_slow");

        Assert.Equal("network_slow", unlockedId);
    }

    [Fact]
    public void Root_ForceUnlock_AlreadyActive_DoesNotRefire()
    {
        var gating = new PassiveGating("root");
        int eventCount = 0;
        gating.PassiveUnlocked += (_, __) => eventCount++;

        gating.ForceUnlock("rest_growth");
        gating.ForceUnlock("rest_growth"); // second call — already unlocked

        Assert.Equal(1, eventCount);
        Assert.True(gating.IsActive("rest_growth"));
    }

    // ── Guard effect on RootAbility ───────────────────────────────────────────

    [Fact]
    public void Root_GetMovementPenalty_ReturnsZero_WhenNetworkSlowLocked()
    {
        var gating = new PassiveGating("root");
        var root = new RootAbility { Gating = gating };

        var territories = new List<Territory>
        {
            new() { Id = "A1", Row = TerritoryRow.Arrival, PresenceCount = 1 },
            new() { Id = "A2", Row = TerritoryRow.Arrival, PresenceCount = 1 },
            new() { Id = "M1", Row = TerritoryRow.Middle }
        };
        territories.First(t => t.Id == "M1").Invaders.Add(
            new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 4, MaxHp = 4, TerritoryId = "M1" });

        // Network Slow locked → returns 0 even with 2 presence neighbors
        Assert.Equal(0, root.GetMovementPenalty("M1", territories));
    }

    [Fact]
    public void Root_ProvokesNatives_ReturnsFalse_WhenForceLocked()
    {
        // B6: presence_provocation is now base (active by default).
        // If someone force-locks it (e.g. for debug), ProvokesNatives must return false.
        var gating = new PassiveGating("root");
        gating.ForceLock("presence_provocation");
        var root = new RootAbility { Gating = gating };
        var territory = new Territory { Id = "M1", Row = TerritoryRow.Middle, PresenceCount = 1 };

        Assert.False(root.ProvokesNatives(territory));
    }

    [Fact]
    public void Root_OnRest_DoesNothing_WhenRestGrowthLocked()
    {
        var gating = new PassiveGating("root");
        var territories = new List<Territory>
        {
            new() { Id = "M1", Row = TerritoryRow.Middle, PresenceCount = 1 }
        };
        var presence = new PresenceSystem(() => territories);
        var root = new RootAbility(presence) { Gating = gating };

        var state = new EncounterState
        {
            Territories = territories,
            Presence    = presence,
            Corruption  = new CorruptionSystem(),
            Weave       = new WeaveSystem(),
        };

        root.OnRest(state, "M1");

        // rest_growth locked → PresenceCount stays at 1
        Assert.Equal(1, territories.First(t => t.Id == "M1").PresenceCount);
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Root_Reset_ClearsUnlocks()
    {
        var gating = new PassiveGating("root");
        gating.ForceUnlock("rest_growth");
        gating.ForceUnlock("network_slow");
        Assert.True(gating.IsActive("rest_growth"));
        Assert.True(gating.IsActive("network_slow"));

        gating.Reset();

        Assert.False(gating.IsActive("rest_growth"));
        Assert.False(gating.IsActive("network_slow"));
        // B6 base 3 still active after reset
        Assert.True(gating.IsActive("network_fear"));
        Assert.True(gating.IsActive("dormancy"));
        Assert.True(gating.IsActive("presence_provocation"));
        Assert.False(gating.IsActive("assimilation")); // assimilation is pool, not base
    }

    // ── Network Fear mechanic ─────────────────────────────────────────────────

    [Fact]
    public void Root_NetworkFear_ZeroInvaders_GeneratesNoFear()
    {
        // 6 territories all with presence, no invaders → 0 fear
        var territories = BoardState.CreatePyramid().Territories.Values.ToList();
        foreach (var t in territories) t.PresenceCount = 1;

        var presence = new PresenceSystem(() => territories);
        var root = new RootAbility(presence);
        var state = new EncounterState { Territories = territories };

        Assert.Equal(0, root.CalculatePassiveFear(state));
    }

    [Fact]
    public void Root_NetworkFear_InvaderWith2PresenceNeighbors_GeneratesNoFear()
    {
        // M1 surrounded by only 2 presence neighbors → no fear (need ≥3)
        var territories = BoardState.CreatePyramid().Territories.Values.ToList();
        var m1 = territories.First(t => t.Id == "M1");
        m1.Invaders.Add(new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 3, MaxHp = 3, TerritoryId = "M1" });

        // Give only 2 neighbors presence (I1 and A1, not A2)
        territories.First(t => t.Id == "I1").PresenceCount = 1;
        territories.First(t => t.Id == "A1").PresenceCount = 1;

        var presence = new PresenceSystem(() => territories);
        var root = new RootAbility(presence);
        var state = new EncounterState { Territories = territories };

        Assert.Equal(0, root.CalculatePassiveFear(state));
    }

    [Fact]
    public void Root_NetworkFear_InvaderWith3PresenceNeighbors_GeneratesFear()
    {
        // I1 is adjacent to M1, M2 — place presence there + A1 to surround a territory
        // Use a simple manual setup: invader at M1, surrounded by 3 presence territories
        var territories = BoardState.CreatePyramid().Territories.Values.ToList();
        var m1 = territories.First(t => t.Id == "M1");
        m1.Invaders.Add(new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 3, MaxHp = 3, TerritoryId = "M1" });

        // Give all 3 neighbors of M1 presence (I1, A1, A2 per standard graph)
        foreach (var neighborId in TerritoryGraph.Standard.GetNeighbors("M1"))
        {
            var neighbor = territories.FirstOrDefault(t => t.Id == neighborId);
            if (neighbor != null) neighbor.PresenceCount = 1;
        }

        var presence = new PresenceSystem(() => territories);
        var root = new RootAbility(presence);
        var state = new EncounterState { Territories = territories };

        // 1 invader in territory surrounded by ≥3 presence → 1 fear
        Assert.Equal(1, root.CalculatePassiveFear(state));
    }

    [Fact]
    public void Root_NetworkFear_CappedAt3()
    {
        // Many invaders in surrounded territories → capped at 3
        var territories = BoardState.CreatePyramid().Territories.Values.ToList();
        foreach (var t in territories) t.PresenceCount = 1;

        // Add 5 invaders to M1 (surrounded)
        var m1 = territories.First(t => t.Id == "M1");
        for (int i = 0; i < 5; i++)
            m1.Invaders.Add(new Invader { Id = $"i{i}", UnitType = UnitType.Marcher, Hp = 3, MaxHp = 3, TerritoryId = "M1" });

        var presence = new PresenceSystem(() => territories);
        var root = new RootAbility(presence);
        var state = new EncounterState { Territories = territories };

        // 5 surrounded invaders → would be 5 fear, but capped at 3
        Assert.Equal(3, root.CalculatePassiveFear(state));
    }

    // ── Upgrade system ────────────────────────────────────────────────────────

    [Fact]
    public void Root_RestGrowth_Upgrade_Places2Presence()
    {
        var gating = new PassiveGating("root");
        gating.ForceUnlock("rest_growth");
        gating.UpgradePassive("rest_growth_u1");

        var territories = new List<Territory>
        {
            new() { Id = "M1", Row = TerritoryRow.Middle, PresenceCount = 1 }
        };
        var presence = new PresenceSystem(() => territories);
        var root = new RootAbility(presence) { Gating = gating };

        var state = new EncounterState
        {
            Territories = territories,
            Presence    = presence,
            Corruption  = new CorruptionSystem(),
            Weave       = new WeaveSystem(),
        };

        root.OnRest(state, "M1");

        // Upgraded rest_growth places 2 presence (capped at MaxPresencePerTerritory=3 but starts at 1)
        Assert.Equal(3, territories.First(t => t.Id == "M1").PresenceCount);
    }

    [Fact]
    public void Root_NetworkSlow_Upgrade_Returns2Penalty()
    {
        var gating = new PassiveGating("root");
        gating.ForceUnlock("network_slow");
        gating.UpgradePassive("network_slow_u1");
        var root = new RootAbility { Gating = gating };

        // Set up: M1 with invader, A1+A2+M2 all with presence (≥3 presence neighbors)
        var territories = new List<Territory>
        {
            new() { Id = "I1", Row = TerritoryRow.Inner,   PresenceCount = 1 },
            new() { Id = "M1", Row = TerritoryRow.Middle },
            new() { Id = "M2", Row = TerritoryRow.Middle,  PresenceCount = 1 },
            new() { Id = "A1", Row = TerritoryRow.Arrival, PresenceCount = 1 },
            new() { Id = "A2", Row = TerritoryRow.Arrival, PresenceCount = 1 },
            new() { Id = "A3", Row = TerritoryRow.Arrival, PresenceCount = 1 },
        };
        territories.First(t => t.Id == "M1").Invaders.Add(
            new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 3, MaxHp = 3, TerritoryId = "M1" });

        Assert.Equal(2, root.GetMovementPenalty("M1", territories));
    }
}
