namespace HollowWardens.Tests;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Turn;
using Xunit;

/// <summary>
/// D28: Presence amplification, vulnerability, and sacrifice.
/// </summary>
public class D28_PresenceValueTests : IDisposable
{
    // Shared helpers

    private static Territory MakeTerritory(string id, int corruption = 0, int presence = 0)
        => new() { Id = id, Row = TerritoryRow.Middle, CorruptionPoints = corruption, PresenceCount = presence };

    private static EncounterState MakeState(params Territory[] territories)
    {
        var state = new EncounterState();
        state.Territories.AddRange(territories);
        state.Presence = new PresenceSystem(() => state.Territories);
        state.Corruption = new CorruptionSystem();
        return state;
    }

    private static TargetInfo Target(string territoryId)
        => new() { TerritoryId = territoryId };

    private static Invader MakeInvader(int hp, int shield = 0)
        => new() { Hp = hp, ShieldValue = shield, UnitType = UnitType.Marcher };

    public void Dispose()
    {
        GameEvents.TerritoryDesecrated = null;
        GameEvents.PresenceSacrificed = null;
        VulnerabilityWiring.UnwireEvents();
    }

    // ═══ AMPLIFICATION ═══

    [Fact]
    public void Amplification_NoPresence_ReturnsBaseValue()
    {
        var state = MakeState(MakeTerritory("M1", presence: 0));
        var result = AmplificationHelper.GetAmplifiedValue(4, state, "M1");
        Assert.Equal(4, result);
    }

    [Fact]
    public void Amplification_OnePresence_AddOne()
    {
        var state = MakeState(MakeTerritory("M1", presence: 1));
        var result = AmplificationHelper.GetAmplifiedValue(4, state, "M1");
        Assert.Equal(5, result);
    }

    [Fact]
    public void Amplification_ThreePresence_AddsThree()
    {
        var state = MakeState(MakeTerritory("M1", presence: 3));
        var result = AmplificationHelper.GetAmplifiedValue(2, state, "M1");
        Assert.Equal(5, result);
    }

    [Fact]
    public void Amplification_NullTerritory_ReturnsBaseValue()
    {
        var state = MakeState();
        var result = AmplificationHelper.GetAmplifiedValue(4, state, "NONEXISTENT");
        Assert.Equal(4, result);
    }

