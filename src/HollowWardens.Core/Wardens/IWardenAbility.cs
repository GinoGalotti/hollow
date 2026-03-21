namespace HollowWardens.Core.Wardens;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public interface IWardenAbility
{
    string WardenId { get; }
    BottomResult OnBottomPlayed(Card card, EncounterTier tier);
    BottomResult OnRestDissolve(Card card);
    void OnResolution(EncounterState state);  // warden-specific resolution behavior
    int CalculatePassiveFear();  // e.g., Root's network fear

    // D29: Network Slow — movement penalty for invaders near dense presence
    int GetMovementPenalty(string territoryId, IEnumerable<Territory> allTerritories) => 0;

    // D29: Presence Provocation — do natives in this territory counter-attack on all actions?
    bool ProvokesNatives(Territory territory) => false;

    // D29: Warden-specific rest behavior (e.g., Root places free presence)
    void OnRest(EncounterState state, string? targetTerritoryId) { }
}
