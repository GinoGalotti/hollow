namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public class BoostNativesEffect : IEffect
{
    private readonly EffectData _data;
    public BoostNativesEffect(EffectData data) => _data = data;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null) return;

        foreach (var native in territory.Natives.Where(n => n.IsAlive))
            native.Damage += _data.Value;
    }
}
