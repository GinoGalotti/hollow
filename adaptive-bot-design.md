# Adaptive Bot Design — Hollow Wardens Sim

## 0. Getting Started

### Read these files first
- `SIM_REFERENCE.md` — CLI flags, balance knobs, encounter IDs, output format
- `src/HollowWardens.Core/Run/IPlayerStrategy.cs` — the strategy interface you'll implement
- `src/HollowWardens.Core/Run/BotStrategy.cs` — `root_wide` bot (legacy, greedy)
- `src/HollowWardens.Core/Run/RootTallStrategy.cs` — `root_tall` bot (current heuristic)
- `src/HollowWardens.Core/Run/EmberBotStrategy.cs` — `ember` bot
- `src/HollowWardens.Sim/Program.cs` — sim runner, `BuildStrategy()` method, CLI parsing
- `src/HollowWardens.Core/Engine/EncounterState.cs` — the game state the bot reads from
- `src/HollowWardens.Core/Engine/ThresholdResolver.cs` — threshold resolution (currently `AutoResolveAll`)

### Independence from B6
This work is **completely independent** of B6 (Root passive redesign). Zero file overlap. B6 touches `RootAbility.cs`, `root.json`, effect types, and passives. This work adds NEW files only:
- `StrategyParams.cs`, `TargetingParams.cs`, `StrategyDefaults.cs` in `Core/Run/`
- `ParameterizedBotStrategy.cs` in `Core/Run/`
- `HillClimber.cs` in `Sim/`
- Additions to `Program.cs` (new CLI flags only)

You can build this on the current codebase. Do NOT wait for B6. Once B6 lands separately, re-run the optimizer to discover what good B6 play looks like.

### What this produces
1. A `ParameterizedBotStrategy` class that replaces hard-coded decision logic with configurable `StrategyParams`
2. A `HillClimber` optimizer that finds optimal params by running thousands of sim seeds
3. Saved params as JSON files that can be loaded with `--strategy optimised --strategy-params path.json`
4. A three-tier comparison capability: `root_wide` (floor) vs `root_tall` (heuristic) vs `optimised` (upper bound)

### Encounter IDs for testing
- `pale_march_standard` — tutorial, mixed invaders, 6 tides
- `pale_march_scouts` — Outrider-heavy, fast pressure, 6 tides
- `pale_march_siege` — Ironclad + Pioneer, 8 tides
- `pale_march_elite` — starting corruption, hard capstone, 6 tides

---

## 1. Why the Current Bots Are Wrong

The three hand-tuned bots share a fundamental flaw: they treat the encounter as a series of independent card-play decisions rather than a 6-tide game with temporal structure, resource trajectories, and positional consequences. Looking at the verbose logs:

**Root seed 2, Tide 4:** Four Marchers advance through to I1 in a single March because the bot spent Tides 2-3 playing fear and presence cards instead of positioning damage. The bot didn't reason about the March card it saw at Preview — it played for the current board, not the next Tide.

**Ember seed 4:** The bot plays `ember_005 Stoke the Fire` (top: ReduceCorruption 2) at Tide 3 with `PRIORITY: any_card` — meaning no real priority drove this decision. It cleansed 2 corruption from I1 when I1 was at 0 corruption from its own cleanse effect. Meanwhile A2 had damaged Marchers that would advance next turn. The bot can't think ahead.

**The three problems:**
1. **No temporal reasoning** — bots don't differentiate Tide 1 (setup) from Tide 5 (emergency)
2. **No threat projection** — bots don't look at Preview information (next action card + arrival points)
3. **No targeting intelligence** — threshold resolution uses `AutoResolveAll` which picks greedy defaults

The hill-climber approach addresses #1 and partially #2 by parameterizing urgency thresholds that shift over time. But it can't fix #3 without an explicit targeting model. Both need to ship together.

---

## 2. The Decision Space

Every turn, the bot makes these decisions in order:

### 2.1 Vigil Phase (up to 2 tops)
For each top slot:
- **Which card to play** from hand (or skip)
- **Where to target** effects that need a territory (PlacePresence, DamageInvaders, ReduceCorruption)
- **Threshold resolution** — when thresholds trigger, where to aim them

### 2.2 Dusk Phase (1 bottom)
- **Which card to sacrifice** (or skip) — the bottom is permanent loss for Ember, dormant for Root
- **Where to target** the bottom's effect
- **Threshold resolution** from bottom's double elements

