namespace HollowWardens.Core.Turn;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;

/// <summary>
/// State machine for a single turn: Vigil → Tide → Dusk → (Rest or next turn).
/// Detects rest conditions at Vigil start and tracks phase transitions.
/// </summary>
public class TurnManager
{
    private readonly EncounterState _state;
    private readonly TurnActions _actions;

    public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Vigil;

    /// <summary>True when StartVigil detects the draw pile is empty. Caller must call Rest().</summary>
    public bool IsRestTurn { get; private set; }

    /// <summary>Number of Rest turns taken so far this encounter.</summary>
    public int RestCount { get; private set; }

    public TurnManager(EncounterState state, EffectResolver resolver)
    {
        _state = state;
        _actions = new TurnActions(state, resolver);
    }

    /// <summary>
    /// Starts a Vigil. Refills hand, checks for rest condition, fires phase event.
    /// If IsRestTurn is true after this, caller should call Rest() and skip Tide/Dusk.
    /// </summary>
    public void StartVigil()
    {
        _state.Deck?.RefillHand();

        if (_state.Deck?.NeedsRest == true)
        {
            IsRestTurn = true;
            _state.Elements?.OnRestTurn();
            CurrentPhase = TurnPhase.Rest;
            GameEvents.PhaseChanged?.Invoke(TurnPhase.Rest);
        }
        else
        {
            IsRestTurn = false;
            _state.Elements?.OnNewTurn();
            CurrentPhase = TurnPhase.Vigil;
            GameEvents.PhaseChanged?.Invoke(TurnPhase.Vigil);
            GameEvents.TurnStarted?.Invoke();
        }
    }

    public void PlayTop(Card card, TargetInfo? target = null)
        => _actions.PlayTop(card, target);

    /// <summary>Signals end of Vigil. Caller should then run the Tide.</summary>
    public void EndVigil()
    {
        CurrentPhase = TurnPhase.Tide;
        GameEvents.PhaseChanged?.Invoke(TurnPhase.Tide);
    }

    public void StartDusk()
    {
        CurrentPhase = TurnPhase.Dusk;
        GameEvents.PhaseChanged?.Invoke(TurnPhase.Dusk);
    }

    public void PlayBottom(Card card, TargetInfo? target = null)
        => _actions.PlayBottom(card, target);

    public void SkipDusk() => _actions.SkipDusk();

    /// <summary>
    /// Ends the current turn: decays elements and fires TurnEnded.
    /// NOT called on Rest turns (decay happened at end of previous turn).
    /// </summary>
    public void EndTurn()
    {
        _state.Elements?.Decay();
        GameEvents.TurnEnded?.Invoke();
    }

    /// <summary>
    /// Executes a Rest: shuffles discards back into draw with one rest-dissolve.
    /// ElementSystem.OnRestTurn() was already called in StartVigil.
    /// </summary>
    public void Rest()
    {
        _actions.Rest();
        RestCount++;
        GameEvents.PhaseChanged?.Invoke(TurnPhase.Rest);
    }

    public void AssignCounterDamage(Territory territory, Dictionary<Invader, int> assignments)
        => _actions.AssignCounterDamage(territory, assignments);
}
