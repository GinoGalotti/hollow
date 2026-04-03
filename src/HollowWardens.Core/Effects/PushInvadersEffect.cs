namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Events;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;

public class PushInvadersEffect : IEffect
{
    private readonly int _count;

    public PushInvadersEffect(EffectData data)
        => _count = data.Value > 0 ? data.Value : int.MaxValue;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null) return;

        var invaders = territory.Invaders.Where(i => i.IsAlive).Take(_count).ToList();
        if (invaders.Count == 0) return;

        var neighbors = state.Graph.GetNeighbors(territory.Id).ToList();
        if (neighbors.Count == 0) return;

        // Auto-selection fallback: push to farthest neighbor from heart
        int currentDist = state.Graph.Distance(territory.Id, state.Graph.HeartId);
        var autoTarget = neighbors
            .Where(n => state.Graph.Distance(n, state.Graph.HeartId) >= currentDist)
            .OrderByDescending(n => state.Graph.Distance(n, state.Graph.HeartId))
            .FirstOrDefault() ?? neighbors[0];

        for (int i = 0; i < invaders.Count; i++)
        {
            var invader = invaders[i];
            // Per-invader player choice: use PushDestinations[i] if provided and valid
            string destId = autoTarget;
            if (target.PushDestinations != null && i < target.PushDestinations.Count)
            {
                var requested = target.PushDestinations[i];
                if (neighbors.Contains(requested))
                    destId = requested;
            }

            var dest = state.GetTerritory(destId);
            if (dest == null) continue;

            territory.Invaders.Remove(invader);
            invader.TerritoryId = destId;
            dest.Invaders.Add(invader);
            GameEvents.InvaderAdvanced?.Invoke(invader, territory.Id, destId);
        }
    }
}
