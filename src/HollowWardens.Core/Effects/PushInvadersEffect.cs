namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Events;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;

public class PushInvadersEffect : IEffect
{
    public PushInvadersEffect(EffectData _) { }

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null) return;

        var invaders = territory.Invaders.Where(i => i.IsAlive).ToList();
        if (invaders.Count == 0) return;

        // Push to an adjacent territory that is farther from I1 (or equal distance).
        int currentDist = state.Graph.Distance(territory.Id, state.Graph.HeartId);
        var pushTargetId = state.Graph.GetNeighbors(territory.Id)
            .Where(n => state.Graph.Distance(n, state.Graph.HeartId) >= currentDist)
            .OrderByDescending(n => state.Graph.Distance(n, state.Graph.HeartId))
            .FirstOrDefault();

        if (pushTargetId == null) return;
        var dest = state.GetTerritory(pushTargetId);
        if (dest == null) return;

        foreach (var invader in invaders)
        {
            territory.Invaders.Remove(invader);
            invader.TerritoryId = pushTargetId;
            dest.Invaders.Add(invader);
            GameEvents.InvaderAdvanced?.Invoke(invader, territory.Id, pushTargetId);
        }
    }
}
