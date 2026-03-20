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
        int currentDist = TerritoryGraph.Distance(territory.Id, "I1");
        var pushTargetId = TerritoryGraph.GetNeighbors(territory.Id)
            .Where(n => TerritoryGraph.Distance(n, "I1") >= currentDist)
            .OrderByDescending(n => TerritoryGraph.Distance(n, "I1"))
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
