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
}
