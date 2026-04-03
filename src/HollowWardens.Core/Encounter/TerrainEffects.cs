namespace HollowWardens.Core.Encounter;

using HollowWardens.Core.Models;

/// <summary>
/// Static helpers that return terrain-based modifiers. All terrain effects are trade-offs:
/// bonuses apply to both the player AND invaders. Plains always returns 0 / false (neutral).
/// </summary>
public static class TerrainEffects
{
    /// <summary>Forest: all damage effects +1 for both player AND invaders.</summary>
    public static int GetDamageModifier(TerrainType terrain)
        => terrain == TerrainType.Forest ? 1 : 0;

    /// <summary>Mountain: fear generation +2. Blighted: effects -1 (applied as negative modifier).</summary>
    public static int GetFearModifier(TerrainType terrain) => terrain switch
    {
        TerrainType.Mountain => 2,
        TerrainType.Blighted => -1,
        _ => 0
    };

    /// <summary>
    /// Wetland: corruption threshold +2 (slower to corrupt — both thresholds shift up).
    /// Returns the number of points to ADD to each corruption level threshold.
    /// </summary>
    public static int GetCorruptionThresholdModifier(TerrainType terrain)
        => terrain == TerrainType.Wetland ? 2 : 0;

    /// <summary>Scorched: invaders entering take 2 damage (entry damage).</summary>
    public static int GetInvaderEntryDamage(TerrainType terrain)
        => terrain == TerrainType.Scorched ? 2 : 0;

    /// <summary>
    /// Sacred: cannot be corrupted past L1 (max corruption level = 1).
    /// Returns the maximum allowed corruption level; int.MaxValue = no cap.
    /// </summary>
    public static int GetCorruptionMaxLevel(TerrainType terrain)
        => terrain == TerrainType.Sacred ? 1 : int.MaxValue;

    /// <summary>Forest: invader Ravage adds +1 corruption (the cost of fighting in the forest).</summary>
    public static int GetInvaderRavageCorruptionModifier(TerrainType terrain)
        => terrain == TerrainType.Forest ? 1 : 0;

    /// <summary>Wetland: invaders heal 1 HP on Rest tides (harder to kill camped invaders).</summary>
    public static int GetInvaderRestHeal(TerrainType terrain)
        => terrain == TerrainType.Wetland ? 1 : 0;

    /// <summary>Mountain: invader counter-attacks deal +1 damage to natives.</summary>
    public static int GetInvaderCounterAttackModifier(TerrainType terrain)
        => terrain == TerrainType.Mountain ? 1 : 0;

    /// <summary>Scorched: natives cannot spawn here; Blighted: presence costs +1.</summary>
    public static bool CanSpawnNatives(TerrainType terrain)
        => terrain != TerrainType.Scorched;

    /// <summary>
    /// Blighted: all card effects in this territory -1. Applied as a modifier to effect values.
    /// </summary>
    public static int GetEffectValueModifier(TerrainType terrain)
        => terrain == TerrainType.Blighted ? -1 : 0;

    /// <summary>
    /// Blighted: auto-corrupts +1 per tide.
    /// </summary>
    public static int GetAutoCorruptionPerTide(TerrainType terrain)
        => terrain == TerrainType.Blighted ? 1 : 0;
}
