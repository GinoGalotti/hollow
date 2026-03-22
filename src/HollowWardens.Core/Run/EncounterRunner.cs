namespace HollowWardens.Core.Run;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Turn;

/// <summary>
/// Top-level encounter loop: wires events, runs turns and tides, hands off to
/// ResolutionRunner, calls warden OnResolution, returns EncounterResult.
/// </summary>
public class EncounterRunner
{
    private readonly ActionDeck _actionDeck;
    private readonly CadenceManager _cadence;
    private readonly SpawnManager _spawn;
    private readonly InvaderFaction _faction;
    private readonly EffectResolver _resolver;

    // Subscriptions stored so they can be removed after the encounter
    private Action<int>? _onFearGenerated;
    private Action<int>? _onDreadAdvanced;
    private Action<Native, Territory>? _onNativeDefeated;
    private Action<Element, int>? _onThresholdTriggered;
    private Action<Card>? _onCardDissolved;
    private Action<Card>? _onCardRestDissolved;

    public EncounterRunner(
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
    /// Runs a complete encounter from setup to reward calculation.
    /// </summary>
    public EncounterResult Run(EncounterState state, IPlayerStrategy strategy)
    {
        WireEvents(state);
        if (state.Presence != null)
            VulnerabilityWiring.WireEvents(state.Presence);
        GameEvents.EncounterStarted?.Invoke(state.Config);

        // ── §1 Initial board setup ──────────────────────────────────────────
        // Spawn natives per config
        int nativeHp     = state.Balance.DefaultNativeHp;
        int nativeDamage = state.Balance.DefaultNativeDamage;
        foreach (var (territoryId, count) in state.Config.NativeSpawns)
        {
            var territory = state.GetTerritory(territoryId);
            if (territory == null || count <= 0) continue;
            for (int i = 0; i < count; i++)
                territory.Natives.Add(new Native { Hp = nativeHp, MaxHp = nativeHp, Damage = nativeDamage, TerritoryId = territoryId });
        }
        // Place starting Presence from warden data
        var startTerritory = state.GetTerritory(state.WardenData?.StartingPresence.Territory ?? "I1");
        if (startTerritory != null)
            startTerritory.PresenceCount = state.WardenData?.StartingPresence.Count ?? 1;

        var turnManager = new TurnManager(state, _resolver);
        var tideRunner = new TideRunner(_actionDeck, _cadence, _spawn, _faction, _resolver);
        tideRunner.CounterAttackHandler = strategy.AssignCounterDamage;

        int tidesExecuted = 0;

        while (tidesExecuted < state.Config.TideCount)
        {
            state.CurrentTide = tidesExecuted + 1;

            // ── Vigil ───────────────────────────────────────────────────────
            turnManager.StartVigil();

            if (turnManager.IsRestTurn)
            {
                // D29: Rest Growth
                var restTarget = strategy.ChooseRestGrowthTarget(state);
                turnManager.Rest(restTarget);
                state.ActionLog.Record(new GameAction
                {
                    TurnNumber        = state.CurrentTide,
                    Phase             = TurnPhase.Rest,
                    Type              = GameActionType.Rest,
                    TargetTerritoryId = restTarget
                });
                // No Tide or Dusk on a rest turn
                continue;
            }

            // Vigil card plays
            PlayTops(turnManager, state, strategy);
            turnManager.EndVigil();

            // ── Tide ────────────────────────────────────────────────────────
            tideRunner.ExecuteTide(tidesExecuted + 1, state);
            tidesExecuted++;
            GameEvents.TideCompleted?.Invoke(tidesExecuted);

            if (state.Weave?.IsGameOver == true) break;

            // ── Dusk ────────────────────────────────────────────────────────
            turnManager.StartDusk();
            PlayBottoms(turnManager, state, strategy);
            turnManager.EndTurn();
        }

        // ── Resolution ──────────────────────────────────────────────────────
        var resRunner = new ResolutionRunner(_resolver);
        resRunner.RunResolution(state, strategy);

        // Warden-specific resolution (e.g., Root assimilation)
        state.Warden?.OnResolution(state);

        UnwireEvents();
        VulnerabilityWiring.UnwireEvents();

        GameEvents.EncounterEnded?.Invoke(RewardCalculator.Calculate(state));
        return RewardCalculator.Calculate(state);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static void PlayTops(TurnManager turnManager, EncounterState state, IPlayerStrategy strategy)
    {
        while (state.Deck != null)
        {
            var card = strategy.ChooseTopPlay(state.Deck.Hand, state);
            if (card == null) break;

            TargetInfo? target = null;
            if (TargetValidator.NeedsTarget(card.TopEffect))
            {
                var territoryId = strategy.ChooseTarget(card.TopEffect, state);
                if (territoryId != null)
                    target = new TargetInfo { TerritoryId = territoryId, SourceCard = card };
            }

            if (!turnManager.PlayTop(card, target)) break; // play limit reached

            state.ActionLog.Record(new GameAction
            {
                TurnNumber = state.CurrentTide,
                Phase      = TurnPhase.Vigil,
                Type       = GameActionType.PlayTop,
                CardId     = card.Id
            });
            if (target != null)
                state.ActionLog.Record(new GameAction
                {
                    TurnNumber        = state.CurrentTide,
                    Phase             = TurnPhase.Vigil,
                    Type              = GameActionType.SelectTarget,
                    TargetTerritoryId = target.TerritoryId
                });
        }
    }

    private static void PlayBottoms(TurnManager turnManager, EncounterState state, IPlayerStrategy strategy)
    {
        while (state.Deck != null)
        {
            var card = strategy.ChooseBottomPlay(state.Deck.Hand, state);
            if (card == null) break;

            TargetInfo? target = null;
            if (TargetValidator.NeedsTarget(card.BottomEffect))
            {
                var territoryId = strategy.ChooseTarget(card.BottomEffect, state);
                if (territoryId != null)
                    target = new TargetInfo { TerritoryId = territoryId, SourceCard = card };
            }

            if (!turnManager.PlayBottom(card, target)) break; // play limit reached

            state.ActionLog.Record(new GameAction
            {
                TurnNumber = state.CurrentTide,
                Phase      = TurnPhase.Dusk,
                Type       = GameActionType.PlayBottom,
                CardId     = card.Id
            });
            if (target != null)
                state.ActionLog.Record(new GameAction
                {
                    TurnNumber        = state.CurrentTide,
                    Phase             = TurnPhase.Dusk,
                    Type              = GameActionType.SelectTarget,
                    TargetTerritoryId = target.TerritoryId
                });
        }
    }

    private void WireEvents(EncounterState state)
    {
        // FearGenerated → FearActions queue (Dread is called directly by effects/passives)
        _onFearGenerated = amount => state.FearActions?.OnFearSpent(amount);
        GameEvents.FearGenerated += _onFearGenerated;

        // DreadAdvanced → retroactively upgrade queued fear actions
        _onDreadAdvanced = level => state.FearActions?.OnDreadAdvanced(level);
        GameEvents.DreadAdvanced += _onDreadAdvanced;

        // NativeDefeated → generates 1 Fear (Dread called directly; FearGenerated event queues FearActions)
        _onNativeDefeated = (native, territory) =>
        {
            const int fearPerNative = 1;
            state.Dread?.OnFearGenerated(fearPerNative);
            GameEvents.FearGenerated?.Invoke(fearPerNative);
        };
        GameEvents.NativeDefeated += _onNativeDefeated;

        // ThresholdTriggered → auto-resolve effects + unlock gated passives
        var resolver = new ThresholdResolver();
        _onThresholdTriggered = (element, tier) =>
        {
            resolver.AutoResolve(element, tier, state);
            state.PassiveGating?.OnThresholdTriggered(element, tier);
        };
        GameEvents.ThresholdTriggered += _onThresholdTriggered;

        // Phoenix Spark: when a card is permanently removed, generate 3 Fear (Ember only, when active)
        _onCardDissolved = _ =>
        {
            if (state.Warden?.WardenId == "ember"
                && (state.PassiveGating == null || state.PassiveGating.IsActive("phoenix_spark")))
            {
                state.Dread?.OnFearGenerated(3);
                GameEvents.FearGenerated?.Invoke(3);
            }
        };
        _onCardRestDissolved = _onCardDissolved;
        GameEvents.CardDissolved     += _onCardDissolved;
        GameEvents.CardRestDissolved += _onCardRestDissolved;
    }

    private void UnwireEvents()
    {
        if (_onFearGenerated != null)     GameEvents.FearGenerated     -= _onFearGenerated;
        if (_onDreadAdvanced != null)     GameEvents.DreadAdvanced     -= _onDreadAdvanced;
        if (_onNativeDefeated != null)    GameEvents.NativeDefeated    -= _onNativeDefeated;
        if (_onThresholdTriggered != null)  GameEvents.ThresholdTriggered  -= _onThresholdTriggered;
        if (_onCardDissolved != null)       GameEvents.CardDissolved        -= _onCardDissolved;
        if (_onCardRestDissolved != null)   GameEvents.CardRestDissolved    -= _onCardRestDissolved;
    }
}
