namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public class ShieldNativesEffect : IEffect
{
    private readonly EffectData _data;
    public ShieldNativesEffect(EffectData data) => _data = data;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null) return;

        // Shield does not stack — highest value wins.
        foreach (var native in territory.Natives.Where(n => n.IsAlive))
            native.ShieldValue = Math.Max(native.ShieldValue, _data.Value);
    }
}
