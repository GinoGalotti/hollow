namespace HollowWardens.Tests.Terrain;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using Xunit;

/// <summary>Tests for Task 4: terrain types, effects, transitions, and terrain preset loading.</summary>
public class TerrainTests
{
    private static Territory MakeTerr(TerrainType terrain, int corruption = 0, int presence = 0) => new Territory
    {
        Id = "T1",
        Terrain = terrain,
        CorruptionPoints = corruption,
        PresenceCount = presence
    };

    // ── TerrainEffects static modifiers ──────────────────────────────────────

    [Fact]
    public void Plains_HasNoModifiers()
    {
        Assert.Equal(0, TerrainEffects.GetDamageModifier(TerrainType.Plains));
        Assert.Equal(0, TerrainEffects.GetFearModifier(TerrainType.Plains));
        Assert.Equal(0, TerrainEffects.GetCorruptionThresholdModifier(TerrainType.Plains));
        Assert.Equal(0, TerrainEffects.GetInvaderEntryDamage(TerrainType.Plains));
        Assert.Equal(int.MaxValue, TerrainEffects.GetCorruptionMaxLevel(TerrainType.Plains));
        Assert.Equal(0, TerrainEffects.GetInvaderRavageCorruptionModifier(TerrainType.Plains));
        Assert.Equal(0, TerrainEffects.GetInvaderRestHeal(TerrainType.Plains));
        Assert.Equal(0, TerrainEffects.GetInvaderCounterAttackModifier(TerrainType.Plains));
        Assert.True(TerrainEffects.CanSpawnNatives(TerrainType.Plains));
    }

    [Fact]
    public void Forest_HasDamageBonus_AndRavageCorruption()
    {
        Assert.Equal(1, TerrainEffects.GetDamageModifier(TerrainType.Forest));
        Assert.Equal(1, TerrainEffects.GetInvaderRavageCorruptionModifier(TerrainType.Forest));
        Assert.Equal(0, TerrainEffects.GetFearModifier(TerrainType.Forest));
    }

    [Fact]
    public void Mountain_HasFearBonus_AndCounterAttackBonus()
    {
        Assert.Equal(2, TerrainEffects.GetFearModifier(TerrainType.Mountain));
        Assert.Equal(1, TerrainEffects.GetInvaderCounterAttackModifier(TerrainType.Mountain));
        Assert.Equal(0, TerrainEffects.GetDamageModifier(TerrainType.Mountain));
    }

    [Fact]
    public void Wetland_HasCorruptionThreshold_AndRestHeal()
    {
        Assert.Equal(2, TerrainEffects.GetCorruptionThresholdModifier(TerrainType.Wetland));
        Assert.Equal(1, TerrainEffects.GetInvaderRestHeal(TerrainType.Wetland));
    }

    [Fact]
    public void Sacred_CapsCorruptionAtL1()
    {
        Assert.Equal(1, TerrainEffects.GetCorruptionMaxLevel(TerrainType.Sacred));
    }

    [Fact]
    public void Scorched_HasEntryDamage_AndNoNativeSpawn()
    {
        Assert.Equal(2, TerrainEffects.GetInvaderEntryDamage(TerrainType.Scorched));
        Assert.False(TerrainEffects.CanSpawnNatives(TerrainType.Scorched));
    }

    [Fact]
    public void Blighted_HasNegativeEffectModifier_AndAutoCorruption()
    {
        Assert.Equal(-1, TerrainEffects.GetEffectValueModifier(TerrainType.Blighted));
        Assert.Equal(1, TerrainEffects.GetAutoCorruptionPerTide(TerrainType.Blighted));
    }

    // ── TerrainTransitions ────────────────────────────────────────────────────

    [Fact]
    public void Forest_TransitionsToScorched_AtCorruptionL2()
    {
        var t = MakeTerr(TerrainType.Forest, corruption: 8); // L2 threshold
        bool changed = TerrainTransitions.CheckTransitions(t);

        Assert.True(changed);
        Assert.Equal(TerrainType.Scorched, t.Terrain);
    }

    [Fact]
    public void Forest_StaysForest_BelowL2()
    {
        var t = MakeTerr(TerrainType.Forest, corruption: 3); // L1
        TerrainTransitions.CheckTransitions(t);

        Assert.Equal(TerrainType.Forest, t.Terrain);
    }

    [Fact]
    public void Mountain_TransitionsToRuins_AtCorruptionL3()
    {
        var t = MakeTerr(TerrainType.Mountain, corruption: 15); // L3
        TerrainTransitions.CheckTransitions(t);

        Assert.Equal(TerrainType.Ruins, t.Terrain);
    }

