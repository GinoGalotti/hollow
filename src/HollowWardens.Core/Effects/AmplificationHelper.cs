namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;

/// <summary>
/// D28: Presence amplification. Every card effect targeting a territory with Presence
/// gets +1 value per Presence token there. Opt-in per effect — only effects whose
/// "value" represents a scalable quantity (damage, corruption reduction, shield)
/// call this helper.
/// </summary>
public static class AmplificationHelper
{
    /// <summary>
    /// Returns baseValue + territory.PresenceCount for the target territory.
    /// Returns baseValue unchanged if territory is null or has no Presence.
    /// </summary>
    public static int GetAmplifiedValue(int baseValue, EncounterState state, string territoryId)
    {
        var territory = state.GetTerritory(territoryId);
        if (territory == null || !territory.HasPresence) return baseValue;
        return baseValue + territory.PresenceCount;
    }
}
