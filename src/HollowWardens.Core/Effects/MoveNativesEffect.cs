namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

/// <summary>
/// Moves up to {value} natives from the target territory to the best adjacent territory.
/// Best destination = adjacent territory with the most alive invaders (to enable counter-attacks),
/// tiebreak by closest to Heart (higher threat territory).
/// </summary>
public class MoveNativesEffect : IEffect
{
    private readonly int _count;

    public MoveNativesEffect(EffectData data) => _count = data.Value;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var source = state.GetTerritory(target.TerritoryId);
        if (source == null) return;

        var aliveNatives = source.Natives.Where(n => n.IsAlive).ToList();
        if (aliveNatives.Count == 0) return;

        // Pick adjacent territory with most invaders; tiebreak by proximity to Heart
        var destId = state.Graph.GetNeighbors(source.Id)
            .Select(n => state.GetTerritory(n))
            .Where(t => t != null)
            .OrderByDescending(t => t!.Invaders.Count(i => i.IsAlive))
            .ThenBy(t => state.Graph.Distance(t!.Id, state.Graph.HeartId))
            .FirstOrDefault()?.Id;

        if (destId == null) return;
        var dest = state.GetTerritory(destId);
        if (dest == null) return;

        int moved = 0;
        foreach (var native in aliveNatives)
        {
            if (moved >= _count) break;
            source.Natives.Remove(native);
            native.TerritoryId = destId;
            dest.Natives.Add(native);
            moved++;
        }
    }
}
