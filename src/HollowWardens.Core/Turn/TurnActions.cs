namespace HollowWardens.Core.Turn;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
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
        _state.Elements?.AddElements(card.Elements, 1);
        ResolveEffect(card.TopEffect, target);
    }

    public void PlayBottom(Card card, TargetInfo? target = null)
    {
        _state.Deck?.PlayBottom(card, _state.Config.Tier);
        _state.Elements?.AddElements(card.Elements, 2);
        ResolveEffect(card.BottomEffect, target);
        if (card.BottomSecondary != null)
            ResolveEffect(card.BottomSecondary, target);
    }

    public void Rest() => _state.Deck?.Rest();

    public void SkipDusk() { }

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
