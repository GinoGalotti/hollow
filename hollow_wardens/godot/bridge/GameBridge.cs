using Godot;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders.PaleMarch;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;

// Aliases to avoid ambiguity with the legacy scripts/core/TurnManager.cs
using CoreTurnManager = HollowWardens.Core.Turn.TurnManager;
using CoreTurnPhase   = HollowWardens.Core.Models.TurnPhase;

/// <summary>
/// Autoload singleton. Owns all Core objects, subscribes to GameEvents and
/// re-emits as Godot signals, drives the encounter game-loop state machine.
/// </summary>
public partial class GameBridge : Node
{
    // ── Signals ──────────────────────────────────────────────────────────────
    [Signal] public delegate void PhaseChangedEventHandler(int phase);
    [Signal] public delegate void TurnStartedEventHandler();
    [Signal] public delegate void TurnEndedEventHandler();
    [Signal] public delegate void RestStartedEventHandler();

    [Signal] public delegate void HandChangedEventHandler();
    [Signal] public delegate void DeckCountsChangedEventHandler(int draw, int discard, int dissolved, int dormant);

    [Signal] public delegate void ElementChangedEventHandler(int element, int value);
    [Signal] public delegate void ThresholdTriggeredEventHandler(int element, int tier);
    [Signal] public delegate void ElementsDecayedEventHandler();

    [Signal] public delegate void FearGeneratedEventHandler(int amount);
    [Signal] public delegate void FearActionQueuedEventHandler();
    [Signal] public delegate void FearActionRevealedEventHandler(string description);
    [Signal] public delegate void DreadAdvancedEventHandler(int level);

    [Signal] public delegate void TideStepStartedEventHandler(int step);
    [Signal] public delegate void ActionCardRevealedEventHandler(string name, bool isPainful);
    [Signal] public delegate void NextActionPreviewedEventHandler(string name, bool isPainful);

    [Signal] public delegate void InvaderArrivedEventHandler(string invaderId, string territoryId, int unitType);
    [Signal] public delegate void InvaderDefeatedEventHandler(string invaderId);
    [Signal] public delegate void InvaderAdvancedEventHandler(string invaderId, string fromId, string toId);

    [Signal] public delegate void CorruptionChangedEventHandler(string territoryId, int points, int level);
    [Signal] public delegate void WeaveChangedEventHandler(int value);
    [Signal] public delegate void CounterAttackReadyEventHandler(string territoryId, int pool);
    [Signal] public delegate void HeartDamageDealtEventHandler(string territoryId);

    [Signal] public delegate void ResolutionTurnStartedEventHandler(int turn);
    [Signal] public delegate void EncounterEndedEventHandler(int result);

    // ── Public State ─────────────────────────────────────────────────────────
    public static GameBridge? Instance { get; private set; }

    public EncounterState State { get; private set; } = null!;

    /// <summary>Current turn phase (from Core TurnManager).</summary>
    public CoreTurnPhase CurrentPhase => _turnManager.CurrentPhase;

    /// <summary>True when the deck needs a Rest this turn.</summary>
    public bool IsRestTurn => _turnManager.IsRestTurn;

    /// <summary>True during the Resolution turns at the end of an encounter.</summary>
    public bool IsInResolution => _inResolution;

    // ── Private Core Objects ─────────────────────────────────────────────────
    private CoreTurnManager _turnManager = null!;
    private TideRunner      _tideRunner  = null!;
    private ActionDeck      _actionDeck  = null!;
    private CadenceManager  _cadence     = null!;
    private SpawnManager    _spawn       = null!;
    private EffectResolver  _resolver    = null!;

    // ── Loop State ───────────────────────────────────────────────────────────
    private int  _tidesExecuted;
    private bool _inResolution;
    private int  _resolutionTurn;

