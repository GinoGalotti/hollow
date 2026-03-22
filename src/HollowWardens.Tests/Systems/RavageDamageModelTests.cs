namespace HollowWardens.Tests.Systems;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using Xunit;

/// <summary>
/// Verifies the Ravage damage model: base +2 corruption per Ravage action, plus per-unit:
/// Marcher=2 (total 4), Ironclad=3 (total 5), Outrider=1 (total 3), Pioneer=2 (total 4).
/// Outrider pre-hit is 2 damage.
/// </summary>
public class RavageDamageModelTests : IDisposable
{
    private readonly CombatSystem _sut = new();

    public void Dispose() => GameEvents.ClearAll();

    private static EncounterState CreateState() => new()
    {
        Territories = new List<Territory>
        {
            new() { Id = "A1", Row = TerritoryRow.Arrival },
        },
        Corruption = new CorruptionSystem(),
        Weave      = new WeaveSystem(),
    };

    private static ActionCard RavageCard() => new() { Id = CombatSystem.RavageId, AdvanceModifier = 1 };

    private static Invader MakeInvader(UnitType type, string tId) =>
        new() { Id = $"{type}1", UnitType = type, Hp = 3, MaxHp = 3, TerritoryId = tId };

    [Fact]
    public void Marcher_CorruptionIs2()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader(UnitType.Marcher, "A1"));

        _sut.ExecuteActivate(RavageCard(), territory, state);

        Assert.Equal(4, territory.CorruptionPoints); // 2 base + 2 Marcher
    }

    [Fact]
    public void Ironclad_CorruptionIs3()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader(UnitType.Ironclad, "A1"));

        _sut.ExecuteActivate(RavageCard(), territory, state);

        Assert.Equal(5, territory.CorruptionPoints); // 2 base + 3 Ironclad
    }

    [Fact]
    public void Outrider_CorruptionIs1()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader(UnitType.Outrider, "A1"));
        // No natives → only corruption counted

        _sut.ExecuteActivate(RavageCard(), territory, state);

        Assert.Equal(3, territory.CorruptionPoints); // 2 base + 1 Outrider
    }

    [Fact]
    public void Pioneer_CorruptionIs2()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader(UnitType.Pioneer, "A1"));

        _sut.ExecuteActivate(RavageCard(), territory, state);

        Assert.Equal(4, territory.CorruptionPoints); // 2 base + 2 Pioneer
    }

    [Fact]
    public void CorruptionEqualsNativeDamagePool()
    {
        // 1 Marcher = 4 corruption = 4 native damage (2 base + 2 per-unit)
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader(UnitType.Marcher, "A1"));

        var native = new Native { Hp = 5, MaxHp = 5, Damage = 3, TerritoryId = "A1" };
        territory.Natives.Add(native);

        _sut.ExecuteActivate(RavageCard(), territory, state);

        Assert.Equal(1, native.Hp); // 5 - 4 = 1
        Assert.Equal(4, territory.CorruptionPoints);
    }

    [Fact]
    public void Outrider_PreHit2_DamagesNativeBeforeMainRavage()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader(UnitType.Outrider, "A1"));

        // Native with HP=3: pre-hit (2) → HP 1, then main damage (1) → HP 0 (killed)
        var native = new Native { Hp = 3, MaxHp = 3, Damage = 3, TerritoryId = "A1" };
        territory.Natives.Add(native);

        _sut.ExecuteActivate(RavageCard(), territory, state);

        Assert.False(native.IsAlive); // pre-hit 2 + main 1 = 3 total
    }
}
