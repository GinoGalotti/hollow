namespace HollowWardens.Core.Wardens;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public interface IWardenAbility
{
    string WardenId { get; }
    BottomResult OnBottomPlayed(Card card, EncounterTier tier);
    BottomResult OnRestDissolve(Card card);
    void OnResolution(EncounterState state);  // warden-specific resolution behavior
    int CalculatePassiveFear(EncounterState state);  // e.g., Root's network fear

    // D29: Network Slow — movement penalty for invaders near dense presence
    int GetMovementPenalty(string territoryId, IEnumerable<Territory> allTerritories) => 0;

    // D29: Presence Provocation — do natives in this territory counter-attack on all actions?
    bool ProvokesNatives(Territory territory) => false;

    // D29: Warden-specific rest behavior (e.g., Root places free presence)
    void OnRest(EncounterState state, string? targetTerritoryId) { }

    // Ember: Tide-start effect (e.g., Ash Trail — corruption + damage in presence territories)
    void OnTideStart(EncounterState state) { }

    // D31: Corruption level that blocks new Presence placement. Default = 2 (Defiled blocks).
    // Ember overrides to 3 (only Desecrated blocks — Ember lives in corrupted land).
    int PresenceBlockLevel() => 2;
}
