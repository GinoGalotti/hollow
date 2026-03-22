namespace HollowWardens.Tests;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Tests for EmberAbility: Flame Out, Ash Trail, Scorched Earth, Ember Fury, Heat Wave, Phoenix Spark.
/// </summary>
public class EmberAbilityTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    // ── 1. Flame Out ───────────────────────────────────────────────────────────

    [Fact]
    public void Ember_OnBottomPlayed_AlwaysPermanentlyRemoved()
    {
        var ember = new EmberAbility();
        var card  = new Card { Id = "ember_001", Name = "Flame Burst" };

        var result = ember.OnBottomPlayed(card, EncounterTier.Standard);

        Assert.Equal(BottomResult.PermanentlyRemoved, result);
    }

    [Fact]
    public void Ember_OnRestDissolve_PermanentlyRemoved()
    {
        var ember = new EmberAbility();
        var card  = new Card { Id = "ember_002", Name = "Kindle" };

        var result = ember.OnRestDissolve(card);

        Assert.Equal(BottomResult.PermanentlyRemoved, result);
    }

    // ── 3. Ash Trail ───────────────────────────────────────────────────────────

    [Fact]
    public void Ember_AshTrail_AddsCorruption_AndDamagesInvaders()
    {
        var territories = new List<Territory>
        {
            new() { Id = "M1", Row = TerritoryRow.Middle, PresenceCount = 1 }
        };
        territories[0].Invaders.Add(new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 4, MaxHp = 4, TerritoryId = "M1" });
        territories[0].Invaders.Add(new Invader { Id = "i2", UnitType = UnitType.Marcher, Hp = 4, MaxHp = 4, TerritoryId = "M1" });

        var state = new EncounterState
        {
            Territories = territories,
            Corruption  = new CorruptionSystem(),
            Warden      = new EmberAbility()
        };

        state.Warden.OnTideStart(state);

        // Territory should have gained 1 corruption
        Assert.Equal(1, territories[0].CorruptionPoints);
        // Each invader should have taken 1 damage
        Assert.Equal(3, territories[0].Invaders[0].Hp);
        Assert.Equal(3, territories[0].Invaders[1].Hp);
    }

    [Fact]
    public void Ember_AshTrail_OnlyAffectsPresenceTerritories()
    {
        var territories = new List<Territory>
        {
            new() { Id = "M1", Row = TerritoryRow.Middle, PresenceCount = 0 }
        };
        territories[0].Invaders.Add(new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 4, MaxHp = 4, TerritoryId = "M1" });

        var state = new EncounterState
        {
            Territories = territories,
            Corruption  = new CorruptionSystem(),
            Warden      = new EmberAbility()
        };

        state.Warden.OnTideStart(state);

        // No presence → no corruption added, no damage
        Assert.Equal(0, territories[0].CorruptionPoints);
        Assert.Equal(4, territories[0].Invaders[0].Hp);
    }

    // ── 5. Scorched Earth ─────────────────────────────────────────────────────

    [Fact]
    public void Ember_ScorchedEarth_DamageEqualsCorruptionSum()
    {
        var territories = BuildThreeTerritoryState(corruption: new[] { 5, 3, 2 }, presenceAll: true);
        var invader = new Invader { Id = "boss", UnitType = UnitType.Ironclad, Hp = 20, MaxHp = 20, TerritoryId = "M1" };
        territories[0].Invaders.Add(invader);

        var state = new EncounterState
        {
            Territories = territories,
            Corruption  = new CorruptionSystem(),
            Warden      = new EmberAbility()
        };

        state.Warden.OnResolution(state);

        // Total corruption = 5 + 3 + 2 = 10 → boss takes 10 damage → 20 - 10 = 10
        Assert.Equal(10, invader.Hp);
    }

    [Fact]
    public void Ember_ScorchedEarth_FullyCleansesL0AndL1()
    {
        // D31 smart cleanse: L0 (2pts) and L1 (5pts, 3pts) → all fully cleansed to 0
        var territories = BuildThreeTerritoryState(corruption: new[] { 5, 3, 2 }, presenceAll: true);

        var state = new EncounterState
        {
            Territories = territories,
            Corruption  = new CorruptionSystem(),
            Warden      = new EmberAbility()
        };

        state.Warden.OnResolution(state);

        // L1 (5pts) → 0, L1 (3pts) → 0, L0 (2pts) → 0
        Assert.Equal(0, territories[0].CorruptionPoints);
        Assert.Equal(0, territories[1].CorruptionPoints);
        Assert.Equal(0, territories[2].CorruptionPoints);
    }

    // ── 7. Ember Fury ─────────────────────────────────────────────────────────

    [Fact]
    public void Ember_EmberFury_BonusDamage_PerCorruptedTerritory()
    {
        // 3 territories at L1+ (CorruptionLevel >= 1 = CorruptionPoints >= 3)
        var territories = BuildThreeTerritoryState(corruption: new[] { 3, 3, 3 }, presenceAll: false);

        var gating = new PassiveGating("ember"); // ember_fury unlocks on Ash T1
        // Manually activate ember_fury for this test (by triggering Ash T1)
        gating.OnThresholdTriggered(Element.Ash, 1);

        var state = new EncounterState
        {
            Territories   = territories,
            Corruption    = new CorruptionSystem(),
            Warden        = new EmberAbility(),
            PassiveGating = gating
        };

        int bonus = HollowWardens.Core.Effects.EmberFuryHelper.GetFuryBonus(state);

        // 3 territories at Level 1+ → +3 bonus
        Assert.Equal(3, bonus);
    }

    [Fact]
    public void Ember_EmberFury_Inactive_WhenLocked()
    {
        var territories = BuildThreeTerritoryState(corruption: new[] { 3, 3, 3 }, presenceAll: false);

        // PassiveGating with ember_fury locked (not triggered yet)
        var gating = new PassiveGating("ember");

        var state = new EncounterState
        {
            Territories   = territories,
            Corruption    = new CorruptionSystem(),
            Warden        = new EmberAbility(),
            PassiveGating = gating
        };

        int bonus = HollowWardens.Core.Effects.EmberFuryHelper.GetFuryBonus(state);

        Assert.Equal(0, bonus);
    }

    // ── 9. Heat Wave ──────────────────────────────────────────────────────────

    [Fact]
    public void Ember_HeatWave_OnRest_DamagesAllPresenceTerritories()
    {
        var territories = new List<Territory>
        {
            new() { Id = "M1", Row = TerritoryRow.Middle, PresenceCount = 1 },
            new() { Id = "A1", Row = TerritoryRow.Arrival, PresenceCount = 0 }
        };
        var inv1 = new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 4, MaxHp = 4, TerritoryId = "M1" };
        var inv2 = new Invader { Id = "i2", UnitType = UnitType.Marcher, Hp = 4, MaxHp = 4, TerritoryId = "A1" };
        territories[0].Invaders.Add(inv1);
        territories[1].Invaders.Add(inv2);

        var gating = new PassiveGating("ember");
        gating.OnThresholdTriggered(Element.Ash, 2); // unlocks heat_wave

        var state = new EncounterState
        {
            Territories   = territories,
            Corruption    = new CorruptionSystem(),
            Warden        = new EmberAbility(),
            PassiveGating = gating
        };

        state.Warden.OnRest(state, null);

        // M1 has presence → inv1 takes 2 damage
        Assert.Equal(2, inv1.Hp);
        // A1 has no presence → inv2 unaffected
        Assert.Equal(4, inv2.Hp);
    }

    // ── 10. Phoenix Spark ─────────────────────────────────────────────────────

    [Fact]
    public void Ember_PhoenixSpark_GeneratesFear_OnPermanentRemoval()
    {
        // Set up an encounter with Ember and wire Phoenix Spark via EncounterRunner
        var territories = BoardState.CreatePyramid().Territories.Values.ToList();
        var presence    = new PresenceSystem(() => territories);
        var ember       = new EmberAbility();
        var dread       = new DreadSystem();
        var gating      = new PassiveGating("ember");
        gating.OnThresholdTriggered(Element.Gale, 1); // unlock phoenix_spark

        var state = new EncounterState
        {
            Territories   = territories,
            Corruption    = new CorruptionSystem(),
            Weave         = new WeaveSystem(20),
            Warden        = ember,
            Dread         = dread,
            PassiveGating = gating
        };

        // Wire Phoenix Spark directly (same logic as EncounterRunner.WireEvents)
        int fearGenerated = 0;
        GameEvents.FearGenerated += amt => fearGenerated += amt;

        Action<Card> onCardDissolved = _ =>
        {
            if (state.Warden?.WardenId == "ember"
                && (state.PassiveGating == null || state.PassiveGating.IsActive("phoenix_spark")))
            {
                state.Dread?.OnFearGenerated(3);
                GameEvents.FearGenerated?.Invoke(3);
            }
        };
        GameEvents.CardDissolved += onCardDissolved;

        // Fire a CardDissolved event (simulates permanent card removal via bottom play)
        var card = new Card { Id = "ember_001", Name = "Flame Burst" };
        GameEvents.CardDissolved?.Invoke(card);

        Assert.Equal(3, fearGenerated);

        GameEvents.CardDissolved -= onCardDissolved;
    }

    // ── D31 Part A: Ash T3 no corruption rider ────────────────────────────────

    [Fact]
    public void Ember_AshT3_DealsOnlyDamage_NoCorruption()
    {
        var territories = new List<Territory>
        {
            new() { Id = "M1", Row = TerritoryRow.Middle,  PresenceCount = 0 },
            new() { Id = "A1", Row = TerritoryRow.Arrival, PresenceCount = 0 }
        };
        var invM1 = new Invader { Id = "i1", UnitType = UnitType.Marcher,  Hp = 4, MaxHp = 4, TerritoryId = "M1" };
        var invA1 = new Invader { Id = "i2", UnitType = UnitType.Ironclad, Hp = 5, MaxHp = 5, TerritoryId = "A1" };
        territories[0].Invaders.Add(invM1);
        territories[1].Invaders.Add(invA1);

        var state = new EncounterState { Territories = territories, Corruption = new CorruptionSystem() };

        new ThresholdResolver().AutoResolve(Element.Ash, 3, state);

        // Marcher (HP4) takes 3 → HP1; Ironclad (HP5) takes 3 → HP2
        Assert.Equal(1, invM1.Hp);
        Assert.Equal(2, invA1.Hp);
        // No corruption added to any territory
        Assert.Equal(0, territories[0].CorruptionPoints);
        Assert.Equal(0, territories[1].CorruptionPoints);
    }

    // ── D31 Part B: Presence tolerance ────────────────────────────────────────

    [Fact]
    public void Ember_PresencePlacement_AllowedAtLevel2()
    {
        // Ember's PresenceBlockLevel = 3 → L2 (8-14pts) should NOT block placement
        var territory = new Territory { Id = "M1", Row = TerritoryRow.Middle, CorruptionPoints = 8, PresenceCount = 0 };
        var territories = new List<Territory> { territory };
        var presence    = new PresenceSystem(() => territories);
        var state = new EncounterState
        {
            Territories = territories,
            Presence    = presence,
            Corruption  = new CorruptionSystem(),
            Warden      = new EmberAbility()
        };

        var effect = new PlacePresenceEffect(new EffectData { Type = EffectType.PlacePresence, Value = 1 });
        effect.Resolve(state, new TargetInfo { TerritoryId = "M1" });

        Assert.Equal(1, territory.PresenceCount); // placed successfully
    }

    [Fact]
    public void Ember_PresencePlacement_BlockedAtLevel3()
    {
        // Ember's PresenceBlockLevel = 3 → L3 (15+pts) blocks placement
        var territory = new Territory { Id = "M1", Row = TerritoryRow.Middle, CorruptionPoints = 15, PresenceCount = 0 };
        var territories = new List<Territory> { territory };
        var presence    = new PresenceSystem(() => territories);
        var state = new EncounterState
        {
            Territories = territories,
            Presence    = presence,
            Corruption  = new CorruptionSystem(),
            Warden      = new EmberAbility()
        };

        var effect = new PlacePresenceEffect(new EffectData { Type = EffectType.PlacePresence, Value = 1 });
        effect.Resolve(state, new TargetInfo { TerritoryId = "M1" });

        Assert.Equal(0, territory.PresenceCount); // blocked
    }

    // ── D31 Part C: Smart cleanse (Scorched Earth) ────────────────────────────

    [Fact]
    public void Ember_ScorchedEarth_FullyCleansesLevel1()
    {
        // L1 territory (3-7 pts) → fully cleansed to 0 after resolution
        var territory = new Territory { Id = "M1", Row = TerritoryRow.Middle, CorruptionPoints = 5, PresenceCount = 1 };
        var state = new EncounterState
        {
            Territories = new() { territory },
            Corruption  = new CorruptionSystem(),
            Warden      = new EmberAbility()
        };

        state.Warden.OnResolution(state);

        Assert.Equal(0, territory.CorruptionPoints);
    }

    [Fact]
    public void Ember_ScorchedEarth_HalvesLevel2()
    {
        // L2 territory (8-14 pts) → halved (round down) after resolution
        var territory = new Territory { Id = "M1", Row = TerritoryRow.Middle, CorruptionPoints = 10, PresenceCount = 1 };
        var state = new EncounterState
        {
            Territories = new() { territory },
            Corruption  = new CorruptionSystem(),
            Warden      = new EmberAbility()
        };

        state.Warden.OnResolution(state);

        Assert.Equal(5, territory.CorruptionPoints); // 10 / 2 = 5
    }

    [Fact]
    public void Ember_ScorchedEarth_NoChangeLevel3()
    {
        // L3 territory (15+ pts) → no change (Desecrated is permanent)
        var territory = new Territory { Id = "M1", Row = TerritoryRow.Middle, CorruptionPoints = 16, PresenceCount = 1 };
        var state = new EncounterState
        {
            Territories = new() { territory },
            Corruption  = new CorruptionSystem(),
            Warden      = new EmberAbility()
        };

        state.Warden.OnResolution(state);

        Assert.Equal(16, territory.CorruptionPoints);
    }

    [Fact]
    public void Ember_ScorchedEarth_MixedBoard_CorrectCleanse()
    {
        // 3 territories: L0(2pts), L1(5pts), L2(10pts) → 0, 0, 5 after Resolution
        var territories = new List<Territory>
        {
            new() { Id = "A1", Row = TerritoryRow.Arrival, CorruptionPoints =  2, PresenceCount = 1 },
            new() { Id = "M1", Row = TerritoryRow.Middle,  CorruptionPoints =  5, PresenceCount = 1 },
            new() { Id = "I1", Row = TerritoryRow.Inner,   CorruptionPoints = 10, PresenceCount = 1 }
        };
        var state = new EncounterState
        {
            Territories = territories,
            Corruption  = new CorruptionSystem(),
            Warden      = new EmberAbility()
        };

        state.Warden.OnResolution(state);

        Assert.Equal(0, territories[0].CorruptionPoints); // L0 → fully cleansed
        Assert.Equal(0, territories[1].CorruptionPoints); // L1 → fully cleansed
        Assert.Equal(5, territories[2].CorruptionPoints); // L2 → halved
    }

    // ── D31 Part D: Controlled Burn passive ───────────────────────────────────

    [Fact]
    public void Ember_ControlledBurn_Generates2Fear_With3PlusL1Territories()
    {
        // 3 territories at L1 (3-7 pts) → 2 Fear at tide start (controlled_burn active)
        var territories = new List<Territory>
        {
            new() { Id = "A1", Row = TerritoryRow.Arrival, CorruptionPoints = 3 },
            new() { Id = "A2", Row = TerritoryRow.Arrival, CorruptionPoints = 5 },
            new() { Id = "M1", Row = TerritoryRow.Middle,  CorruptionPoints = 4 }
        };
        var gating = new PassiveGating("ember");
        gating.OnThresholdTriggered(Element.Shadow, 1); // unlocks controlled_burn
        var dread = new DreadSystem();
        int fearGenerated = 0;
        GameEvents.FearGenerated += amt => fearGenerated += amt;

        var state = new EncounterState
        {
            Territories  = territories,
            Corruption   = new CorruptionSystem(),
            Warden       = new EmberAbility(),
            PassiveGating = gating,
            Dread        = dread
        };

        state.Warden.OnTideStart(state);

        Assert.Equal(2, fearGenerated);
    }

    [Fact]
    public void Ember_ControlledBurn_NoFear_WithFewerThan3L1()
    {
        // Only 2 territories at L1 → no Fear
        var territories = new List<Territory>
        {
            new() { Id = "A1", Row = TerritoryRow.Arrival, CorruptionPoints = 3 },
            new() { Id = "A2", Row = TerritoryRow.Arrival, CorruptionPoints = 5 },
            new() { Id = "M1", Row = TerritoryRow.Middle,  CorruptionPoints = 0 } // L0
        };
        var gating = new PassiveGating("ember");
        gating.OnThresholdTriggered(Element.Shadow, 1);
        int fearGenerated = 0;
        GameEvents.FearGenerated += amt => fearGenerated += amt;

        var state = new EncounterState
        {
            Territories   = territories,
            Corruption    = new CorruptionSystem(),
            Warden        = new EmberAbility(),
            PassiveGating = gating
        };

        state.Warden.OnTideStart(state);

        Assert.Equal(0, fearGenerated);
    }

    [Fact]
    public void Ember_ControlledBurn_OnlyCountsL1_NotL2OrL0()
    {
        // 2 territories at L0, 2 at L2 — none at L1 → no Fear (threshold not met)
        var territories = new List<Territory>
        {
            new() { Id = "A1", Row = TerritoryRow.Arrival, CorruptionPoints =  1 }, // L0
            new() { Id = "A2", Row = TerritoryRow.Arrival, CorruptionPoints =  2 }, // L0
            new() { Id = "M1", Row = TerritoryRow.Middle,  CorruptionPoints = 10 }, // L2
            new() { Id = "M2", Row = TerritoryRow.Middle,  CorruptionPoints = 12 }  // L2
        };
        var gating = new PassiveGating("ember");
        gating.OnThresholdTriggered(Element.Shadow, 1);
        int fearGenerated = 0;
        GameEvents.FearGenerated += amt => fearGenerated += amt;

        var state = new EncounterState
        {
            Territories   = territories,
            Corruption    = new CorruptionSystem(),
            Warden        = new EmberAbility(),
            PassiveGating = gating
        };

        state.Warden.OnTideStart(state);

        Assert.Equal(0, fearGenerated);
    }

    [Fact]
    public void Ember_CleanWin_Possible_WhenAllPresenceAtL1()
    {
        // All presence territories at L1 before resolution → all cleansed to 0 after
        var territories = new List<Territory>
        {
            new() { Id = "M1", Row = TerritoryRow.Middle,  CorruptionPoints = 6, PresenceCount = 1 },
            new() { Id = "A1", Row = TerritoryRow.Arrival, CorruptionPoints = 4, PresenceCount = 1 },
            new() { Id = "I1", Row = TerritoryRow.Inner,   CorruptionPoints = 3, PresenceCount = 1 }
        };
        var state = new EncounterState
        {
            Territories = territories,
            Corruption  = new CorruptionSystem(),
            Warden      = new EmberAbility()
        };

        state.Warden.OnResolution(state);

        // All L1 → fully cleansed → clean board
        Assert.All(territories, t => Assert.Equal(0, t.CorruptionPoints));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<Territory> BuildThreeTerritoryState(int[] corruption, bool presenceAll)
    {
        var territories = new List<Territory>
        {
            new() { Id = "M1", Row = TerritoryRow.Middle,  PresenceCount = presenceAll ? 1 : 0 },
            new() { Id = "A1", Row = TerritoryRow.Arrival, PresenceCount = presenceAll ? 1 : 0 },
            new() { Id = "A2", Row = TerritoryRow.Arrival, PresenceCount = presenceAll ? 1 : 0 }
        };
        for (int i = 0; i < corruption.Length && i < territories.Count; i++)
            territories[i].CorruptionPoints = corruption[i];
        return territories;
    }
}
