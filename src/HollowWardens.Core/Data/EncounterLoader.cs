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
        Waves              = AddB2Marchers(BuildWaves()),
        EscalationSchedule = new List<EscalationEntry>
        {
            new() { Tide = 4, CardId = "pm_corrupt", Pool = ActionPool.Painful }
        },
        RewardTiers = DefaultRewardTiers()
    };

    public static EncounterConfig Create(string encounterId) => encounterId switch
    {
        "pale_march_standard" => CreatePaleMarchStandard(),
        "pale_march_scouts"   => CreatePaleMarchScouts(),
        "pale_march_siege"    => CreatePaleMarchSiege(),
        "pale_march_elite"    => CreatePaleMarchElite(),
        "pale_march_frontier" => CreatePaleMarchFrontier(),
        _ => throw new ArgumentException($"Unknown encounter: {encounterId}")
    };

    public static EncounterConfig CreatePaleMarchScouts() => new()
    {
        Id        = "enc_pale_march_scouts",
        Tier      = EncounterTier.Standard,
        FactionId = "pale_march",
        TideCount = 6,
        Cadence   = new CadenceConfig { Mode = "rule_based", MaxPainfulStreak = 1, EasyFrequency = 2 },
        NativeSpawns = new Dictionary<string, int>
        {
            ["A1"] = 1, ["A2"] = 1, ["A3"] = 1,
            ["M1"] = 2, ["M2"] = 1, ["I1"] = 2
        },
        RewardTiers = DefaultRewardTiers(),
        Waves = AddB2Marchers(new List<SpawnWave>
        {
            new() { TurnNumber = 1, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Outrider}, ["A2"]=new(){UnitType.Outrider} } },
                new() { Weight=35, Units=new(){ ["A2"]=new(){UnitType.Outrider}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Outrider, UnitType.Outrider} } }
            }},
            new() { TurnNumber = 2, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Outrider}, ["A3"]=new(){UnitType.Marcher} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Marcher}, ["A2"]=new(){UnitType.Outrider} } },
                new() { Weight=25, Units=new(){ ["A2"]=new(){UnitType.Outrider, UnitType.Marcher} } }
            }},
            new() { TurnNumber = 3, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Outrider}, ["A2"]=new(){UnitType.Outrider}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Outrider, UnitType.Outrider}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Outrider}, ["A3"]=new(){UnitType.Outrider, UnitType.Outrider} } }
            }},
            new() { TurnNumber = 4, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Outrider, UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Outrider}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=25, Units=new(){ ["A2"]=new(){UnitType.Outrider, UnitType.Outrider}, ["A3"]=new(){UnitType.Marcher} } }
            }},
            new() { TurnNumber = 5, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Outrider, UnitType.Ironclad}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Outrider}, ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Outrider, UnitType.Outrider} } }
            }},
            new() { TurnNumber = 6, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Outrider, UnitType.Marcher}, ["A2"]=new(){UnitType.Outrider}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Outrider}, ["A2"]=new(){UnitType.Outrider, UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Outrider, UnitType.Outrider}, ["A3"]=new(){UnitType.Outrider, UnitType.Marcher} } }
            }},
            new() { TurnNumber = 7, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Outrider, UnitType.Marcher}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Outrider, UnitType.Outrider}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Marcher} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Outrider, UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider, UnitType.Marcher} } }
            }},
        })
    };

    public static EncounterConfig CreatePaleMarchSiege() => new()
    {
        Id        = "enc_pale_march_siege",
        Tier      = EncounterTier.Standard,
        FactionId = "pale_march",
        TideCount = 8,
        Cadence   = new CadenceConfig { Mode = "rule_based", MaxPainfulStreak = 1, EasyFrequency = 2 },
        NativeSpawns = new Dictionary<string, int>
        {
            ["A1"] = 0, ["A2"] = 0, ["A3"] = 0,
            ["M1"] = 1, ["M2"] = 1, ["I1"] = 2
        },
        EscalationSchedule = new List<EscalationEntry>
        {
            new() { Tide = 3, CardId = "pm_corrupt", Pool = ActionPool.Painful },
            new() { Tide = 6, CardId = "pm_fortify", Pool = ActionPool.Painful }
        },
        RewardTiers = SiegeRewardTiers(),
        Waves = AddB2Marchers(new List<SpawnWave>
        {
            new() { TurnNumber = 1, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Marcher}, ["A2"]=new(){UnitType.Pioneer} } },
                new() { Weight=35, Units=new(){ ["A2"]=new(){UnitType.Pioneer}, ["A3"]=new(){UnitType.Marcher} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Marcher, UnitType.Pioneer} } }
            }},
            new() { TurnNumber = 2, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad}, ["A2"]=new(){UnitType.Marcher} } },
                new() { Weight=35, Units=new(){ ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Marcher} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher} } }
            }},
            new() { TurnNumber = 3, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad}, ["A2"]=new(){UnitType.Pioneer} } },
                new() { Weight=35, Units=new(){ ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Pioneer} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Pioneer} } }
            }},
            new() { TurnNumber = 4, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Marcher, UnitType.Ironclad}, ["A3"]=new(){UnitType.Marcher} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Marcher}, ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Marcher} } },
                new() { Weight=25, Units=new(){ ["A2"]=new(){UnitType.Marcher, UnitType.Ironclad}, ["A3"]=new(){UnitType.Marcher} } }
            }},
            new() { TurnNumber = 5, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad}, ["A2"]=new(){UnitType.Pioneer}, ["A3"]=new(){UnitType.Marcher} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A3"]=new(){UnitType.Pioneer} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Marcher}, ["A2"]=new(){UnitType.Ironclad, UnitType.Pioneer} } }
            }},
            new() { TurnNumber = 6, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A3"]=new(){UnitType.Ironclad} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Ironclad} } },
                new() { Weight=25, Units=new(){ ["A2"]=new(){UnitType.Ironclad, UnitType.Ironclad}, ["A3"]=new(){UnitType.Marcher} } }
            }},
            new() { TurnNumber = 7, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Pioneer}, ["A3"]=new(){UnitType.Ironclad} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad}, ["A2"]=new(){UnitType.Pioneer}, ["A3"]=new(){UnitType.Ironclad} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Ironclad}, ["A3"]=new(){UnitType.Pioneer} } }
            }},
            new() { TurnNumber = 8, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Ironclad} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Ironclad}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Marcher} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Marcher, UnitType.Ironclad}, ["A3"]=new(){UnitType.Marcher, UnitType.Ironclad} } }
            }},
            new() { TurnNumber = 9, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad}, ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Ironclad} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Ironclad}, ["A3"]=new(){UnitType.Ironclad} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Ironclad, UnitType.Ironclad} } }
            }},
        }, count: 2)
    };

    public static EncounterConfig CreatePaleMarchElite() => new()
    {
        Id        = "enc_pale_march_elite",
        Tier      = EncounterTier.Elite,
        FactionId = "pale_march",
        TideCount = 6,
        Cadence   = new CadenceConfig { Mode = "rule_based", MaxPainfulStreak = 1, EasyFrequency = 2 },
        NativeSpawns = new Dictionary<string, int>
        {
            ["A1"] = 0, ["A2"] = 0, ["A3"] = 0,
            ["M1"] = 1, ["M2"] = 2, ["I1"] = 2
        },
        StartingCorruption = new Dictionary<string, int>
        {
            ["A1"] = 3, ["A2"] = 2, ["M1"] = 3
        },
        EscalationSchedule = new List<EscalationEntry>
        {
            new() { Tide = 2, CardId = "pm_corrupt", Pool = ActionPool.Painful },
            new() { Tide = 4, CardId = "pm_fortify", Pool = ActionPool.Painful },
            new() { Tide = 5, CardId = "pm_march",   Pool = ActionPool.Painful }
        },
        RewardTiers = EliteRewardTiers(),
        Waves = AddB2Marchers(new List<SpawnWave>
        {
            new() { TurnNumber = 1, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Marcher, UnitType.Outrider}, ["A3"]=new(){UnitType.Marcher} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Marcher}, ["A2"]=new(){UnitType.Outrider}, ["A3"]=new(){UnitType.Marcher} } },
                new() { Weight=25, Units=new(){ ["A2"]=new(){UnitType.Marcher, UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider} } }
            }},
            new() { TurnNumber = 2, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Marcher, UnitType.Ironclad}, ["A3"]=new(){UnitType.Marcher} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Marcher}, ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Marcher} } },
                new() { Weight=25, Units=new(){ ["A2"]=new(){UnitType.Marcher, UnitType.Marcher}, ["A3"]=new(){UnitType.Ironclad} } }
            }},
            new() { TurnNumber = 3, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Outrider}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Outrider, UnitType.Marcher}, ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Outrider, UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider} } }
            }},
            new() { TurnNumber = 4, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Pioneer}, ["A2"]=new(){UnitType.Outrider}, ["A3"]=new(){UnitType.Ironclad} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Outrider}, ["A2"]=new(){UnitType.Pioneer}, ["A3"]=new(){UnitType.Ironclad} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Ironclad}, ["A3"]=new(){UnitType.Pioneer, UnitType.Outrider} } }
            }},
            new() { TurnNumber = 5, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A2"]=new(){UnitType.Outrider}, ["A3"]=new(){UnitType.Ironclad, UnitType.Marcher} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Ironclad, UnitType.Marcher}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Outrider}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Ironclad, UnitType.Marcher} } }
            }},
            new() { TurnNumber = 6, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Pioneer}, ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Ironclad} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Ironclad}, ["A2"]=new(){UnitType.Pioneer}, ["A3"]=new(){UnitType.Ironclad} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad}, ["A2"]=new(){UnitType.Ironclad, UnitType.Ironclad}, ["A3"]=new(){UnitType.Pioneer} } }
            }},
            new() { TurnNumber = 7, ArrivalPoints = new() { "A1","A2","A3" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Ironclad, UnitType.Marcher} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Ironclad, UnitType.Marcher}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Ironclad} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A3"]=new(){UnitType.Ironclad, UnitType.Ironclad, UnitType.Marcher} } }
            }},
        })
    };

    public static EncounterConfig CreatePaleMarchFrontier() => new()
    {
        Id          = "enc_pale_march_frontier",
        Tier        = EncounterTier.Standard,
        FactionId   = "pale_march",
        TideCount   = 7,
        BoardLayout = "wide",
        Cadence     = new CadenceConfig { Mode = "rule_based", MaxPainfulStreak = 1, EasyFrequency = 2 },
        NativeSpawns = new Dictionary<string, int>
        {
            ["A1"] = 1, ["A2"] = 0, ["A3"] = 0, ["A4"] = 1,
            ["M1"] = 1, ["M2"] = 1, ["M3"] = 1,
            ["B1"] = 1, ["B2"] = 1,
            ["I1"] = 2
        },
        RewardTiers = DefaultRewardTiers(),
        Waves = new List<SpawnWave>
        {
            new() { TurnNumber = 1, ArrivalPoints = new() { "A1","A2","A3","A4" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Marcher, UnitType.Outrider}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Marcher}, ["A4"]=new(){UnitType.Outrider} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Marcher}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider}, ["A4"]=new(){UnitType.Marcher} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Marcher, UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider}, ["A4"]=new(){UnitType.Marcher} } }
            }},
            new() { TurnNumber = 2, ArrivalPoints = new() { "A1","A2","A3","A4" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Marcher, UnitType.Outrider}, ["A2"]=new(){UnitType.Outrider}, ["A3"]=new(){UnitType.Marcher}, ["A4"]=new(){UnitType.Marcher} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Outrider, UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider}, ["A4"]=new(){UnitType.Marcher, UnitType.Outrider} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Marcher}, ["A2"]=new(){UnitType.Outrider}, ["A3"]=new(){UnitType.Marcher}, ["A4"]=new(){UnitType.Outrider} } }
            }},
            new() { TurnNumber = 3, ArrivalPoints = new() { "A1","A2","A3","A4" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Marcher, UnitType.Ironclad}, ["A2"]=new(){UnitType.Outrider}, ["A3"]=new(){UnitType.Marcher}, ["A4"]=new(){UnitType.Ironclad} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Marcher, UnitType.Outrider}, ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Marcher}, ["A4"]=new(){UnitType.Outrider} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A3"]=new(){UnitType.Outrider, UnitType.Marcher}, ["A4"]=new(){UnitType.Ironclad} } }
            }},
            new() { TurnNumber = 4, ArrivalPoints = new() { "A1","A2","A3","A4" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A2"]=new(){UnitType.Pioneer}, ["A3"]=new(){UnitType.Marcher}, ["A4"]=new(){UnitType.Ironclad, UnitType.Marcher} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Marcher, UnitType.Marcher}, ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Pioneer}, ["A4"]=new(){UnitType.Marcher} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Pioneer}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Marcher}, ["A4"]=new(){UnitType.Ironclad} } }
            }},
            new() { TurnNumber = 5, ArrivalPoints = new() { "A1","A2","A3","A4" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A2"]=new(){UnitType.Outrider}, ["A3"]=new(){UnitType.Ironclad}, ["A4"]=new(){UnitType.Marcher, UnitType.Outrider} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Outrider}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Marcher}, ["A4"]=new(){UnitType.Ironclad, UnitType.Outrider} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A3"]=new(){UnitType.Ironclad, UnitType.Outrider}, ["A4"]=new(){UnitType.Marcher} } }
            }},
            new() { TurnNumber = 6, ArrivalPoints = new() { "A1","A2","A3","A4" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Outrider}, ["A2"]=new(){UnitType.Pioneer}, ["A3"]=new(){UnitType.Ironclad}, ["A4"]=new(){UnitType.Ironclad, UnitType.Outrider} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Ironclad}, ["A2"]=new(){UnitType.Pioneer}, ["A3"]=new(){UnitType.Outrider}, ["A4"]=new(){UnitType.Ironclad} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Outrider, UnitType.Pioneer}, ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Ironclad}, ["A4"]=new(){UnitType.Outrider, UnitType.Pioneer} } }
            }},
            new() { TurnNumber = 7, ArrivalPoints = new() { "A1","A2","A3","A4" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A2"]=new(){UnitType.Ironclad}, ["A3"]=new(){UnitType.Ironclad}, ["A4"]=new(){UnitType.Ironclad, UnitType.Marcher} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Ironclad}, ["A2"]=new(){UnitType.Marcher}, ["A3"]=new(){UnitType.Ironclad}, ["A4"]=new(){UnitType.Ironclad} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A3"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A4"]=new(){UnitType.Ironclad} } }
            }},
            new() { TurnNumber = 8, ArrivalPoints = new() { "A1","A2","A3","A4" }, Options = new()
            {
                new() { Weight=40, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A2"]=new(){UnitType.Ironclad, UnitType.Pioneer}, ["A3"]=new(){UnitType.Ironclad}, ["A4"]=new(){UnitType.Ironclad, UnitType.Marcher} } },
                new() { Weight=35, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Ironclad}, ["A2"]=new(){UnitType.Pioneer}, ["A3"]=new(){UnitType.Ironclad, UnitType.Marcher}, ["A4"]=new(){UnitType.Ironclad} } },
                new() { Weight=25, Units=new(){ ["A1"]=new(){UnitType.Ironclad, UnitType.Marcher, UnitType.Pioneer}, ["A3"]=new(){UnitType.Ironclad, UnitType.Ironclad}, ["A4"]=new(){UnitType.Marcher} } }
            }},
        }
    };

    /// <summary>Default reward tiers: Clean=Tier1, Weathered=Tier2, Breach=Tier3.</summary>
    public static Dictionary<string, RewardTierConfig> DefaultRewardTiers() => new()
    {
        ["root"]  = new() { Tier1MinResult = "clean",     Tier2MinResult = "weathered" },
        ["ember"] = new() { Tier1MinResult = "clean",     Tier2MinResult = "weathered" }
    };

    /// <summary>Siege reward tiers: Weathered with high weave (60%+) = Tier1 for both wardens.</summary>
    public static Dictionary<string, RewardTierConfig> SiegeRewardTiers() => new()
    {
        ["root"]  = new() { Tier1MinResult = "clean",     Tier2MinResult = "weathered" },
        ["ember"] = new() { Tier1MinResult = "weathered", Tier1MinWeavePercent = 60, Tier2MinResult = "weathered" }
    };

    /// <summary>Elite reward tiers: Clean=Tier1, Weathered+50%weave=Tier2.</summary>
    public static Dictionary<string, RewardTierConfig> EliteRewardTiers() => new()
    {
        ["root"]  = new() { Tier1MinResult = "clean", Tier2MinResult = "weathered", Tier2MinWeavePercent = 50 },
        ["ember"] = new() { Tier1MinResult = "clean", Tier2MinResult = "weathered", Tier2MinWeavePercent = 50 }
    };

    private static List<SpawnWave> AddB2Marchers(List<SpawnWave> waves, int count = 1)
    {
        foreach (var wave in waves)
            foreach (var option in wave.Options)
            {
                if (!option.Units.ContainsKey("A1"))
                    option.Units["A1"] = new();
                for (int i = 0; i < count; i++)
                    option.Units["A1"].Add(UnitType.Marcher);
            }
        return waves;
    }

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
