namespace HollowWardens.Core.Encounter;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders;
using HollowWardens.Core.Models;

/// <summary>
/// Executes the full Tide sequence:
/// FearActions → Activate → CounterAttack → Advance → Arrive → Escalate → Preview
/// </summary>
public class TideRunner
{
    private readonly ActionDeck _actionDeck;
    private readonly CadenceManager _cadence;
    private readonly SpawnManager _spawn;
    private readonly InvaderFaction _faction;
    private readonly EffectResolver _resolver;

    // Card drawn during Preview for use in the next tide
    private ActionCard? _previewedCard;

    /// <summary>
    /// Optional handler for player-assigned counter-attack damage.
    /// Receives (territory, damagePool, state) and returns per-invader assignments.
    /// When null, auto-assign (lowest HP first) is used.
    /// </summary>
    public Func<Territory, int, EncounterState, Dictionary<Invader, int>?>? CounterAttackHandler;

    public TideRunner(
        ActionDeck actionDeck,
        CadenceManager cadence,
        SpawnManager spawn,
        InvaderFaction faction,
        EffectResolver resolver)
    {
        _actionDeck = actionDeck;
        _cadence = cadence;
        _spawn = spawn;
        _faction = faction;
        _resolver = resolver;
    }

    /// <summary>
    /// Preloads the first action card so Tide 1 uses it without re-drawing.
    /// Call this during initial encounter setup before the first Vigil.
    /// </summary>
    public void PreloadPreview(ActionCard card) => _previewedCard = card;

    /// <summary>
    /// Spawns Wave 1 invaders before the first Vigil so A-row is populated at game start.
    /// Tide 1's Arrive step will then spawn Wave 2, Tide 2 spawns Wave 3, etc.
    /// </summary>
    public void SpawnInitialWave(EncounterState state)
    {
        SpawnWaveForTide(1, state);
    }

