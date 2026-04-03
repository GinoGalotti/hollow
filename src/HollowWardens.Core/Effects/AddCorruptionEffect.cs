namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;

public class AddCorruptionEffect : IEffect
{
    private readonly EffectData _data;
    public AddCorruptionEffect(EffectData data) => _data = data;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null) return;

        state.Corruption?.AddCorruption(territory, _data.Value);
    }
}
