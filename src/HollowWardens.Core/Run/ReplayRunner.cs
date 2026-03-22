namespace HollowWardens.Core.Run;

using HollowWardens.Core.Cards;
using HollowWardens.Core.Data;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders.PaleMarch;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;

/// <summary>
/// Replays a recorded encounter from an exported state string.
/// Uses the same seed to reconstruct identical RNG, then drives a full
/// EncounterRunner with a ReplayStrategy that feeds back the recorded actions.
/// </summary>
public class ReplayRunner
{
    private readonly int _seed;
    private readonly string[] _rawActions;
    private readonly string _wardenJsonPath;

    public EncounterState? State { get; private set; }

    /// <summary>Fires after each action is replayed.</summary>
    public event Action<int, GameAction, EncounterState>? ActionReplayed;

    /// <summary>Fires when replay completes.</summary>
    public event Action<EncounterState>? ReplayCompleted;

    /// <param name="seed">The seed from <see cref="ActionLog.ImportFull"/>.</param>
    /// <param name="rawActions">The raw action tokens from <see cref="ActionLog.ImportFull"/>.</param>
    /// <param name="wardenJsonPath">Absolute path to the warden JSON file.</param>
    public ReplayRunner(int seed, string[] rawActions, string wardenJsonPath)
    {
        _seed           = seed;
        _rawActions     = rawActions;
        _wardenJsonPath = wardenJsonPath;
    }

    /// <summary>
    /// Rebuilds the encounter with the recorded seed and replays all actions
    /// using EncounterRunner + a ReplayStrategy. Returns the final EncounterState.
    /// </summary>
    public EncounterState Replay()
    {
        var actions  = _rawActions.Select(ParseAction).ToArray();
        var strategy = new ReplayStrategy(actions, ActionReplayed);

        var (state, runner) = BuildEncounter();
        State = state;

        runner.Run(state, strategy);

        ReplayCompleted?.Invoke(state);
        return state;
    }

    private (EncounterState state, EncounterRunner runner) BuildEncounter()
    {
        var random      = GameRandom.FromSeed(_seed);
        var wardenData  = WardenLoader.Load(_wardenJsonPath);
        var territories = Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var presence    = new PresenceSystem(() => territories);
        var warden      = new RootAbility(presence);
        var dread       = new DreadSystem();
        var config      = EncounterLoader.CreatePaleMarchStandard();

        var state = new EncounterState
        {
            Config      = config,
            Territories = territories,
            Elements    = new ElementSystem(),
            Dread       = dread,
            Weave       = new WeaveSystem(20),
            Combat      = new CombatSystem(),
            Presence    = presence,
            Corruption  = new CorruptionSystem(),
            FearActions = new FearActionSystem(dread, FearActionPool.Build(), random),
            Warden      = warden,
            WardenData  = wardenData,
            Random      = random,
            ActionLog   = new ActionLog()
        };

        var startingCards = wardenData.Cards.Where(c => c.IsStarting).ToList();
        state.Deck = new DeckManager(warden, startingCards, random, shuffle: true);

        var faction    = new PaleMarchFaction();
        var actionDeck = new ActionDeck(faction.BuildPainfulPool(), faction.BuildEasyPool(), random, shuffle: true);
        var cadence    = new CadenceManager(config.Cadence);
        var spawn      = new SpawnManager(config.Waves, random);
        var resolver   = new EffectResolver();

        VulnerabilityWiring.WireEvents(presence);

        var runner = new EncounterRunner(actionDeck, cadence, spawn, faction, resolver);
        return (state, runner);
    }

    // ── Parsing ────────────────────────────────────────────────────────────

    private static GameAction ParseAction(string raw)
    {
        // Format: "{Timestamp}:{Type}:{CardId}:{TargetTerritoryId}"
        // CardId and TargetTerritoryId are "-" when absent
        var parts = raw.Split(':', 4);
        if (parts.Length < 4) return new GameAction();

        Enum.TryParse<GameActionType>(parts[1], out var type);
        return new GameAction
        {
            Timestamp         = int.TryParse(parts[0], out var ts) ? ts : 0,
            Type              = type,
            CardId            = parts[2] == "-" ? null : parts[2],
            TargetTerritoryId = parts[3] == "-" ? null : parts[3],
        };
    }

    // ── ReplayStrategy ─────────────────────────────────────────────────────

    private sealed class ReplayStrategy : IPlayerStrategy
    {
        private readonly Queue<GameAction> _queue;
        private readonly Action<int, GameAction, EncounterState>? _onActionReplayed;
        private int _replayIndex;

        public ReplayStrategy(
            GameAction[] actions,
            Action<int, GameAction, EncounterState>? onActionReplayed)
        {
            _queue             = new Queue<GameAction>(actions);
            _onActionReplayed  = onActionReplayed;
        }

        public Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state)
        {
            while (_queue.TryPeek(out var action))
            {
                if (action.Type == GameActionType.PlayTop)
                {
                    _queue.Dequeue();
                    FireActionReplayed(action, state);
                    return hand.FirstOrDefault(c => c.Id == action.CardId);
                }
                // Consume Tide-phase skips that appear before any PlayBottom we might reach
                if (action.Type == GameActionType.SkipPhase && action.Phase == TurnPhase.Tide)
                {
                    _queue.Dequeue();
                    continue;
                }
                // SkipPhase(Vigil) or any non-PlayTop action → end vigil
                if (action.Type == GameActionType.SkipPhase)
                    _queue.Dequeue();
                return null;
            }
            return null;
        }

        public Card? ChooseBottomPlay(IReadOnlyList<Card> hand, EncounterState state)
        {
            while (_queue.TryPeek(out var action))
            {
                // Consume intermediate tide-phase skips before dusk
                if (action.Type == GameActionType.SkipPhase && action.Phase == TurnPhase.Tide)
                {
                    _queue.Dequeue();
                    continue;
                }
                if (action.Type == GameActionType.PlayBottom)
                {
                    _queue.Dequeue();
                    FireActionReplayed(action, state);
                    return hand.FirstOrDefault(c => c.Id == action.CardId);
                }
                // SkipPhase(Dusk) or any non-PlayBottom action → end dusk
                if (action.Type == GameActionType.SkipPhase)
                    _queue.Dequeue();
                return null;
            }
            return null;
        }

        public string? ChooseTarget(EffectData effect, EncounterState state)
        {
            if (_queue.TryPeek(out var action) && action.Type == GameActionType.SelectTarget)
            {
                _queue.Dequeue();
                FireActionReplayed(action, state);
                return action.TargetTerritoryId;
            }
            return null;
        }

        public Dictionary<Invader, int>? AssignCounterDamage(
            Territory territory, int damagePool, EncounterState state) => null;

        public string? ChooseRestGrowthTarget(EncounterState state)
        {
            while (_queue.TryPeek(out var action))
            {
                if (action.Type == GameActionType.Rest)
                {
                    _queue.Dequeue();
                    FireActionReplayed(action, state);
                    return action.TargetTerritoryId;
                }
                if (action.Type == GameActionType.SkipPhase)
                {
                    _queue.Dequeue();
                    return null;
                }
                return null;
            }
            return null;
        }

        private void FireActionReplayed(GameAction action, EncounterState state)
        {
            _onActionReplayed?.Invoke(_replayIndex++, action, state);
        }
    }
}
