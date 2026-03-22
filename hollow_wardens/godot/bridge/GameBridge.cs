using Godot;
using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Data;
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

    [Signal] public delegate void TargetingModeChangedEventHandler(bool active);
    [Signal] public delegate void CardPlayFeedbackEventHandler(string message, string targetTerritoryId, int category);

    [Signal] public delegate void ThresholdPendingEventHandler(int element, int tier, string description);
    [Signal] public delegate void ThresholdExpiredEventHandler(int element, int tier);
    [Signal] public delegate void ThresholdResolvedEventHandler(int element, int tier, string description);

    [Signal] public delegate void PassiveUnlockedEventHandler(string passiveId);

    [Signal] public delegate void FearActionPendingEventHandler(string description, bool needsTarget);
    [Signal] public delegate void CounterAttackPendingGodotEventHandler(string territoryId, int pool);

    /// <summary>Emitted after BuildEncounter() completes — State is valid, first turn not yet started.</summary>
    [Signal] public delegate void EncounterReadyEventHandler();

    // ── Public State ─────────────────────────────────────────────────────────
    public static GameBridge? Instance { get; private set; }

    /// <summary>Set before the encounter starts to select which warden to play.</summary>
    public static string SelectedWardenId { get; set; } = "root";

    public EncounterState State { get; private set; } = null!;

    /// <summary>Current turn phase (from Core TurnManager).</summary>
    public CoreTurnPhase CurrentPhase => _turnManager.CurrentPhase;

    /// <summary>True when the deck needs a Rest this turn.</summary>
    public bool IsRestTurn => _turnManager.IsRestTurn;

    /// <summary>True during the Resolution turns at the end of an encounter.</summary>
    public bool IsInResolution => _inResolution;

    /// <summary>True while waiting for the player to select a territory target.</summary>
    public bool IsWaitingForTarget { get; private set; }

    /// <summary>Card awaiting target selection (valid when IsWaitingForTarget).</summary>
    public Card? PendingCard { get; private set; }

    /// <summary>Effect that requires targeting (valid when IsWaitingForTarget).</summary>
    public EffectData? PendingEffect { get; private set; }

    /// <summary>True when the pending play is a bottom half (Dusk), false for top half (Vigil).</summary>
    public bool IsPendingBottom { get; private set; }

    // ── Interactive Tide State ────────────────────────────────────────────────
    /// <summary>Current sub-phase within an interactive Tide sequence.</summary>
    public TideSubPhase TideSubPhase { get; private set; } = TideSubPhase.None;

    /// <summary>The fear action currently awaiting player confirmation (valid during FearActions sub-phase).</summary>
    public FearActionData? CurrentFearAction { get; private set; }

    /// <summary>True while a fear action is being presented to the player.</summary>
    public bool IsResolvingFearAction => TideSubPhase == TideSubPhase.FearActions && CurrentFearAction != null;

    /// <summary>Territory ID for the current counter-attack assignment (valid during CounterAttack sub-phase).</summary>
    public string? CounterAttackTerritory { get; private set; }

    /// <summary>Damage pool available for counter-attack assignment.</summary>
    public int CounterAttackPool { get; private set; }

    /// <summary>True while waiting for the player to assign counter-attack damage.</summary>
    public bool IsWaitingForCounterAttack { get; private set; }

    // ── Private Core Objects ─────────────────────────────────────────────────
    private CoreTurnManager   _turnManager        = null!;
    private TideRunner        _tideRunner         = null!;
    private ActionDeck        _actionDeck         = null!;
    private CadenceManager    _cadence            = null!;
    private SpawnManager      _spawn              = null!;
    private EffectResolver    _resolver           = null!;
    private ThresholdResolver _thresholdResolver  = null!;

    // ── Loop State ───────────────────────────────────────────────────────────
    private int  _tidesExecuted;
    private bool _inResolution;
    private int  _resolutionTurn;

    // ── Interactive Tide Fields ───────────────────────────────────────────────
    private int                    _currentTideNumber;
    private bool                   _isFirstTide;
    private ActionCard?            _currentActionCard;
    private List<FearActionData>   _pendingFearActions = new();
    private int                    _fearActionIndex;
    private bool                   _isFearTargeting;
    private bool                   _isThresholdTargeting;
    private Element                _pendingThresholdElement;
    private int                    _pendingThresholdTier;
    private List<Territory>        _counterAttackTargets = new();
    private int                    _counterAttackTargetIndex;

    // ── Event Handler Fields ──────────────────────────────────────────────────
    private Action<Element, int, string>?      _hThresholdPending;
    private Action<Element, int>?              _hThresholdExpired;
    private Action<Element, int, string>?      _hThresholdResolved;

    private Action<CoreTurnPhase>?             _hPhaseChanged;
    private Action?                            _hTurnStarted, _hTurnEnded, _hRestStarted;
    private Action<Card, CoreTurnPhase>?       _hCardPlayed;
    private Action<Card>?                      _hCardDissolved, _hCardDormant, _hCardRestDissolved, _hCardAwakened;
    private Action<Element, int>?              _hElementChanged;
    private Action<Element, int>?              _hThresholdTriggered;
    private Action<Element, int>?              _hThresholdAutoResolve;
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
        // BuildEncounter is deferred — WardenSelectController calls StartWithWarden() after selection.
        // If no WardenSelectController is present (e.g. in tests), call StartWithWarden("root") directly.
    }

    /// <summary>Called by WardenSelectController after the player chooses a warden.</summary>
    public void StartWithWarden(string wardenId)
    {
        SelectedWardenId = wardenId;
        BuildEncounter();
        SubscribeToGameEvents();
        EmitSignal(SignalName.EncounterReady);
        Callable.From(StartFirstTurn).CallDeferred();
    }

    public override void _ExitTree()
    {
        UnsubscribeFromGameEvents();
        GameEvents.ClearAll();
        Instance = null;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Returns a compact string encoding the current seed + all player actions for export/replay.</summary>
    public string ExportEncounterState()
    {
        int seed = State?.Random?.Seed ?? 0;
        return State?.ActionLog.ExportFull(seed) ?? $"SEED:{seed}|";
    }

    /// <summary>
    /// Parses an exported state string and replays the encounter with the recorded seed.
    /// Results are logged to the console; the current live encounter is not affected.
    /// </summary>
    public void ImportAndReplay(string data)
    {
        var (seed, rawActions) = ActionLog.ImportFull(data);
        GD.Print($"[Replay] seed={seed}  actions={rawActions.Length}");

        var resDir   = ProjectSettings.GlobalizePath("res://");
        var jsonPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(resDir, "..", "data", "wardens", "root.json"));

        var runner = new ReplayRunner(seed, rawActions, jsonPath);
        runner.ActionReplayed += (idx, action, state) =>
        {
            GD.Print($"[Replay] Action {idx}: {action.Type} card={action.CardId} target={action.TargetTerritoryId}");
        };
        runner.ReplayCompleted += state =>
        {
            GD.Print($"[Replay] Complete. Weave={state.Weave?.CurrentWeave} Tides={state.CurrentTide}");
        };

        runner.Replay();
    }

    /// <summary>True when a top-half play is currently permitted (Vigil under 2-play limit, or Resolution).</summary>
    public bool CanPlayTop()    => _inResolution || _turnManager.CanPlayTop();

    /// <summary>True when a bottom-half play is currently permitted (Dusk under 1-play limit).</summary>
    public bool CanPlayBottom() => _turnManager.CanPlayBottom();

    /// <summary>Resolves a player-pending threshold action, entering targeting mode when needed.</summary>
    public void ResolveThreshold(int elementIdx, int tier)
    {
        var element = (Element)elementIdx;
        var targetEffect = ThresholdResolver.GetTargetEffect(element, tier);
        if (targetEffect != null)
        {
            EnterThresholdTargetingMode(element, tier, targetEffect);
            return;
        }

        _thresholdResolver.Resolve(element, tier, State);
        EmitSignal(SignalName.CardPlayFeedback, $"★ {element} T{tier}", "", 3);
    }

    private void EnterThresholdTargetingMode(Element element, int tier, EffectData effect)
    {
        _isThresholdTargeting    = true;
        _pendingThresholdElement = element;
        _pendingThresholdTier    = tier;
        IsWaitingForTarget       = true;
        PendingCard              = null;
        PendingEffect            = effect;
        IsPendingBottom          = false;
        EmitSignal(SignalName.TargetingModeChanged, true);
    }

    private void ResolveThresholdTargeted(string territoryId)
    {
        var element = _pendingThresholdElement;
        var tier    = _pendingThresholdTier;
        _isThresholdTargeting = false;
        ExitTargetingMode();
        _thresholdResolver.Resolve(element, tier, State, territoryId);
        EmitSignal(SignalName.CardPlayFeedback, $"★ {element} T{tier}", territoryId, 3);
    }

    public void PlayTop(Card card)
    {
        bool inVigil = _turnManager.CurrentPhase == CoreTurnPhase.Vigil;
        if (!inVigil && !_inResolution) return;
        if (IsWaitingForTarget) return;

        if (TargetValidator.NeedsTarget(card.TopEffect))
        {
            EnterTargetingMode(card, card.TopEffect, isPendingBottom: false);
            return;
        }

        if (!_inResolution && !_turnManager.CanPlayTop()) return; // play limit reached (2 tops per Vigil)

        EmitCardPlayFeedback(card.TopEffect, card.Name, "", _inResolution ? 2 : 0);
        State.ActionLog.Record(new GameAction
        {
            TurnNumber = State.CurrentTide,
            Phase      = _turnManager.CurrentPhase,
            Type       = GameActionType.PlayTop,
            CardId     = card.Id
        });
        _turnManager.PlayTop(card); // effects fire here
        EmitSignal(SignalName.HandChanged);
        EmitDeckCounts();
    }

    public void PlayBottom(Card card)
    {
        if (_turnManager.CurrentPhase != CoreTurnPhase.Dusk) return;
        if (IsWaitingForTarget) return;

        if (TargetValidator.NeedsTarget(card.BottomEffect))
        {
            EnterTargetingMode(card, card.BottomEffect, isPendingBottom: true);
            return;
        }

        if (!_turnManager.CanPlayBottom()) return; // play limit reached (1 bottom per Dusk)

        EmitCardPlayFeedback(card.BottomEffect, card.Name, "", 1);
        State.ActionLog.Record(new GameAction
        {
            TurnNumber = State.CurrentTide,
            Phase      = _turnManager.CurrentPhase,
            Type       = GameActionType.PlayBottom,
            CardId     = card.Id
        });
        _turnManager.PlayBottom(card); // effects fire here
        EmitSignal(SignalName.HandChanged);
        EmitDeckCounts();
    }

    /// <summary>
    /// Completes a pending targeted play with the chosen territory.
    /// No-op when not in targeting mode.
    /// </summary>
    public void CompleteTargetedPlay(string territoryId)
    {
        if (!IsWaitingForTarget) return;

        if (_isThresholdTargeting)
        {
            ResolveThresholdTargeted(territoryId);
            return;
        }

        if (_isFearTargeting)
        {
            ResolveFearActionTargeted(territoryId);
            return;
        }

        if (PendingCard == null || PendingEffect == null) return;

        var card           = PendingCard;
        var effect         = PendingEffect;
        bool isPendingBot  = IsPendingBottom;
        var target         = new TargetInfo { TerritoryId = territoryId, SourceCard = card };

        bool canPlay = isPendingBot
            ? _turnManager.CanPlayBottom()
            : (_inResolution || _turnManager.CanPlayTop());

        ExitTargetingMode();
        if (!canPlay) return;

        EmitCardPlayFeedback(effect, card.Name, territoryId, isPendingBot ? 1 : (_inResolution ? 2 : 0));
        State.ActionLog.Record(new GameAction
        {
            TurnNumber = State.CurrentTide,
            Phase      = _turnManager.CurrentPhase,
            Type       = isPendingBot ? GameActionType.PlayBottom : GameActionType.PlayTop,
            CardId     = card.Id
        });
        State.ActionLog.Record(new GameAction
        {
            TurnNumber        = State.CurrentTide,
            Phase             = _turnManager.CurrentPhase,
            Type              = GameActionType.SelectTarget,
            TargetTerritoryId = territoryId
        });
        if (isPendingBot)
            _turnManager.PlayBottom(card, target); // effects fire here
        else
            _turnManager.PlayTop(card, target);    // effects fire here
        EmitSignal(SignalName.HandChanged);
        EmitDeckCounts();
    }

    /// <summary>Cancels targeting mode without resolving the pending card, threshold, or fear action.</summary>
    public void CancelTargeting()
    {
        if (!IsWaitingForTarget) return;
        if (_isThresholdTargeting)
        {
            _isThresholdTargeting = false;
            ExitTargetingMode();
            return;
        }
        if (_isFearTargeting)
        {
            // Reset fear targeting but stay in FearActions sub-phase (re-show overlay)
            _isFearTargeting = false;
            ExitTargetingMode();
            if (CurrentFearAction != null)
                EmitSignal(SignalName.FearActionPending, CurrentFearAction.Description, true);
            return;
        }
        ExitTargetingMode();
    }

    public void EndCurrentPhase()
    {
        if (_inResolution) { AdvanceResolutionTurn(); return; }

        State.ActionLog.Record(new GameAction
        {
            TurnNumber = State.CurrentTide,
            Phase      = _turnManager.CurrentPhase,
            Type       = GameActionType.SkipPhase
        });
        switch (_turnManager.CurrentPhase)
        {
            case CoreTurnPhase.Vigil: StartInteractiveTide(); break;
            case CoreTurnPhase.Tide:  OnTideSpacePressed();   break;
            case CoreTurnPhase.Dusk:  EndDusk();              break;
            case CoreTurnPhase.Rest:  ExecuteRest();          break;
        }
    }

    public void TriggerRest()
    {
        if (!_turnManager.IsRestTurn) return;
        State.ActionLog.Record(new GameAction
        {
            TurnNumber = State.CurrentTide,
            Phase      = _turnManager.CurrentPhase,
            Type       = GameActionType.Rest
        });
        ExecuteRest();
    }

    // ── Encounter Setup ──────────────────────────────────────────────────────

    private void BuildEncounter()
    {
        var random  = GameRandom.NewRandom();
        var balance = new HollowWardens.Core.Encounter.BalanceConfig();
        GD.Print($"Encounter seed: {random.Seed}");

        var territories = BoardState.CreatePyramid().Territories.Values.ToList();
        var dread       = new DreadSystem(balance);
        var presence    = new PresenceSystem(() => territories, balance.MaxPresencePerTerritory);

        var resDir   = ProjectSettings.GlobalizePath("res://");
        var jsonPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(resDir, "..", "data", "wardens", $"{SelectedWardenId}.json"));
        var wardenData = WardenLoader.Load(jsonPath);

        IWardenAbility warden = wardenData.WardenId switch
        {
            "root"  => new RootAbility(presence, balance),
            "ember" => new EmberAbility(),
            _       => throw new ArgumentException($"Unknown warden: {wardenData.WardenId}")
        };

        var gating = new PassiveGating(wardenData.WardenId);
        if (warden is RootAbility rootAbility)
            rootAbility.Gating = gating;
        gating.PassiveUnlocked += (id, _) => EmitSignal(SignalName.PassiveUnlocked, id);

        var config  = EncounterLoader.CreatePaleMarchStandard();
        var faction = new PaleMarchFaction();
        faction.HpBonus = balance.InvaderHpBonus;
        State = new EncounterState
        {
            Config        = config,
            Territories   = territories,
            Elements      = new ElementSystem(balance),
            Dread         = dread,
            Weave         = new WeaveSystem(balance.StartingWeave, balance.MaxWeave),
            Combat        = new CombatSystem(),
            Presence      = presence,
            Corruption    = new CorruptionSystem(),
            FearActions   = new FearActionSystem(dread, FearActionPool.Build(), random, balance),
            Warden        = warden,
            Random        = random,
            ActionLog     = new ActionLog(),
            PassiveGating = gating,
            Balance       = balance
        };

        var startingCards = wardenData.Cards.Where(c => c.IsStarting).ToList();
        State.Deck        = new DeckManager(warden, startingCards, random, shuffle: true);
        State.WardenData  = wardenData;

        _resolver   = new EffectResolver();
        _actionDeck = new ActionDeck(faction.BuildPainfulPool(), faction.BuildEasyPool(), random, shuffle: true);
        _cadence    = new CadenceManager(config.Cadence);
        _spawn      = new SpawnManager(config.Waves, random);

        _thresholdResolver = new ThresholdResolver();
        _turnManager = new CoreTurnManager(State, _resolver);
        _tideRunner  = new TideRunner(_actionDeck, _cadence, _spawn, faction, _resolver);
        _tideRunner.CounterAttackHandler = (t, pool, s) => null;

        InitialEncounterSetup();
    }

    private void InitialEncounterSetup()
    {
        // Spawn natives per config
        int nativeHp     = State.Balance.DefaultNativeHp;
        int nativeDamage = State.Balance.DefaultNativeDamage;
        foreach (var (territoryId, count) in State.Config.NativeSpawns)
        {
            var territory = State.GetTerritory(territoryId);
            if (territory == null || count <= 0) continue;
            for (int i = 0; i < count; i++)
                territory.Natives.Add(new Native { Hp = nativeHp, MaxHp = nativeHp, Damage = nativeDamage, TerritoryId = territoryId });
        }

        // Place starting Presence from warden data
        var startTerritory = State.GetTerritory(State.WardenData?.StartingPresence.Territory ?? "I1");
        if (startTerritory != null)
            startTerritory.PresenceCount = State.WardenData?.StartingPresence.Count ?? 1;

        // Preview first action card (Tide 1 will use it without re-drawing)
        var firstCard = _actionDeck.Draw(_cadence.NextPool());
        State.CurrentActionCard = firstCard;
        _tideRunner.PreloadPreview(firstCard);

        // Spawn Wave 1 invaders before first Vigil so A-row is populated at game start
        _tideRunner.SpawnInitialWave(State);
        GD.Print($"[InitialSetup] A-row invaders: A1={State.GetTerritory("A1")?.Invaders.Count ?? 0} A2={State.GetTerritory("A2")?.Invaders.Count ?? 0} A3={State.GetTerritory("A3")?.Invaders.Count ?? 0}");
    }

    // ── Game Loop ────────────────────────────────────────────────────────────

    private void StartFirstTurn()
    {
        GameEvents.EncounterStarted?.Invoke(State.Config);
        // Fire the initial card preview now that subscriptions are active
        if (State.CurrentActionCard != null)
            GameEvents.NextActionPreviewed?.Invoke(State.CurrentActionCard);
        // Announce Wave 1 pre-deployment (done during setup, before first Vigil)
        // Emitted here rather than in InitialEncounterSetup so UI subscribers are ready.
        EmitSignal(SignalName.CardPlayFeedback, "Wave 1 deployed to Arrival row", "", 2);
        BeginNextTurn();
    }

    private void BeginNextTurn()
    {
        State.CurrentTide = _tidesExecuted + 1;
        _turnManager.StartVigil();
        EmitSignal(SignalName.HandChanged);
        EmitDeckCounts();
    }

    // ── Interactive Tide State Machine ────────────────────────────────────────

    private void StartInteractiveTide()
    {
        _turnManager.EndVigil();
        _currentTideNumber = _tidesExecuted + 1;
        _isFirstTide       = _currentTideNumber == 1;
        _currentActionCard = _tideRunner.BeginTide(_currentTideNumber, State);

        if (_isFirstTide)
        {
            RunTideAdvanceArrive(); // Tide 1: skip FearActions/Activate/CounterAttack
        }
        else
        {
            _tideRunner.ApplyPassiveFear(State);
            _pendingFearActions = _tideRunner.DrainFearActions(State);
            _fearActionIndex    = 0;
            AdvanceFearQueue();
        }
    }

    private void OnTideSpacePressed()
    {
        switch (TideSubPhase)
        {
            case TideSubPhase.FearActions:
                ConfirmFearAction(); // Space confirms untargeted fear actions
                break;
            case TideSubPhase.WaitAfterCombat:
                TideSubPhase = TideSubPhase.None;
                _turnManager.StartDusk();
                EmitSignal(SignalName.HandChanged);
                EmitDeckCounts();
                break;
        }
    }

    // ── Fear Action Resolution ────────────────────────────────────────────────

    private void AdvanceFearQueue()
    {
        if (_fearActionIndex >= _pendingFearActions.Count)
        {
            RunTideActivate();
            return;
        }
        CurrentFearAction = _pendingFearActions[_fearActionIndex];
        TideSubPhase = TideSubPhase.FearActions;
        GameEvents.FearActionRevealed?.Invoke(CurrentFearAction); // debug log
        bool needsTarget = CurrentFearAction.Effect.Range > 0;

        // If targeted but no valid targets exist, skip this fear action (wasted)
        if (needsTarget)
        {
            var validTargets = TargetValidator.GetValidTargets(State, CurrentFearAction.Effect.Range, CurrentFearAction.Effect.Type);
            if (validTargets.Count == 0)
            {
                GD.Print($"[FearAction] No valid targets for '{CurrentFearAction.Description}' — skipped");
                _fearActionIndex++;
                CurrentFearAction = null;
                AdvanceFearQueue();
                return;
            }
        }

        EmitSignal(SignalName.FearActionPending, CurrentFearAction.Description, needsTarget);
        if (needsTarget)
            EnterFearTargetingMode(CurrentFearAction);
    }

    /// <summary>Confirms an untargeted fear action. No-op if the current fear action requires a target.</summary>
    public void ConfirmFearAction()
    {
        if (TideSubPhase != TideSubPhase.FearActions || CurrentFearAction == null) return;
        if (CurrentFearAction.Effect.Range > 0) return; // targeted: must use territory click
        ExecuteCurrentFearAction(new TargetInfo());
        _fearActionIndex++;
        CurrentFearAction = null;
        AdvanceFearQueue();
    }

    /// <summary>Completes a targeted fear action after the player clicks a territory.</summary>
    public void ResolveFearActionTargeted(string territoryId)
    {
        if (!_isFearTargeting || CurrentFearAction == null) return;
        _isFearTargeting = false;
        ExitTargetingMode();
        ExecuteCurrentFearAction(new TargetInfo { TerritoryId = territoryId });
        _fearActionIndex++;
        CurrentFearAction = null;
        AdvanceFearQueue();
    }

    private void ExecuteCurrentFearAction(TargetInfo target)
    {
        if (CurrentFearAction == null) return;
        State.FearActions?.BeginResolution(); // Bugfix: prevent fear loop during resolution
        try
        {
            var effect = _resolver.Resolve(CurrentFearAction.Effect);
            effect.Resolve(State, target);
        }
        catch (NotImplementedException) { }
        finally
        {
            State.FearActions?.EndResolution();
        }
    }

    private void EnterFearTargetingMode(FearActionData fa)
    {
        _isFearTargeting = true;
        IsWaitingForTarget = true;
        PendingCard        = null;
        PendingEffect      = fa.Effect;
        IsPendingBottom    = false;
        EmitSignal(SignalName.TargetingModeChanged, true);
    }

    // ── Activate + CounterAttack ──────────────────────────────────────────────

    private void RunTideActivate()
    {
        TideSubPhase = TideSubPhase.Activate;
        _tideRunner.RunActivate(_currentActionCard!, State);

        bool isProvoked = !_isFirstTide && (State.Combat?.IsProvokedAction(_currentActionCard!) ?? false);
        if (isProvoked)
            StartCounterAttackPhase();
        else
            RunTideAdvanceArrive();
    }

    private void StartCounterAttackPhase()
    {
        _counterAttackTargets      = _tideRunner.GetCounterAttackTargets(_currentActionCard!, State);
        _counterAttackTargetIndex  = 0;
        PresentNextCounterAttack();
    }

    private void PresentNextCounterAttack()
    {
        if (_counterAttackTargetIndex >= _counterAttackTargets.Count)
        {
            RunTideAdvanceArrive();
            return;
        }
        var territory = _counterAttackTargets[_counterAttackTargetIndex];
        int pool      = State.Combat?.CalculateNativeDamagePool(territory) ?? 0;

        CounterAttackTerritory  = territory.Id;
        CounterAttackPool       = pool;
        IsWaitingForCounterAttack = true;
        TideSubPhase = TideSubPhase.CounterAttack;

        GameEvents.CounterAttackReady?.Invoke(territory, pool);
        EmitSignal(SignalName.CounterAttackPendingGodot, territory.Id, pool);
    }

    /// <summary>Submits damage assignments for the current counter-attack territory.</summary>
    public void SubmitCounterAttack(Dictionary<string, int> invaderIdToDamage)
    {
        if (!IsWaitingForCounterAttack || CounterAttackTerritory == null) return;
        var territory = State.GetTerritory(CounterAttackTerritory);
        if (territory != null && invaderIdToDamage.Count > 0)
        {
            var assignments = new Dictionary<Invader, int>();
            foreach (var (id, dmg) in invaderIdToDamage)
            {
                var inv = territory.Invaders.FirstOrDefault(i => i.Id == id);
                if (inv != null) assignments[inv] = dmg;
            }
            if (assignments.Count > 0)
                State.Combat?.ApplyCounterAttack(territory, assignments);
        }
        ClearCounterAttackState();
        _counterAttackTargetIndex++;
        PresentNextCounterAttack();
    }

    /// <summary>Skips counter-attack for the current territory (assigns 0 damage).</summary>
    public void SkipCounterAttack()
    {
        if (!IsWaitingForCounterAttack) return;
        ClearCounterAttackState();
        _counterAttackTargetIndex++;
        PresentNextCounterAttack();
    }

    private void ClearCounterAttackState()
    {
        IsWaitingForCounterAttack = false;
        CounterAttackTerritory    = null;
        CounterAttackPool         = 0;
    }

    // ── Advance, Arrive, Preview ──────────────────────────────────────────────

    private void RunTideAdvanceArrive()
    {
        TideSubPhase = TideSubPhase.AdvanceArrive;
        _tideRunner.RunAdvance(_currentActionCard!, State);
        _tideRunner.RunArrive(_currentTideNumber, State);
        if (!_isFirstTide)
            _tideRunner.RunEscalate(_currentTideNumber, State);
        _tideRunner.RunPreview(_currentTideNumber, State);

        _tidesExecuted++;

        if (State.Weave?.IsGameOver == true) { EndEncounter(); return; }
        if (_tidesExecuted >= State.Config.TideCount) { BeginResolution(); return; }

        TideSubPhase = TideSubPhase.WaitAfterCombat;
        // Player presses Space (EndCurrentPhase → OnTideSpacePressed) to enter Dusk
    }

    private void EndDusk()
    {
        _thresholdResolver.ClearUnresolved();
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

    private void EnterTargetingMode(Card card, EffectData effect, bool isPendingBottom)
    {
        IsWaitingForTarget = true;
        PendingCard        = card;
        PendingEffect      = effect;
        IsPendingBottom    = isPendingBottom;
        EmitSignal(SignalName.TargetingModeChanged, true);
    }

    private void ExitTargetingMode()
    {
        IsWaitingForTarget = false;
        PendingCard        = null;
        PendingEffect      = null;
        IsPendingBottom    = false;
        EmitSignal(SignalName.TargetingModeChanged, false);
    }

    private void EmitCardPlayFeedback(EffectData effect, string cardName, string territoryId, int category = 0)
    {
        string effectDesc = effect.Type switch
        {
            EffectType.PlacePresence    => territoryId.Length > 0 ? $"Presence on {territoryId}" : "Place Presence",
            EffectType.GenerateFear     => $"+{effect.Value} Fear",
            EffectType.DamageInvaders   => FormatAmplified("Damage", effect.Value, territoryId),
            EffectType.ReduceCorruption => FormatAmplified("Cleanse", effect.Value, territoryId),
            EffectType.RestoreWeave     => $"+{effect.Value} Weave",
            EffectType.PushInvaders     => "Push Invaders",
            EffectType.Purify           => "Purify",
            _                           => effect.Type.ToString()
        };
        string msg = cardName.Length > 0 ? $"{cardName} → {effectDesc}" : effectDesc;
        EmitSignal(SignalName.CardPlayFeedback, msg, territoryId, category);
    }

    private string FormatAmplified(string label, int baseValue, string territoryId)
    {
        if (territoryId.Length == 0) return $"{baseValue} {label}";
        var territory = State.GetTerritory(territoryId);
        if (territory == null || !territory.HasPresence) return $"{baseValue} {label}";
        int amplified = baseValue + territory.PresenceCount;
        return $"{amplified} {label} ({baseValue}+{territory.PresenceCount})";
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

        _hElementChanged      = (e, v) => EmitSignal(SignalName.ElementChanged,    (int)e, v);
        _hThresholdTriggered  = (e, t) => EmitSignal(SignalName.ThresholdTriggered, (int)e, t);
        _hThresholdAutoResolve = (e, t) =>
        {
            _thresholdResolver.OnThresholdTriggered(e, t, State);
            State.PassiveGating?.OnThresholdTriggered(e, t);
        };
        _hElementsDecayed      = ()     => EmitSignal(SignalName.ElementsDecayed);
        GameEvents.ElementChanged     += _hElementChanged;
        GameEvents.ThresholdTriggered += _hThresholdTriggered;
        GameEvents.ThresholdTriggered += _hThresholdAutoResolve;
        GameEvents.ElementsDecayed    += _hElementsDecayed;

        _hThresholdPending  = (e, t, d) => EmitSignal(SignalName.ThresholdPending,  (int)e, t, d);
        _hThresholdExpired  = (e, t)    => EmitSignal(SignalName.ThresholdExpired,  (int)e, t);
        _hThresholdResolved = (e, t, d) => EmitSignal(SignalName.ThresholdResolved, (int)e, t, d);
        GameEvents.ThresholdPending  += _hThresholdPending;
        GameEvents.ThresholdExpired  += _hThresholdExpired;
        GameEvents.ThresholdResolved += _hThresholdResolved;

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
        GameEvents.ThresholdTriggered -= _hThresholdAutoResolve;
        GameEvents.ElementsDecayed    -= _hElementsDecayed;

        GameEvents.ThresholdPending  -= _hThresholdPending;
        GameEvents.ThresholdExpired  -= _hThresholdExpired;
        GameEvents.ThresholdResolved -= _hThresholdResolved;

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
