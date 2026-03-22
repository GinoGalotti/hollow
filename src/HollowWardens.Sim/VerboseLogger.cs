namespace HollowWardens.Sim;

using System.Text;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;

/// <summary>
/// Writes a detailed turn-by-turn log for a single encounter to a file.
/// Subscribe via WireEvents() before running, call Finalize() after.
/// </summary>
public class VerboseLogger
{
    private readonly EncounterState _state;
    private readonly string         _logPath;
    private readonly IPlayerStrategy _strategy;
    private readonly StringBuilder  _log = new();
    private int _currentTide;
    private string _currentPhase = "";

    public VerboseLogger(EncounterState state, string logPath, IPlayerStrategy strategy)
    {
        _state    = state;
        _logPath  = logPath;
        _strategy = strategy;
    }

    public void WireEvents()
    {
        GameEvents.EncounterStarted  += OnEncounterStarted;
        GameEvents.TideStepStarted   += OnTideStepStarted;
        GameEvents.ActionCardRevealed += OnActionCardRevealed;
        GameEvents.PhaseChanged      += OnPhaseChanged;
        GameEvents.TurnStarted       += OnTurnStarted;
        GameEvents.CardPlayed        += OnCardPlayed;
        GameEvents.InvaderDefeated   += OnInvaderDefeated;
        GameEvents.InvaderArrived    += OnInvaderArrived;
        GameEvents.InvaderAdvanced   += OnInvaderAdvanced;
        GameEvents.FearGenerated     += OnFearGenerated;
        GameEvents.FearSpent         += OnFearSpent;
        GameEvents.FearActionRevealed += OnFearActionRevealed;
        GameEvents.DreadAdvanced     += OnDreadAdvanced;
        GameEvents.CorruptionChanged += OnCorruptionChanged;
        GameEvents.WeaveChanged      += OnWeaveChanged;
        GameEvents.TideCompleted     += OnTideCompleted;
        GameEvents.EncounterEnded    += OnEncounterEnded;
        GameEvents.ThresholdTriggered += OnThresholdTriggered;
        GameEvents.ThresholdResolved  += OnThresholdResolved;
    }

    public void UnwireEvents()
    {
        GameEvents.EncounterStarted  -= OnEncounterStarted;
        GameEvents.TideStepStarted   -= OnTideStepStarted;
        GameEvents.ActionCardRevealed -= OnActionCardRevealed;
        GameEvents.PhaseChanged      -= OnPhaseChanged;
        GameEvents.TurnStarted       -= OnTurnStarted;
        GameEvents.CardPlayed        -= OnCardPlayed;
        GameEvents.InvaderDefeated   -= OnInvaderDefeated;
        GameEvents.InvaderArrived    -= OnInvaderArrived;
        GameEvents.InvaderAdvanced   -= OnInvaderAdvanced;
        GameEvents.FearGenerated     -= OnFearGenerated;
        GameEvents.FearSpent         -= OnFearSpent;
        GameEvents.FearActionRevealed -= OnFearActionRevealed;
        GameEvents.DreadAdvanced     -= OnDreadAdvanced;
        GameEvents.CorruptionChanged -= OnCorruptionChanged;
        GameEvents.WeaveChanged      -= OnWeaveChanged;
        GameEvents.TideCompleted     -= OnTideCompleted;
        GameEvents.EncounterEnded    -= OnEncounterEnded;
        GameEvents.ThresholdTriggered -= OnThresholdTriggered;
        GameEvents.ThresholdResolved  -= OnThresholdResolved;
    }

    public void Finalize(EncounterResult result)
    {
        _log.AppendLine();
        _log.AppendLine($"=== ENCOUNTER RESULT: {result} ===");
        _log.AppendLine($"Final weave: {_state.Weave?.CurrentWeave ?? 0}");
        var dir = Path.GetDirectoryName(_logPath);
        if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllText(_logPath, _log.ToString(), Encoding.UTF8);
    }

    // ── Event handlers ──────────────────────────────────────────────────────

    private void OnEncounterStarted(EncounterConfig config)
    {
        _log.AppendLine($"=== ENCOUNTER LOG — seed {_state.Random?.Seed ?? 0} ===");
        _log.AppendLine($"Warden: {_state.WardenData?.WardenId ?? "unknown"}  Tides: {config.TideCount}");
        _log.AppendLine();
    }

    private void OnTideStepStarted(TideStep step)
    {
        if (step == TideStep.FearActions)
        {
            _log.AppendLine();
            _log.AppendLine($"--- TIDE {_currentTide + 1} — FEAR STEP ---");
        }
        else
        {
            _log.AppendLine($"  [TIDE STEP: {step}]");
        }
    }

    private void OnActionCardRevealed(ActionCard card)
    {
        _currentTide++;
        _log.AppendLine($"--- TIDE {_currentTide} ---");
        _log.AppendLine($"  Action Card: {card.Name} ({card.Pool}) advance={card.AdvanceModifier}{(card.IsEscalation ? " ESCALATION" : "")}");
        LogBoardState();
        LogHand();
    }

