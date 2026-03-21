namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using Xunit;

/// <summary>
/// Verifies the provocation rule:
/// Only Ravage and Corrupt action cards trigger native counter-attack.
/// March, Rest, Settle, and Regroup do not.
/// </summary>
public class NativeProvocationTests : IDisposable
{
    private readonly CombatSystem _sut = new();
    public void Dispose() => GameEvents.ClearAll();

    private static ActionCard Card(string id) => new() { Id = id, AdvanceModifier = 1 };

    [Fact]
    public void IsProvoked_Ravage_ReturnsTrue()
    {
        Assert.True(_sut.IsProvokedAction(Card("ravage")));
    }

    [Fact]
    public void IsProvoked_PmRavage_ReturnsTrue()
    {
        Assert.True(_sut.IsProvokedAction(Card("pm_ravage")));
    }

    [Fact]
    public void IsProvoked_PmCorrupt_ReturnsTrue()
    {
        Assert.True(_sut.IsProvokedAction(Card("pm_corrupt")));
    }

    [Fact]
    public void IsProvoked_PmMarch_ReturnsFalse()
    {
        Assert.False(_sut.IsProvokedAction(Card("pm_march")));
    }

    [Fact]
    public void IsProvoked_PmRest_ReturnsFalse()
    {
        Assert.False(_sut.IsProvokedAction(Card("pm_rest")));
    }

    [Fact]
    public void IsProvoked_PmSettle_ReturnsFalse()
    {
        Assert.False(_sut.IsProvokedAction(Card("pm_settle")));
    }

    [Fact]
    public void ExecuteActivate_PmRavage_ExecutesRavageEffect()
    {
        // Verifies the Contains-based fix: "pm_ravage" now correctly executes Ravage
        var state = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader("i1", UnitType.Marcher, 2, "A1"));
        territory.Natives.Add(MakeNative(2, "A1"));

        _sut.ExecuteActivate(Card("pm_ravage"), territory, state);

        // Ravage should have added corruption and damaged the native
        Assert.True(territory.CorruptionPoints > 0, "pm_ravage should add corruption");
    }

    [Fact]
    public void ExecuteActivate_PmMarch_ExecutesMarchEffect()
    {
        var state = CreateState();
        var territory = state.GetTerritory("A1")!;
        var invader = MakeInvader("i1", UnitType.Marcher, 2, "A1");
        invader.Hp = 1; // wounded
        territory.Invaders.Add(invader);

        _sut.ExecuteActivate(Card("pm_march"), territory, state);

        // March should restore 1 HP (up to max)
        Assert.Equal(2, invader.Hp);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static EncounterState CreateState() => new()
    {
        Territories = new List<Territory>
        {
            new() { Id = "A1", Row = TerritoryRow.Arrival },
            new() { Id = "A2", Row = TerritoryRow.Arrival },
            new() { Id = "A3", Row = TerritoryRow.Arrival },
            new() { Id = "M1", Row = TerritoryRow.Middle  },
            new() { Id = "M2", Row = TerritoryRow.Middle  },
            new() { Id = "I1", Row = TerritoryRow.Inner   },
        },
        Corruption = new CorruptionSystem(),
        Weave      = new WeaveSystem(),
    };

    private static Invader MakeInvader(string id, UnitType type, int hp, string tId) =>
        new() { Id = id, UnitType = type, Hp = hp, MaxHp = hp, TerritoryId = tId };

    private static Native MakeNative(int hp, string tId) =>
        new() { Hp = hp, MaxHp = 2, Damage = 3, TerritoryId = tId };
}
