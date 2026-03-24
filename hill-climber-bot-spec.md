# Adaptive Bot / Hill-Climber Spec — Hollow Wardens Sim

## Context

The sim currently has three hand-tuned bot strategies:

| Class | Warden | Behaviour |
|-------|--------|-----------|
| `BotStrategy` | Root (legacy wide) | Spreads presence, then damage/fear. Still registered as `root_wide`. |
| `RootTallStrategy` | Root (default) | Spreads to 3 territories, then stacks presence toward a threshold. |
| `EmberBotStrategy` | Ember | Aggressive damage-first, fear secondary, late cleanse only. |

All implement `IPlayerStrategy` in `src/HollowWardens.Core/Run/`:

```csharp
public interface IPlayerStrategy
{
    Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state);
    Card? ChooseBottomPlay(IReadOnlyList<Card> hand, EncounterState state);
    Dictionary<Invader, int>? AssignCounterDamage(Territory territory, int damagePool, EncounterState state);
    string? ChooseRestGrowthTarget(EncounterState state) => null;
    string? ChooseTarget(EffectData effect, EncounterState state) => null;
    void ResolvePendingThresholds(ThresholdResolver resolver, EncounterState state)
        => resolver.AutoResolveAll(state);
}
```

The sim runner (`src/HollowWardens.Sim/Program.cs`) selects strategy via:

```csharp
static IPlayerStrategy BuildStrategy(string? strategyName, string? profilePath, string wardenId)
```

Strategy can be specified in a sim profile JSON via `"strategy": "root_tall"`.

---

## Problem

Hand-tuned bots give us a **floor** — the balance data only reflects how well we guessed the strategy.
We need three data points per mechanic test:

1. **simple_bot** (`root_wide`) — greedy floor. Currently used.
2. **warden_bot** (`root_tall`) — tuned heuristics. Just shipped.
3. **optimal_bot** — hill-climber upper bound. **Not yet built.**

The gap between `warden_bot` and `optimal_bot` is a skill-gap metric: a large gap means the mechanic heavily rewards optimal play; a small gap means the heuristic bot already plays near-optimally. This is valuable for balance and design confidence.

---

## Proposed Design: Parameterised Strategy + Hill-Climbing

### Step 1 — StrategyParams (data class)

A flat struct of tuneable thresholds that control all decision branch-points:

```csharp
public class StrategyParams
{
    // Presence
    public int SpreadTarget       { get; set; } = 3;   // expand until this many territories
    public int StackTarget        { get; set; } = 3;   // then stack toward this presence per territory

    // Card play urgency thresholds
    public int DamageUrgency      { get; set; } = 1;   // min invaders before damage is priority
    public int CleanseUrgency     { get; set; } = 5;   // corruption pts before cleanse is priority
    public int WeaveRestoreUrgency{ get; set; } = 10;  // weave below this → prioritise RestoreWeave

    // Bottom play weights (relative, treated as priority scores)
    public int BottomDamageWeight { get; set; } = 100;
    public int BottomFearWeight   { get; set; } = 60;
    public int BottomCleanseWeight{ get; set; } = 90;
    public int BottomPresenceWeight{ get; set; } = 50;

    // Warden-specific
    public int PassiveUnlockPriority { get; set; } = 2; // 1=before spread, 2=before stack, 3=deprioritise
}
```

### Step 2 — ParameterizedBotStrategy

A single bot class that reads from `StrategyParams` instead of hard-coded constants. Both `RootTallStrategy` and `EmberBotStrategy` can be replaced by this class with appropriate defaults.

```csharp
public class ParameterizedBotStrategy : IPlayerStrategy
{
    public StrategyParams Params { get; }
    public ParameterizedBotStrategy(StrategyParams p) { Params = p; }
    // ... decision logic driven entirely by Params ...
}
```

### Step 3 — HillClimber

A class in `src/HollowWardens.Sim/` that runs the optimisation loop:

