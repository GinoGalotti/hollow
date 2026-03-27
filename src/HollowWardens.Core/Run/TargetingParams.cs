namespace HollowWardens.Core.Run;

using System.Text.Json.Serialization;

/// <summary>
/// Targeting intelligence parameters for ParameterizedBotStrategy.
/// Controls how the bot selects territories for each effect type and threshold resolution.
/// All boolean flags are optimisable by the hill-climber.
/// </summary>
public class TargetingParams
{
    // --- Damage Targeting ---
    [JsonPropertyName("prefer_kills_over_damage")]
    public bool PreferKillsOverDamage { get; set; } = true;   // target where damage kills most invaders vs. most total damage

    [JsonPropertyName("prefer_arrival_row")]
    public bool PreferArrivalRow { get; set; } = true;         // bias toward A-row (kill before advance)

    [JsonPropertyName("threat_row_weight")]
    public int ThreatRowWeight { get; set; } = 3;              // score multiplier for M-row/I1 targets (1 = no bias)

    [JsonPropertyName("target_weakest_first")]
    public bool TargetWeakestFirst { get; set; } = true;       // counter-attack: maximize kills (weakest first) vs. focus fire

    // --- Presence Targeting ---
    [JsonPropertyName("presence_prefer_stack")]
    public bool PresencePreferStack { get; set; } = true;      // place in existing territory vs. new territory

    [JsonPropertyName("presence_prefer_threshold")]
    public bool PresencePreferThreshold { get; set; } = true;  // place where it crosses assimilation spawn threshold

    [JsonPropertyName("presence_prefer_adj_invader")]
    public bool PresencePreferAdjInvader { get; set; } = false; // place adjacent to invader-heavy territories

    // --- Cleanse Targeting ---
    [JsonPropertyName("cleanse_prefer_highest")]
    public bool CleansePreferHighest { get; set; } = false;    // highest absolute corruption

    [JsonPropertyName("cleanse_prefer_near_threshold")]
    public bool CleansePreferNearThreshold { get; set; } = true; // closest to leveling up (L0→L1 at 3, L1→L2 at 8)

    [JsonPropertyName("cleanse_prefer_presence")]
    public bool CleansePreferPresence { get; set; } = true;    // only cleanse territories with presence

    // --- Threshold Resolution Targeting ---
    [JsonPropertyName("ash_t1_prefer_most_invaders")]
    public bool AshT1PreferMostInvaders { get; set; } = true;  // vs. prefer weakest invaders for kills

    [JsonPropertyName("ash_t2_prefer_high_corruption")]
    public bool AshT2PreferHighCorruption { get; set; } = false; // Ash T2 adds corruption — avoid already-corrupted by default

    [JsonPropertyName("ash_t3_prefer_high_presence")]
    public bool AshT3PreferHighPresence { get; set; } = true;  // Ash T3 = 2 dmg per presence — target stacked territory

    [JsonPropertyName("root_t1_prefer_near_threshold")]
    public bool RootT1PreferNearThreshold { get; set; } = true; // Root T1 = Reduce Corruption ×3 — target near level-up

    [JsonPropertyName("root_t2_prefer_frontline")]
    public bool RootT2PreferFrontline { get; set; } = true;    // Root T2 = Place Presence — prefer A-row/M-row

    [JsonPropertyName("gale_t1_push_toward_spawn")]
    public bool GaleT1PushTowardSpawn { get; set; } = true;    // Gale = push invaders backward vs. sideways

    // --- Provocation Territory Selection (relevant when ProvocationTerritoryLimit > 0) ---
    [JsonPropertyName("provocation_prefer_most_invaders")]
    public bool ProvocationPreferMostInvaders { get; set; } = true;   // pick territories with most invaders present

    [JsonPropertyName("provocation_prefer_heart_proximity")]
    public bool ProvocationPreferHeartProximity { get; set; } = true; // tiebreak toward M-row/I1 over A-row

    [JsonPropertyName("provocation_prefer_most_natives")]
    public bool ProvocationPreferMostNatives { get; set; } = false;   // tiebreak toward most natives (counter-attack power)
}
