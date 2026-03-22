namespace HollowWardens.Tests;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Tests for the passive gating system: Root starts with 3 passives active;
/// rest_growth, presence_provocation, network_slow unlock on threshold triggers.
/// Also covers: Network Fear cap at 4.
/// </summary>
public class PassiveGatingTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Root_StartsWithThreeActivePassives()
    {
        var gating = new PassiveGating("root");
        Assert.Equal(3, gating.ActivePassives.Count);
        Assert.True(gating.IsActive("network_fear"));
        Assert.True(gating.IsActive("dormancy"));
        Assert.True(gating.IsActive("assimilation"));
    }

    [Fact]
    public void Root_NetworkSlowInactive_AtStart()
    {
        var gating = new PassiveGating("root");
        Assert.False(gating.IsActive("network_slow"));
    }

    [Fact]
    public void Root_ProvocationInactive_AtStart()
    {
        var gating = new PassiveGating("root");
        Assert.False(gating.IsActive("presence_provocation"));
    }

    [Fact]
    public void Root_RestGrowthInactive_AtStart()
    {
        var gating = new PassiveGating("root");
        Assert.False(gating.IsActive("rest_growth"));
    }

    // ── Unlock on threshold ───────────────────────────────────────────────────

    [Fact]
    public void Root_RestGrowth_UnlocksOnRootT1()
    {
        var gating = new PassiveGating("root");
        gating.OnThresholdTriggered(Element.Root, 1);
        Assert.True(gating.IsActive("rest_growth"));
    }

    [Fact]
    public void Root_Provocation_UnlocksOnRootT2()
    {
        var gating = new PassiveGating("root");
        gating.OnThresholdTriggered(Element.Root, 2);
        Assert.True(gating.IsActive("presence_provocation"));
    }

    [Fact]
    public void Root_NetworkSlow_UnlocksOnShadowT1()
    {
        var gating = new PassiveGating("root");
        gating.OnThresholdTriggered(Element.Shadow, 1);
        Assert.True(gating.IsActive("network_slow"));
    }

    [Fact]
    public void Root_UnlockFiresEvent()
    {
        var gating = new PassiveGating("root");
        string? unlockedId = null;
        gating.PassiveUnlocked += (id, _) => unlockedId = id;

        gating.OnThresholdTriggered(Element.Shadow, 1);

        Assert.Equal("network_slow", unlockedId);
    }

    [Fact]
    public void Root_DuplicateThreshold_DoesNotReUnlock()
    {
        var gating = new PassiveGating("root");
        int eventCount = 0;
        gating.PassiveUnlocked += (_, __) => eventCount++;

        gating.OnThresholdTriggered(Element.Root, 1);
        gating.OnThresholdTriggered(Element.Root, 1); // second trigger — already unlocked

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
    public void Root_ProvokesNatives_ReturnsFalse_WhenLocked()
    {
        var gating = new PassiveGating("root");
        var root = new RootAbility { Gating = gating };
        var territory = new Territory { Id = "M1", Row = TerritoryRow.Middle, PresenceCount = 1 };

        // presence_provocation locked → false
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
        gating.OnThresholdTriggered(Element.Root, 1);
        gating.OnThresholdTriggered(Element.Shadow, 1);
        Assert.True(gating.IsActive("rest_growth"));
        Assert.True(gating.IsActive("network_slow"));

        gating.Reset();

        Assert.False(gating.IsActive("rest_growth"));
        Assert.False(gating.IsActive("network_slow"));
        // Base 3 still active
        Assert.True(gating.IsActive("network_fear"));
        Assert.True(gating.IsActive("dormancy"));
        Assert.True(gating.IsActive("assimilation"));
    }

    // ── Network Fear cap ──────────────────────────────────────────────────────

    [Fact]
    public void Root_NetworkFearCapped_At4()
    {
        // All 6 territories with presence → 9 undirected edges → normally 9 fear
        var territories = BoardState.CreatePyramid().Territories.Values.ToList();
        foreach (var t in territories)
            t.PresenceCount = 1;

        var presence = new PresenceSystem(() => territories);
        var root = new RootAbility(presence);

        int rawFear = presence.CalculateNetworkFear(); // should be > 4
        int cappedFear = root.CalculatePassiveFear();

        Assert.True(rawFear > 4, $"Expected raw fear > 4 but got {rawFear}");
        Assert.Equal(4, cappedFear);
    }
}
