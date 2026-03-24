namespace HollowWardens.Core.Encounter;

using HollowWardens.Core.Models;

public class EncounterConfig
{
    public string Id { get; set; } = string.Empty;
    public EncounterTier Tier { get; set; }
    public string FactionId { get; set; } = string.Empty;
    public int TideCount { get; set; }
    public int ResolutionTurns => Tier switch
    {
        EncounterTier.Standard => 2,
        EncounterTier.Elite    => 3,
        EncounterTier.Boss     => 1,
        _                      => 2
    };
    public CadenceConfig Cadence { get; set; } = new();
    public Dictionary<string, int> NativeSpawns { get; set; } = new();
    public List<SpawnWave> Waves { get; set; } = new();
    public List<EscalationEntry> EscalationSchedule { get; set; } = new();
    public Dictionary<string, int>? StartingCorruption { get; set; }

    // ── Board Layout ─────────────────────────────────────────────────────────
    /// <summary>Layout ID passed to TerritoryGraph.Create. Default: "standard" (3-2-1 pyramid).</summary>
    public string BoardLayout { get; set; } = "standard";

    // ── Element / Threshold ──────────────────────────────────────────────────
    /// <summary>Override element decay for this encounter (null = use BalanceConfig).</summary>
    public int? ElementDecayOverride { get; set; }

    /// <summary>Starting element counts applied before Tide 1. E.g., {"Root": 3, "Mist": 2}.</summary>
    public Dictionary<string, int>? StartingElements { get; set; }

    /// <summary>Threshold damage bonus for this encounter. Added to T1/T2/T3 globally.</summary>
    public int ThresholdDamageBonus { get; set; } = 0;

    // ── Player Modifiers ─────────────────────────────────────────────────────
    /// <summary>Override vigil play limit for this encounter.</summary>
    public int? VigilPlayLimitOverride { get; set; }

    /// <summary>Override dusk play limit for this encounter.</summary>
    public int? DuskPlayLimitOverride { get; set; }

    /// <summary>Override hand limit for this encounter.</summary>
    public int? HandLimitOverride { get; set; }

    // ── Natives ──────────────────────────────────────────────────────────────
    /// <summary>Override native HP for this encounter.</summary>
    public int? NativeHpOverride { get; set; }

    /// <summary>Override native damage for this encounter.</summary>
    public int? NativeDamageOverride { get; set; }

    // ── Fear / Heart ─────────────────────────────────────────────────────────
    /// <summary>Fear generation multiplier. 0.5 = half fear, 2.0 = double.</summary>
    public float FearMultiplier { get; set; } = 1.0f;

    /// <summary>Heart damage multiplier. Invaders at I1 deal ×N weave damage.</summary>
    public float HeartDamageMultiplier { get; set; } = 1.0f;

    // ── Invader Modifiers ────────────────────────────────────────────────────
    /// <summary>Invaders gain +1 HP per L1+ territory on arrival.</summary>
    public bool InvaderCorruptionScaling { get; set; } = false;

    /// <summary>All invaders gain this shield value on arrival.</summary>
    public int InvaderArrivalShield { get; set; } = 0;

    /// <summary>Invaders regenerate N HP on Rest action (capped at MaxHp).</summary>
    public int InvaderRegenOnRest { get; set; } = 0;

    /// <summary>Invader advance speed bonus. +1 = all invaders move 1 extra step per Advance.</summary>
    public int InvaderAdvanceBonus { get; set; } = 0;

    // ── Environmental ────────────────────────────────────────────────────────
    /// <summary>Tides where double waves arrive (surge). E.g., [3, 5].</summary>
    public List<int>? SurgeTides { get; set; }

    /// <summary>Pre-placed infrastructure at encounter start. Territory → count.</summary>
    public Dictionary<string, int>? StartingInfrastructure { get; set; }

    /// <summary>Presence placement adds N corruption to the territory.</summary>
    public int PresencePlacementCorruptionCost { get; set; } = 0;

    // ── Complex Environmental ────────────────────────────────────────────────
    /// <summary>Corruption spreads N points to 1 random adjacent L0 territory at Tide end.</summary>
    public int CorruptionSpread { get; set; } = 0;

    /// <summary>Territories immune to corruption beyond L0.</summary>
    public List<string>? SacredTerritories { get; set; }

    /// <summary>At Tide end, all natives lose N HP (erosion).</summary>
    public int NativeErosionPerTide { get; set; } = 0;

    /// <summary>Every N tides, a random territory gains +3 corruption (blight pulse). 0 = disabled.</summary>
    public int BlightPulseInterval { get; set; } = 0;

    /// <summary>Tides where play structure is inverted (Dusk → Tide → Vigil).</summary>
    public List<int>? EclipseTides { get; set; }
    // TODO: Eclipse phase inversion — requires TurnManager refactor

    // ── Reward Tiers ─────────────────────────────────────────────────────────
    /// <summary>Per-warden reward tier thresholds for this encounter.</summary>
    public Dictionary<string, RewardTierConfig>? RewardTiers { get; set; }
}

/// <summary>
/// Defines the minimum result/weave thresholds for each reward tier in an encounter.
/// </summary>
public class RewardTierConfig
{
    /// <summary>Minimum encounter result for Tier 1 (e.g. "clean").</summary>
    public string Tier1MinResult { get; set; } = "clean";

    /// <summary>Optional minimum weave % required for Tier 1 (0-100). null = no weave requirement.</summary>
    public int? Tier1MinWeavePercent { get; set; }

    /// <summary>Minimum encounter result for Tier 2 (e.g. "weathered").</summary>
    public string Tier2MinResult { get; set; } = "weathered";

    /// <summary>Optional minimum weave % required for Tier 2.</summary>
    public int? Tier2MinWeavePercent { get; set; }
}

public class CadenceConfig
{
    public string Mode { get; set; } = "rule_based";  // or "manual"
    public int MaxPainfulStreak { get; set; } = 1;
    public int EasyFrequency { get; set; } = 2;
    public string[]? ManualPattern { get; set; }  // ["P","E","P","P","E",...]
}

public class EscalationEntry
{
    public int Tide { get; set; }
    public string CardId { get; set; } = string.Empty;
    public ActionPool Pool { get; set; }
}