    [Fact]
    public void DamageInvaders_AmplifiedByPresence()
    {
        var territory = MakeTerritory("M1", presence: 2);
        var marcher = MakeInvader(hp: 4);
        territory.Invaders.Add(marcher);
        var state = MakeState(territory);

        var effect = new DamageInvadersEffect(new EffectData { Type = EffectType.DamageInvaders, Value = 2 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(0, marcher.Hp); // 2 + 2 presence = 4
    }

    [Fact]
    public void DamageInvaders_NoPresence_BaseValueOnly()
    {
        var territory = MakeTerritory("M1", presence: 0);
        var marcher = MakeInvader(hp: 4);
        territory.Invaders.Add(marcher);
        var state = MakeState(territory);

        var effect = new DamageInvadersEffect(new EffectData { Type = EffectType.DamageInvaders, Value = 2 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(2, marcher.Hp);
    }

    [Fact]
    public void ReduceCorruption_AmplifiedByPresence()
    {
        var territory = MakeTerritory("M1", corruption: 5, presence: 1);
        var state = MakeState(territory);

        var effect = new ReduceCorruptionEffect(new EffectData { Type = EffectType.ReduceCorruption, Value = 2 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(2, territory.CorruptionPoints); // 5 - (2+1) = 2
    }

    [Fact]
    public void ReduceCorruption_HighPresence_ClampsToZero()
    {
        var territory = MakeTerritory("M1", corruption: 3, presence: 5);
        var state = MakeState(territory);

        var effect = new ReduceCorruptionEffect(new EffectData { Type = EffectType.ReduceCorruption, Value = 2 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(0, territory.CorruptionPoints);
    }

    // ═══ VULNERABILITY ═══

    [Fact]
    public void Vulnerability_Level2_BlocksPresencePlacement()
    {
        var territory = MakeTerritory("M1", corruption: 8, presence: 0);
        var state = MakeState(territory);

        var effect = new PlacePresenceEffect(new EffectData { Type = EffectType.PlacePresence, Value = 1 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(0, territory.PresenceCount);
    }

    [Fact]
    public void Vulnerability_Level1_AllowsPresencePlacement()
    {
        var territory = MakeTerritory("M1", corruption: 5, presence: 0);
        var state = MakeState(territory);

        var effect = new PlacePresenceEffect(new EffectData { Type = EffectType.PlacePresence, Value = 1 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(1, territory.PresenceCount);
    }

    [Fact]
    public void Vulnerability_Level3_BlocksPresencePlacement()
    {
        var territory = MakeTerritory("M1", corruption: 15, presence: 0);
        var state = MakeState(territory);

        var effect = new PlacePresenceEffect(new EffectData { Type = EffectType.PlacePresence, Value = 1 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(0, territory.PresenceCount);
    }

    [Fact]
    public void Vulnerability_Level3_DestroysAllPresence_OnCrossing()
    {
        var territory = MakeTerritory("M1", corruption: 14, presence: 3);
        var state = MakeState(territory);
        VulnerabilityWiring.WireEvents(state.Presence!);

        state.Corruption!.AddCorruption(territory, 1);

        Assert.Equal(15, territory.CorruptionPoints);
        Assert.Equal(3, territory.CorruptionLevel);
        Assert.Equal(0, territory.PresenceCount);
    }

    [Fact]
    public void Vulnerability_Level3_DoesNotFire_WhenAlreadyDesecrated()
    {
        var territory = MakeTerritory("M1", corruption: 15, presence: 0);
        var state = MakeState(territory);
        int fireCount = 0;
        GameEvents.TerritoryDesecrated += _ => fireCount++;

        state.Corruption!.AddCorruption(territory, 5);

        Assert.Equal(0, fireCount);
    }

    [Fact]
    public void Vulnerability_Level1ToLevel3_DestroysPresence()
    {
        var territory = MakeTerritory("M1", corruption: 5, presence: 2);
        var state = MakeState(territory);
        VulnerabilityWiring.WireEvents(state.Presence!);

        state.Corruption!.AddCorruption(territory, 15);

        Assert.Equal(3, territory.CorruptionLevel);
        Assert.Equal(0, territory.PresenceCount);
    }

    // ═══ SACRIFICE ═══

    [Fact]
    public void Sacrifice_RemovesOnePresence_CleanseThreeCorruption()
    {
        var territory = MakeTerritory("M1", corruption: 7, presence: 2);
        var state = MakeState(territory);
        var actions = new TurnActions(state, new EffectResolver());

        var result = actions.SacrificePresence("M1");

        Assert.True(result);
        Assert.Equal(1, territory.PresenceCount);
        Assert.Equal(4, territory.CorruptionPoints);
    }

    [Fact]
    public void Sacrifice_FailsOnEmptyTerritory()
    {
        var territory = MakeTerritory("M1", corruption: 7, presence: 0);
        var state = MakeState(territory);
        var actions = new TurnActions(state, new EffectResolver());

        var result = actions.SacrificePresence("M1");

        Assert.False(result);
        Assert.Equal(7, territory.CorruptionPoints);
    }

    [Fact]
    public void Sacrifice_AllowedDuringVigil()
    {
        var territory = MakeTerritory("M1", corruption: 6, presence: 1);
        var state = MakeState(territory);
        var tm = new TurnManager(state, new EffectResolver());
        SetPhase(tm, TurnPhase.Vigil);

        Assert.True(tm.CanSacrifice());
        Assert.True(tm.SacrificePresence("M1"));
    }

    [Fact]
    public void Sacrifice_AllowedDuringDusk()
    {
        var territory = MakeTerritory("M1", corruption: 6, presence: 1);
        var state = MakeState(territory);
        var tm = new TurnManager(state, new EffectResolver());
        SetPhase(tm, TurnPhase.Dusk);

        Assert.True(tm.CanSacrifice());
        Assert.True(tm.SacrificePresence("M1"));
    }

    [Fact]
    public void Sacrifice_BlockedDuringTide()
    {
        var territory = MakeTerritory("M1", corruption: 6, presence: 1);
        var state = MakeState(territory);
        var tm = new TurnManager(state, new EffectResolver());
        SetPhase(tm, TurnPhase.Tide);

        Assert.False(tm.CanSacrifice());
        Assert.False(tm.SacrificePresence("M1"));
        Assert.Equal(1, territory.PresenceCount);
    }

    [Fact]
    public void Sacrifice_BlockedDuringRest()
    {
        var territory = MakeTerritory("M1", corruption: 6, presence: 1);
        var state = MakeState(territory);
        var tm = new TurnManager(state, new EffectResolver());
        SetPhase(tm, TurnPhase.Rest);

        Assert.False(tm.CanSacrifice());
    }

    [Fact]
    public void Sacrifice_DoesNotConsumePlaySlot()
    {
        var territory = MakeTerritory("M1", corruption: 6, presence: 3);
        var state = MakeState(territory);
        var tm = new TurnManager(state, new EffectResolver());
        SetPhase(tm, TurnPhase.Vigil);

        tm.SacrificePresence("M1");
        tm.SacrificePresence("M1");

        Assert.Equal(0, tm.VigilPlaysThisTurn);
        Assert.True(tm.CanPlayTop());
    }

    [Fact]
    public void Sacrifice_CorruptionClampsToZero()
    {
        var territory = MakeTerritory("M1", corruption: 2, presence: 1);
        var state = MakeState(territory);
        var actions = new TurnActions(state, new EffectResolver());

        actions.SacrificePresence("M1");

        Assert.Equal(0, territory.CorruptionPoints);
    }

    [Fact]
    public void Sacrifice_FiresEvent()
    {
        var territory = MakeTerritory("M1", corruption: 6, presence: 2);
        var state = MakeState(territory);
        var actions = new TurnActions(state, new EffectResolver());

        Territory? firedTerritory = null;
        int firedCount = 0;
        GameEvents.PresenceSacrificed += (t, c) => { firedTerritory = t; firedCount = c; };

        actions.SacrificePresence("M1");

        Assert.Equal(territory, firedTerritory);
        Assert.Equal(1, firedCount);
    }

    // ═══ PRESENCE CAP ═══

    [Fact]
    public void PlacePresence_CapsAtMaxPerTerritory()
    {
        var territory = MakeTerritory("M1", presence: 0);
        var state = MakeState(territory);

        state.Presence!.PlacePresence(territory, 5);

        Assert.Equal(PresenceSystem.MaxPresencePerTerritory, territory.PresenceCount);
    }

    [Fact]
    public void PlacePresence_AtMax_AddsNothing()
    {
        var territory = MakeTerritory("M1", presence: PresenceSystem.MaxPresencePerTerritory);
        var state = MakeState(territory);

        state.Presence!.PlacePresence(territory, 1);

        Assert.Equal(PresenceSystem.MaxPresencePerTerritory, territory.PresenceCount);
    }

    [Fact]
    public void PlacePresence_BelowMax_AddsNormally()
    {
        var territory = MakeTerritory("M1", presence: 1);
        var state = MakeState(territory);

        state.Presence!.PlacePresence(territory, 1);

        Assert.Equal(2, territory.PresenceCount);
    }

    [Fact]
    public void PlacePresence_BulkAdd_ClampsToMax()
    {
        var territory = MakeTerritory("M1", presence: 2);
        var state = MakeState(territory);

        state.Presence!.PlacePresence(territory, 3);

        Assert.Equal(PresenceSystem.MaxPresencePerTerritory, territory.PresenceCount);
    }

    // ═══ SHIELD/BOOST NATIVES AMPLIFICATION ═══

    [Fact]
    public void ShieldNatives_AmplifiedByPresence()
    {
        // ShieldNatives value=2, 1 presence → natives get shield 3
        var territory = MakeTerritory("M1", presence: 1);
        var native = new Native { Hp = 3, MaxHp = 3, Damage = 2, TerritoryId = "M1" };
        territory.Natives.Add(native);
        var state = MakeState(territory);

        var effect = new ShieldNativesEffect(new EffectData { Type = EffectType.ShieldNatives, Value = 2 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(3, native.ShieldValue); // 2 + 1 presence
    }

    [Fact]
    public void ShieldNatives_NoPresence_BaseValueOnly()
    {
        // ShieldNatives value=2, 0 presence → natives get shield 2
        var territory = MakeTerritory("M1", presence: 0);
        var native = new Native { Hp = 3, MaxHp = 3, Damage = 2, TerritoryId = "M1" };
        territory.Natives.Add(native);
        var state = MakeState(territory);

        var effect = new ShieldNativesEffect(new EffectData { Type = EffectType.ShieldNatives, Value = 2 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(2, native.ShieldValue);
    }

    [Fact]
    public void BoostNatives_AmplifiedByPresence()
    {
        // BoostNatives value=2, 2 presence → natives get boost 4 (2+2)
        var territory = MakeTerritory("M1", presence: 2);
        var native = new Native { Hp = 3, MaxHp = 3, Damage = 1, TerritoryId = "M1" };
        territory.Natives.Add(native);
        var state = MakeState(territory);

        var effect = new BoostNativesEffect(new EffectData { Type = EffectType.BoostNatives, Value = 2 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(5, native.Damage); // 1 base + 4 boost (2 + 2 presence)
    }

    [Fact]
    public void BoostNatives_NoPresence_BaseValueOnly()
    {
        // BoostNatives value=2, 0 presence → natives get boost 2
        var territory = MakeTerritory("M1", presence: 0);
        var native = new Native { Hp = 3, MaxHp = 3, Damage = 1, TerritoryId = "M1" };
        territory.Natives.Add(native);
        var state = MakeState(territory);

        var effect = new BoostNativesEffect(new EffectData { Type = EffectType.BoostNatives, Value = 2 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(3, native.Damage); // 1 base + 2 boost
    }

    // ═══ COMBINED SCENARIOS ═══

    [Fact]
    public void Sacrifice_ThenDamage_AmplifiedByRemainingPresence()
    {
        var territory = MakeTerritory("M1", corruption: 6, presence: 3);
        var marcher = MakeInvader(hp: 5);
        territory.Invaders.Add(marcher);
        var state = MakeState(territory);
        var actions = new TurnActions(state, new EffectResolver());

        actions.SacrificePresence("M1"); // now 2 presence

        var effect = new DamageInvadersEffect(new EffectData { Type = EffectType.DamageInvaders, Value = 2 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(1, marcher.Hp); // 5 - (2 + 2) = 1
    }

    [Fact]
    public void Desecration_ThenPlacement_Blocked()
    {
        var territory = MakeTerritory("M1", corruption: 14, presence: 2);
        var state = MakeState(territory);
        VulnerabilityWiring.WireEvents(state.Presence!);

        state.Corruption!.AddCorruption(territory, 1); // → 15, Level 3
        Assert.Equal(0, territory.PresenceCount);

        var effect = new PlacePresenceEffect(new EffectData { Type = EffectType.PlacePresence, Value = 1 });
        effect.Resolve(state, Target("M1"));
        Assert.Equal(0, territory.PresenceCount); // still blocked
    }

    [Fact]
    public void Amplification_Spec_Example_ReduceCorruption2_With1Presence_Equals3()
    {
        var territory = MakeTerritory("M1", corruption: 5, presence: 1);
        var state = MakeState(territory);

        var effect = new ReduceCorruptionEffect(new EffectData { Type = EffectType.ReduceCorruption, Value = 2 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(2, territory.CorruptionPoints); // 5 - (2+1) = 2
    }

    [Fact]
    public void Amplification_Spec_Example_DamageInvaders4_With2Presence_Equals6()
    {
        var territory = MakeTerritory("M1", presence: 2);
        var ironclad = MakeInvader(hp: 6);
        territory.Invaders.Add(ironclad);
        var state = MakeState(territory);

        var effect = new DamageInvadersEffect(new EffectData { Type = EffectType.DamageInvaders, Value = 4 });
        effect.Resolve(state, Target("M1"));

        Assert.Equal(0, ironclad.Hp); // 6 - (4+2) = 0
    }

    // ═══ HELPERS ═══

    /// <summary>
    /// Force-set TurnManager.CurrentPhase via reflection (avoids needing full
    /// DeckManager setup for sacrifice phase-gating tests).
    /// </summary>
    private static void SetPhase(TurnManager tm, TurnPhase phase)
    {
        var field = typeof(TurnManager).GetField("<CurrentPhase>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(tm, phase);
    }
}
