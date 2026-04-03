namespace HollowWardens.Core.Encounter;

using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;

/// <summary>
/// Data-driven terrain transition rules. Call <see cref="CheckTransitions"/> at the end of each
/// Tide to evaluate all active territories for state changes.
/// </summary>
public static class TerrainTransitions
{
    private const int ScorchedCleanTidesRequired = 3;
    private const int FertileInvaderThreshold = 3;

    /// <summary>
    /// Evaluate and apply terrain transitions for a single territory at Tide end.
    /// Transitions are deterministic — no randomness required.
    /// </summary>
    public static bool CheckTransitions(Territory territory)
    {
        var before = territory.Terrain;

        switch (territory.Terrain)
        {
            case TerrainType.Forest:
                // Forest → Scorched when corruption reaches L2
                if (territory.CorruptionLevel >= 2)
                    territory.Terrain = TerrainType.Scorched;
                break;

            case TerrainType.Mountain:
                // Mountain → Ruins when corruption reaches L3
                if (territory.CorruptionLevel >= 3)
                    territory.Terrain = TerrainType.Ruins;
                break;

            case TerrainType.Blighted:
                // Blighted → Plains when corruption is cleansed to 0
                if (territory.CorruptionPoints == 0)
                    territory.Terrain = TerrainType.Plains;
                break;

            case TerrainType.Scorched:
                // Scorched → Plains after 3 consecutive clean tides (0 corruption)
                if (territory.CorruptionPoints == 0)
                {
                    territory.TerrainTimer++;
                    if (territory.TerrainTimer >= ScorchedCleanTidesRequired)
                    {
                        territory.Terrain = TerrainType.Plains;
                        territory.TerrainTimer = 0;
                    }
                }
                else
                {
                    territory.TerrainTimer = 0; // reset if corruption returns
                }
                break;

            case TerrainType.Fertile:
                // Fertile → Plains when 3+ invaders present (trampled)
                if (territory.Invaders.Count(i => i.IsAlive) >= FertileInvaderThreshold)
                    territory.Terrain = TerrainType.Plains;
                break;
        }

        // Global: any territory at L3 corruption → Blighted (overrides other terrain)
        if (territory.CorruptionLevel >= 3 &&
            territory.Terrain != TerrainType.Blighted &&
            territory.Terrain != TerrainType.Ruins) // Ruins stay as ruins even at L3
        {
            territory.Terrain = TerrainType.Blighted;
        }

        return territory.Terrain != before;
    }

    /// <summary>
    /// Called when invaders Settle on Sacred ground — territory becomes Blighted (desecration).
    /// </summary>
    public static void OnInvaderSettle(Territory territory)
    {
        if (territory.Terrain == TerrainType.Sacred)
            territory.Terrain = TerrainType.Blighted;
    }

    /// <summary>
    /// Apply auto-corruption from Blighted terrain at the end of each tide.
    /// </summary>
    public static void ApplyBlightedAutoCorruption(Territory territory, ICorruptionSystem? corruption)
    {
        int auto = TerrainEffects.GetAutoCorruptionPerTide(territory.Terrain);
        if (auto > 0)
            corruption?.AddCorruption(territory, auto);
    }

    /// <summary>
    /// Apply Wetland rest-heal to invaders in a territory.
    /// </summary>
    public static void ApplyWetlandRestHeal(Territory territory)
    {
        int heal = TerrainEffects.GetInvaderRestHeal(territory.Terrain);
        if (heal <= 0) return;

        foreach (var invader in territory.Invaders.Where(i => i.IsAlive))
            invader.Hp = Math.Min(invader.MaxHp, invader.Hp + heal);
    }
}
