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

        // D28/D31 Vulnerability: block Presence placement at the warden's tolerance threshold.
        // Default = Level 2 (Defiled). Ember overrides to Level 3 (only Desecrated blocks).
        int blockLevel = state.Warden?.PresenceBlockLevel() ?? 2;
        if (territory.CorruptionLevel >= blockLevel) return;

        state.Presence?.PlacePresence(territory, _data.Value > 0 ? _data.Value : 1);

        if (state.Config.PresencePlacementCorruptionCost > 0)
            state.Corruption?.AddCorruption(territory, state.Config.PresencePlacementCorruptionCost);
    }
}
