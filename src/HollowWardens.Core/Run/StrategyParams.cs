namespace HollowWardens.Core.Run;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Phase-aware strategy parameters for ParameterizedBotStrategy.
/// Two-phase model: early (1 to PhaseTransitionTide) = engine-building,
/// late (PhaseTransitionTide+1 to end) = threat response.
/// All int/bool fields are hill-climber optimisable via PerturbableParams.
/// </summary>
public class StrategyParams
{
    // ── Phase Transition ──────────────────────────────────────────────────────
    [JsonPropertyName("phase_transition_tide")]
    public int PhaseTransitionTide { get; set; } = 3;

    // ── Presence Strategy ─────────────────────────────────────────────────────
    [JsonPropertyName("spread_target")]
    public int SpreadTarget { get; set; } = 3;            // territories to expand before stacking

    [JsonPropertyName("stack_target")]
    public int StackTarget { get; set; } = 3;             // presence per territory to aim for

    [JsonPropertyName("prefer_tall_over_wide")]
    public bool PreferTallOverWide { get; set; } = true;  // stack existing vs. spread new

    // ── Early Phase Priorities (ordinal ranks, 1=highest) ────────────────────
    [JsonPropertyName("early_presence_priority")]
    public int EarlyPresencePriority { get; set; } = 1;

    [JsonPropertyName("early_damage_priority")]
    public int EarlyDamagePriority { get; set; } = 3;

    [JsonPropertyName("early_cleanse_priority")]
    public int EarlyCleansePriority { get; set; } = 4;

    [JsonPropertyName("early_fear_priority")]
    public int EarlyFearPriority { get; set; } = 2;

    [JsonPropertyName("early_weave_priority")]
    public int EarlyWeavePriority { get; set; } = 5;

    [JsonPropertyName("early_passive_unlock_priority")]
    public int EarlyPassiveUnlockPriority { get; set; } = 2;

    // ── Late Phase Priorities ─────────────────────────────────────────────────
    [JsonPropertyName("late_damage_priority")]
    public int LateDamagePriority { get; set; } = 1;

    [JsonPropertyName("late_cleanse_priority")]
    public int LateCleansePriority { get; set; } = 2;

    [JsonPropertyName("late_presence_priority")]
    public int LatePresencePriority { get; set; } = 4;

    [JsonPropertyName("late_fear_priority")]
    public int LateFearPriority { get; set; } = 3;

    [JsonPropertyName("late_weave_priority")]
    public int LateWeavePriority { get; set; } = 2;

    [JsonPropertyName("late_passive_unlock_priority")]
    public int LatePassiveUnlockPriority { get; set; } = 5;

    // ── Urgency Thresholds ────────────────────────────────────────────────────
    [JsonPropertyName("damage_urgency_invader_count")]
    public int DamageUrgencyInvaderCount { get; set; } = 2;  // invaders near Heart triggering damage urgency

    [JsonPropertyName("cleanse_urgency_corruption")]
    public int CleanseUrgencyCorruption { get; set; } = 5;   // corruption points triggering cleanse urgency

    [JsonPropertyName("weave_urgency_threshold")]
    public int WeaveUrgencyThreshold { get; set; } = 12;     // weave below this triggers weave priority

    [JsonPropertyName("heart_threat_tide")]
    public int HeartThreatTide { get; set; } = 4;            // tide from which M-row invaders are treated as critical

    // ── Native Effect Priorities ──────────────────────────────────────────────
    [JsonPropertyName("early_spawn_natives_priority")]
    public int EarlySpawnNativesPriority { get; set; } = 3;   // early: mid-priority (needs invaders to matter)

    [JsonPropertyName("late_spawn_natives_priority")]
    public int LateSpawnNativesPriority { get; set; } = 2;    // late: builds Assimilation army

    [JsonPropertyName("early_move_natives_priority")]
    public int EarlyMoveNativesPriority { get; set; } = 5;    // early: low (few natives to reposition)

