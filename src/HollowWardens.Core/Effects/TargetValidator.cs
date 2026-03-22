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

    // These types always require a territory target, even at Range 0 (global reach = any territory).
    private static readonly HashSet<EffectType> TerritoryAtAnyRangeTypes = new()
    {
        EffectType.DamageInvaders,
        EffectType.ReduceCorruption,
        EffectType.ShieldNatives,
        EffectType.BoostNatives,
        EffectType.SlowInvaders,
    };

    /// <summary>
    /// Returns true when this effect requires the player to select a territory
    /// before resolution. True for Range > 0 (non-global types) and for
    /// territory-targeting types that work at any range (including Range 0 = global).
    /// </summary>
    public static bool NeedsTarget(EffectData effect)
        => !NoTargetTypes.Contains(effect.Type) &&
           (effect.Range > 0 || TerritoryAtAnyRangeTypes.Contains(effect.Type));

    /// <summary>
    /// Returns the IDs of all territories valid for the given effect.
    /// For territory-targeting types at Range 0: returns all relevant territories on the board
    /// (global reach — e.g. "any territory" for DamageInvaders, ReduceCorruption).
    /// For Range > 0: territories reachable within range steps from any presence token.
    /// Returns an empty list when no presence exists and range > 0.
    /// </summary>
    public static List<string> GetValidTargets(EncounterState state, int range, EffectType? effectType = null)
    {
        var result = new HashSet<string>();

        // Range 0 for territory-targeting types = global reach (any territory on the board)
        bool isGlobal = range == 0 && effectType.HasValue && TerritoryAtAnyRangeTypes.Contains(effectType.Value);
        if (isGlobal)
        {
            result.UnionWith(state.Graph.AllTerritoryIds);
        }
        else
        {
            foreach (var territory in state.Territories.Where(t => t.HasPresence))
            {
                foreach (var id in state.Graph.AllTerritoryIds)
                {
                    if (state.Graph.Distance(territory.Id, id) <= range)
                        result.Add(id);
                }
            }
        }

        var list = result.ToList();
        if (effectType == EffectType.ReduceCorruption)
            list = list.Where(id =>
            {
                var t = state.GetTerritory(id);
                return t != null && t.CorruptionPoints > 0;
            }).ToList();
        else if (effectType == EffectType.DamageInvaders || effectType == EffectType.SlowInvaders)
            list = list.Where(id => state.GetTerritory(id)?.Invaders.Any(i => i.IsAlive) == true).ToList();
        else if (effectType == EffectType.ShieldNatives || effectType == EffectType.BoostNatives)
            list = list.Where(id => state.GetTerritory(id)?.Natives.Any(n => n.IsAlive) == true).ToList();
        return list;
    }
}