    [Fact]
    public void Blighted_TransitionsToPains_WhenCorruptionCleansed()
    {
        var t = MakeTerr(TerrainType.Blighted, corruption: 0);
        TerrainTransitions.CheckTransitions(t);

        Assert.Equal(TerrainType.Plains, t.Terrain);
    }

    [Fact]
    public void Scorched_TransitionsToPlains_After3CleanTides()
    {
        var t = MakeTerr(TerrainType.Scorched, corruption: 0);

        TerrainTransitions.CheckTransitions(t);
        Assert.Equal(TerrainType.Scorched, t.Terrain); // still scorched after 1
        Assert.Equal(1, t.TerrainTimer);

        TerrainTransitions.CheckTransitions(t);
        Assert.Equal(TerrainType.Scorched, t.Terrain); // still scorched after 2
        Assert.Equal(2, t.TerrainTimer);

        TerrainTransitions.CheckTransitions(t);
        Assert.Equal(TerrainType.Plains, t.Terrain);   // transitions after 3
        Assert.Equal(0, t.TerrainTimer);
    }

    [Fact]
    public void Scorched_TimerResetsIfCorruptionReturns()
    {
        var t = MakeTerr(TerrainType.Scorched, corruption: 0);
        TerrainTransitions.CheckTransitions(t);
        Assert.Equal(1, t.TerrainTimer);

        t.CorruptionPoints = 3; // corruption returns
        TerrainTransitions.CheckTransitions(t);
        Assert.Equal(0, t.TerrainTimer); // timer reset
    }

    [Fact]
    public void Fertile_TransitionsToPlains_When3PlusInvaders()
    {
        var t = MakeTerr(TerrainType.Fertile);
        for (int i = 0; i < 3; i++)
            t.Invaders.Add(new Invader { Hp = 3, MaxHp = 3 });

        TerrainTransitions.CheckTransitions(t);

        Assert.Equal(TerrainType.Plains, t.Terrain);
    }

    [Fact]
    public void Sacred_TransitionsToBlighted_OnInvaderSettle()
    {
        var t = MakeTerr(TerrainType.Sacred);
        TerrainTransitions.OnInvaderSettle(t);

        Assert.Equal(TerrainType.Blighted, t.Terrain);
    }

    [Fact]
    public void AnyTerrain_TransitionsToBlighted_AtL3Corruption()
    {
        var t = MakeTerr(TerrainType.Wetland, corruption: 15); // L3
        TerrainTransitions.CheckTransitions(t);

        Assert.Equal(TerrainType.Blighted, t.Terrain);
    }

    // ── Sacred terrain caps corruption ────────────────────────────────────────

    [Fact]
    public void Sacred_AddCorruption_CannotExceedL1()
    {
        var corruption = new CorruptionSystem();
        var t = new Territory { Id = "T1", Terrain = TerrainType.Sacred };

        corruption.AddCorruption(t, 10); // would be L2 normally

        Assert.True(t.CorruptionLevel <= 1, $"Expected L1 or less but got L{t.CorruptionLevel}");
    }

    // ── Wetland rest heal ─────────────────────────────────────────────────────

    [Fact]
    public void Wetland_AppliesRestHealToInvaders()
    {
        var t = MakeTerr(TerrainType.Wetland);
        t.Invaders.Add(new Invader { Hp = 2, MaxHp = 3 });

        TerrainTransitions.ApplyWetlandRestHeal(t);

        Assert.Equal(3, t.Invaders[0].Hp); // healed 1
    }

    // ── Territory defaults ────────────────────────────────────────────────────

    [Fact]
    public void Territory_DefaultsToPlainsAndZeroTimer()
    {
        var t = new Territory();
        Assert.Equal(TerrainType.Plains, t.Terrain);
        Assert.Equal(0, t.TerrainTimer);
    }

    // ── EncounterConfig terrain fields ────────────────────────────────────────

    [Fact]
    public void EncounterConfig_HasTerrainPresetAndOverrides()
    {
        var config = new EncounterConfig();
        Assert.Equal("all_plains", config.TerrainPreset);
        Assert.Null(config.TerrainOverrides);

        config.TerrainPreset = "standard_mixed";
        config.TerrainOverrides = new Dictionary<string, string> { ["I1"] = "sacred" };

        Assert.Equal("standard_mixed", config.TerrainPreset);
        Assert.Equal("sacred", config.TerrainOverrides["I1"]);
    }
}