    /// <summary>
    /// Runs one complete Tide and returns the action card used.
    /// Tide 1 is a ramp-up tide: Fear Actions, Activate, CounterAttack, and Escalate
    /// are skipped — only Advance, Arrive, and Preview run.
    /// </summary>
    public ActionCard ExecuteTide(int tideNumber, EncounterState state)
    {
        bool isFirstTide = tideNumber == 1;

        // ── Determine action card ───────────────────────────────────────────
        ActionCard actionCard;
        if (_previewedCard != null)
        {
            actionCard = _previewedCard;
            _previewedCard = null;
        }
        else
        {
            actionCard = _actionDeck.Draw(_cadence.NextPool());
        }
        state.CurrentActionCard = actionCard;
        GameEvents.ActionCardRevealed?.Invoke(actionCard);

        // D29: Reset slow flags at Tide start
        foreach (var t in state.Territories)
            foreach (var inv in t.Invaders)
                inv.IsSlowed = false;

        // Warden tide-start effect (e.g., Ember Ash Trail)
        state.Warden?.OnTideStart(state);

        // ── Step 1: Fear Actions (skipped on Tide 1) ────────────────────────
        if (!isFirstTide)
        {
            GameEvents.TideStepStarted?.Invoke(TideStep.FearActions);

            // Warden passive fear (e.g., Root network fear) generates at Tide start
            int passiveFear = state.Warden?.CalculatePassiveFear() ?? 0;
            if (passiveFear > 0)
            {
                state.Dread?.OnFearGenerated(passiveFear);
                GameEvents.FearGenerated?.Invoke(passiveFear);
            }

            // Reveal and resolve queued fear actions
            var fearActions = state.FearActions?.RevealAndDequeue() ?? new List<FearActionData>();
            state.FearActions?.BeginResolution(); // Bugfix: prevent loop during resolution
            try
            {
                foreach (var fa in fearActions)
                {
                    try
                    {
                        var effect = _resolver.Resolve(fa.Effect);
                        effect.Resolve(state, new TargetInfo());
                    }
                    catch (NotImplementedException) { }
                }
            }
            finally
            {
                state.FearActions?.EndResolution();
            }
        }

        // ── Step 2: Activate (skipped on Tide 1) ───────────────────────────
        if (!isFirstTide)
        {
            GameEvents.TideStepStarted?.Invoke(TideStep.Activate);
            foreach (var territory in state.TerritoriesWithInvaders().ToList())
                state.Combat?.ExecuteActivate(actionCard, territory, state);
        }

        // ── Step 3: CounterAttack (skipped on Tide 1; only after Ravage or Corrupt) ──
        // D29: Presence Provocation — counter-attack on ALL actions in provoked territories
        bool isCardProvoked = state.Combat?.IsProvokedAction(actionCard) ?? false;
        bool hasWardenProvocation = !isFirstTide && state.Warden != null
            && state.Territories.Any(t =>
                t.Natives.Any(n => n.IsAlive) && t.Invaders.Any(i => i.IsAlive)
                && (state.Warden.ProvokesNatives(t)));
        bool isProvoked = !isFirstTide && (isCardProvoked || hasWardenProvocation);
        if (isProvoked)
        {
            GameEvents.TideStepStarted?.Invoke(TideStep.CounterAttack);
            foreach (var territory in state.Territories
                .Where(t => t.Natives.Any(n => n.IsAlive) && t.Invaders.Any(i => i.IsAlive)
                    && (isCardProvoked || (state.Warden?.ProvokesNatives(t) ?? false)))
                .ToList())
            {
                int pool = state.Combat?.CalculateNativeDamagePool(territory) ?? 0;
                GameEvents.CounterAttackReady?.Invoke(territory, pool);
                if (pool > 0)
                {
                    var assignments = CounterAttackHandler?.Invoke(territory, pool, state);
                    if (assignments != null)
                        state.Combat?.ApplyCounterAttack(territory, assignments);
                    else
                        state.Combat?.AutoAssignCounterAttack(territory);
                }
            }
        }

        // ── Step 4: Advance ────────────────────────────────────────────────
        GameEvents.TideStepStarted?.Invoke(TideStep.Advance);
        state.Combat?.ExecuteAdvance(actionCard, state);
        state.Combat?.ExecuteHeartMarch(state);

        // ── Step 5: Arrive ─────────────────────────────────────────────────
        GameEvents.TideStepStarted?.Invoke(TideStep.Arrive);
        SpawnWaveForTide(tideNumber + 1, state);

        // ── Step 6: Escalate (skipped on Tide 1) ───────────────────────────
        if (!isFirstTide)
        {
            GameEvents.TideStepStarted?.Invoke(TideStep.Escalate);
            ApplyEscalation(tideNumber, state);
        }

        // ── Step 7: Preview ────────────────────────────────────────────────
        GameEvents.TideStepStarted?.Invoke(TideStep.Preview);
        _previewedCard = _actionDeck.Draw(_cadence.NextPool());
        GameEvents.NextActionPreviewed?.Invoke(_previewedCard);
        _spawn.PreviewWave(tideNumber + 1);

        return actionCard;
    }

    // ── Step methods for interactive GameBridge tide state machine ──────────────

    /// <summary>Determines and reveals the action card. Call first each tide.</summary>
    public ActionCard BeginTide(int tideNumber, EncounterState state)
    {
        ActionCard actionCard;
        if (_previewedCard != null) { actionCard = _previewedCard; _previewedCard = null; }
        else actionCard = _actionDeck.Draw(_cadence.NextPool());
        state.CurrentActionCard = actionCard;
        GameEvents.ActionCardRevealed?.Invoke(actionCard);

        // D29: Reset slow flags at Tide start
        foreach (var t in state.Territories)
            foreach (var inv in t.Invaders)
                inv.IsSlowed = false;

        // Warden tide-start effect (e.g., Ember Ash Trail)
        state.Warden?.OnTideStart(state);

        return actionCard;
    }

    /// <summary>Applies passive warden fear at the start of a non-first Tide.</summary>
    public void ApplyPassiveFear(EncounterState state)
    {
        int passiveFear = state.Warden?.CalculatePassiveFear() ?? 0;
        if (passiveFear > 0) { state.Dread?.OnFearGenerated(passiveFear); GameEvents.FearGenerated?.Invoke(passiveFear); }
    }

