namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;

/// <summary>
/// Pure static helpers for territory targeting: which effects need an explicit
/// player target and which territories are valid choices given current presence.
/// </summary>
public static class TargetValidator
{
    // These types resolve globally regardless of Range — no territory selection needed.
    private static readonly HashSet<EffectType> NoTargetTypes = new()
    {
        EffectType.GenerateFear,
        EffectType.RestoreWeave,
        EffectType.AwakeDormant,
    };

    /// <summary>
    /// Returns true when this effect requires the player to select a territory
    /// before resolution: Range > 0 AND the type is not self-resolving globally.
    /// </summary>
    public static bool NeedsTarget(EffectData effect)
        => effect.Range > 0 && !NoTargetTypes.Contains(effect.Type);

    /// <summary>
    /// Returns the IDs of all territories reachable within <paramref name="range"/>
    /// steps from any territory that currently has at least one Presence token.
    /// Returns an empty list when no presence exists on the board.
    /// When <paramref name="effectType"/> is <see cref="EffectType.ReduceCorruption"/>,
    /// only territories with CorruptionPoints &gt; 0 are returned.
    /// </summary>
    public static List<string> GetValidTargets(EncounterState state, int range, EffectType? effectType = null)
    {
        var result = new HashSet<string>();
        foreach (var territory in state.Territories.Where(t => t.HasPresence))
        {
            foreach (var id in TerritoryGraph.AllTerritoryIds)
            {
                if (TerritoryGraph.Distance(territory.Id, id) <= range)
                    result.Add(id);
            }
        }
        var list = result.ToList();
        if (effectType == EffectType.ReduceCorruption)
            list = list.Where(id =>
            {
                var t = state.GetTerritory(id);
                return t != null && t.CorruptionPoints > 0;
            }).ToList();
        return list;
    }
}
