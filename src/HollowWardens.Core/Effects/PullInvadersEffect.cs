namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;

public class PullInvadersEffect : IEffect
{
    private readonly int _count;

    public PullInvadersEffect(EffectData data)
        => _count = data.Value > 0 ? data.Value : int.MaxValue;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null) return;

        var neighbors = state.Graph.GetNeighbors(territory.Id);
        int pulled = 0;

        int entryDamage = TerrainEffects.GetInvaderEntryDamage(territory.Terrain);

        foreach (var neighborId in neighbors)
        {
            if (pulled >= _count) break;
            var neighbor = state.GetTerritory(neighborId);
            if (neighbor == null) continue;

            var invaders = neighbor.Invaders.Where(i => i.IsAlive).ToList();
            foreach (var invader in invaders)
            {
                if (pulled >= _count) break;
                neighbor.Invaders.Remove(invader);
                invader.TerritoryId = territory.Id;
                territory.Invaders.Add(invader);
                GameEvents.InvaderAdvanced?.Invoke(invader, neighborId, territory.Id);

                // Scorched terrain: pulled invaders take entry damage on arrival
                if (entryDamage > 0)
                {
                    invader.Hp = Math.Max(0, invader.Hp - entryDamage);
                    if (!invader.IsAlive)
                    {
                        GameEvents.InvaderDefeated?.Invoke(invader);
                        state.Dread?.OnFearGenerated(1);
                        GameEvents.FearGenerated?.Invoke(1);
                    }
                }

                pulled++;
            }
        }
    }
}