    [JsonPropertyName("late_move_natives_priority")]
    public int LateMoveNativesPriority { get; set; } = 3;     // late: critical (garrison before March)

    // ── Bottom Play Weights ───────────────────────────────────────────────────
    [JsonPropertyName("bottom_damage_weight")]
    public int BottomDamageWeight { get; set; } = 100;

    [JsonPropertyName("bottom_fear_weight")]
    public int BottomFearWeight { get; set; } = 60;

    [JsonPropertyName("bottom_cleanse_weight")]
    public int BottomCleanseWeight { get; set; } = 90;

    [JsonPropertyName("bottom_presence_weight")]
    public int BottomPresenceWeight { get; set; } = 50;

    [JsonPropertyName("bottom_weave_weight")]
    public int BottomWeaveWeight { get; set; } = 40;

    [JsonPropertyName("bottom_spawn_natives_weight")]
    public int BottomSpawnNativesWeight { get; set; } = 65;   // between fear (60) and cleanse (90)

    [JsonPropertyName("bottom_push_invaders_weight")]
    public int BottomPushInvadersWeight { get; set; } = 75;   // buy time when invaders are near Heart

    // ── Targeting ─────────────────────────────────────────────────────────────
    [JsonPropertyName("targeting")]
    public TargetingParams Targeting { get; set; } = new();

    // ── Rest ──────────────────────────────────────────────────────────────────
    [JsonPropertyName("voluntary_rest_min_elements")]
    public int VoluntaryRestMinElements { get; set; } = 8;   // element pool >= this AND hand thin → consider rest

    [JsonPropertyName("voluntary_rest_max_hand_size")]
    public int VoluntaryRestMaxHandSize { get; set; } = 2;   // only rest if hand is this small or smaller

    // ── Serialization ─────────────────────────────────────────────────────────
    private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

    public static StrategyParams FromJson(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<StrategyParams>(json, _jsonOpts) ?? new StrategyParams();
    }