### 2.3 Rest Decision
- When hand is empty / deck is empty, rest is forced
- But voluntary rest is sometimes optimal (bank element carryover, trigger Rest Growth)
- Current bots never voluntarily rest

### 2.4 Threshold Targeting (D41)
Each threshold that fires needs a target territory. The current `AutoResolveAll` picks greedily. But optimal targeting depends heavily on context:
- Ash T1 (1 dmg to all in one territory) — should target the territory where that 1 damage kills the most invaders (low-HP invaders), not just the most-populated territory
- Root T1 (Reduce Corruption ×3) — should target the territory closest to the next corruption level threshold, not just the highest corruption
- Ash T3 (2 dmg per Presence in territory) — should target the territory where the player has the MOST presence for maximum damage

### 2.5 Counter-Attack Assignment
When natives counter-attack, damage must be assigned across invaders. Optimal assignment maximizes kills (target weakest first) or maximizes damage on the biggest threat (target the Ironclad about to march on the Heart).

---

## 3. StrategyParams — Phase-Aware Design

The flat param struct from the original spec is insufficient. Encounters have distinct phases and the bot's priorities should shift across them. But we don't want per-tide granularity (too many params, hill-climber won't converge).

### 3.1 Two-Phase Model

Split the encounter at a configurable tide (`phase_transition_tide`). Early phase is about engine-building. Late phase is about threat response.

```csharp
public class StrategyParams
{
    // --- Phase Transition ---
    public int PhaseTransitionTide { get; set; } = 3;
    // Tides 1 to PhaseTransitionTide = "early" phase
    // Tides PhaseTransitionTide+1 to end = "late" phase

    // --- Presence Strategy ---
    public int SpreadTarget        { get; set; } = 3;   // territories to expand to before stacking
    public int StackTarget         { get; set; } = 3;   // presence per territory to aim for
    public bool PreferTallOverWide { get; set; } = true; // stack existing vs spread new

    // --- Early Phase Priorities (ordinal ranks, 1=highest) ---
    public int EarlyPresencePriority    { get; set; } = 1;
    public int EarlyDamagePriority      { get; set; } = 3;
    public int EarlyCleansePriority     { get; set; } = 4;
    public int EarlyFearPriority        { get; set; } = 2;
    public int EarlyWeavePriority       { get; set; } = 5;
    public int EarlyPassiveUnlockPriority { get; set; } = 2;

    // --- Late Phase Priorities ---
    public int LateDamagePriority       { get; set; } = 1;
    public int LateCleansePriority      { get; set; } = 2;
    public int LatePresencePriority     { get; set; } = 4;
    public int LateFearPriority         { get; set; } = 3;
    public int LateWeavePriority        { get; set; } = 2;
    public int LatePassiveUnlockPriority { get; set; } = 5;

    // --- Urgency Thresholds (override priorities when conditions are critical) ---
    public int DamageUrgencyInvaderCount { get; set; } = 2;  // invaders in M-row or I1 triggers damage urgency
    public int CleanseUrgencyCorruption  { get; set; } = 5;  // corruption in any territory triggers cleanse urgency
    public int WeaveUrgencyThreshold     { get; set; } = 12; // weave below this triggers weave priority
    public int HeartThreatTide           { get; set; } = 4;  // from this tide onward, treat M-row invaders as critical

    // --- Bottom Play Preferences (weighted scores, not ordinal) ---
    public int BottomDamageWeight   { get; set; } = 100;
    public int BottomFearWeight     { get; set; } = 60;
    public int BottomCleanseWeight  { get; set; } = 90;
    public int BottomPresenceWeight { get; set; } = 50;
    public int BottomWeaveWeight    { get; set; } = 40;

    // --- Targeting ---
    public TargetingParams Targeting { get; set; } = new();

    // --- Rest ---
    public int VoluntaryRestMinElements { get; set; } = 8;  // if element pool >= this AND hand is thin, consider voluntary rest
    public int VoluntaryRestMaxHandSize { get; set; } = 2;  // only voluntarily rest if hand is this small or smaller
}
```

### 3.2 TargetingParams

Separate struct for targeting intelligence — this is where the biggest skill gap lives.

```csharp
public class TargetingParams
{
    // --- Damage Targeting ---
    public bool PreferKillsOverDamage    { get; set; } = true;   // target where damage results in kills vs. most total damage
    public bool PreferArrivalRow         { get; set; } = true;   // bias toward A-row (kill before advance)
    public int  ThreatRowWeight          { get; set; } = 3;      // multiplier for M-row/I1 targets (1 = no bias, higher = prefer threats near Heart)
    public bool TargetWeakestFirst       { get; set; } = true;   // in counter-attacks: maximize kills vs. focus fire

    // --- Presence Targeting ---
    public bool PresencePreferStack      { get; set; } = true;   // place presence in existing territory vs. new territory
    public bool PresencePreferThreshold  { get; set; } = true;   // place where it crosses assimilation_spawn_threshold
    public bool PresencePreferAdjInvader { get; set; } = false;  // place adjacent to invader-heavy territories

    // --- Cleanse Targeting ---
    public bool CleansePreferHighest     { get; set; } = false;  // highest absolute corruption
    public bool CleansePreferNearThreshold { get; set; } = true; // territory closest to leveling up (L0→L1 at 3, L1→L2 at 8)
    public bool CleansePreferPresence    { get; set; } = true;   // only cleanse territories with presence

    // --- Threshold Resolution Targeting ---
    // Per-element targeting preferences (the big ones)
    public bool AshT1PreferMostInvaders  { get; set; } = true;   // vs. prefer weakest invaders for kills
    public bool AshT2PreferHighCorruption { get; set; } = false; // Ash T2 adds corruption — avoid already-corrupted territories
    public bool AshT3PreferHighPresence  { get; set; } = true;   // Ash T3 = 2 dmg per presence in territory — target stacked territory
    public bool RootT1PreferNearThreshold { get; set; } = true;  // Root T1 = Reduce Corruption ×3 — target near level-up
    public bool RootT2PreferFrontline    { get; set; } = true;   // Root T2 = Place Presence — prefer A-row/M-row
    public bool GaleT1PushTowardSpawn    { get; set; } = true;   // Gale = push invaders — push backward vs. sideways
}
```

### 3.3 Warden Defaults

```csharp
public static class StrategyDefaults
{
    public static StrategyParams Root => new()
    {
        PhaseTransitionTide = 3,
        SpreadTarget = 3,
        StackTarget = 3,
        PreferTallOverWide = true,
        EarlyPresencePriority = 1,
        EarlyDamagePriority = 4,
        EarlyFearPriority = 2,
        EarlyCleansePriority = 5,
        EarlyPassiveUnlockPriority = 2,
        LateDamagePriority = 2,
        LateCleansePriority = 1,
        LateFearPriority = 3,
        LatePresencePriority = 4,
        DamageUrgencyInvaderCount = 2,
        CleanseUrgencyCorruption = 5,
        HeartThreatTide = 4,
        Targeting = new()
        {
            PreferKillsOverDamage = true,
            PreferArrivalRow = false,     // Root prefers M-row (choke point)
            ThreatRowWeight = 2,
            PresencePreferStack = true,   // tall presence for Assimilation
            PresencePreferThreshold = true,
            CleansePreferNearThreshold = true,
            RootT1PreferNearThreshold = true,
            RootT2PreferFrontline = true,
        }
    };

    public static StrategyParams Ember => new()
    {
        PhaseTransitionTide = 2,          // Ember's engine starts fast
        SpreadTarget = 3,
        StackTarget = 2,                  // Ember doesn't need tall stacks
        PreferTallOverWide = false,       // Ember prefers wide (Ash Trail hits all presence territories)
        EarlyPresencePriority = 1,
        EarlyDamagePriority = 2,
        EarlyFearPriority = 3,
        EarlyCleansePriority = 5,         // Ember ignores early corruption (it's fuel)
        EarlyPassiveUnlockPriority = 3,
        LateDamagePriority = 1,
        LateCleansePriority = 3,          // Ember cleanses late only to avoid Desecration
        LateFearPriority = 2,
        LatePresencePriority = 4,
        DamageUrgencyInvaderCount = 3,    // Ember can tolerate more invaders (thresholds handle them)
        CleanseUrgencyCorruption = 12,    // Ember only panics near L3 (15)
        HeartThreatTide = 5,              // Ember's threshold engine usually clears M-row passively
        Targeting = new()
        {
            PreferKillsOverDamage = true,
            PreferArrivalRow = true,       // Ember kills at arrival point
            ThreatRowWeight = 1,           // No special bias — thresholds are board-wide
            PresencePreferStack = false,   // Wide for Ash Trail
            CleansePreferHighest = true,   // Ember cleanses the worst territory to prevent L3
            AshT1PreferMostInvaders = true,
            AshT2PreferHighCorruption = false, // avoid adding to already-corrupted
            AshT3PreferHighPresence = true,
        }
    };
}
```

---

## 4. ParameterizedBotStrategy — Architecture

### 4.1 Decision Flow

```
ChooseTopPlay(hand, state):
  1. Check urgency overrides:
     - If weave < WeaveUrgencyThreshold AND hand has RestoreWeave → play it
     - If any M-row/I1 invader count >= DamageUrgencyInvaderCount → damage priority
     - If any territory corruption >= CleanseUrgencyCorruption → cleanse priority
  2. Determine current phase (state.CurrentTide <= PhaseTransitionTide?)
  3. Score each hand card based on phase priorities:
     - Card effect type → priority rank from current phase
     - Card elements → bonus if they'd push toward a threshold
     - Card target quality → score from TargetingParams
  4. Return highest-scoring card (or null if no card scores above 0)

ChooseBottomPlay(hand, state):
  1. Score each card's bottom effect using BottomXxxWeight
  2. Multiply by targeting quality score
  3. Factor in element bonus: bottom's 2x elements × (distance to next threshold / threshold)
  4. Return highest-scoring card (or null if no bottom is worth sacrificing)

ChooseTarget(effect, state):
  1. Enumerate valid target territories
  2. Score each territory based on TargetingParams for this effect type
  3. Return highest-scoring territory

ResolvePendingThresholds(resolver, state):
  1. For each pending threshold:
     - Use TargetingParams to pick optimal target
     - resolver.Resolve(threshold, target)
  2. Order: resolve damage thresholds first (kill before other effects fire)
```

### 4.2 Scoring Formula for Card Selection

Each card in hand gets a composite score:

```
card_score = priority_score × 10
           + element_value × 3
           + target_quality × 2
           + urgency_bonus × 20

where:
  priority_score = (6 - phase_priority_rank)  // rank 1 = score 5, rank 5 = score 1
  element_value  = sum of (threshold_distance_reduction per element on this card)
                   i.e., how much closer does playing this card bring us to any threshold
  target_quality = targeting score for the best valid target (0-5 scale)
  urgency_bonus  = 1 if this card addresses an active urgency condition, else 0
```

The weights (10, 3, 2, 20) are themselves tunable — the hill-climber could optimize them. But starting with these hard-coded values gives a reasonable baseline.

### 4.3 Threat Projection (Simple)

The bot should look at the preview information available at the end of each Tide:
- **Next action card** (Ravage? March? Settle?)
- **Next arrival locations** (which A-row territories get new invaders)

Use this to adjust targeting:
- If next action is March (advance=2), prioritize killing invaders in A-row (they'll jump to M-row)
- If next action is Ravage, prioritize cleansing territories about to be hit
- If arrivals are at A1 and A3, don't waste damage on A2

This doesn't require lookahead search — just a single "next turn context" that modifies the scoring weights.

```csharp
// Inside ChooseTopPlay, after base scoring:
if (state.PreviewedActionCard?.AdvanceModifier >= 2)
{
    // March incoming — boost damage priority for A-row invaders
    foreach (var card in scoredCards.Where(c => c.Effect.Type == EffectType.DamageInvaders))
        card.Score += Params.Targeting.ThreatRowWeight * CountARowInvaders(state);
}
```

---

## 5. Hill-Climber Design

### 5.1 Score Function

```
score(params, seeds, warden, encounter) =
    clean%
    - 3.0 × breach%
    - 0.5 × |weathered% - 27.5|
    + 0.1 × avg_heart_damage_events    // small bonus for creating tension
    - 0.2 × |avg_final_weave - 16|     // penalize being too far from target weave

Target: score ≈ 50-60 for a well-balanced encounter
```

The `|weathered% - 27.5|` term penalizes outcomes that are too clean OR too weathered — it rewards landing in the target band. The heart damage and weave terms are small tiebreakers that prefer "interesting" games over boring wins.

### 5.2 Perturbation Strategy

Pure random ±1 on a single param is too slow with 30+ params. Use **momentum-biased perturbation**:

```
Track: last_improvement_by_param: Dictionary<string, int>  // iteration count

Perturb(params, iteration):
    // 70% chance: pick from params that improved recently (last 20 iterations)
    // 30% chance: pick uniformly at random (exploration)
    recent_improvers = params where last_improvement_by_param[p] > iteration - 20
    if random < 0.7 AND recent_improvers.Any():
        param = random.Choice(recent_improvers)
    else:
        param = random.Choice(all_params)

    direction = random < 0.5 ? +1 : -1
    // For boolean params, just flip
    // For int params, ±1 clamped to valid range
    return modified_params
```

### 5.3 Restart Strategy

Every 60 iterations, **shake** instead of restart:
- Pick 4 random params
- Perturb each by ±random(1,3)
- This escapes local maxima without throwing away the general structure

### 5.4 Convergence Detection

Stop early if the best score hasn't improved by ≥0.5 in the last 40 iterations. No point burning CPU on noise.

### 5.5 Speed Budget

The inner loop (100 seeds × 6 tides × ~25 decisions per encounter) must complete in <5 seconds. This means:
- No allocation-heavy code in the decision loop
- Pre-compute territory scores at Tide start, not per card evaluation
- Cache element pool distances to thresholds

At 5 seconds per evaluation, 200 iterations = ~17 minutes. Acceptable for overnight runs. For interactive use, drop to 50 seeds and 100 iterations (~4 minutes).

---

## 6. CLI Integration

```
# Optimize Root on standard encounter
dotnet run --project src/HollowWardens.Sim/ -- \
  --warden root --encounter pale_march_standard \
  --optimise --optimise-seeds 1-100 \
  --optimise-iterations 200 \
  --optimise-output sim-output/root-standard-opt.json

# Run with optimized params
dotnet run --project src/HollowWardens.Sim/ -- \
  --profile sim-profiles/b6-test.json \
  --strategy optimised \
  --strategy-params sim-output/root-standard-opt.json \
  --output sim-results/b6-optimised/

# Three-tier comparison
dotnet run -- --warden root --encounter pale_march_standard --strategy root_wide    # floor
dotnet run -- --warden root --encounter pale_march_standard --strategy root_tall    # heuristic
dotnet run -- --warden root --encounter pale_march_standard --strategy optimised \
  --strategy-params sim-output/root-standard-opt.json                               # upper bound
```

### 6.1 Output JSON

```json
{
  "warden": "root",
  "encounter": "pale_march_standard",
  "iterations": 200,
  "seeds": "1-100",
  "final_score": 52.3,
  "results": {
    "clean_pct": 58.0,
    "weathered_pct": 34.0,
    "breach_pct": 8.0,
    "avg_weave": 16.2,
    "avg_heart_damage": 1.1
  },
  "params": { /* full StrategyParams JSON */ },
  "history": [
    { "iteration": 0, "score": 31.2, "param_changed": null },
    { "iteration": 1, "score": 33.8, "param_changed": "EarlyPresencePriority" },
    ...
  ]
}
```

The `history` array lets you see which params the optimizer found productive — that's diagnostic gold for understanding what good play looks like.

---

## 7. Integration With B6 (After B6 Lands)

B6 is being built in a separate Claude Code session. Once it ships, re-run the optimizer to discover what good play looks like under the new mechanics. The B6 redesign changes Root's passive structure (Provocation becomes base, Assimilation becomes pool) and adds native-interactive cards (MoveNatives, SpawnNatives, PushInvaders). The optimal strategy will shift.

**Run this sequence once B6 is merged:**

```powershell
# 1. Optimize on B6 build
dotnet run --project src/HollowWardens.Sim/ -- --warden root --encounter pale_march_standard --optimise --optimise-seeds 1-100 --optimise-iterations 200 --optimise-output sim-output/b6-root-opt.json

# 2. Compare three tiers on B6
dotnet run -- --warden root --seeds 1-500 --encounter pale_march_standard --strategy root_wide --output sim-results/b6-floor/
dotnet run -- --warden root --seeds 1-500 --encounter pale_march_standard --strategy root_tall --output sim-results/b6-heuristic/
dotnet run -- --warden root --seeds 1-500 --encounter pale_march_standard --strategy optimised --strategy-params sim-output/b6-root-opt.json --output sim-results/b6-optimal/
```

**What the gaps tell you:**
- `floor ≈ heuristic ≈ optimal` → B6 mechanic doesn't reward skill. Boring but balanced.
- `floor << heuristic ≈ optimal` → heuristic captures most skill expression. Good.
- `floor ≈ heuristic << optimal` → heuristic doesn't understand B6 mechanics (likely — it was written pre-B6). **Update the heuristic bot based on the optimized params.**
- `floor << heuristic << optimal` → large skill gap. B6 rewards mastery. Ideal.

**Read the optimized params to understand the new meta.** If the optimizer converges on `SpreadTarget=5, StackTarget=1, PreferTallOverWide=false`, that confirms Root's identity is wide play. If it converges on `SpreadTarget=3, StackTarget=3, PreferTallOverWide=true`, the native army strategy is dominant. Either way, data beats guesswork.

---

## 8. File Changes

| File | Change |
|------|--------|
| `src/HollowWardens.Core/Run/StrategyParams.cs` | New — the data class (§3) |
| `src/HollowWardens.Core/Run/TargetingParams.cs` | New — targeting sub-struct (§3.2) |
| `src/HollowWardens.Core/Run/StrategyDefaults.cs` | New — Root/Ember defaults (§3.3) |
| `src/HollowWardens.Core/Run/ParameterizedBotStrategy.cs` | New — the decision engine (§4) |
| `src/HollowWardens.Sim/HillClimber.cs` | New — optimizer loop (§5) |
| `src/HollowWardens.Sim/Program.cs` | Add `--optimise`, `--strategy-params` flags (§6) |
| `src/HollowWardens.Sim/SimProfile.cs` | Add `strategy_params_path` field |
| `SIM_REFERENCE.md` | Document new CLI flags and strategy params |

### 8.1 Do NOT change:
- `IPlayerStrategy` interface — `ParameterizedBotStrategy` implements it as-is
- `RootTallStrategy`, `EmberBotStrategy`, `BotStrategy` — keep as named strategy options until the parameterized bot is validated
- Any game logic in Core — the bot is a consumer of state, not a modifier

---

## 9. Open Design Questions

### 9.1 Should threshold resolution order be a param?
Currently thresholds resolve in the order they triggered. But resolving damage thresholds before placement thresholds might be better (kill invaders, then place presence in the cleared territory). Making resolution order a param lets the optimizer discover whether order matters.

**Recommendation:** Yes, add `ThresholdResolutionOrder` as an enum param: `{ AsTriggered, DamageFirst, PlacementFirst, CleanseFist }`.

### 9.2 Should the bot reason about deck state?
The bot currently doesn't know what's left in its deck or what cards will come back after rest. A bot that knows "I have 2 cards in hand, 3 in deck, 5 in discard — rest will give me a full hand next turn" might make better rest decisions.

**Recommendation:** Not for v1. Deck awareness adds complexity without a clear parameterizable knob. The `VoluntaryRestMinElements` + `VoluntaryRestMaxHandSize` params capture the key rest heuristic. Revisit if the optimizer's rest behavior looks wrong.

### 9.3 Should the optimizer run per-encounter or globally?
Different encounters have different optimal strategies. `pale_march_scouts` (Outrider swarm) rewards kill-at-arrival. `pale_march_siege` (Ironclads) rewards patient presence-stacking and letting Assimilation handle late-game.

**Recommendation:** Optimize per-encounter first to understand the landscape. Then test whether a single "generalist" param set (optimized on a mix of all 4 encounters) performs within 5% of per-encounter optimals. If yes, ship the generalist. If no, support per-encounter strategy overrides in SimProfile.

### 9.4 Should bottom play consider deck compression?
Ember's deck shrinks permanently with each bottom. A bot that plays bottoms early gets a thinner, more consistent deck later — but has fewer options. Root's dormancy means bottoms aren't permanent, so the tradeoff is different.

**Recommendation:** Add `BottomEarlyBias` (int, 0-5) as a param. At 0, the bot only plays bottoms for their effect. At 5, the bot aggressively plays bottoms early for deck thinning. Let the optimizer find the right balance per warden.

### 9.5 How should the bot handle Ember's corruption-as-fuel dynamic?
Ember wants corruption to build (Ember Fury damage bonus, Scorched Earth resolution payoff) but not to hit L3 (Desecrated, blocks presence). The bot needs to reason about "optimal corruption level" — enough to fuel the engine, not enough to lock territories.

**Recommendation:** `EmberTargetCorruptionLevel` (int, 1-2) as a param. At 1, the bot tolerates Tainted (3+ pts) per territory. At 2, it tolerates Defiled (8+ pts). Cleanse urgency triggers when any territory exceeds the target level.

---

## 10. Step-by-Step Execution Plan

### Step 1: Read the existing bots
Read `BotStrategy.cs`, `RootTallStrategy.cs`, and `EmberBotStrategy.cs` to understand the current decision logic and how they implement `IPlayerStrategy`. Also read `Program.cs` `BuildStrategy()` to understand how strategies are selected.

### Step 2: Create the data classes
Create `src/HollowWardens.Core/Run/StrategyParams.cs` with the full struct from §3.1.
Create `src/HollowWardens.Core/Run/TargetingParams.cs` with the struct from §3.2.
Create `src/HollowWardens.Core/Run/StrategyDefaults.cs` with Root and Ember defaults from §3.3.

Both `StrategyParams` and `TargetingParams` must be JSON-serializable (System.Text.Json). Add `[JsonPropertyName("...")]` attributes if needed. Add a static `FromJson(string path)` and `ToJson(string path)` method to `StrategyParams` for save/load.

### Step 3: Implement ParameterizedBotStrategy
Create `src/HollowWardens.Core/Run/ParameterizedBotStrategy.cs` implementing `IPlayerStrategy`.

Follow the decision flow in §4.1:
- `ChooseTopPlay`: urgency check → phase check → score each card → return best
- `ChooseBottomPlay`: weight-based scoring → element bonus → return best
- `ChooseTarget`: enumerate valid territories → score with TargetingParams → return best
- `ResolvePendingThresholds`: iterate pending thresholds → pick targets using TargetingParams → resolve in order
- `AssignCounterDamage`: use `TargetWeakestFirst` param to decide kill-maximizing vs focus-fire

Use the scoring formula from §4.2. Implement the simple threat projection from §4.3 (check `state.PreviewedActionCard` if available).

### Step 4: Wire into CLI
In `Program.cs` `BuildStrategy()`, add:
- `"parameterized"` or `"smart"` → creates `ParameterizedBotStrategy(StrategyDefaults.Root)` or `StrategyDefaults.Ember` based on warden
- `"optimised"` → loads `StrategyParams.FromJson(strategyParamsPath)` and creates `ParameterizedBotStrategy(loadedParams)`

Add CLI flags:
- `--strategy <name>` — already exists, add new options
- `--strategy-params <path>` — path to saved StrategyParams JSON

### Step 5: Validate the parameterized bot
Before building the optimizer, verify the parameterized bot produces reasonable results with default params.

Run these comparisons on `pale_march_standard` (500 seeds each):

```powershell
# Existing bots
dotnet run --project src/HollowWardens.Sim/ -- --warden root --seeds 1-500 --strategy root_wide --output sim-results/bot-wide/
dotnet run --project src/HollowWardens.Sim/ -- --warden root --seeds 1-500 --strategy root_tall --output sim-results/bot-tall/

# New parameterized bot with Root defaults
dotnet run --project src/HollowWardens.Sim/ -- --warden root --seeds 1-500 --strategy smart --output sim-results/bot-smart/

# Ember too
dotnet run --project src/HollowWardens.Sim/ -- --warden ember --seeds 1-500 --strategy smart --output sim-results/bot-smart-ember/
```

Compare the three summaries. The parameterized bot with defaults should perform at least as well as `root_tall` on clean% and breach%. If it's dramatically worse, the scoring formula or defaults need tuning before the optimizer can work.

**Do NOT proceed to Step 6 until the parameterized bot performs comparably to the hand-tuned bots.** The optimizer can only improve — it can't fix a broken decision engine.

### Step 6: Implement the HillClimber
Create `src/HollowWardens.Sim/HillClimber.cs` with the algorithm from §5.

```csharp
public static class HillClimber
{
    public static (StrategyParams bestParams, double bestScore, List<HistoryEntry> history) Optimise(
        string wardenId,
        string encounterId,
        int[] seeds,
        StrategyParams startParams,
        int maxIterations = 200,
        int perturbMagnitude = 1,
        int shakeInterval = 60,
        int convergenceWindow = 40,
        double convergenceMinImprovement = 0.5
    );
}
```

Use the score function from §5.1. Implement momentum-biased perturbation from §5.2. Implement shake-not-restart from §5.3. Implement convergence detection from §5.4.

The inner loop must run fast: 100 seeds × 1 encounter in < 5 seconds. Profile if needed — pre-compute territory scores at tide start, cache element distances to thresholds.

### Step 7: Wire optimizer into CLI
Add to `Program.cs`:
- `--optimise` flag — triggers optimizer instead of normal sim
- `--optimise-seeds <range>` — seeds for inner loop (default: 1-100)
- `--optimise-iterations <int>` — max iterations (default: 200)
- `--optimise-output <path>` — where to save the result JSON

When `--optimise` is set, run `HillClimber.Optimise()` and save the output JSON (format in §6.1).

### Step 8: Run the optimizer
Optimize Root on standard encounter first:

```powershell
dotnet run --project src/HollowWardens.Sim/ -- --warden root --encounter pale_march_standard --optimise --optimise-seeds 1-100 --optimise-iterations 200 --optimise-output sim-output/root-standard-opt.json
```

This should take ~15-20 minutes. When complete, examine the output JSON:
- What score did it achieve?
- What params did it converge on? (Read the `params` object)
- Which params changed most? (Read the `history` array for frequent `param_changed` values)

### Step 9: Validate optimized params on full seed set
Run the optimized bot on 500 seeds to verify it generalizes:

```powershell
dotnet run --project src/HollowWardens.Sim/ -- --warden root --seeds 1-500 --encounter pale_march_standard --strategy optimised --strategy-params sim-output/root-standard-opt.json --output sim-results/root-opt-validate/ --verbose
```

Compare against the hand-tuned bots from Step 5. The optimized bot should be equal or better.

### Step 10: Three-tier comparison
Run all three strategies on the same encounter and present the results:

```powershell
# Floor
dotnet run --project src/HollowWardens.Sim/ -- --warden root --seeds 1-500 --encounter pale_march_standard --strategy root_wide --output sim-results/compare-floor/
# Heuristic
dotnet run --project src/HollowWardens.Sim/ -- --warden root --seeds 1-500 --encounter pale_march_standard --strategy root_tall --output sim-results/compare-heuristic/
# Optimised
dotnet run --project src/HollowWardens.Sim/ -- --warden root --seeds 1-500 --encounter pale_march_standard --strategy optimised --strategy-params sim-output/root-standard-opt.json --output sim-results/compare-optimal/
```

Present the results as a comparison table. The gap between tiers is the skill-gap diagnostic:
- `floor ≈ heuristic ≈ optimal` → mechanic doesn't reward skill
- `floor << heuristic ≈ optimal` → heuristic is near-optimal, good
- `floor ≈ heuristic << optimal` → heuristic doesn't understand the mechanic, needs updating
- `floor << heuristic << optimal` → large skill gap, ideal for strategy game

### Step 11: Optimize across encounters
After standard, optimize on scouts, siege, and elite:

```powershell
dotnet run --project src/HollowWardens.Sim/ -- --warden root --encounter pale_march_scouts --optimise --optimise-seeds 1-100 --optimise-iterations 200 --optimise-output sim-output/root-scouts-opt.json
dotnet run --project src/HollowWardens.Sim/ -- --warden root --encounter pale_march_siege --optimise --optimise-seeds 1-100 --optimise-iterations 200 --optimise-output sim-output/root-siege-opt.json
dotnet run --project src/HollowWardens.Sim/ -- --warden root --encounter pale_march_elite --optimise --optimise-seeds 1-100 --optimise-iterations 200 --optimise-output sim-output/root-elite-opt.json
```

Compare the optimized params across encounters. If they're similar (within ±2 on most params), a single "generalist" params set works. If they diverge significantly, document which params differ and why.

### Step 12: Optimize Ember
Repeat Steps 8-11 for Ember warden using `StrategyDefaults.Ember` as the start point.

### Step 13: Update SIM_REFERENCE.md
Add documentation for:
- New strategy names: `smart`, `optimised`
- New CLI flags: `--strategy-params`, `--optimise`, `--optimise-seeds`, `--optimise-iterations`, `--optimise-output`
- Brief description of the score function and what the output JSON contains

---

## 11. Do NOT Change

- `IPlayerStrategy` interface — `ParameterizedBotStrategy` implements it as-is
- `RootTallStrategy`, `EmberBotStrategy`, `BotStrategy` — keep as named strategy options, do not replace
- Any game logic in Core — the bot is a consumer of state, not a modifier
- `RootAbility.cs`, `root.json`, passive data — that's B6 scope, separate work
- Encounter configs — no encounter changes, this is bot-only work

This ordering means B6 immediately benefits from better targeting (Phase 1) without waiting for the full optimizer. The hill-climber (Phase 2) then validates whether the Phase 1 defaults are close to optimal.
