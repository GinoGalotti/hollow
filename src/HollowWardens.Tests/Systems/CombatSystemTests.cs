namespace HollowWardens.Tests.Systems;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using Xunit;

public class CombatSystemTests : IDisposable
{
    private readonly CombatSystem _sut = new();

    public void Dispose() => GameEvents.ClearAll();

    // ── Helpers ───────────────────────────────────────────────────────────────

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

    private static ActionCard RavageCard() => new() { Id = CombatSystem.RavageId, AdvanceModifier = 1 };
    private static ActionCard MarchCard()  => new() { Id = CombatSystem.MarchId,  AdvanceModifier = 2 };
    private static ActionCard SettleCard() => new() { Id = CombatSystem.SettleId, AdvanceModifier = 0 };

    private static Invader MakeInvader(string id, UnitType type, int hp, string tId) =>
        new() { Id = id, UnitType = type, Hp = hp, MaxHp = hp, TerritoryId = tId };

    private static Native MakeNative(int hp, string tId) =>
        new() { Hp = hp, MaxHp = 2, Damage = 3, TerritoryId = tId };

    // ── Activate: Ravage ──────────────────────────────────────────────────────

    [Fact]
    public void RavageDealsCorruption()
    {
        var state = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader("i1", UnitType.Marcher, 2, "A1"));
        territory.Invaders.Add(MakeInvader("i2", UnitType.Marcher, 2, "A1"));

        _sut.ExecuteActivate(RavageCard(), territory, state);

