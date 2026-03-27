namespace HollowWardens.Core.Run;

/// <summary>
/// Warden-specific default StrategyParams. Use these as the starting point for
/// the hill-climber optimizer, or as the "smart" bot defaults without optimization.
/// </summary>
public static class StrategyDefaults
{
    /// <summary>
    /// Root warden defaults. Prioritises early presence + passive unlock, then shifts to
    /// cleanse/damage in late game. Wide presence setup (SpreadTarget 3) with tall stacking
    /// (StackTarget 3) for Assimilation. M-row is the choke point.
    /// </summary>
    public static StrategyParams Root => new()
    {
        PhaseTransitionTide       = 3,
        SpreadTarget              = 3,
        StackTarget               = 3,
        PreferTallOverWide        = true,

        EarlyPresencePriority     = 1,
        EarlyDamagePriority       = 4,
        EarlyFearPriority         = 2,
        EarlyCleansePriority      = 5,
        EarlyWeavePriority        = 6,
        EarlyPassiveUnlockPriority = 2,

        LateDamagePriority        = 2,
        LateCleansePriority       = 1,
        LateFearPriority          = 3,
        LatePresencePriority      = 4,
        LateWeavePriority         = 5,
        LatePassiveUnlockPriority = 6,

        DamageUrgencyInvaderCount = 2,
        CleanseUrgencyCorruption  = 5,
        WeaveUrgencyThreshold     = 12,
        HeartThreatTide           = 4,

        EarlySpawnNativesPriority = 3,    // mid-priority early (needs invaders to matter)
        LateSpawnNativesPriority  = 2,    // high-priority late (builds Assimilation army)
        EarlyMoveNativesPriority  = 5,    // low early (few natives to reposition)
        LateMoveNativesPriority   = 3,    // mid-priority late (reposition garrison before March)

        BottomDamageWeight        = 100,
        BottomFearWeight          = 60,
        BottomCleanseWeight       = 90,
        BottomPresenceWeight      = 50,
        BottomWeaveWeight         = 40,
        BottomSpawnNativesWeight  = 65,   // between fear (60) and cleanse (90)
        BottomPushInvadersWeight  = 75,   // buy time when invaders near Heart

        Targeting = new()
        {
            PreferKillsOverDamage     = true,
            PreferArrivalRow          = false,   // Root prefers M-row choke point
            ThreatRowWeight           = 2,
            TargetWeakestFirst        = true,
            PresencePreferStack       = true,    // tall presence for Assimilation
            PresencePreferThreshold   = true,
            CleansePreferNearThreshold = true,
            CleansePreferPresence     = true,
            RootT1PreferNearThreshold = true,
            RootT2PreferFrontline     = true,
            AshT1PreferMostInvaders   = true,
            AshT3PreferHighPresence   = true,
            ProvocationPreferMostInvaders   = true,
            ProvocationPreferHeartProximity = true,
            ProvocationPreferMostNatives    = false,
        }
    };

    /// <summary>
    /// Ember warden defaults. Fast engine (phase transition at tide 2), wide presence
    /// (Ash Trail hits all presence territories), damage-first from early game.
    /// Tolerates high corruption as fuel — only panics near L3 (15 pts).
    /// </summary>
    public static StrategyParams Ember => new()
    {
        PhaseTransitionTide       = 2,   // Ember's engine starts fast
        SpreadTarget              = 3,
        StackTarget               = 2,   // Ember doesn't need tall stacks
        PreferTallOverWide        = false, // wide for Ash Trail

        EarlyPresencePriority     = 1,
        EarlyDamagePriority       = 2,
        EarlyFearPriority         = 3,
        EarlyCleansePriority      = 5,   // Ember ignores early corruption (it's fuel)
        EarlyWeavePriority        = 6,
        EarlyPassiveUnlockPriority = 3,

        LateDamagePriority        = 1,
        LateCleansePriority       = 3,   // Ember cleanses late only to avoid Desecration
        LateFearPriority          = 2,
        LatePresencePriority      = 4,
        LateWeavePriority         = 5,
        LatePassiveUnlockPriority = 6,

        DamageUrgencyInvaderCount = 3,   // Ember tolerates more invaders (thresholds handle them)
        CleanseUrgencyCorruption  = 12,  // Ember only panics near L3 (15 pts)
        WeaveUrgencyThreshold     = 10,
        HeartThreatTide           = 5,   // Ember's threshold engine usually clears M-row passively

        EarlySpawnNativesPriority = 6,    // irrelevant for Ember (no native mechanics)
        LateSpawnNativesPriority  = 6,
        EarlyMoveNativesPriority  = 6,
        LateMoveNativesPriority   = 6,

        BottomDamageWeight        = 100,
        BottomFearWeight          = 70,
        BottomCleanseWeight       = 80,
        BottomPresenceWeight      = 50,
        BottomWeaveWeight         = 35,
        BottomSpawnNativesWeight  = 10,   // Ember has no native kit
        BottomPushInvadersWeight  = 55,   // generic utility — lower than Ember's damage/fear emphasis

        Targeting = new()
        {
            PreferKillsOverDamage       = true,
            PreferArrivalRow            = true,  // Ember kills at arrival point
            ThreatRowWeight             = 1,     // no special bias — thresholds are board-wide
            TargetWeakestFirst          = true,
            PresencePreferStack         = false, // wide for Ash Trail
            PresencePreferThreshold     = false,
            CleansePreferHighest        = true,  // Ember cleanses the worst territory to prevent L3
            CleansePreferNearThreshold  = false,
            CleansePreferPresence       = false,
            AshT1PreferMostInvaders     = true,
            AshT2PreferHighCorruption   = false, // avoid adding to already-corrupted
            AshT3PreferHighPresence     = true,
            ProvocationPreferMostInvaders   = true,   // defaults not meaningful for Ember
            ProvocationPreferHeartProximity = true,
            ProvocationPreferMostNatives    = false,
        }
    };
}
