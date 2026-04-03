namespace HollowWardens.Core.Run;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Turn;

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

    // ── Pairing system extensions ─────────────────────────────────────────────

    /// <summary>
    /// True when this strategy uses the pairing system. EncounterRunner uses this as the gate
    /// to enter the pairing loop instead of checking whether ChoosePair returns non-null.
    /// </summary>
    bool UsesPairingSystem => false;

    /// <summary>
    /// Choose a pair of cards for the turn. Return null to fall back to legacy ChooseTopPlay/ChooseBottomPlay.
    /// </summary>
    CardPair? ChoosePair(IReadOnlyList<Card> hand, EncounterState state) => null;

    /// <summary>Should the bot take a rest turn? Default: rest when hand has 0–2 playable cards.</summary>
    bool ShouldRest(IReadOnlyList<Card> hand, EncounterState state)
        => hand.Count(c => !c.IsDormant) <= 2;

    /// <summary>
    /// Should the bot reroll this dissolved card (pay 2 weave to swap it for a random survivor)?
    /// Default: reroll if card bottom value > 8 and weave > 6.
    /// </summary>
    bool ShouldReroll(Card dissolved, EncounterState state)
        => dissolved.BottomEffect.Value > 8 && (state.Weave?.CurrentWeave ?? 0) > 6;

    /// <summary>
    /// Should Root use Elemental Offering on this card this cycle? Default: never.
    /// </summary>
    bool ShouldUseOffering(Card card, IReadOnlyList<Card> hand, EncounterState state) => false;
}