        Assert.Equal(2, territory.CorruptionPoints); // 1 per Marcher
    }

    [Fact]
    public void RavageDamagesNatives()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader("i1", UnitType.Marcher, 2, "A1"));
        var native = MakeNative(2, "A1");
        territory.Natives.Add(native);

        _sut.ExecuteActivate(RavageCard(), territory, state);

        // 1 Marcher → 1 damage; native not killed
        Assert.Equal(1, native.Hp);
    }

    [Fact]
    public void IroncladDealsExtraCorruption()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader("i1", UnitType.Ironclad, 3, "A1"));

        _sut.ExecuteActivate(RavageCard(), territory, state);

        Assert.Equal(2, territory.CorruptionPoints); // base 1 + Ironclad bonus 1
    }

    [Fact]
    public void OutriderDamagesNativeBeforeRavage()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader("i1", UnitType.Outrider, 2, "A1"));

        var nativeLow  = new Native { Hp = 1, MaxHp = 2, Damage = 3, TerritoryId = "A1" };
        var nativeHigh = MakeNative(2, "A1");
        territory.Natives.Add(nativeLow);
        territory.Natives.Add(nativeHigh);

        _sut.ExecuteActivate(RavageCard(), territory, state);

        // Outrider pre-hit kills nativeLow (HP 1).
        // Main ravage: 1 damage → nativeHigh (HP 2 → 1).
        Assert.False(nativeLow.IsAlive);
        Assert.Equal(1, nativeHigh.Hp);
    }

    [Fact]
    public void PioneerBuildsInfrastructureAfterActivate()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader("i1", UnitType.Pioneer, 1, "A1"));

        _sut.ExecuteActivate(RavageCard(), territory, state);

        Assert.Single(territory.Tokens);
        Assert.Equal(TokenType.Infrastructure, territory.Tokens[0].Type);
    }

    [Fact]
    public void MarchGivesShieldAndHeals()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        var invader   = new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 1, MaxHp = 3, TerritoryId = "A1" };
        territory.Invaders.Add(invader);

        _sut.ExecuteActivate(MarchCard(), territory, state);

        Assert.Equal(2, invader.ShieldValue);
        Assert.Equal(2, invader.Hp); // healed 1, was at 1/3
    }

    [Fact]
    public void SettleGivesShield1ToAll()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        territory.Invaders.Add(MakeInvader("i1", UnitType.Marcher,  2, "A1"));
        territory.Invaders.Add(MakeInvader("i2", UnitType.Ironclad, 3, "A1"));

        _sut.ExecuteActivate(SettleCard(), territory, state);

        Assert.Equal(1, territory.Invaders[0].ShieldValue);
        Assert.Equal(1, territory.Invaders[1].ShieldValue);
    }

    // ── Invader → Native damage distribution ─────────────────────────────────

    [Fact]
    public void AutoMaximizeKillsLowestFirst()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        // 2 Marchers → 2 total damage
        territory.Invaders.Add(MakeInvader("i1", UnitType.Marcher, 2, "A1"));
        territory.Invaders.Add(MakeInvader("i2", UnitType.Marcher, 2, "A1"));

        var nativeLow  = new Native { Hp = 1, MaxHp = 2, Damage = 3, TerritoryId = "A1" };
        var nativeHigh = MakeNative(2, "A1");
        territory.Natives.Add(nativeLow);
        territory.Natives.Add(nativeHigh);

        _sut.ExecuteActivate(RavageCard(), territory, state);

        // 2 damage: kill nativeLow (1), then nativeHigh takes 1 → HP 1
        Assert.False(nativeLow.IsAlive);
        Assert.Equal(1, nativeHigh.Hp);
    }

    [Fact]
    public void ExactDamageToKillBeforeMovingOn()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        // 3 Marchers → 3 total damage
        for (int i = 0; i < 3; i++)
            territory.Invaders.Add(MakeInvader($"i{i}", UnitType.Marcher, 2, "A1"));

        var native1 = MakeNative(2, "A1"); // needs exactly 2 to kill
        var native2 = MakeNative(2, "A1");
        territory.Natives.Add(native1);
        territory.Natives.Add(native2);

        _sut.ExecuteActivate(RavageCard(), territory, state);

        // 3 damage: kill native1 (2), remaining 1 → native2 HP 1
        Assert.False(native1.IsAlive);
        Assert.Equal(1, native2.Hp);
    }

    [Fact]
    public void ExcessDamageNotWasted()
    {
        var state     = CreateState();
        var territory = state.GetTerritory("A1")!;
        // 3 Marchers → 3 total damage
        for (int i = 0; i < 3; i++)
            territory.Invaders.Add(MakeInvader($"i{i}", UnitType.Marcher, 2, "A1"));

        var native1 = new Native { Hp = 1, MaxHp = 2, Damage = 3, TerritoryId = "A1" };
        var native2 = MakeNative(2, "A1");
        territory.Natives.Add(native1);
        territory.Natives.Add(native2);

        _sut.ExecuteActivate(RavageCard(), territory, state);

        // 3 damage: kill native1 (1), remaining 2 → kill native2 (2)
        Assert.False(native1.IsAlive);
        Assert.False(native2.IsAlive);
    }

    // ── Native counter-attack ─────────────────────────────────────────────────

    [Fact]
    public void PoolDamageFromAllSurvivors()
    {
        var territory = new Territory { Id = "A1" };
        territory.Natives.Add(MakeNative(2, "A1"));
        territory.Natives.Add(MakeNative(2, "A1"));

        int pool = _sut.CalculateNativeDamagePool(territory);

        Assert.Equal(6, pool); // 2 × Damage(3)
    }

    [Fact]
    public void PlayerAssignmentRespected()
    {
        var territory = new Territory { Id = "A1" };
        var inv1 = MakeInvader("i1", UnitType.Marcher, 3, "A1");
        var inv2 = MakeInvader("i2", UnitType.Marcher, 3, "A1");
        territory.Invaders.Add(inv1);
        territory.Invaders.Add(inv2);

        _sut.ApplyCounterAttack(territory, new Dictionary<Invader, int>
        {
            [inv1] = 2,
            [inv2] = 1,
        });

        Assert.Equal(1, inv1.Hp); // 3 - 2
        Assert.Equal(2, inv2.Hp); // 3 - 1
    }

    [Fact]
    public void AutoAssignTargetsLowestFirst()
    {
        var territory = new Territory { Id = "A1" };
        territory.Natives.Add(MakeNative(2, "A1")); // pool = 3

        var invLow  = new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 1, MaxHp = 3, TerritoryId = "A1" };
        var invHigh = MakeInvader("i2", UnitType.Marcher, 3, "A1");
        territory.Invaders.Add(invLow);
        territory.Invaders.Add(invHigh);

        _sut.AutoAssignCounterAttack(territory);

        // Pool 3: kill invLow (1), remaining 2 → invHigh HP 3-2=1
        Assert.False(invLow.IsAlive);
        Assert.Equal(1, invHigh.Hp);
    }

    // ── Advance ───────────────────────────────────────────────────────────────

    [Fact]
    public void NormalMovementOneStep()
    {
        var state     = CreateState();
        var a1        = state.GetTerritory("A1")!;
        var invader   = MakeInvader("i1", UnitType.Marcher, 2, "A1");
        a1.Invaders.Add(invader);

        _sut.ExecuteAdvance(new ActionCard { AdvanceModifier = 1 }, state);

        Assert.Equal("M1", invader.TerritoryId);
        Assert.Contains(invader, state.GetTerritory("M1")!.Invaders);
        Assert.DoesNotContain(invader, a1.Invaders);
    }

    [Fact]
    public void MarchGivesExtraStep()
    {
        var state   = CreateState();
        var a1      = state.GetTerritory("A1")!;
        var invader = MakeInvader("i1", UnitType.Marcher, 2, "A1");
        a1.Invaders.Add(invader);

        _sut.ExecuteAdvance(MarchCard(), state); // AdvanceModifier = 2

        // A1 → M1 → I1
        Assert.Equal("I1", invader.TerritoryId);
    }

    [Fact]
    public void SettleHoldsPosition()
    {
        var state   = CreateState();
        var a1      = state.GetTerritory("A1")!;
        var invader = MakeInvader("i1", UnitType.Marcher, 2, "A1");
        a1.Invaders.Add(invader);

        _sut.ExecuteAdvance(SettleCard(), state); // AdvanceModifier = 0

        Assert.Equal("A1", invader.TerritoryId);
        Assert.Contains(invader, a1.Invaders);
    }

    [Fact]
    public void IroncladAlternatesMovement()
    {
        var state   = CreateState();
        var a1      = state.GetTerritory("A1")!;
        var invader = new Invader
        {
            Id               = "i1",
            UnitType         = UnitType.Ironclad,
            Hp               = 3, MaxHp = 3,
            TerritoryId      = "A1",
            AlternateMoveTurn = true, // should move this tide
        };
        a1.Invaders.Add(invader);

        // First advance: should move
        _sut.ExecuteAdvance(new ActionCard { AdvanceModifier = 1 }, state);
        Assert.Equal("M1", invader.TerritoryId);
        Assert.False(invader.AlternateMoveTurn); // toggled

        // Second advance: should hold
        _sut.ExecuteAdvance(new ActionCard { AdvanceModifier = 1 }, state);
        Assert.Equal("M1", invader.TerritoryId);
        Assert.True(invader.AlternateMoveTurn); // toggled back
    }

    [Fact]
    public void OutriderAlwaysExtraStep()
    {
        var state   = CreateState();
        var a1      = state.GetTerritory("A1")!;
        var invader = MakeInvader("i1", UnitType.Outrider, 2, "A1");
        a1.Invaders.Add(invader);

        // Normal action (AdvanceModifier=1) + Outrider +1 = 2 steps: A1 → M1 → I1
        _sut.ExecuteAdvance(new ActionCard { AdvanceModifier = 1 }, state);

        Assert.Equal("I1", invader.TerritoryId);
    }

    // ── Heart March ───────────────────────────────────────────────────────────

    [Fact]
    public void HeartMarchDealWeaveFromI1()
    {
        var state   = CreateState();
        var i1      = state.GetTerritory("I1")!;
        var invader = new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 3, MaxHp = 3, TerritoryId = "I1" };
        i1.Invaders.Add(invader);

        int weaveBefore = state.Weave!.CurrentWeave;

        // Advance records pre-advance I1 set; invader stays (Inner row not moved).
        _sut.ExecuteAdvance(new ActionCard { AdvanceModifier = 1 }, state);
        _sut.ExecuteHeartMarch(state);

        Assert.Equal(weaveBefore - 3, state.Weave.CurrentWeave);
    }

    [Fact]
    public void NewArrivalsAtI1DontMarchSameTide()
    {
        var state   = CreateState();
        var m1      = state.GetTerritory("M1")!;
        var invader = MakeInvader("i1", UnitType.Marcher, 2, "M1");
        m1.Invaders.Add(invader);

        int weaveBefore = state.Weave!.CurrentWeave;

        // Invader moves M1 → I1 during advance (not a pre-existing I1 resident).
        _sut.ExecuteAdvance(new ActionCard { AdvanceModifier = 1 }, state);
        _sut.ExecuteHeartMarch(state);

        Assert.Equal(weaveBefore, state.Weave.CurrentWeave); // no damage
    }

    [Fact]
    public void HeartDamageEqualsRemainingHp()
    {
        var state   = CreateState();
        var i1      = state.GetTerritory("I1")!;
        // Invader is at 1 HP but has MaxHp of 3 — damage should be current HP, not max.
        var invader = new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 1, MaxHp = 3, TerritoryId = "I1" };
        i1.Invaders.Add(invader);

        int weaveBefore = state.Weave!.CurrentWeave;

        _sut.ExecuteAdvance(new ActionCard { AdvanceModifier = 1 }, state);
        _sut.ExecuteHeartMarch(state);

        Assert.Equal(weaveBefore - 1, state.Weave.CurrentWeave);
    }
}