    public void ToJson(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, JsonSerializer.Serialize(this, _jsonOpts));
    }

    public StrategyParams Clone()
    {
        var json = JsonSerializer.Serialize(this, _jsonOpts);
        return JsonSerializer.Deserialize<StrategyParams>(json, _jsonOpts)!;
    }

    // ── Hill-Climber Support ──────────────────────────────────────────────────

    /// <summary>All parameter names that the hill-climber can perturb.</summary>
    public static IReadOnlyList<string> PerturbableParams { get; } = new[]
    {
        "PhaseTransitionTide", "SpreadTarget", "StackTarget", "PreferTallOverWide",
        "EarlyPresencePriority", "EarlyDamagePriority", "EarlyCleansePriority",
        "EarlyFearPriority", "EarlyWeavePriority", "EarlyPassiveUnlockPriority",
        "EarlySpawnNativesPriority", "EarlyMoveNativesPriority",
        "LateDamagePriority", "LateCleansePriority", "LatePresencePriority",
        "LateFearPriority", "LateWeavePriority", "LatePassiveUnlockPriority",
        "LateSpawnNativesPriority", "LateMoveNativesPriority",
        "DamageUrgencyInvaderCount", "CleanseUrgencyCorruption", "WeaveUrgencyThreshold", "HeartThreatTide",
        "BottomDamageWeight", "BottomFearWeight", "BottomCleanseWeight", "BottomPresenceWeight", "BottomWeaveWeight",
        "BottomSpawnNativesWeight", "BottomPushInvadersWeight",
        "Targeting.PreferKillsOverDamage", "Targeting.PreferArrivalRow", "Targeting.ThreatRowWeight",
        "Targeting.TargetWeakestFirst", "Targeting.PresencePreferStack", "Targeting.PresencePreferThreshold",
        "Targeting.CleansePreferNearThreshold", "Targeting.CleansePreferHighest",
        "Targeting.AshT1PreferMostInvaders", "Targeting.AshT3PreferHighPresence",
        "Targeting.RootT1PreferNearThreshold", "Targeting.RootT2PreferFrontline",
        "Targeting.ProvocationPreferMostInvaders", "Targeting.ProvocationPreferHeartProximity",
        "Targeting.ProvocationPreferMostNatives",
    };

    /// <summary>Returns a clone with one parameter perturbed by ±1 (or flipped for booleans).</summary>
    public StrategyParams WithPerturbation(string paramName, Random rng)
    {
        var copy = Clone();
        copy.ApplyPerturbation(paramName, rng);
        return copy;
    }

    /// <summary>Returns a clone with multiple random parameters shaken by ±1–3.</summary>
    public StrategyParams WithShake(Random rng, int paramCount = 4)
    {
        var copy = Clone();
        for (int i = 0; i < paramCount; i++)
        {
            var paramName = PerturbableParams[rng.Next(PerturbableParams.Count)];
            int magnitude = rng.Next(1, 4);
            for (int j = 0; j < magnitude; j++)
                copy.ApplyPerturbation(paramName, rng);
        }
        return copy;
    }

    private void ApplyPerturbation(string paramName, Random rng)
    {
        int dir = rng.Next(2) == 0 ? 1 : -1;
        switch (paramName)
        {
            case "PhaseTransitionTide":         PhaseTransitionTide         = Clamp(PhaseTransitionTide         + dir, 1, 6);  break;
            case "SpreadTarget":                SpreadTarget                = Clamp(SpreadTarget                + dir, 1, 6);  break;
            case "StackTarget":                 StackTarget                 = Clamp(StackTarget                 + dir, 1, 6);  break;
            case "PreferTallOverWide":          PreferTallOverWide          = !PreferTallOverWide;                             break;
            case "EarlyPresencePriority":       EarlyPresencePriority       = Clamp(EarlyPresencePriority       + dir, 1, 6);  break;
            case "EarlyDamagePriority":         EarlyDamagePriority         = Clamp(EarlyDamagePriority         + dir, 1, 6);  break;
            case "EarlyCleansePriority":        EarlyCleansePriority        = Clamp(EarlyCleansePriority        + dir, 1, 6);  break;
            case "EarlyFearPriority":           EarlyFearPriority           = Clamp(EarlyFearPriority           + dir, 1, 6);  break;
            case "EarlyWeavePriority":          EarlyWeavePriority          = Clamp(EarlyWeavePriority          + dir, 1, 6);  break;
            case "EarlyPassiveUnlockPriority":  EarlyPassiveUnlockPriority  = Clamp(EarlyPassiveUnlockPriority  + dir, 1, 6);  break;
            case "LateDamagePriority":          LateDamagePriority          = Clamp(LateDamagePriority          + dir, 1, 6);  break;
            case "LateCleansePriority":         LateCleansePriority         = Clamp(LateCleansePriority         + dir, 1, 6);  break;
            case "LatePresencePriority":        LatePresencePriority        = Clamp(LatePresencePriority        + dir, 1, 6);  break;
            case "LateFearPriority":            LateFearPriority            = Clamp(LateFearPriority            + dir, 1, 6);  break;
            case "LateWeavePriority":           LateWeavePriority           = Clamp(LateWeavePriority           + dir, 1, 6);  break;
            case "LatePassiveUnlockPriority":   LatePassiveUnlockPriority   = Clamp(LatePassiveUnlockPriority   + dir, 1, 6);  break;
            case "DamageUrgencyInvaderCount":   DamageUrgencyInvaderCount   = Clamp(DamageUrgencyInvaderCount   + dir, 1, 6);  break;
            case "CleanseUrgencyCorruption":    CleanseUrgencyCorruption    = Clamp(CleanseUrgencyCorruption    + dir, 3, 14); break;
            case "WeaveUrgencyThreshold":       WeaveUrgencyThreshold       = Clamp(WeaveUrgencyThreshold       + dir, 5, 18); break;
            case "HeartThreatTide":             HeartThreatTide             = Clamp(HeartThreatTide             + dir, 1, 6);  break;
            case "EarlySpawnNativesPriority":   EarlySpawnNativesPriority   = Clamp(EarlySpawnNativesPriority   + dir, 1, 6);  break;
            case "LateSpawnNativesPriority":    LateSpawnNativesPriority    = Clamp(LateSpawnNativesPriority    + dir, 1, 6);  break;
            case "EarlyMoveNativesPriority":    EarlyMoveNativesPriority    = Clamp(EarlyMoveNativesPriority    + dir, 1, 6);  break;
            case "LateMoveNativesPriority":     LateMoveNativesPriority     = Clamp(LateMoveNativesPriority     + dir, 1, 6);  break;
            case "BottomDamageWeight":          BottomDamageWeight          = Clamp(BottomDamageWeight          + dir * 10, 50,  150); break;
            case "BottomFearWeight":            BottomFearWeight            = Clamp(BottomFearWeight            + dir * 10, 20,  100); break;
            case "BottomCleanseWeight":         BottomCleanseWeight         = Clamp(BottomCleanseWeight         + dir * 10, 30,  120); break;
            case "BottomPresenceWeight":        BottomPresenceWeight        = Clamp(BottomPresenceWeight        + dir * 10, 10,  90);  break;
            case "BottomWeaveWeight":           BottomWeaveWeight           = Clamp(BottomWeaveWeight           + dir * 10, 10,  80);  break;
            case "BottomSpawnNativesWeight":    BottomSpawnNativesWeight    = Clamp(BottomSpawnNativesWeight    + dir * 10, 10,  100); break;
            case "BottomPushInvadersWeight":    BottomPushInvadersWeight    = Clamp(BottomPushInvadersWeight    + dir * 10, 10,  100); break;
            // Targeting sub-params
            case "Targeting.PreferKillsOverDamage":    Targeting.PreferKillsOverDamage    = !Targeting.PreferKillsOverDamage;    break;
            case "Targeting.PreferArrivalRow":         Targeting.PreferArrivalRow         = !Targeting.PreferArrivalRow;         break;
            case "Targeting.ThreatRowWeight":          Targeting.ThreatRowWeight          = Clamp(Targeting.ThreatRowWeight     + dir, 1, 5); break;
            case "Targeting.TargetWeakestFirst":       Targeting.TargetWeakestFirst       = !Targeting.TargetWeakestFirst;       break;
            case "Targeting.PresencePreferStack":      Targeting.PresencePreferStack      = !Targeting.PresencePreferStack;      break;
            case "Targeting.PresencePreferThreshold":  Targeting.PresencePreferThreshold  = !Targeting.PresencePreferThreshold;  break;
            case "Targeting.CleansePreferNearThreshold": Targeting.CleansePreferNearThreshold = !Targeting.CleansePreferNearThreshold; break;
            case "Targeting.CleansePreferHighest":     Targeting.CleansePreferHighest     = !Targeting.CleansePreferHighest;     break;
            case "Targeting.AshT1PreferMostInvaders":  Targeting.AshT1PreferMostInvaders  = !Targeting.AshT1PreferMostInvaders;  break;
            case "Targeting.AshT3PreferHighPresence":  Targeting.AshT3PreferHighPresence  = !Targeting.AshT3PreferHighPresence;  break;
            case "Targeting.RootT1PreferNearThreshold": Targeting.RootT1PreferNearThreshold = !Targeting.RootT1PreferNearThreshold; break;
            case "Targeting.RootT2PreferFrontline":    Targeting.RootT2PreferFrontline    = !Targeting.RootT2PreferFrontline;    break;
            case "Targeting.ProvocationPreferMostInvaders":   Targeting.ProvocationPreferMostInvaders   = !Targeting.ProvocationPreferMostInvaders;   break;
            case "Targeting.ProvocationPreferHeartProximity": Targeting.ProvocationPreferHeartProximity = !Targeting.ProvocationPreferHeartProximity; break;
            case "Targeting.ProvocationPreferMostNatives":    Targeting.ProvocationPreferMostNatives    = !Targeting.ProvocationPreferMostNatives;    break;
        }
    }

    private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
}
