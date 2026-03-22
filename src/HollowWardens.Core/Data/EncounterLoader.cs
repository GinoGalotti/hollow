namespace HollowWardens.Core.Data;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public static class EncounterLoader
{
    /// <summary>
    /// Returns the hardcoded config for the first Standard Pale March encounter:
    /// 6 Tides, rule-based cadence, native seeding, 6 spawn waves, escalation at Tide 4.
    /// </summary>
    public static EncounterConfig CreatePaleMarchStandard() => new()
    {
        Id        = "enc_pale_march_01",
        Tier      = EncounterTier.Standard,
        FactionId = "pale_march",
        TideCount = 6,
        Cadence   = new CadenceConfig
        {
            Mode             = "rule_based",
            MaxPainfulStreak = 1,
            EasyFrequency    = 2
        },
        NativeSpawns = new Dictionary<string, int>
        {
            ["A1"] = 0, ["A2"] = 0, ["A3"] = 0,
            ["M1"] = 2, ["M2"] = 2, ["I1"] = 2
        },
        Waves              = BuildWaves(),
        EscalationSchedule = new List<EscalationEntry>
        {
            new() { Tide = 4, CardId = "pm_corrupt", Pool = ActionPool.Painful }
        }
    };

    private static List<SpawnWave> BuildWaves() => new()
    {
        // Wave 1 — Tide 1: gentle start, 2 Marchers
        new SpawnWave
        {
            TurnNumber    = 1,
            ArrivalPoints = new() { "A1", "A2", "A3" },
            Options       = new()
            {
                new SpawnWaveOption { Weight = 50, Units = new() { ["A1"] = new() { UnitType.Marcher }, ["A2"] = new() { UnitType.Marcher } } },
                new SpawnWaveOption { Weight = 30, Units = new() { ["A2"] = new() { UnitType.Marcher }, ["A3"] = new() { UnitType.Marcher } } },
                new SpawnWaveOption { Weight = 20, Units = new() { ["A1"] = new() { UnitType.Marcher, UnitType.Marcher } } }
            }
        },
        // Wave 2 — Tide 2: 3 units, wider spread
        new SpawnWave
        {
            TurnNumber    = 2,
            ArrivalPoints = new() { "A1", "A2", "A3" },
            Options       = new()
            {
                new SpawnWaveOption { Weight = 40, Units = new() { ["A1"] = new() { UnitType.Marcher }, ["A2"] = new() { UnitType.Marcher }, ["A3"] = new() { UnitType.Marcher } } },
                new SpawnWaveOption { Weight = 35, Units = new() { ["A1"] = new() { UnitType.Marcher, UnitType.Pioneer }, ["A3"] = new() { UnitType.Marcher } } },
                new SpawnWaveOption { Weight = 25, Units = new() { ["A2"] = new() { UnitType.Marcher, UnitType.Marcher }, ["A3"] = new() { UnitType.Marcher } } }
            }
        },
        // Wave 3 — Tide 3: 4 units, Outriders introduced
        new SpawnWave
        {
            TurnNumber    = 3,
            ArrivalPoints = new() { "A1", "A2", "A3" },
            Options       = new()
            {
                new SpawnWaveOption { Weight = 35, Units = new() { ["A1"] = new() { UnitType.Marcher, UnitType.Outrider }, ["A3"] = new() { UnitType.Marcher, UnitType.Marcher } } },
                new SpawnWaveOption { Weight = 35, Units = new() { ["A2"] = new() { UnitType.Marcher, UnitType.Outrider }, ["A1"] = new() { UnitType.Marcher }, ["A3"] = new() { UnitType.Marcher } } },
                new SpawnWaveOption { Weight = 30, Units = new() { ["A1"] = new() { UnitType.Marcher, UnitType.Marcher }, ["A3"] = new() { UnitType.Marcher, UnitType.Outrider } } }
            }
        },
        // Wave 4 — Tide 4: 4 units, Ironclad first appears (escalation triggers this tide)
        new SpawnWave
        {
            TurnNumber    = 4,
            ArrivalPoints = new() { "A1", "A2", "A3" },
            Options       = new()
            {
                new SpawnWaveOption { Weight = 40, Units = new() { ["A1"] = new() { UnitType.Marcher, UnitType.Ironclad }, ["A2"] = new() { UnitType.Marcher }, ["A3"] = new() { UnitType.Outrider } } },
                new SpawnWaveOption { Weight = 35, Units = new() { ["A1"] = new() { UnitType.Outrider, UnitType.Outrider }, ["A2"] = new() { UnitType.Ironclad }, ["A3"] = new() { UnitType.Marcher } } },
                new SpawnWaveOption { Weight = 25, Units = new() { ["A2"] = new() { UnitType.Marcher, UnitType.Ironclad }, ["A1"] = new() { UnitType.Outrider }, ["A3"] = new() { UnitType.Marcher } } }
            }
        },
        // Wave 5 — Tide 5: 4 units, Pioneers + Outriders
        new SpawnWave
        {
            TurnNumber    = 5,
            ArrivalPoints = new() { "A1", "A2", "A3" },
            Options       = new()
            {
                new SpawnWaveOption { Weight = 40, Units = new() { ["A1"] = new() { UnitType.Marcher, UnitType.Pioneer }, ["A2"] = new() { UnitType.Outrider }, ["A3"] = new() { UnitType.Marcher } } },
                new SpawnWaveOption { Weight = 35, Units = new() { ["A1"] = new() { UnitType.Marcher, UnitType.Outrider }, ["A3"] = new() { UnitType.Marcher, UnitType.Pioneer } } },
                new SpawnWaveOption { Weight = 25, Units = new() { ["A2"] = new() { UnitType.Marcher, UnitType.Marcher }, ["A1"] = new() { UnitType.Pioneer }, ["A3"] = new() { UnitType.Outrider } } }
            }
        },
        // Wave 6 — Tide 6: 4 units, 2 Marchers + 1 Ironclad + 1 Outrider
        new SpawnWave
        {
            TurnNumber    = 6,
            ArrivalPoints = new() { "A1", "A2", "A3" },
            Options       = new()
            {
                new SpawnWaveOption { Weight = 35, Units = new() { ["A1"] = new() { UnitType.Ironclad, UnitType.Outrider }, ["A2"] = new() { UnitType.Marcher }, ["A3"] = new() { UnitType.Marcher } } },
                new SpawnWaveOption { Weight = 35, Units = new() { ["A1"] = new() { UnitType.Marcher, UnitType.Ironclad }, ["A2"] = new() { UnitType.Marcher }, ["A3"] = new() { UnitType.Outrider } } },
                new SpawnWaveOption { Weight = 30, Units = new() { ["A2"] = new() { UnitType.Marcher, UnitType.Outrider }, ["A1"] = new() { UnitType.Ironclad }, ["A3"] = new() { UnitType.Marcher } } }
            }
        },
        // Wave 7 — overflow/offset wave: heavy Ironclad assault (6 tides need 7 waves for N+1 offset)
        new SpawnWave
        {
            TurnNumber    = 7,
            ArrivalPoints = new() { "A1", "A2", "A3" },
            Options       = new()
            {
                new SpawnWaveOption { Weight = 40, Units = new() { ["A1"] = new() { UnitType.Ironclad, UnitType.Ironclad }, ["A3"] = new() { UnitType.Marcher, UnitType.Outrider } } },
                new SpawnWaveOption { Weight = 35, Units = new() { ["A1"] = new() { UnitType.Ironclad, UnitType.Marcher }, ["A2"] = new() { UnitType.Ironclad }, ["A3"] = new() { UnitType.Outrider } } },
                new SpawnWaveOption { Weight = 25, Units = new() { ["A2"] = new() { UnitType.Ironclad, UnitType.Outrider }, ["A1"] = new() { UnitType.Ironclad }, ["A3"] = new() { UnitType.Marcher } } }
            }
        }
    };
}