    // ── Event Handler Fields ──────────────────────────────────────────────────
    private Action<CoreTurnPhase>?             _hPhaseChanged;
    private Action?                            _hTurnStarted, _hTurnEnded, _hRestStarted;
    private Action<Card, CoreTurnPhase>?       _hCardPlayed;
    private Action<Card>?                      _hCardDissolved, _hCardDormant, _hCardRestDissolved, _hCardAwakened;
    private Action<Element, int>?              _hElementChanged;
    private Action<Element, int>?              _hThresholdTriggered;
    private Action?                            _hElementsDecayed;
    private Action<int>?                       _hFearGeneratedRelay;
    private Action<int>?                       _hFearGeneratedQueue;
    private Action?                            _hFearActionQueued;
    private Action<FearActionData>?            _hFearActionRevealed;
    private Action<int>?                       _hDreadAdvancedRelay;
    private Action<int>?                       _hDreadAdvancedUpgrade;
    private Action<TideStep>?                  _hTideStepStarted;
    private Action<ActionCard>?                _hActionCardRevealed;
    private Action<ActionCard>?                _hNextActionPreviewed;
    private Action<Invader, Territory>?        _hInvaderArrived;
    private Action<Invader>?                   _hInvaderDefeated;
    private Action<Invader, string, string>?   _hInvaderAdvanced;
    private Action<Territory, int, int>?       _hCorruptionChanged;
    private Action<int>?                       _hWeaveChanged;
    private Action<Territory, int>?            _hCounterAttackReady;
    private Action<Territory>?                 _hHeartDamage;
    private Action<Native, Territory>?         _hNativeDefeated;
    private Action<int>?                       _hResolutionTurnStarted;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        Instance = this;
        BuildEncounter();
        SubscribeToGameEvents();
        Callable.From(StartFirstTurn).CallDeferred();
    }

    public override void _ExitTree()
    {
        UnsubscribeFromGameEvents();
        GameEvents.ClearAll();
        Instance = null;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void PlayTop(Card card)
    {
        bool canPlay = _turnManager.CurrentPhase == CoreTurnPhase.Vigil || _inResolution;
        if (!canPlay) return;
        _turnManager.PlayTop(card);
        EmitSignal(SignalName.HandChanged);
        EmitDeckCounts();
    }

    public void PlayBottom(Card card)
    {
        if (_turnManager.CurrentPhase != CoreTurnPhase.Dusk) return;
        _turnManager.PlayBottom(card);
        EmitSignal(SignalName.HandChanged);
        EmitDeckCounts();
    }

    public void EndCurrentPhase()
    {
        if (_inResolution) { AdvanceResolutionTurn(); return; }

        switch (_turnManager.CurrentPhase)
        {
            case CoreTurnPhase.Vigil: ExecuteTideAndDusk(); break;
            case CoreTurnPhase.Dusk:  EndDusk();            break;
            case CoreTurnPhase.Rest:  ExecuteRest();        break;
        }
    }

    public void TriggerRest()
    {
        if (_turnManager.IsRestTurn) ExecuteRest();
    }

    // ── Encounter Setup ──────────────────────────────────────────────────────

    private void BuildEncounter()
    {
        var territories = BoardState.CreatePyramid().Territories.Values.ToList();
        var dread       = new DreadSystem();
        var fearPools   = BuildFearPools();
        var presence    = new PresenceSystem(() => territories);
        var warden      = new RootAbility(presence);

        var config = new EncounterConfig
        {
            Id       = "enc_01",
            Tier     = EncounterTier.Standard,
            FactionId = "pale_march",
            TideCount = 7,
            Cadence   = new CadenceConfig { Mode = "rule_based", MaxPainfulStreak = 1, EasyFrequency = 2 }
        };

        State = new EncounterState
        {
            Config      = config,
            Territories = territories,
            Elements    = new ElementSystem(),
            Dread       = dread,
            Weave       = new WeaveSystem(20),
            Combat      = new CombatSystem(),
            Presence    = presence,
            Corruption  = new CorruptionSystem(),
            FearActions = new FearActionSystem(dread, fearPools),
            Warden      = warden
        };

        var rootCards = BuildRootStarterDeck();
        State.Deck = new DeckManager(warden, rootCards, shuffle: true);

        _resolver    = new EffectResolver();
        var faction  = new PaleMarchFaction();
        _actionDeck  = new ActionDeck(faction.BuildPainfulPool(), faction.BuildEasyPool(), shuffle: true);
        _cadence     = new CadenceManager(config.Cadence);
        _spawn       = new SpawnManager(new List<SpawnWave>());

        _turnManager = new CoreTurnManager(State, _resolver);
        _tideRunner  = new TideRunner(_actionDeck, _cadence, _spawn, faction, _resolver);
        _tideRunner.CounterAttackHandler = (t, pool, s) => null;
    }

    private static Dictionary<int, List<FearActionData>> BuildFearPools() => new()
    {
        [1] = new List<FearActionData>
        {
            new() { Id = "fa_ravage",  Description = "Invaders Ravage in place",          DreadLevel = 1, Effect = new() { Type = EffectType.GenerateFear, Value = 0 } },
            new() { Id = "fa_corrupt", Description = "Corrupt: +1 to invader territory",  DreadLevel = 1, Effect = new() { Type = EffectType.GenerateFear, Value = 0 } },
        },
        [2] = new List<FearActionData>
        {
            new() { Id = "fa_surge",   Description = "Reinforcements: Marcher arrives",   DreadLevel = 2, Effect = new() { Type = EffectType.GenerateFear, Value = 0 } },
        },
    };

    private static List<Card> BuildRootStarterDeck() => new()
    {
        MakeCard("root_001","Tendrils of Reclamation", EffectType.ReduceCorruption,1,1, EffectType.ReduceCorruption,2,1),
        MakeCard("root_002","Deep Roots",              EffectType.PlacePresence,   1,1, EffectType.ReduceCorruption,1,0),
        MakeCard("root_003","Earthen Mending",         EffectType.ReduceCorruption,1,0, EffectType.ReduceCorruption,1,2),
        MakeCard("root_004","Ancient Cleansing",       EffectType.ReduceCorruption,1,1, EffectType.ReduceCorruption,2,0),
        MakeCard("root_005","Subterranean Surge",      EffectType.ReduceCorruption,1,2, EffectType.PlacePresence,   1,1),
        MakeCard("root_006","Root Network",            EffectType.PlacePresence,   1,2, EffectType.PlacePresence,   1,1),
        MakeCard("root_007","Verdant Weave",           EffectType.RestoreWeave,    2,0, EffectType.PlacePresence,   1,0),
        MakeCard("root_008","Thorn Ward",              EffectType.ShieldNatives,   1,1, EffectType.ReduceCorruption,1,1),
        MakeCard("root_009","Warding Vines",           EffectType.ShieldNatives,   2,1, EffectType.DamageInvaders,  1,1),
        MakeCard("root_010","Awakening Pulse",         EffectType.AwakeDormant,    0,0, EffectType.PlacePresence,   1,2),
    };

    private static Card MakeCard(string id, string name,
        EffectType topType, int topVal, int topRange,
        EffectType botType, int botVal, int botRange) => new()
    {
        Id = id, Name = name,
        Elements     = Array.Empty<Element>(),
        TopEffect    = new EffectData { Type = topType, Value = topVal, Range = topRange },
        BottomEffect = new EffectData { Type = botType, Value = botVal, Range = botRange }
    };

    // ── Game Loop ────────────────────────────────────────────────────────────

    private void StartFirstTurn()
    {
        GameEvents.EncounterStarted?.Invoke(State.Config);
        BeginNextTurn();
    }

    private void BeginNextTurn()
    {
        State.CurrentTide = _tidesExecuted + 1;
        _turnManager.StartVigil();
        EmitSignal(SignalName.HandChanged);
        EmitDeckCounts();
    }

    private void ExecuteTideAndDusk()
    {
        _turnManager.EndVigil();
        _tideRunner.ExecuteTide(_tidesExecuted + 1, State);
        _tidesExecuted++;

        if (State.Weave?.IsGameOver == true) { EndEncounter(); return; }
        if (_tidesExecuted >= State.Config.TideCount) { BeginResolution(); return; }

        _turnManager.StartDusk();
        EmitSignal(SignalName.HandChanged);
        EmitDeckCounts();
    }

    private void EndDusk()
    {
        _turnManager.EndTurn();
        if (_tidesExecuted >= State.Config.TideCount) { BeginResolution(); return; }
        BeginNextTurn();
    }

    private void ExecuteRest()
    {
        _turnManager.Rest();
        EmitSignal(SignalName.HandChanged);
        EmitDeckCounts();
        BeginNextTurn();
    }

    private void BeginResolution()
    {
        _inResolution = true;
        _resolutionTurn = 0;
        State.CurrentPhase = CoreTurnPhase.Resolution;
        EmitSignal(SignalName.PhaseChanged, (int)CoreTurnPhase.Resolution);
        AdvanceResolutionTurn();
    }

    private void AdvanceResolutionTurn()
    {
        _resolutionTurn++;
        if (_resolutionTurn > State.Config.ResolutionTurns) { EndEncounter(); return; }

        GameEvents.ResolutionTurnStarted?.Invoke(_resolutionTurn);
        State.Deck?.RefillHand();
        EmitSignal(SignalName.HandChanged);
        EmitDeckCounts();
    }

    private void EndEncounter()
    {
        State.Warden?.OnResolution(State);
        var result = RewardCalculator.Calculate(State);
        GameEvents.EncounterEnded?.Invoke(result);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void EmitDeckCounts()
    {
        if (State.Deck == null) return;
        EmitSignal(SignalName.DeckCountsChanged,
            State.Deck.DrawPileCount, State.Deck.DiscardCount,
            State.Deck.DissolvedCount, State.Deck.DormantCount);
    }

    // ── Event Subscriptions ──────────────────────────────────────────────────

    private void SubscribeToGameEvents()
    {
        _hFearGeneratedQueue   = a => State.FearActions?.OnFearSpent(a);
        _hDreadAdvancedUpgrade = l => State.FearActions?.OnDreadAdvanced(l);
        _hNativeDefeated       = (_, __) => { State.Dread?.OnFearGenerated(1); GameEvents.FearGenerated?.Invoke(1); };
        GameEvents.FearGenerated  += _hFearGeneratedQueue;
        GameEvents.DreadAdvanced  += _hDreadAdvancedUpgrade;
        GameEvents.NativeDefeated += _hNativeDefeated;

        _hPhaseChanged = p => EmitSignal(SignalName.PhaseChanged, (int)p);
        GameEvents.PhaseChanged += _hPhaseChanged;

        _hTurnStarted = () => EmitSignal(SignalName.TurnStarted);
        GameEvents.TurnStarted += _hTurnStarted;

        _hTurnEnded = () => EmitSignal(SignalName.TurnEnded);
        GameEvents.TurnEnded += _hTurnEnded;

        _hRestStarted = () => EmitSignal(SignalName.RestStarted);
        GameEvents.RestStarted += _hRestStarted;

        _hCardPlayed        = (_, __) => { EmitSignal(SignalName.HandChanged); EmitDeckCounts(); };
        _hCardDissolved     = _ => EmitDeckCounts();
        _hCardDormant       = _ => EmitDeckCounts();
        _hCardRestDissolved = _ => EmitDeckCounts();
        _hCardAwakened      = _ => { EmitSignal(SignalName.HandChanged); EmitDeckCounts(); };
        GameEvents.CardPlayed        += _hCardPlayed;
        GameEvents.CardDissolved     += _hCardDissolved;
        GameEvents.CardDormant       += _hCardDormant;
        GameEvents.CardRestDissolved += _hCardRestDissolved;
        GameEvents.CardAwakened      += _hCardAwakened;

        _hElementChanged     = (e, v) => EmitSignal(SignalName.ElementChanged,    (int)e, v);
        _hThresholdTriggered = (e, t) => EmitSignal(SignalName.ThresholdTriggered, (int)e, t);
        _hElementsDecayed    = ()     => EmitSignal(SignalName.ElementsDecayed);
        GameEvents.ElementChanged     += _hElementChanged;
        GameEvents.ThresholdTriggered += _hThresholdTriggered;
        GameEvents.ElementsDecayed    += _hElementsDecayed;

        _hFearGeneratedRelay  = a  => EmitSignal(SignalName.FearGenerated,     a);
        _hFearActionQueued    = () => EmitSignal(SignalName.FearActionQueued);
        _hFearActionRevealed  = fa => EmitSignal(SignalName.FearActionRevealed, fa.Description);
        _hDreadAdvancedRelay  = l  => EmitSignal(SignalName.DreadAdvanced,     l);
        GameEvents.FearGenerated      += _hFearGeneratedRelay;
        GameEvents.FearActionQueued   += _hFearActionQueued;
        GameEvents.FearActionRevealed += _hFearActionRevealed;
        GameEvents.DreadAdvanced      += _hDreadAdvancedRelay;

        _hTideStepStarted     = s => EmitSignal(SignalName.TideStepStarted,    (int)s);
        _hActionCardRevealed  = c => EmitSignal(SignalName.ActionCardRevealed,  c.Name, c.Pool == ActionPool.Painful);
        _hNextActionPreviewed = c => EmitSignal(SignalName.NextActionPreviewed, c.Name, c.Pool == ActionPool.Painful);
        GameEvents.TideStepStarted     += _hTideStepStarted;
        GameEvents.ActionCardRevealed  += _hActionCardRevealed;
        GameEvents.NextActionPreviewed += _hNextActionPreviewed;

        _hInvaderArrived  = (i, t)    => EmitSignal(SignalName.InvaderArrived,   i.Id, t.Id, (int)i.UnitType);
        _hInvaderDefeated = i         => EmitSignal(SignalName.InvaderDefeated,  i.Id);
        _hInvaderAdvanced = (i, f, t) => EmitSignal(SignalName.InvaderAdvanced,  i.Id, f, t);
        GameEvents.InvaderArrived  += _hInvaderArrived;
        GameEvents.InvaderDefeated += _hInvaderDefeated;
        GameEvents.InvaderAdvanced += _hInvaderAdvanced;

        _hCorruptionChanged  = (t, p, l) => EmitSignal(SignalName.CorruptionChanged, t.Id, p, l);
        _hWeaveChanged       = v         => EmitSignal(SignalName.WeaveChanged,    v);
        _hCounterAttackReady = (t, p)    => EmitSignal(SignalName.CounterAttackReady, t.Id, p);
        _hHeartDamage        = t         => EmitSignal(SignalName.HeartDamageDealt, t.Id);
        GameEvents.CorruptionChanged  += _hCorruptionChanged;
        GameEvents.WeaveChanged       += _hWeaveChanged;
        GameEvents.CounterAttackReady += _hCounterAttackReady;
        GameEvents.HeartDamageDealt   += _hHeartDamage;

        _hResolutionTurnStarted = n => EmitSignal(SignalName.ResolutionTurnStarted, n);
        GameEvents.ResolutionTurnStarted += _hResolutionTurnStarted;
    }

    private void UnsubscribeFromGameEvents()
    {
        GameEvents.FearGenerated  -= _hFearGeneratedQueue;
        GameEvents.DreadAdvanced  -= _hDreadAdvancedUpgrade;
        GameEvents.NativeDefeated -= _hNativeDefeated;

        GameEvents.PhaseChanged      -= _hPhaseChanged;
        GameEvents.TurnStarted       -= _hTurnStarted;
        GameEvents.TurnEnded         -= _hTurnEnded;
        GameEvents.RestStarted       -= _hRestStarted;
        GameEvents.CardPlayed        -= _hCardPlayed;
        GameEvents.CardDissolved     -= _hCardDissolved;
        GameEvents.CardDormant       -= _hCardDormant;
        GameEvents.CardRestDissolved -= _hCardRestDissolved;
        GameEvents.CardAwakened      -= _hCardAwakened;

        GameEvents.ElementChanged     -= _hElementChanged;
        GameEvents.ThresholdTriggered -= _hThresholdTriggered;
        GameEvents.ElementsDecayed    -= _hElementsDecayed;

        GameEvents.FearGenerated      -= _hFearGeneratedRelay;
        GameEvents.FearActionQueued   -= _hFearActionQueued;
        GameEvents.FearActionRevealed -= _hFearActionRevealed;
        GameEvents.DreadAdvanced      -= _hDreadAdvancedRelay;

        GameEvents.TideStepStarted     -= _hTideStepStarted;
        GameEvents.ActionCardRevealed  -= _hActionCardRevealed;
        GameEvents.NextActionPreviewed -= _hNextActionPreviewed;

        GameEvents.InvaderArrived  -= _hInvaderArrived;
        GameEvents.InvaderDefeated -= _hInvaderDefeated;
        GameEvents.InvaderAdvanced -= _hInvaderAdvanced;

        GameEvents.CorruptionChanged  -= _hCorruptionChanged;
        GameEvents.WeaveChanged       -= _hWeaveChanged;
        GameEvents.CounterAttackReady -= _hCounterAttackReady;
        GameEvents.HeartDamageDealt   -= _hHeartDamage;

        GameEvents.ResolutionTurnStarted -= _hResolutionTurnStarted;
    }
}
