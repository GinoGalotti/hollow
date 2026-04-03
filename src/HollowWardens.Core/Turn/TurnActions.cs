namespace HollowWardens.Core.Turn;

using HollowWardens.Core.Cards;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;

/// <summary>
/// Executes player actions against an EncounterState. Called by TurnManager.
/// </summary>
public class TurnActions
{
    private readonly EncounterState _state;
    private readonly EffectResolver _resolver;

    public TurnActions(EncounterState state, EffectResolver resolver)
    {
        _state = state;
        _resolver = resolver;
    }

    public void PlayTop(Card card, TargetInfo? target = null)
    {
        _state.Deck?.PlayTop(card);
        _state.Elements?.AddElements(card.Elements, _state.Balance.TopElementMultiplier);
        ResolveEffect(card.TopEffect, target);
    }

    public void PlayBottom(Card card, TargetInfo? target = null)
    {
        _state.Deck?.PlayBottom(card, _state.Config.Tier);
        // Flame Out: permanently consumed bottoms generate a bigger element surge (3× instead of 2×)
        int bottomMult = _state.Warden?.WardenId == "ember"
            ? 3
            : _state.Balance.BottomElementMultiplier;
        _state.Elements?.AddElements(card.Elements, bottomMult);
        ResolveEffect(card.BottomEffect, target);
        if (card.BottomSecondary != null)
            ResolveEffect(card.BottomSecondary, target);
    }

    public void Rest(string? restGrowthTarget = null)
    {
        _state.Deck?.Rest();

        // D29: Warden rest behavior (e.g., Root Rest Growth)
        _state.Warden?.OnRest(_state, restGrowthTarget);
    }

    public void SkipDusk() { }

    /// <summary>
    /// D28 Sacrifice: Remove 1 Presence from the target territory and cleanse 3 Corruption.
    /// Free action — does not consume a play slot.
    /// Returns false if territory has no Presence.
    /// </summary>
    public bool SacrificePresence(string territoryId)
    {
        var territory = _state.GetTerritory(territoryId);
        if (territory == null || !territory.HasPresence) return false;

        _state.Presence?.RemovePresence(territory, _state.Balance.SacrificePresenceCost);
        _state.Corruption?.ReduceCorruption(territory, _state.Balance.SacrificeCorruptionCleanse);

        GameEvents.PresenceSacrificed?.Invoke(territory, 1);
        return true;
    }

    // ── Pairing system actions ────────────────────────────────────────────────

    /// <summary>Resolve the top effect and route the card to top-discard (pairing system).</summary>
    public void PlayPairTop(Card card, TargetInfo? target = null)
    {
        _state.Deck?.PlayAsTop(card);
        ResolveEffect(card.TopEffect, target);
    }

    /// <summary>Resolve the bottom effect(s) and route the card to bottom-discard (pairing system).</summary>
    public void PlayPairBottom(Card card, TargetInfo? target = null)
    {
        _state.Deck?.PlayAsBottom(card);
        ResolveEffect(card.BottomEffect, target);
        if (card.BottomSecondary != null)
            ResolveEffect(card.BottomSecondary, target);
    }

    /// <summary>
    /// Add elements from both pair cards: top ×1, bottom ×2. Called in the Elements phase.
    /// </summary>
    public void ExecutePairElements(CardPair pair)
    {
        _state.Elements?.AddElements(pair.TopCard.Elements, _state.Balance.TopElementMultiplier);
        _state.Elements?.AddElements(pair.BottomCard.Elements, _state.Balance.BottomElementMultiplier);
    }

    public void AssignCounterDamage(Territory territory, Dictionary<Invader, int> assignments)
        => _state.Combat?.ApplyCounterAttack(territory, assignments);

    private void ResolveEffect(EffectData data, TargetInfo? target)
    {
        try
        {
            var effect = _resolver.Resolve(data);
            effect.Resolve(_state, target ?? new TargetInfo());
        }
        catch (NotImplementedException) { /* unimplemented effect types are silently skipped */ }
    }
}
