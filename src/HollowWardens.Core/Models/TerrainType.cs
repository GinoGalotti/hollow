namespace HollowWardens.Core.Models;

/// <summary>
/// Terrain types for territories. Every terrain creates trade-offs — bonuses for both player AND invaders.
/// Plains is the default/neutral state.
/// </summary>
public enum TerrainType
{
    Plains,     // No special effect — default state
    Forest,     // Player damage +1 / Invader Ravage corruption +1
    Mountain,   // Fear generation +2 / Invader counter-attacks +1 to natives
    Wetland,    // Corruption thresholds +2 / Invaders heal 1 HP on Rest tides
    Sacred,     // Cannot be corrupted past L1 / Invaders that Settle desecrate it (→ Blighted)
    Scorched,   // Invaders entering take 2 damage / Natives can't spawn; presence costs +1
    Blighted,   // No player bonus / +1 corruption per tide; all card effects -1
    Ruins,      // Card effects here ×1.5 / Invaders resting here gain +2 HP
    Fertile,    // Natives: +1 HP, +1 damage / Invaders: +1 HP on arrival — trampled to Plains when 3+ invaders
}
