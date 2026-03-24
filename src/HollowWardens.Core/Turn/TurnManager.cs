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

    /// <summary>Number of top-half plays made this Vigil. Resets at the start of each turn.</summary>
    public int VigilPlaysThisTurn { get; private set; }

    /// <summary>Number of bottom-half plays made this Dusk. Resets at the start of Dusk.</summary>
    public int DuskPlaysThisTurn { get; private set; }

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
        VigilPlaysThisTurn = 0;
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

    /// <summary>
    /// Plays the top half of a card. Returns false if the Vigil play limit (2) is reached.
    /// Outside Vigil (e.g., Resolution) the limit does not apply.
    /// </summary>
    public bool PlayTop(Card card, TargetInfo? target = null)
    {
        if (CurrentPhase == TurnPhase.Vigil)
        {
            if (VigilPlaysThisTurn >= _state.Balance.VigilPlayLimit) return false;
            VigilPlaysThisTurn++;
        }
        _actions.PlayTop(card, target);
        return true;
    }

    /// <summary>Signals end of Vigil. Caller should then run the Tide.</summary>
    public void EndVigil()
    {
        CurrentPhase = TurnPhase.Tide;
        GameEvents.PhaseChanged?.Invoke(TurnPhase.Tide);
    }

    public void StartDusk()
    {
        DuskPlaysThisTurn = 0;
        CurrentPhase = TurnPhase.Dusk;
        GameEvents.PhaseChanged?.Invoke(TurnPhase.Dusk);
    }

    /// <summary>
    /// Plays the bottom half of a card. Returns false if the Dusk play limit (1) is reached.
    /// Outside Dusk the limit does not apply.
    /// </summary>
    public bool PlayBottom(Card card, TargetInfo? target = null)
    {
        if (CurrentPhase == TurnPhase.Dusk)
        {
            if (DuskPlaysThisTurn >= _state.Balance.DuskPlayLimit) return false;
            DuskPlaysThisTurn++;
        }
        _actions.PlayBottom(card, target);
        return true;
    }

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
    public void Rest(string? restGrowthTarget = null)
    {
        _actions.Rest(restGrowthTarget);
        RestCount++;
        GameEvents.PhaseChanged?.Invoke(TurnPhase.Rest);
    }

    /// <summary>True when a top play is currently allowed (Vigil phase, under the VigilPlayLimit).</summary>
    public bool CanPlayTop()
        => CurrentPhase == TurnPhase.Vigil && VigilPlaysThisTurn < _state.Balance.VigilPlayLimit;

    /// <summary>True when a bottom play is currently allowed (Dusk phase, under the DuskPlayLimit).</summary>
    public bool CanPlayBottom()
        => CurrentPhase == TurnPhase.Dusk && DuskPlaysThisTurn < _state.Balance.DuskPlayLimit;

    /// <summary>D28: True when sacrifice is allowed — Vigil or Dusk phase only.</summary>
    public bool CanSacrifice()
        => CurrentPhase == TurnPhase.Vigil || CurrentPhase == TurnPhase.Dusk;

    /// <summary>
    /// D28 Sacrifice: Remove 1 Presence → cleanse 3 Corruption. Free action (no play slot consumed).
    /// Returns false if wrong phase or territory has no Presence.
    /// </summary>
    public bool SacrificePresence(string territoryId)
    {
        if (!CanSacrifice()) return false;
        return _actions.SacrificePresence(territoryId);
    }

    /// <summary>
    /// D41: Resolve a pending elemental threshold ability. Free action — no play slot consumed.
    /// Pass <paramref name="targetTerritoryId"/> for effects that require a territory selection.
    /// </summary>
    public void UseThreshold(ThresholdResolver resolver, Element element, int tier, string? targetTerritoryId = null)
        => resolver.Resolve(element, tier, _state, targetTerritoryId);

    public void AssignCounterDamage(Territory territory, Dictionary<Invader, int> assignments)
        => _actions.AssignCounterDamage(territory, assignments);
}
