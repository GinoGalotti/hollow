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
        foreach (var (territoryId, count) in state.Config.NativeSpawns)
        {
            var territory = state.GetTerritory(territoryId);
            if (territory == null || count <= 0) continue;
            for (int i = 0; i < count; i++)
                territory.Natives.Add(new Native { Hp = 2, MaxHp = 2, Damage = 3, TerritoryId = territoryId });
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
                // No Tide or Dusk on a rest turn
                continue;
            }

            // Vigil card plays
            PlayTops(turnManager, state, strategy);
            turnManager.EndVigil();

            // ── Tide ────────────────────────────────────────────────────────
            tideRunner.ExecuteTide(tidesExecuted + 1, state);
            tidesExecuted++;

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
            if (!turnManager.PlayTop(card)) break; // play limit reached
        }
    }

    private static void PlayBottoms(TurnManager turnManager, EncounterState state, IPlayerStrategy strategy)
    {
        while (state.Deck != null)
        {
            var card = strategy.ChooseBottomPlay(state.Deck.Hand, state);
            if (card == null) break;
            if (!turnManager.PlayBottom(card)) break; // play limit reached
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

        // ThresholdTriggered → auto-resolve Tier 1 effects
        var resolver = new ThresholdResolver();
        _onThresholdTriggered = (element, tier) => resolver.AutoResolve(element, tier, state);
        GameEvents.ThresholdTriggered += _onThresholdTriggered;
    }

    private void UnwireEvents()
    {
        if (_onFearGenerated != null)     GameEvents.FearGenerated     -= _onFearGenerated;
        if (_onDreadAdvanced != null)     GameEvents.DreadAdvanced     -= _onDreadAdvanced;
        if (_onNativeDefeated != null)    GameEvents.NativeDefeated    -= _onNativeDefeated;
        if (_onThresholdTriggered != null) GameEvents.ThresholdTriggered -= _onThresholdTriggered;
    }
}
