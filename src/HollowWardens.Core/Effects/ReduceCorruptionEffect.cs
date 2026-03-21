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

        // D28: Presence amplification — +1 cleanse per Presence in target territory
        var amount = AmplificationHelper.GetAmplifiedValue(_data.Value, state, target.TerritoryId);

        state.Corruption?.ReduceCorruption(territory, amount);
    }
}
