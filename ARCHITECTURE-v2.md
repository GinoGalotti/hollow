# Hollow Wardens — Architecture v2.0
> Godot 4.6 .NET (C#) — Complete rebuild plan informed by all design decisions (D1–D23)
> Replaces the Phase 5.5 migration approach with a clean rewrite of the logic layer.

---

## Why Rewrite Instead of Migrate

The Phase 1–5 code was built against a fundamentally different game:
- **Card model:** 3-layer (top/bottom/dissolve) → 2-layer (top/bottom-as-dissolve)
- **Draw model:** unspecified → refill-to-hand-limit with rest-dissolve
- **Elements:** halve decay, 2/3/5 thresholds → reduce-by-1, 4/7/11, phase-flexible, universal
- **Tide sequence:** Spawn → Advance → Activate → Native counter-attack → Escalate → Preview
- **Invader model:** single type per faction → 4 unit types with modifier system, two-pool action deck with cadence
- **Fear/Dread:** single system → Fear (resource) + Dread (escalation track), retroactive upgrade
- **Damage:** split evenly → asymmetric (auto-maximize invader, player-assigned native)

Migrating 111 tests that test the wrong behaviors is more work than writing new tests for the right ones. The Godot scene tree, signals, and singletons are all wired for the old flow. A clean rewrite of the **logic layer** (keeping Godot project structure, assets, and theme) is faster and produces better code.

---

## Core Architectural Principle: Separate Logic from Engine

The single biggest improvement over v1. Every game system is a **pure C# class** with no Godot dependency. Godot Nodes are thin wrappers that observe state and render it.

```
┌─────────────────────────────────────────────┐
│  PURE C# (no Godot references)              │
│                                             │
│  Models:     CardModel, TerritoryModel,     │
│              InvaderModel, NativeModel       │
│  Systems:    ElementSystem, DreadSystem,     │
│              CombatSystem, CorruptionSystem  │
│  Managers:   TurnManager, TideRunner,        │
│              DeckManager, CadenceManager     │
│  State:      GameState, EncounterState       │
│  Config:     BalanceConfig (all tunable      │
│              constants, stored on            │
│              EncounterState, read by all)    │
│  Wardens:    RootAbility, EmberAbility,      │
│              PassiveGating, IWardenAbility   │
│  Sim:        BotStrategy, EmberBotStrategy,  │
│              SimStatsCollector, ReplayRunner │
│  Events:     GameEvents (C# events, not      │
│              Godot signals)                  │
│                                             │
├─────────────────────────────────────────────┤
│  GODOT LAYER (Nodes, scenes, UI)            │
│                                             │
│  Adapters:   GameBridge (singleton Node that │
│              owns the logic layer and relays │
│              C# events → Godot signals)      │
│  Scenes:     Card.tscn, Territory.tscn,     │
│              Hand.tscn, etc.                │
│  UI:         All display, animation, input   │
│                                             │
└─────────────────────────────────────────────┘
```

**Why this matters:**
- Pure C# classes are testable with `dotnet test` — no Godot editor needed
- Logic changes don't break scenes; scene changes don't break logic
- AI (Claude Code) can iterate on game rules without touching UI
- Multiplayer or porting to another engine only requires replacing the Godot layer

---

## Project Structure (v2)

```
hollow_wardens/
├── project.godot
│
├── src/                          ← ALL pure C# logic lives here
│   ├── HollowWardens.Core/      ← Class library (.csproj, no Godot refs)
│   │   ├── Models/
│   │   │   ├── Card.cs               (CardId, Name, Elements[], Rarity, TopEffect, BottomEffect, IsDormant)
│   │   │   ├── Territory.cs          (Id, CorruptionPoints, CorruptionLevel, PresenceCount, Row)
│   │   │   ├── Invader.cs            (Id, UnitType, Hp, MaxHp, ShieldValue, TerritoryId)
│   │   │   ├── Native.cs             (Hp, MaxHp, Damage, ShieldValue, TerritoryId)
│   │   │   ├── BoardToken.cs         (abstract: TokenType, Hp?, TerritoryId)
│   │   │   ├── Infrastructure.cs     (extends BoardToken)
│   │   │   └── Enums.cs              (Element, CardRarity, UnitType, TerritoryRow, TideStep, etc.)
│   │   │
│   │   ├── Data/
│   │   │   ├── CardLoader.cs          [Obsolete] — use WardenLoader
│   │   │   ├── WardenLoader.cs        (loads unified warden JSON)
│   │   │   ├── WardenData.cs          (WardenData, PassiveData, ElementAffinity, StartingPresence)
│   │   │   ├── EncounterLoader.cs
│   │   │   └── FearActionPool.cs
│   │   │
│   │   ├── Effects/
│   │   │   ├── IEffect.cs            (interface: Resolve(EncounterState, TargetInfo))
│   │   │   ├── EffectType.cs         (enum of all effect types)
│   │   │   ├── EffectData.cs         (type + value + range — the data, not the behavior)
│   │   │   ├── EffectResolver.cs     (maps EffectData → IEffect implementation)
│   │   │   ├── PlacePresenceEffect.cs
│   │   │   ├── ReduceCorruptionEffect.cs
│   │   │   ├── DamageInvadersEffect.cs
│   │   │   ├── GenerateFearEffect.cs
│   │   │   ├── AmplificationHelper.cs (D28: +1 value per presence in target territory)
│   │   │   ├── SlowInvadersEffect.cs  (D29: halves invader movement next tide)
│   │   │   └── ... (one per effect type)
│   │   │
│   │   ├── Systems/
│   │   │   ├── ElementSystem.cs      (pool tracking, reduce-by-1 decay, threshold checking 4/7/11, once-per-tier triggering, banked thresholds)
│   │   │   ├── DreadSystem.cs        (total fear tracking, dread level 1–4, retroactive upgrade of queued actions)
│   │   │   ├── FearActionSystem.cs   (per-5-fear queueing, reveal queue, dread pool drawing)
│   │   │   ├── CorruptionSystem.cs   (points math, level computation, persistence rules between encounters)
│   │   │   ├── CombatSystem.cs       (invader→native auto-maximize, native→invader player-assigned, shield logic)
│   │   │   ├── PresenceSystem.cs     (placement validation, adjacency, network fear calculation for Root)
│   │   │   ├── VulnerabilityWiring.cs (D28: desecration destroys presence)
│   │   │   └── WeaveSystem.cs        (run health, heart damage, drain, restoration)
│   │   │
│   │   ├── Encounter/
│   │   │   ├── EncounterState.cs     (the full mutable state of an active encounter)
│   │   │   ├── EncounterConfig.cs    (immutable config: tide count, cadence, waves, tier)
│   │   │   ├── BalanceConfig.cs      (all tunable constants — stored on EncounterState, read by all systems)
│   │   │   ├── TideRunner.cs         (executes: Fear → Activate → Counter → Advance → Arrive → Escalate → Preview)
│   │   │   ├── CadenceManager.cs     (two-pool: painful/easy, max streak, easy frequency, hand-authored override)
│   │   │   ├── SpawnManager.cs       (weighted wave options, location preview, composition reveal)
│   │   │   ├── ActionDeck.cs         (per-faction: shuffle, draw, escalation card injection)
│   │   │   └── ResolutionRunner.cs   (2/3/1 turns by tier, breach detection, reward calculation)
│   │   │
│   │   ├── Cards/
│   │   │   ├── DeckManager.cs        (refill model: draw to hand limit, rest = shuffle discard + rest-dissolve)
│   │   │   ├── HandManager.cs        (current hand, play validation, phase restrictions)
│   │   │   └── CardPool.cs           (starting deck, draft pool, dissolved pile, dormant tracking)
│   │   │
│   │   ├── Invaders/
│   │   │   ├── InvaderFaction.cs     (faction identity, action pools, unit type roster)
│   │   │   ├── UnitTypeModifier.cs   (abstract: ModifyActivate, ModifyAdvance, ModifyArrive)
│   │   │   ├── PaleMarch/
│   │   │   │   ├── PaleMarchFaction.cs
│   │   │   │   ├── Marcher.cs        (no modifiers)
│   │   │   │   ├── Ironclad.cs       (+1 corruption on Ravage, alternating movement, etc.)
│   │   │   │   ├── Outrider.cs       (+1 movement always, never rests, etc.)
│   │   │   │   └── Pioneer.cs        (build infrastructure after any action)
│   │   │   └── InvaderPathfinding.cs (pyramid movement: toward Heart, through Presence/Natives)
│   │   │
│   │   ├── Wardens/
│   │   │   ├── IWardenAbility.cs     (WardenId, OnBottomPlayed, OnRestDissolve, OnResolution, OnRest,
│   │   │   │                          OnTideStart, CalculatePassiveFear, GetMovementPenalty,
│   │   │   │                          ProvokesNatives, PresenceBlockLevel — all with defaults)
│   │   │   ├── RootAbility.cs        (dormancy, network fear + cap, assimilation per-PresenceCount,
│   │   │   │                          D28: sacrifice, D29: GetMovementPenalty, ProvokesNatives, OnRest)
│   │   │   ├── EmberAbility.cs       (Flame Out, Ash Trail, Scorched Earth, Heat Wave, Ember Fury,
│   │   │   │                          PresenceBlockLevel=3 — full implementation)
│   │   │   ├── PassiveGating.cs      (per-encounter passive unlock tracking; Root: 3 base + 3 unlockable;
│   │   │   │                          Ember: 3 base + 4 unlockable — see D32)
│   │   │   └── VeilAbility.cs        (stub)
│   │   │
│   │   ├── Turn/
│   │   │   ├── TurnManager.cs        (state machine: Vigil → Tide → Dusk, rest handling)
│   │   │   ├── TurnPhase.cs          (enum: Vigil, Tide, Dusk, Rest, Resolution)
│   │   │   └── TurnActions.cs        (PlayTop, PlayBottom, Rest, SkipDusk, AssignCounterDamage)
│   │   │
│   │   ├── Map/
│   │   │   ├── TerritoryGraph.cs     (static adjacency, pyramid 3-2-1, row classification, CanAttackHeart)
│   │   │   └── BoardState.cs         (all territories + their contents, convenience queries)
│   │   │
│   │   ├── Run/
│   │   │   ├── GameState.cs          (persistent run state: weave, dread level, total fear, deck, corruption carry)
│   │   │   ├── RunConfig.cs          (realm structure, encounter sequence, difficulty)
│   │   │   ├── RewardCalculator.cs   (clean/weathered/breach, draft options, weave restore)
│   │   │   ├── EncounterRunner.cs    (full encounter lifecycle: start → tides → resolution → result)
│   │   │   ├── ReplayRunner.cs       (deterministic replay from seed + action string)
│   │   │   ├── IPlayerStrategy.cs    (interface for automated/bot play)
│   │   │   ├── BotStrategy.cs        (Root bot: presence → damage → cleanse → fear → any card)
│   │   │   ├── EmberBotStrategy.cs   (Ember bot: presence → damage → cleanse at 7+ corruption)
│   │   │   ├── SimStats.cs           (per-encounter + per-tide data model)
│   │   │   └── SimStatsCollector.cs  (aggregates stats across N encounters)
│   │   │
│   │   └── Events/
│   │       └── GameEvents.cs         (static C# events — the logic layer's event bus)
│   │
│   ├── HollowWardens.Sim/       ← Console app for balance simulation (see D34)
│   │   ├── Program.cs             (CLI: --seeds, --warden, --verbose, --profile)
│   │   ├── SimProfile.cs          (JSON-driven config with warden/encounter/balance overrides)
│   │   ├── SimProfileApplier.cs   (applies SimProfile overrides to BalanceConfig)
│   │   └── VerboseLogger.cs       (turn-by-turn decision logs with bot reasoning)
│   │
│   └── HollowWardens.Tests/     ← xUnit test project, references Core only
│       ├── Systems/
│       │   ├── ElementSystemTests.cs
│       │   ├── DreadSystemTests.cs
│       │   ├── CombatSystemTests.cs
│       │   └── CorruptionSystemTests.cs
│       ├── Encounter/
│       │   ├── TideRunnerTests.cs
│       │   ├── CadenceManagerTests.cs
│       │   └── SpawnManagerTests.cs
│       ├── Cards/
│       │   ├── DeckManagerTests.cs
│       │   ├── RefillModelTests.cs
│       │   └── RestDissolveTests.cs
│       ├── Wardens/
│       │   ├── RootDormancyTests.cs
│       │   └── RootNetworkFearTests.cs
│       └── Integration/
│           ├── FullEncounterTests.cs
│           └── TurnSequenceTests.cs
│
├── godot/                        ← ALL Godot-specific code lives here
│   ├── bridge/
│   │   ├── GameBridge.cs             (autoload singleton: owns Core objects, relays C# events → Godot signals)
│   │   ├── InputHandler.cs           (translates Godot input → TurnActions)
│   │   └── AudioBridge.cs            (plays sounds on game events)
│   │
│   ├── scenes/
│   │   ├── game/
│   │   │   ├── Game.tscn
│   │   │   ├── Encounter.tscn
│   │   │   └── RealmMap.tscn
│   │   ├── entities/
│   │   │   ├── CardView.tscn         (renders a Card model)
│   │   │   ├── InvaderView.tscn      (renders an Invader model)
│   │   │   ├── TerritoryView.tscn    (renders a Territory model)
│   │   │   ├── NativeView.tscn
│   │   │   └── PresenceView.tscn
│   │   ├── ui/
│   │   │   ├── HandDisplay.tscn
│   │   │   ├── DeckCounter.tscn          (visible deck size for rest anticipation)
│   │   │   ├── ElementTrackerHUD.tscn    (6 counters with 4/7/11 markers)
│   │   │   ├── DreadBar.tscn            (bar with threshold markers at 15/30/45)
│   │   │   ├── FearActionQueue.tscn     (face-down cards with flip animation)
│   │   │   ├── WeaveBar.tscn
│   │   │   ├── TidePreview.tscn         (shows next action card + arrival locations)
│   │   │   ├── PhaseIndicator.tscn
│   │   │   ├── CounterAttackUI.tscn     (player assigns native damage to invaders)
│   │   │   ├── EncounterResult.tscn
│   │   │   └── RealmReward.tscn
│   │   └── menus/
│   │       ├── MainMenu.tscn
│   │       └── RunStart.tscn
│   │
│   ├── views/                    ← C# scripts for scene nodes (observe models, render)
│   │   ├── CardViewController.cs
│   │   ├── TerritoryViewController.cs
│   │   ├── HandDisplayController.cs
│   │   ├── ElementTrackerController.cs
│   │   ├── DreadBarController.cs
│   │   ├── FearActionQueueController.cs
│   │   ├── CounterAttackController.cs
│   │   ├── TidePreviewController.cs
│   │   ├── PhaseIndicatorController.cs
│   │   ├── PassivePanelController.cs   (warden passives panel — locked/unlocked state)
│   │   └── WardenSelectController.cs   (warden selection screen — shown before encounter)
│   │
│   └── resources/
│       ├── cards/
│       │   ├── root/             (10 starting + 18 draft, loaded from JSON)
│       │   └── shared/
│       ├── encounters/
│       │   └── realm1/           (encounter configs with cadence + wave data)
│       ├── factions/
│       │   └── pale_march/       (action cards, unit types, escalation schedule)
│       ├── fear_actions/
│       │   ├── global/
│       │   └── pale_march/
│       └── wardens/
│
├── data/                         ← JSON source files (compiled to resources)
│   ├── cards-root.json
│   ├── encounters-realm1.json
│   ├── fear-actions.json
│   ├── pale-march-actions.json
│   └── pale-march-waves.json
│
└── assets/
    ├── art/kenney/
    ├── audio/
    ├── fonts/
    └── hollow_wardens_theme.tres
```

---

## Recent Architecture Additions (D28–D29)

### Data layer
- `Data/WardenLoader.cs` — loads unified warden JSON (passives + cards + metadata). Replaces CardLoader for new warden format.
- `Data/WardenData.cs` — data classes: `WardenData`, `ElementAffinity`, `StartingPresence`, `PassiveData`.
- `Data/CardLoader.cs` — marked `[Obsolete]`, kept for backward compatibility.

### Effects layer
- `Effects/AmplificationHelper.cs` — D28: static helper, +1 value per presence in target territory. Used by EffectResolver for all territorial effects.
- `Effects/SlowInvadersEffect.cs` — D29: marks invaders as slowed (halved movement next tide).

### Systems layer
- `Systems/VulnerabilityWiring.cs` — D28: event wiring for Desecration (Level 3 destroys presence).

### Run layer
- `Run/ReplayRunner.cs` — deterministic replay from exported seed + action strings.
- `Run/BotStrategy.cs`, `Run/EmberBotStrategy.cs` — deterministic AI (`IPlayerStrategy`) for Root and Ember. Used by the Sim project.
- `Run/SimStatsCollector.cs` / `Run/SimStats.cs` — per-encounter and per-tide statistics collection.
- `Run/EncounterRunner.cs` — full encounter lifecycle used by both sim and replay.

### Encounter layer
- `Encounter/BalanceConfig.cs` — D34: all tunable constants centralized. Stored on `EncounterState`, read by all systems. SimProfile overrides populate this before encounter start.

### Sim project (D34)
- `src/HollowWardens.Sim/` — console app for balance testing. `Program.cs` CLI with `--seeds`, `--warden`, `--verbose`, `--profile` flags.
- `SimProfile.cs` / `SimProfileApplier.cs` — JSON-driven A/B testing configuration.
- `VerboseLogger.cs` — detailed turn-by-turn decision logging (first 5 encounters + all breaches).

### Encounter layer changes
- `EncounterState.WardenData` — holds loaded warden definition for UI access.
- `TideRunner` — D29: provocation logic, slow reset, wave offset (tide N spawns wave N+1).
- `CombatSystem.GetSteps` — D29: applies SlowInvaders halving + Network Slow penalty.

### Wardens layer changes
- `IWardenAbility` — D29: added `GetMovementPenalty`, `ProvokesNatives`, `OnRest`. D31/D33: added `OnTideStart`, `PresenceBlockLevel`. Full list: `WardenId`, `OnBottomPlayed`, `OnRestDissolve`, `OnResolution`, `OnRest`, `OnTideStart`, `CalculatePassiveFear`, `GetMovementPenalty`, `ProvokesNatives`, `PresenceBlockLevel`.
- `RootAbility` — implements all D28/D29 mechanics + D30 assimilation nerf (per-PresenceCount) + D31 NetworkFearCap.
- `EmberAbility` — full implementation: Flame Out, Ash Trail, Scorched Earth, Heat Wave, Ember Fury, PresenceBlockLevel=3.
- `PassiveGating` — D32: per-encounter unlock tracking. Root: 3 base + 3 unlockable. Ember: 3 base + 4 unlockable.
- `Effects/EmberFuryHelper.cs` — D33: applies Ember Fury bonus damage (+1 per Tainted+ territory).

### Turn layer changes
- `TurnActions`/`TurnManager` — D28: `SacrificePresence`. D29: `Rest` accepts growth target.

---

## The GameEvents Bus (Pure C#)

The logic layer communicates through static C# events. No Godot signals in Core.

```csharp
// src/HollowWardens.Core/Events/GameEvents.cs
public static class GameEvents
{
    // Turn flow
    public static event Action<TurnPhase> PhaseChanged;
    public static event Action TurnStarted;
    public static event Action TurnEnded;
    public static event Action RestStarted;

    // Card actions
    public static event Action<Card, TurnPhase> CardPlayed;        // top or bottom, which phase
    public static event Action<Card> CardDissolved;                 // bottom played (gone for encounter)
    public static event Action<Card> CardDormant;                   // Root: bottom → dormant
    public static event Action<Card> CardRestDissolved;             // lost to rest-dissolve
    public static event Action<Card> CardAwakened;                  // dormant → active
    public static event Action<int> DeckRefilled;                   // cards drawn at Vigil start
    public static event Action<int> DeckShuffled;                   // rest: discard → deck

    // Elements
    public static event Action<Element, int> ElementChanged;        // element pool updated
    public static event Action<Element, int> ThresholdTriggered;    // tier 1/2/3 fired
    public static event Action ThresholdBanked;                     // player banked for later phase
    public static event Action ElementsDecayed;                     // end-of-turn decay

    // Fear & Dread
    public static event Action<int> FearGenerated;                  // amount
    public static event Action<int> FearSpent;                      // amount
    public static event Action FearActionQueued;                    // face-down card added
    public static event Action<FearActionData> FearActionRevealed;  // card flipped
    public static event Action<int> DreadAdvanced;                  // new dread level
    public static event Action DreadUpgradeApplied;                 // queued actions upgraded

    // Tide
    public static event Action<TideStep> TideStepStarted;          // Fear/Activate/Advance/Arrive/Escalate/Preview
    public static event Action<ActionCard> ActionCardRevealed;      // which action this Tide
    public static event Action<ActionCard> NextActionPreviewed;     // end-of-Tide preview

    // Combat
    public static event Action<Invader, Territory> InvaderActivated;
    public static event Action<Invader, string, string> InvaderAdvanced;  // from, to
    public static event Action<Invader, Territory> InvaderArrived;
    public static event Action<Invader> InvaderDefeated;
    public static event Action<Native, Territory> NativeDamaged;
    public static event Action<Native, Territory> NativeDefeated;
    public static event Action<Territory, int> CounterAttackReady;  // territory, total damage pool — UI must assign

    // Territory
    public static event Action<Territory, int, int> CorruptionChanged;  // territory, new points, new level
    public static event Action<Territory> HeartDamageDealt;              // invader marched on Heart
    public static event Action<int> WeaveChanged;                       // new weave value

    // Encounter
    public static event Action<EncounterConfig> EncounterStarted;
    public static event Action<EncounterResult> EncounterEnded;
    public static event Action<int> ResolutionTurnStarted;
    public static event Action<SpawnWave> WaveLocationsRevealed;    // location preview
    public static event Action<SpawnWave> WaveCompositionRevealed;  // unit breakdown

    // Helper to clear all subscribers (call between encounters/tests)
    public static void ClearAll() { /* reflection or manual nulling */ }
}
```

**GameBridge** (Godot autoload) subscribes to these C# events and re-emits them as Godot signals for the UI layer. It also owns the `TurnManager`, `EncounterState`, and `GameState` instances.

---

## Key System Designs

### ElementSystem

```csharp
public class ElementSystem
{
    private readonly Dictionary<Element, int> _pool = new();
    private readonly HashSet<(Element, int)> _firedThisurn = new();  // (element, tier) pairs
    private readonly List<(Element, int)> _bankedEffects = new();    // banked for other phase

    private static readonly int[] Thresholds = { 4, 7, 11 };

    public int Get(Element e) => _pool.GetValueOrDefault(e);

    public void AddElements(Element[] elements, int multiplier = 1)
    {
        foreach (var e in elements)
        {
            _pool[e] = _pool.GetValueOrDefault(e) + multiplier;
            GameEvents.ElementChanged?.Invoke(e, _pool[e]);
            CheckThresholds(e);
        }
    }

    public void Decay()
    {
        foreach (var key in _pool.Keys.ToList())
        {
            _pool[key] = Math.Max(0, _pool[key] - 1);
            GameEvents.ElementChanged?.Invoke(key, _pool[key]);
        }
        _firedThisTurn.Clear();
        _bankedEffects.Clear();  // unresolved banked effects are lost
        GameEvents.ElementsDecayed?.Invoke();
    }

    public void OnNewTurn()
    {
        _firedThisTurn.Clear();
        // Check carryover thresholds (for Rest turns)
        foreach (var e in _pool.Keys)
            CheckThresholds(e);
    }

    private void CheckThresholds(Element e)
    {
        int val = _pool.GetValueOrDefault(e);
        foreach (int t in Thresholds)
        {
            int tier = Array.IndexOf(Thresholds, t) + 1;
            if (val >= t && !_firedThisTurn.Contains((e, tier)))
            {
                _firedThisTurn.Add((e, tier));
                GameEvents.ThresholdTriggered?.Invoke(e, tier);
                // Player decides: resolve now or bank for other phase
            }
        }
    }
}
```

### TideRunner

```csharp
public class TideRunner
{
    private readonly EncounterState _state;
    private readonly CombatSystem _combat;
    private readonly FearActionSystem _fear;
    private readonly SpawnManager _spawn;
    private readonly CadenceManager _cadence;
    private readonly ActionDeck _actionDeck;

    public void ExecuteTide(int tideNumber)
    {
        // 1. Fear Actions
        GameEvents.TideStepStarted?.Invoke(TideStep.FearActions);
        _fear.RevealAndResolveQueued(_state);

        // 2. Activate — current action card (previewed last Tide)
        GameEvents.TideStepStarted?.Invoke(TideStep.Activate);
        var actionCard = _state.CurrentActionCard;
        GameEvents.ActionCardRevealed?.Invoke(actionCard);
        foreach (var territory in _state.TerritoriesWithInvaders())
        {
            _combat.ExecuteActivate(actionCard, territory, _state);
        }

        // 3. Native counter-attack (player-assigned)
        GameEvents.TideStepStarted?.Invoke(TideStep.CounterAttack);
        foreach (var territory in _state.TerritoriesWithSurvivingNatives())
        {
            int damagePool = _combat.CalculateNativeDamagePool(territory);
            if (damagePool > 0)
            {
                // Signal UI to let player assign damage
                GameEvents.CounterAttackReady?.Invoke(territory, damagePool);
                // Block until player submits assignment (async or coroutine)
            }
        }

        // 4. Advance
        GameEvents.TideStepStarted?.Invoke(TideStep.Advance);
        _combat.ExecuteAdvance(actionCard, _state);
        _combat.ExecuteHeartMarch(_state);  // I1 invaders that were there before this Tide

        // 5. Arrive
        GameEvents.TideStepStarted?.Invoke(TideStep.Arrive);
        var wave = _spawn.ResolveWave(tideNumber);  // picks weighted option, reveals composition
        GameEvents.WaveCompositionRevealed?.Invoke(wave);
        _spawn.PlaceUnits(wave, _state);

        // 6. Escalate (every 3 Tides)
        if (tideNumber > 0 && tideNumber % 3 == 0)
        {
            GameEvents.TideStepStarted?.Invoke(TideStep.Escalate);
            _actionDeck.AddEscalationCard(tideNumber);
        }

        // 7. Preview next Tide
        GameEvents.TideStepStarted?.Invoke(TideStep.Preview);
        var nextPool = _cadence.GetNextPool(tideNumber + 1);
        var nextAction = _actionDeck.DrawFromPool(nextPool);
        _state.CurrentActionCard = nextAction;
        GameEvents.NextActionPreviewed?.Invoke(nextAction);

        var nextLocations = _spawn.PreviewLocations(tideNumber + 1);
        GameEvents.WaveLocationsRevealed?.Invoke(nextLocations);
    }
}
```

### DeckManager (Refill Model)

```csharp
public class DeckManager
{
    private List<Card> _drawPile;
    private List<Card> _discardPile = new();
    private List<Card> _hand = new();
    private List<Card> _dissolvedThisEncounter = new();
    private readonly int _handLimit;
    private readonly IWardenAbility _warden;

    public int DrawPileCount => _drawPile.Count;
    public int HandCount => _hand.Count;
    public IReadOnlyList<Card> Hand => _hand;

    public void RefillHand()
    {
        while (_hand.Count < _handLimit && _drawPile.Count > 0)
        {
            _hand.Add(_drawPile[0]);
            _drawPile.RemoveAt(0);
        }
        GameEvents.DeckRefilled?.Invoke(_hand.Count);
    }

    public void PlayTop(Card card)
    {
        _hand.Remove(card);
        _discardPile.Add(card);
        GameEvents.CardPlayed?.Invoke(card, TurnPhase.Vigil);
    }

    public void PlayBottom(Card card, EncounterTier tier)
    {
        _hand.Remove(card);
        // Let warden override behavior (Root: dormancy)
        var result = _warden.OnBottomPlayed(card, tier);
        switch (result)
        {
            case BottomResult.Dissolved:
                _dissolvedThisEncounter.Add(card);
                GameEvents.CardDissolved?.Invoke(card);
                break;
            case BottomResult.Dormant:
                card.IsDormant = true;
                _drawPile.Add(card);  // stays in deck as dead draw
                GameEvents.CardDormant?.Invoke(card);
                break;
            case BottomResult.PermanentlyRemoved:
                // Boss: double-dissolve of dormant card
                GameEvents.CardDissolved?.Invoke(card);
                break;
        }
        GameEvents.CardPlayed?.Invoke(card, TurnPhase.Dusk);
    }

    public void Rest()
    {
        GameEvents.RestStarted?.Invoke();

        // Shuffle discards back into draw pile
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile);

        // Rest-dissolve: remove 1 random card
        if (_drawPile.Count > 0)
        {
            int index = Random.Shared.Next(_drawPile.Count);
            var victim = _drawPile[index];
            _drawPile.RemoveAt(index);

            var result = _warden.OnRestDissolve(victim);
            switch (result)
            {
                case BottomResult.Dissolved:
                    _dissolvedThisEncounter.Add(victim);
                    GameEvents.CardRestDissolved?.Invoke(victim);
                    break;
                case BottomResult.Dormant:  // Root
                    victim.IsDormant = true;
                    _drawPile.Add(victim);
                    GameEvents.CardDormant?.Invoke(victim);
                    break;
            }
        }

        GameEvents.DeckShuffled?.Invoke(_drawPile.Count);
    }
}
```

### CadenceManager

```csharp
public class CadenceManager
{
    private readonly int _maxPainfulStreak;
    private readonly int _easyFrequency;  // force easy after this many painful
    private readonly string[] _overridePattern;  // null = use rules, ["P","E","P",...] = hand-authored
    private int _painfulStreak = 0;
    private int _tideIndex = 0;

    public ActionPool GetNextPool(int tideNumber)
    {
        _tideIndex = tideNumber;

        // Hand-authored pattern takes priority
        if (_overridePattern != null && tideNumber < _overridePattern.Length)
            return _overridePattern[tideNumber] == "P" ? ActionPool.Painful : ActionPool.Easy;

        // Rule-based: force easy after max streak
        if (_painfulStreak >= _maxPainfulStreak)
        {
            _painfulStreak = 0;
            return ActionPool.Easy;
        }

        // Default to painful, increment streak
        _painfulStreak++;
        return ActionPool.Painful;
    }

    public void OnEasyDrawn() => _painfulStreak = 0;
}
```

---

## Build Order (Phases)

### Phase 1 — Foundation (est. 2–3 days)
> Goal: Models, events, and basic systems testable in isolation.

1. Create `HollowWardens.Core` class library project (no Godot refs)
2. Create `HollowWardens.Tests` xUnit project
3. Implement **Models**: Card, Territory, Invader, Native, BoardToken, Enums
4. Implement **TerritoryGraph**: pyramid 3-2-1 adjacency, row classification
5. Implement **GameEvents**: all event declarations + ClearAll()
6. Implement **CorruptionSystem**: points math, level thresholds (3/8/15), persistence rules
7. **Tests**: Territory adjacency, corruption level computation, persistence between encounters

**Milestone: `dotnet test` passes 20+ tests, zero Godot code touched.**

### Phase 2 — Card Engine (est. 2–3 days)
> Goal: Refill draw model working, rest-dissolve, dormancy.

1. Implement **DeckManager**: refill-to-hand-limit, play top (→ discard), play bottom (→ dissolved/dormant), rest (shuffle + rest-dissolve)
2. Implement **HandManager**: current hand, play validation
3. Implement **CardPool**: starting deck init, dissolved tracking, dormant tracking
4. Implement **IWardenAbility** interface + **RootAbility** (dormancy on bottom, dormancy on rest-dissolve, awaken)
5. Implement **EffectData** + **EffectResolver** (stub implementations — effects don't need to *do* anything yet, just be dispatchable)
6. **Tests**: Refill math (10 cards, hand 5, 4 play turns then rest), rest-dissolve removes 1 card, Root dormancy flow, awaken-all, deck size after 2 cycles

**Milestone: Can simulate 8 turns of card play/rest with correct deck math.**

### Phase 3 — Element & Dread Systems (est. 2 days)
> Goal: Element engine building and fear/dread pipeline working.

1. Implement **ElementSystem**: pool tracking, reduce-by-1 decay, threshold checking at 4/7/11, once-per-tier-per-turn, banked effects, rest-turn carryover check
2. Implement **DreadSystem**: total fear tracking, dread level computation (every 15), advance notification
3. Implement **FearActionSystem**: per-5-fear queueing, reveal queue, dread pool drawing, retroactive upgrade on dread advance
4. Wire elements to card play: top = ×1, bottom = ×2
5. **Tests**: Element accumulation over 5 turns (verify 4/7 thresholds fire at right turns), decay math, dread level transitions, retroactive upgrade of queued actions, rest turn threshold firing

**Milestone: Can simulate element engine building across turns, verify threshold triggers match the math tables from the design doc.**

### Phase 4 — Encounter Engine (est. 3–4 days)
> Goal: Full Tide sequence running, invaders with unit types.

1. Implement **CadenceManager**: two-pool (painful/easy), max streak, override pattern
2. Implement **ActionDeck**: shuffle, draw from pool, escalation card injection
3. Implement **SpawnManager**: weighted wave options, location preview, composition reveal
4. Implement **CombatSystem**: invader activate with unit-type modifiers, invader→native auto-maximize kills, native counter-attack pool (player-assigned stub — just auto-assign lowest HP for now), shield logic, heart march
5. Implement **Pale March faction**: MarkerModifier, IroncladModifier, OutriderModifier, PioneerModifier
6. Implement **TideRunner**: full sequence (Fear → Activate → Counter → Advance → Arrive → Escalate → Preview)
7. Implement **EncounterState**: mutable state container, territory queries
8. Implement **TurnManager** state machine: Vigil → Tide → Dusk → (Rest), phase transitions
9. **Tests**: Cadence patterns (P-E-P-E, P-P-E, P-P-P-E), action deck cycling + escalation, Ironclad +1 corruption on Ravage, Outrider +1 movement, Pioneer infrastructure placement, Tide sequence order verification, heart march with grace period

**Milestone: Can simulate a full 7-Tide standard encounter purely in tests. Tide sequence, invader movement, combat, element building, rest cycling — all working.**

### Phase 5 — Root Warden Integration (est. 2 days)
> Goal: Root-specific systems fully working.

1. Implement **RootAbility** fully: network fear passive (adjacency counting), assimilation on resolution
2. Implement **PresenceSystem**: placement validation (range checking on pyramid), adjacency queries
3. Load Root cards from JSON (10 starting, 18 draft)
4. Wire all Root-specific effects: AwakeDormant, network fear scaling (Rootbound), Lattice of Life doubling
5. Implement **ResolutionRunner**: 2 turns standard, 3 elite, 1 boss, breach detection
6. Implement **RewardCalculator**: clean/weathered/breach tiers
7. **Tests**: Network fear counting, assimilation invader removal, dormancy + awaken cycle across full encounter, resolution with Root, reward tier calculation

**Milestone: Can run a full Root encounter in tests — card play, elements, tides, combat, resolution, rewards. The game works as pure C#.**

### Phase 6 — Godot Integration (est. 3–4 days)
> Goal: Playable in Godot with functional (not pretty) UI.

1. Implement **GameBridge** autoload: creates Core objects, subscribes to GameEvents, emits Godot signals
2. Implement **InputHandler**: translate clicks/keys → TurnActions → TurnManager
3. Create scene tree: Game.tscn containing territory views, hand display, HUD elements
4. Implement **CardViewController**: renders card model (element icons, top/bottom text, dormant state)
5. Implement **HandDisplayController**: shows hand, play interaction (drag to territory or click)
6. Implement **TerritoryViewController**: shows corruption level, invaders, natives, presence tokens
7. Implement **ElementTrackerController**: 6 counters with 4/7/11 markers, decay animation
8. Implement **DreadBarController**: bar with threshold markers, dread level pips
9. Implement **FearActionQueueController**: face-down cards, flip animation at Tide start
10. Implement **TidePreviewController**: shows next action card + arrival locations
11. Implement **CounterAttackController**: UI for player to assign native damage to invaders
12. Implement **DeckCounter**: visible deck/discard sizes
13. Implement **PhaseIndicator**: Vigil/Tide/Dusk current state

**Milestone: Playable in Godot. Ugly but functional. Can play through a full encounter with mouse/keyboard.**

### Phase 7 — First Playtest (est. 1–2 days)
> Goal: Play 3 Standard encounters + 1 Boss with The Root. Validate all design decisions.

Playtest checklist (from master doc):
- [ ] Is bottom-as-dissolve creating real decisions?
- [ ] Are element thresholds triggering often enough to matter?
- [ ] Is the pyramid map creating interesting spatial decisions?
- [ ] Is Native counter-attack meaningful?
- [ ] Does the refill model create visible deck depletion and natural Rest timing?
- [ ] Does rest-dissolve feel like a fair tax or too punishing?
- [ ] Is 10 cards the right starting deck size for 4 play turns before Rest?
- [ ] Does the two-pool action cadence create the right pain/relief rhythm?
- [ ] Are unit-type modifiers readable at a glance?
- [ ] Does Dread Level retroactive upgrade feel rewarding?
- [ ] Is player-assigned native counter-attack worth the interaction cost?

---

## Data Pipeline

### JSON → Runtime

All game data lives in `/data/` as JSON. A build step (or runtime loader) converts to C# objects.

```
data/wardens/root.json  → WardenLoader.Load("root")   → WardenData (cards + passives + metadata)
data/wardens/ember.json → WardenLoader.Load("ember")  → WardenData (cards + passives + metadata)
data/encounters-realm1.json → EncounterLoader.Load("realm1") → List<EncounterConfig>
data/pale-march-actions.json → FactionLoader.Load("pale_march") → InvaderFaction
data/pale-march-waves.json → (embedded in EncounterConfig)
data/fear-actions.json → FearActionLoader.Load() → Dictionary<int, List<FearActionData>>

Note: `CardLoader.cs` is marked `[Obsolete]` — kept for backward compatibility but superseded by `WardenLoader`.
```

For V1, loading from JSON at runtime is fine — no need for Godot Resources (.tres). The data is small and loads instantly. If performance matters later, bake to .tres.

### Warden JSON Schema (v2.2 — unified warden file)

Each warden is a single file at `data/wardens/{id}.json` containing metadata, element affinity, hand limit, starting presence, passives, and cards.

```json
{
  "warden_id": "root",
  "version": "2.2",
  "name": "The Root",
  "archetype": "...",
  "flavor": "...",
  "element_affinity": { "primary": "Root", "secondary": "Mist", "tertiary": "Shadow" },
  "hand_limit": 5,
  "starting_presence": { "territory": "I1", "count": 1 },
  "resolution_style": "assimilation",
  "passives": [
    { "id": "network_fear", "description": "...", "icon": "..." }
  ],
  "cards": [
    {
      "id": "root_001",
      "name": "Tendrils of Reclamation",
      "rarity": "dormant",
      "starting": true,
      "elements": ["Root", "Mist"],
      "top": { "type": "ReduceCorruption", "value": 2, "range": 1 },
      "bottom": { "type": "ReduceCorruption", "value": 5, "range": 2,
                   "secondary": { "type": "RestoreWeave", "value": 1 } }
    }
  ]
}
```

Note `secondary` field on bottom effects — several Root cards have compound bottoms (cleanse + weave, damage + shield natives, etc.). The EffectResolver chains these.

### Encounter JSON Schema

```json
{
  "id": "realm1_encounter1",
  "tier": "standard",
  "faction": "pale_march",
  "tide_count": 6,
  "resolution_turns": 2,
  "cadence": {
    "mode": "rule_based",
    "max_painful_streak": 1,
    "easy_frequency": 2
  },
  "native_spawns": {
    "A1": 0, "A2": 1, "A3": 0,
    "M1": 2, "M2": 2, "I1": 2
  },
  "waves": [
    {
      "turn": 1,
      "arrival_points": ["A1", "A2"],
      "options": [
        { "weight": 50, "units": { "A1": ["marcher"], "A2": ["marcher"] } },
        { "weight": 30, "units": { "A1": ["marcher", "outrider"], "A2": [] } },
        { "weight": 20, "units": { "A1": ["ironclad"], "A2": ["marcher"] } }
      ]
    }
  ],
  "escalation_schedule": [
    { "tide": 3, "card": "corrupt", "pool": "painful" },
    { "tide": 6, "card": "fortify", "pool": "painful" }
  ]
}
```

---

## Testing Strategy

**Current count: 415 tests passing** (`dotnet test` in `src/HollowWardens.Tests/`).

### Test files (current)

```
Systems/    ElementSystemTests, DreadSystemTests, CombatSystemTests, CorruptionSystemTests,
            FearActionSystemTests, RavageDamageModelTests, WeaveSystemTests
Cards/      DeckManagerTests, DormantToDiscardTests, RootDormancyTests
Encounter/  ActionDeckTests, CadenceManagerTests, SpawnManagerTests
Foundation/ ActionLogTests, GameRandomTests, SeededDeterminismTests
Invaders/   PathfindingTests, UnitModifierTests
Map/        BoardStateTests
Effects/    EffectTests
Integration/ BottomBudgetTest, BottomPlayTests, CadencePatternTest, CardPlayLimitTests,
             DreadThresholdPushTest, FearWiringTests, FrontloadingPenaltyTest,
             FullStandardEncounterTest, HeartMarchGracePeriodTest, InitialWaveTests,
             NativeProvocationTests, RootFullEncounterTest, StartingStateTests,
             TargetingTests, ThresholdPendingTests, ThresholdResolverTests,
             ThresholdT2T3Tests, ThresholdTargetingTests, TideArrivalTests,
             TideRampTests, TideSequenceOrderTest
Root        D28_PresenceValueTests, D29_RootCombatTests
Ember       EmberAbilityTests, EmberLoaderTests
Cross       BalanceConfigTests, BotStrategyTests, PassiveGatingTests, ReplayTests,
            SimProfileTests, WardenLoaderTests
```

### Key test coverage

| Area | What's verified |
|------|----------------|
| **ElementSystem** | Accumulation + decay math. Tier fires once per turn (not per card). Bottom ×2 multiplier. Rest-turn carryover. |
| **DeckManager** | Refill model, rest-dissolve, Root dormancy (bottom → discard, rest-dissolve → draw pile). |
| **RootAbility** | Dormancy flow. Network fear undirected edges (1/pair) + cap. Assimilation removes up to PresenceCount. Network Slow outnumber check. ProvokesNatives. Rest Growth. |
| **EmberAbility** | Ash Trail applies corruption + damage at tide start. Scorched Earth damage + smart cleanse. Flame Out = permanent removal. Heat Wave on Rest. |
| **PassiveGating** | Root/Ember unlock conditions. ForceUnlock/ForceLock for testing. Reset for new encounter. |
| **BalanceConfig** | All systems read from config. SimProfile overrides populate config correctly. |
| **Replay** | Same seed + actions = identical state at each Tide step. |
| **BotStrategy** | Bot makes legal decisions each turn for Root and Ember wardens. |
| **Integration** | Full encounter from Wave 0 through resolution. Tide sequence order. Threshold targeting. Fear wiring. |

---

## Migration Path from v1

### What to keep
- `project.godot` and project settings
- All assets (`assets/` folder: fonts, art, audio, theme)
- Godot scene files can be rebuilt but some UI layout work carries over
- The *design knowledge* encoded in the 111 passing tests — even though the tests themselves are wrong, they document edge cases worth re-testing

### What to discard
- All `scripts/` C# files (they mix logic and Godot, test the wrong behavior)
- All `.tres` card resources (replaced by JSON loading)
- The test suite (rewrite from scratch against new systems)
- The singleton pattern (replaced by GameBridge ownership)

### Migration steps
1. Create `src/` directory alongside existing code
2. Build Core + Tests until Phase 5 milestone (full encounter in tests)
3. Only then touch Godot: create `godot/` with bridge and views
4. Verify everything works in-engine
5. Delete old `scripts/` and `tests/` directories
6. Clean commit: "v2 architecture complete"

---

## Estimated Timeline

| Phase | Effort | Cumulative |
|-------|--------|------------|
| 1 — Foundation | 2–3 days | 2–3 days |
| 2 — Card Engine | 2–3 days | 4–6 days |
| 3 — Elements & Dread | 2 days | 6–8 days |
| 4 — Encounter Engine | 3–4 days | 9–12 days |
| 5 — Root Integration | 2 days | 11–14 days |
| 6 — Godot Integration | 3–4 days | 14–18 days |
| 7 — First Playtest | 1–2 days | 15–20 days |

**Total: ~3–4 weeks to a playable game with all design decisions implemented.**

The critical insight: Phases 1–5 are pure C#. No Godot editor needed. You can develop and test the entire game logic in your IDE, and only touch Godot for Phase 6. This is much faster than the old approach of building logic and UI simultaneously.
