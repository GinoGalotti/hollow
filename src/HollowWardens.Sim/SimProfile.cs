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

    [JsonPropertyName("warden_overrides")]
    public WardenOverrides? WardenOverrides { get; set; }

    [JsonPropertyName("encounter_overrides")]
    public EncounterOverrides? EncounterOverrides { get; set; }

    [JsonPropertyName("balance_overrides")]
    public Dictionary<string, object>? BalanceOverrides { get; set; }
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
}

public class EscalationOverride
{
    public int Tide { get; set; }
    public string Card { get; set; } = "";
    public string Pool { get; set; } = "painful";
}
