namespace HollowWardens.Core.Run;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

/// <summary>
/// Decides player actions during an encounter. Implement for tests or AI play.
/// </summary>
public interface IPlayerStrategy
{
    /// <summary>Called during Vigil. Return a card to play as top, or null to end Vigil.</summary>
    Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state);

    /// <summary>Called during Dusk. Return a card to play as bottom, or null to end Dusk.</summary>
    Card? ChooseBottomPlay(IReadOnlyList<Card> hand, EncounterState state);

    /// <summary>Assigns counter-attack damage. Default: auto-assign (lowest HP first).</summary>
    Dictionary<Invader, int>? AssignCounterDamage(Territory territory, int damagePool, EncounterState state);
}
