namespace HollowWardens.Core.Run;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

/// <summary>
/// Calculates encounter reward based on board state at resolution end.
/// Breach > Weathered > Clean, checked in that order.
/// </summary>
public static class RewardCalculator
{
    public static EncounterResult Calculate(EncounterState state)
    {
        // Breach: Weave depleted OR any territory Desecrated (level 3)
        if (state.Weave?.IsGameOver == true)
            return EncounterResult.Breach;

        if (state.Territories.Any(t => t.CorruptionLevel == 3))
            return EncounterResult.Breach;

        // Weathered: any alive invaders remain OR any corruption present
        if (state.TerritoriesWithInvaders().Any())
            return EncounterResult.Weathered;

        if (state.Territories.Any(t => t.CorruptionLevel >= 1))
            return EncounterResult.Weathered;

        return EncounterResult.Clean;
    }
}
