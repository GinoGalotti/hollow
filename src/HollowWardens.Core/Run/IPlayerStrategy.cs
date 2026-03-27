namespace HollowWardens.Core.Run;

using HollowWardens.Core.Effects;
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

    // D29: Choose target territory for Rest Growth (null = skip)
    string? ChooseRestGrowthTarget(EncounterState state) => null;

    /// <summary>Choose a target territory for a targeted effect. Return null for untargeted effects.</summary>
    string? ChooseTarget(EffectData effect, EncounterState state) => null;

    /// <summary>
    /// D41: Resolve all pending threshold effects after a play phase completes.
    /// Default: auto-resolve all with no territory selection (bot/sim behavior).
    /// Interactive implementations override this to leave pending entries for player input.
    /// </summary>
    void ResolvePendingThresholds(ThresholdResolver resolver, EncounterState state)
        => resolver.AutoResolveAll(state);

    /// <summary>
    /// Rank presence territories for Provocation selection (highest priority first).
    /// Called by RootAbility when ProvocationTerritoryLimit > 0 to pick which territories
    /// have active Provocation this tide.
    /// Return null to use RootAbility's default heuristic (most invaders → closest to Heart → most natives).
    /// </summary>
    IEnumerable<string>? RankProvocationTerritories(IReadOnlyList<Territory> candidates, EncounterState state) => null;
}