```
HillClimber.Optimise(
    wardenId: "root",
    encounterId: "pale_march_standard",
    seeds: [1..100],             // fast inner loop
    startParams: StrategyParams.RootDefaults,
    maxIterations: 200,
    perturbMagnitude: 1          // ±1 on a random param per iteration
) → (bestParams, bestScore, scoreHistory)
```

**Algorithm (random-restart greedy hill-climb):**

```
score(params) = run N seeds with ParameterizedBotStrategy(params)
                → clean% − 3 × breach%          (weighted outcome)

currentParams = startParams
currentScore  = score(currentParams)

for i in 1..maxIterations:
    candidate = Perturb(currentParams)          // ±1 on one random int param
    candidateScore = score(candidate)
    if candidateScore > currentScore:
        currentParams = candidate
        currentScore  = candidateScore

// Optional: restart from random params every K iterations to escape local maxima
```

**Perturb function:** pick one param at random, apply ±1 (clamped to valid range per param). Use a seeded `Random` so runs are reproducible.

**Score function:** `clean% - 3.0 * breach%` — matches balance targets (clean=good, breach=bad, weighted 3:1).

### Step 4 — CLI integration

```
dotnet run --project src/HollowWardens.Sim -- \
  --warden root --encounter pale_march_standard \
  --optimise --optimise-seeds 1-100 --optimise-iterations 200 \
  --optimise-output sim-output/optimised-root.json
```

Output JSON: `{ "params": { ... }, "score": 47.3, "clean_pct": 52.0, "breach_pct": 8.0, "history": [...] }`

---

## Integration Points

| File | Change needed |
|------|---------------|
| `src/HollowWardens.Core/Run/StrategyParams.cs` | New — the data class |
| `src/HollowWardens.Core/Run/ParameterizedBotStrategy.cs` | New — replaces hard-coded strategy |
| `src/HollowWardens.Sim/HillClimber.cs` | New — optimisation loop |
| `src/HollowWardens.Sim/Program.cs` | Add `--optimise` CLI flag, call `HillClimber.Optimise` |
| `src/HollowWardens.Sim/BuildStrategy()` | Add `"optimised"` case that loads saved params JSON |
| `src/HollowWardens.Sim/SimProfile.cs` | Add `"strategy_params_path"` field for loading saved params |

---

## Usage Pattern

For a new mechanic like B6 Assimilation:

```bash
# 1. Optimise on a fast seed set
dotnet run -- --warden root --encounter pale_march_standard \
  --optimise --optimise-seeds 1-100 --optimise-iterations 200 \
  --optimise-output sim-output/root-standard-opt.json

# 2. Validate optimised params on full seed set
dotnet run -- --profile sim-profiles/b6-t3-standard.json \
  --strategy-params sim-output/root-standard-opt.json

# 3. Compare: simple vs warden_bot vs optimal
dotnet run -- ... --strategy root_wide     # floor
dotnet run -- ... --strategy root_tall     # heuristic
dotnet run -- ... --strategy optimised     # upper bound
```

---

## Constraints / Do-Not-Do List

- Do NOT use MCTS or neural nets — too slow for 500-seed runs.
- The inner loop must complete 100 seeds in < 5 seconds to keep 200-iteration optimisation under 20 minutes.
- `StrategyParams` should be serialisable to/from JSON so optimised params can be saved and replayed.
- The optimised bot should be usable in chain-sim mode too.
- Do NOT replace `RootTallStrategy` / `EmberBotStrategy` yet — keep them as fast named options. `ParameterizedBotStrategy` is an addition, not a replacement (until proven stable).

---

## Open Questions for Opus

1. Is a flat param struct the right level of granularity, or should params be per-phase (spread-phase vs stack-phase)?
2. Should the score function include `weathered%` (e.g. `clean − 3×breach + 0.5×weathered`)?
3. Random-restart interval: restart every 50 iterations? Accept only improvements ≥ 0.5pp to avoid noise chasing?
4. Is there a smarter perturbation strategy (e.g. perturb the param with the highest gradient from the last N steps)?
