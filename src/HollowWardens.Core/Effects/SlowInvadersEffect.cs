namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

/// <summary>
/// D29: Marks all alive invaders in target territory as slowed.
/// Slowed invaders have their Advance movement halved (round down) next Tide.
/// </summary>
public class SlowInvadersEffect : IEffect
{
    private readonly EffectData _data;
    public SlowInvadersEffect(EffectData data) => _data = data;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null) return;

        foreach (var invader in territory.Invaders.Where(i => i.IsAlive))
            invader.IsSlowed = true;
    }
}
