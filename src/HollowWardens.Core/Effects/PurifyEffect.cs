namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public class PurifyEffect : IEffect
{
    public PurifyEffect(EffectData _) { }

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null) return;
        state.Corruption?.PurifyLevel(territory);
    }
}
