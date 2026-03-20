namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public class PlacePresenceEffect : IEffect
{
    private readonly EffectData _data;
    public PlacePresenceEffect(EffectData data) => _data = data;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null) return;
        state.Presence?.PlacePresence(territory, _data.Value > 0 ? _data.Value : 1);
    }
}
