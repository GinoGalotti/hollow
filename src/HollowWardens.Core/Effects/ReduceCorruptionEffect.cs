namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public class ReduceCorruptionEffect : IEffect
{
    private readonly EffectData _data;
    public ReduceCorruptionEffect(EffectData data) => _data = data;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null) return;
        state.Corruption?.ReduceCorruption(territory, _data.Value);
    }
}
