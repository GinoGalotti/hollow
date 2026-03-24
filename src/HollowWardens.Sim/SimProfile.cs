namespace HollowWardens.Sim;

using System.Text.Json.Serialization;

public class SimProfile
{
    public string Name { get; set; } = "unnamed";

    // New: seed range/list, e.g. "1-500" or "42,100,200"
    public string? Seeds { get; set; }

    // Backward-compat fields (ignored when Seeds is set)
    public int Runs { get; set; } = 100;
    public int Seed { get; set; } = 42;

    public string Warden { get; set; } = "root";
    public string Encounter { get; set; } = "pale_march_standard";
    public string? Output { get; set; }

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "single"; // "single" or "chain"

    [JsonPropertyName("realm")]
    public string? Realm { get; set; }

    [JsonPropertyName("chain_overrides")]
    public ChainOverrides? ChainOverrides { get; set; }

    [JsonPropertyName("warden_overrides")]
    public WardenOverrides? WardenOverrides { get; set; }

    [JsonPropertyName("encounter_overrides")]
    public EncounterOverrides? EncounterOverrides { get; set; }

    [JsonPropertyName("balance_overrides")]
    public Dictionary<string, object>? BalanceOverrides { get; set; }

    [JsonPropertyName("board_carryover")]
    public BoardCarryoverOverride? BoardCarryover { get; set; }
}

public class BoardCarryoverOverride
{
    [JsonPropertyName("starting_weave")]
    public int? StartingWeave { get; set; }

    [JsonPropertyName("starting_corruption")]
    public Dictionary<string, int>? StartingCorruption { get; set; }

    [JsonPropertyName("dread_level")]
    public int? DreadLevel { get; set; }

    [JsonPropertyName("total_fear")]
    public int? TotalFear { get; set; }

    [JsonPropertyName("removed_cards")]
    public List<string>? RemovedCards { get; set; }
}

public class WardenOverrides
{
    [JsonPropertyName("hand_limit")]
    public int? HandLimit { get; set; }

    [JsonPropertyName("starting_presence")]
    public StartingPresenceOverride? StartingPresence { get; set; }

    [JsonPropertyName("add_cards")]
    public List<string>? AddCards { get; set; }

    [JsonPropertyName("remove_cards")]
    public List<string>? RemoveCards { get; set; }

    [JsonPropertyName("upgrade_cards")]
    public Dictionary<string, CardUpgrade>? UpgradeCards { get; set; }

    [JsonPropertyName("force_passives")]
    public List<string>? ForcePassives { get; set; }

    [JsonPropertyName("lock_passives")]
    public List<string>? LockPassives { get; set; }

    [JsonPropertyName("starting_elements")]
    public Dictionary<string, int>? StartingElements { get; set; }
}

public class StartingPresenceOverride
{
    public string Territory { get; set; } = "I1";
    public int Count { get; set; } = 1;
}

public class CardUpgrade
{
    public EffectOverride? Top { get; set; }
    public EffectOverride? Bottom { get; set; }
}

public class EffectOverride
{
    public int? Value { get; set; }
    public int? Range { get; set; }
    public string? Type { get; set; }
}

public class EncounterOverrides
{
    [JsonPropertyName("tide_count")]
    public int? TideCount { get; set; }

    [JsonPropertyName("starting_corruption")]
    public Dictionary<string, int>? StartingCorruption { get; set; }

    [JsonPropertyName("native_spawns")]
    public Dictionary<string, int>? NativeSpawns { get; set; }

    [JsonPropertyName("extra_invaders_per_wave")]
    public int? ExtraInvadersPerWave { get; set; }

    [JsonPropertyName("escalation_schedule")]
    public List<EscalationOverride>? EscalationSchedule { get; set; }

    // Easy tier
    [JsonPropertyName("element_decay_override")]
    public int? ElementDecayOverride { get; set; }

    [JsonPropertyName("starting_elements")]
    public Dictionary<string, int>? StartingElements { get; set; }

    [JsonPropertyName("threshold_damage_bonus")]
    public int? ThresholdDamageBonus { get; set; }

    [JsonPropertyName("vigil_play_limit_override")]
    public int? VigilPlayLimitOverride { get; set; }

    [JsonPropertyName("dusk_play_limit_override")]
    public int? DuskPlayLimitOverride { get; set; }

    [JsonPropertyName("hand_limit_override")]
    public int? HandLimitOverride { get; set; }

    [JsonPropertyName("native_hp_override")]
    public int? NativeHpOverride { get; set; }

    [JsonPropertyName("native_damage_override")]
    public int? NativeDamageOverride { get; set; }

    [JsonPropertyName("fear_multiplier")]
    public float? FearMultiplier { get; set; }

    [JsonPropertyName("heart_damage_multiplier")]
    public float? HeartDamageMultiplier { get; set; }

    // Medium tier
    [JsonPropertyName("invader_corruption_scaling")]
    public bool? InvaderCorruptionScaling { get; set; }

    [JsonPropertyName("invader_arrival_shield")]
    public int? InvaderArrivalShield { get; set; }

    [JsonPropertyName("invader_regen_on_rest")]
    public int? InvaderRegenOnRest { get; set; }

    [JsonPropertyName("invader_advance_bonus")]
    public int? InvaderAdvanceBonus { get; set; }

    [JsonPropertyName("surge_tides")]
    public List<int>? SurgeTides { get; set; }

    [JsonPropertyName("starting_infrastructure")]
    public Dictionary<string, int>? StartingInfrastructure { get; set; }

    [JsonPropertyName("presence_placement_corruption_cost")]
    public int? PresencePlacementCorruptionCost { get; set; }

    // Hard tier
    [JsonPropertyName("corruption_spread")]
    public int? CorruptionSpread { get; set; }

    [JsonPropertyName("sacred_territories")]
    public List<string>? SacredTerritories { get; set; }

    [JsonPropertyName("native_erosion_per_tide")]
    public int? NativeErosionPerTide { get; set; }

    [JsonPropertyName("blight_pulse_interval")]
    public int? BlightPulseInterval { get; set; }

    [JsonPropertyName("eclipse_tides")]
    public List<int>? EclipseTides { get; set; }

    // Board layout
    [JsonPropertyName("board_layout")]
    public string? BoardLayout { get; set; }
}

public class EscalationOverride
{
    public int Tide { get; set; }
    public string Card { get; set; } = "";
    public string Pool { get; set; } = "painful";
}

public class ChainOverrides
{
    [JsonPropertyName("starting_max_weave")]
    public int? StartingMaxWeave { get; set; }

    [JsonPropertyName("starting_tokens")]
    public int? StartingTokens { get; set; }

    [JsonPropertyName("force_encounters")]
    public List<string>? ForceEncounters { get; set; }

    [JsonPropertyName("disable_events")]
    public bool DisableEvents { get; set; } = false;

    [JsonPropertyName("bot_config")]
    public string? BotConfigPath { get; set; }
}