    /// <summary>Drains the fear action queue WITHOUT firing FearActionRevealed (player reveals each one).</summary>
    public List<FearActionData> DrainFearActions(EncounterState state)
    {
        GameEvents.TideStepStarted?.Invoke(TideStep.FearActions);
        return state.FearActions?.DrainQueue() ?? new List<FearActionData>();
    }

    /// <summary>Runs the Activate step for all territories with invaders.</summary>
    public void RunActivate(ActionCard actionCard, EncounterState state)
    {
        GameEvents.TideStepStarted?.Invoke(TideStep.Activate);
        foreach (var territory in state.TerritoriesWithInvaders().ToList())
            state.Combat?.ExecuteActivate(actionCard, territory, state);
    }

    /// <summary>Returns territories eligible for native counter-attack. Fires TideStep.CounterAttack event.</summary>
    public List<Territory> GetCounterAttackTargets(ActionCard actionCard, EncounterState state)
    {
        GameEvents.TideStepStarted?.Invoke(TideStep.CounterAttack);
        bool isCardProvoked = state.Combat?.IsProvokedAction(actionCard) ?? false;
        // D29: Presence Provocation — filter by card-provoked OR warden-provoked territories
        return state.Territories
            .Where(t => t.Natives.Any(n => n.IsAlive) && t.Invaders.Any(i => i.IsAlive)
                && (isCardProvoked || (state.Warden?.ProvokesNatives(t) ?? false)))
            .ToList();
    }

    /// <summary>Runs Advance + HeartMarch steps.</summary>
    public void RunAdvance(ActionCard actionCard, EncounterState state)
    {
        GameEvents.TideStepStarted?.Invoke(TideStep.Advance);
        state.Combat?.ExecuteAdvance(actionCard, state);
        state.Combat?.ExecuteHeartMarch(state);
    }

    /// <summary>Runs the Arrive step — spawns the next wave (tideNumber + 1).</summary>
    public void RunArrive(int tideNumber, EncounterState state)
    {
        GameEvents.TideStepStarted?.Invoke(TideStep.Arrive);
        SpawnWaveForTide(tideNumber + 1, state);
    }

    /// <summary>Runs the Escalate step.</summary>
    public void RunEscalate(int tideNumber, EncounterState state)
    {
        GameEvents.TideStepStarted?.Invoke(TideStep.Escalate);
        ApplyEscalation(tideNumber, state);
    }

    /// <summary>Runs the Preview step — draws next action card and previews next spawn.</summary>
    public void RunPreview(int tideNumber, EncounterState state)
    {
        GameEvents.TideStepStarted?.Invoke(TideStep.Preview);
        _previewedCard = _actionDeck.Draw(_cadence.NextPool());
        GameEvents.NextActionPreviewed?.Invoke(_previewedCard);
        _spawn.PreviewWave(tideNumber + 1);
    }

    // ──────────────────────────────────────────────────────────────────────────

    private void SpawnWaveForTide(int tideNumber, EncounterState state)
    {
        // SpawnManager.PreviewWave returns the wave for this tide (fires WaveLocationsRevealed)
        var wave = _spawn.PreviewWave(tideNumber);
        if (wave == null) return;

        var composition = _spawn.RevealComposition(wave);
        if (composition == null) return;

        foreach (var (territoryId, unitTypes) in composition.Units)
        {
            var territory = state.GetTerritory(territoryId);
            if (territory == null) continue;
            foreach (var unitType in unitTypes)
            {
                var invader = _faction.CreateUnit(unitType, territoryId);
                territory.Invaders.Add(invader);
                GameEvents.InvaderArrived?.Invoke(invader, territory);
            }
        }
    }

    private void ApplyEscalation(int tideNumber, EncounterState state)
    {
        foreach (var entry in state.Config.EscalationSchedule.Where(e => e.Tide == tideNumber))
        {
            // Build the escalation card from the faction or a generic card
            var card = BuildEscalationCard(entry);
            if (card != null)
                _actionDeck.AddEscalationCard(card);
        }
    }

    private static ActionCard? BuildEscalationCard(EscalationEntry entry) =>
        new ActionCard
        {
            Id = entry.CardId,
            Name = entry.CardId,
            Pool = entry.Pool,
            AdvanceModifier = 1,
            IsEscalation = true
        };
}