    private void OnPhaseChanged(TurnPhase phase)
    {
        _currentPhase = phase.ToString();
        if (phase is TurnPhase.Vigil or TurnPhase.Dusk or TurnPhase.Resolution)
            _log.AppendLine($"  [PHASE: {phase}]");
    }

    private void OnTurnStarted()
    {
        // Nothing extra — board state already logged on action card reveal
    }

    private void OnCardPlayed(Card card, TurnPhase phase)
    {
        string reason = GetLastDecisionReason();
        string elements = card.Elements.Length > 0 ? string.Join(",", card.Elements) : "none";
        _log.AppendLine($"    PLAY ({phase}): {card.Id} [{card.Name}] | {reason}");
        _log.AppendLine($"      Elements: {elements}  Top: {card.TopEffect.Type}({card.TopEffect.Value})  Bot: {card.BottomEffect.Type}({card.BottomEffect.Value})");
    }

    private void OnInvaderDefeated(Invader invader)
    {
        _log.AppendLine($"    KILLED: {invader.UnitType} [{invader.Id}] at {invader.TerritoryId}");
    }

    private void OnInvaderArrived(Invader invader, Territory territory)
    {
        _log.AppendLine($"    ARRIVED: {invader.UnitType} [{invader.Id}] → {territory.Id}");
    }

    private void OnInvaderAdvanced(Invader invader, string from, string to)
    {
        _log.AppendLine($"    ADVANCE: {invader.UnitType} [{invader.Id}] {from} → {to}");
    }

    private void OnFearGenerated(int amount)
    {
        _log.AppendLine($"    FEAR: +{amount} generated");
    }

    private void OnFearSpent(int amount)
    {
        _log.AppendLine($"    FEAR: -{amount} spent");
    }

    private void OnFearActionRevealed(FearActionData action)
    {
        _log.AppendLine($"    FEAR ACTION: [{action.Id}] {action.Description}");
    }

    private void OnDreadAdvanced(int level)
    {
        _log.AppendLine($"    DREAD: advanced to level {level}");
    }

    private void OnCorruptionChanged(Territory territory, int newPoints, int newLevel)
    {
        _log.AppendLine($"    CORRUPTION: {territory.Id} → {newPoints}pts (L{newLevel})");
    }

    private void OnWeaveChanged(int newWeave)
    {
        _log.AppendLine($"    WEAVE: → {newWeave}");
    }

    private void OnTideCompleted(int tide)
    {
        _log.AppendLine($"  [TIDE {tide} COMPLETE] weave={_state.Weave?.CurrentWeave ?? 0} dread={_state.Dread?.DreadLevel ?? 0}");
    }

    private void OnEncounterEnded(EncounterResult result)
    {
        // handled in Finalize
    }

    private void OnThresholdTriggered(Element element, int tier)
        => _log.AppendLine($"    THRESHOLD: {element} T{tier} triggered");

    private void OnThresholdResolved(Element element, int tier, string description)
        => _log.AppendLine($"    THRESHOLD RESOLVED: {element} T{tier} — {description}");

    // ── Helpers ─────────────────────────────────────────────────────────────

    private string GetLastDecisionReason()
    {
        return _strategy switch
        {
            BotStrategy bot       => bot.LastDecisionReason,
            EmberBotStrategy ember => ember.LastDecisionReason,
            _                     => "(no reason)"
        };
    }

    private void LogBoardState()
    {
        _log.AppendLine("  Board state:");
        foreach (var t in _state.Territories.OrderBy(t => t.Id))
        {
            var invaders = t.Invaders.Where(i => i.IsAlive)
                .Select(i => $"{i.UnitType}({i.Hp}/{i.MaxHp})")
                .ToList();
            var natives = t.Natives.Where(n => n.IsAlive)
                .Select(n => $"Native({n.Hp}/{n.MaxHp})")
                .ToList();
            string invStr  = invaders.Count > 0 ? string.Join(" ", invaders) : "—";
            string natStr  = natives.Count  > 0 ? string.Join(" ", natives)  : "—";
            _log.AppendLine($"    {t.Id,-4} pres={t.PresenceCount} corr={t.CorruptionPoints}/L{t.CorruptionLevel}  inv=[{invStr}]  nat=[{natStr}]");
        }
    }

    private void LogHand()
    {
        var hand = _state.Deck?.Hand;
        if (hand == null || hand.Count == 0) { _log.AppendLine("  Hand: (empty)"); return; }
        _log.AppendLine("  Hand:");
        foreach (var card in hand)
        {
            string dormant  = card.IsDormant ? " [DORMANT]" : "";
            string elements = card.Elements.Length > 0 ? string.Join(",", card.Elements) : "none";
            _log.AppendLine($"    {card.Id,-10} {card.Name,-24} | {elements,-20} | Top:{card.TopEffect.Type}({card.TopEffect.Value}) Bot:{card.BottomEffect.Type}({card.BottomEffect.Value}){dormant}");
        }
    }
}
