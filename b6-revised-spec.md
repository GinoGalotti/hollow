# B6: Root Assimilation Redesign — Revised Spec

## Read First
- `CLAUDE-decisions.md` §D42, §D43
- `CLAUDE-balance.md` §B6
- `SIM_REFERENCE.md`
- `src/HollowWardens.Core/Wardens/RootAbility.cs`

## The Problem (Unchanged)

D42's same-territory Assimilation (≥2 presence + ≥2 natives + invaders → convert) collapsed clean rates to 0% across all encounters. Root has no tools to build a native army, so the condition never triggers.

Additional finding: **Root's identity is wide-presence network control.** Sim evidence:
- Wide bot (B6): 0% clean / 36% breach on standard
- Tall bot (B6): 0% clean / 52% breach on standard — dramatically WORSE

Tall play cripples Network Fear/Slow (which need ≥3 presence-territory neighbors per territory to fire). Root's primary defense is the wide network. Any Assimilation redesign must work WITH wide play, not against it.

## The Design: Presence-Scaled Native Spawning

**Base Assimilation — at Tide start:**

1. Player picks ONE territory where Root has presence
2. Spawn natives in that territory based on a formula tied to presence count

**The formula is configurable for A/B testing.** Three candidates:

| Formula | Name | Presence=1 | Presence=2 | Presence=3 | Total over 6 tides (if same territory, no deaths) |
|---------|------|------------|------------|------------|---------------------------------------------------|
| `presence` | linear | 1 | 2 | 3 | 6 / 12 / 18 |
| `1 + floor(presence / 2)` | scaled | 1 | 2 | 2 | 6 / 12 / 12 |
| `ceil(presence / 2)` | half | 1 | 1 | 2 | 6 / 6 / 12 |

**Why this creates the wide vs. tall choice:**

- **Wide Root** (1 presence in 5 territories): picks a different territory each tide, spawns 1 native each time. After 6 tides: 6 natives spread across 5 territories. Network Fear/Slow fully active. Natives are scattered but Provocation makes each one useful for counter-attacks.

- **Tall Root** (3 presence in 2 territories, 1 in others): picks the stacked territory each tide, spawns 2-3 natives there. After 6 tides: 12-18 natives concentrated. Conversion upgrade becomes very powerful. But fewer Network neighbors means weaker Fear/Slow coverage.

- **Hybrid** (2 presence in 3-4 territories): spawns 1-2 per tide, decent spread. Gets some of both.

The player makes this choice every tide — it's not locked in at the start. "I've been spawning at M1 for 3 tides, but A2 just got overrun — I'll spawn there this tide instead." That's tactical flexibility.

## Implementation

### Step 1: Add configurable knobs to BalanceConfig

```
assimilation_spawn_mode: "linear" | "scaled" | "half"
```

- `linear`: spawn count = presence in chosen territory
- `scaled`: spawn count = 1 + floor(presence / 2)
- `half`: spawn count = ceil(presence / 2)

Also add to `SIM_REFERENCE.md` under a new "### Natives / Assimilation" section:

```
| Key | Default | Description |
|-----|---------|-------------|
| assimilation_spawn_mode | "scaled" | Formula for native spawn count: "linear" (=presence), "scaled" (=1+floor(presence/2)), "half" (=ceil(presence/2)) |
```

### Step 2: Implement base Assimilation

In `RootAbility.cs`, change the Assimilation logic (when `Gating.IsActive("assimilation")`):

**Timing: Tide start** (not Resolution). Natives spawned at tide start can counter-attack that same tide if Provocation is active.

```
At Tide start (after element decay, before player Vigil):
  If assimilation is active:
    Let player choose one territory where Root has presence >= 1
    Calculate spawn_count based on assimilation_spawn_mode:
      linear: spawn_count = presence_count_in_territory
      scaled: spawn_count = 1 + floor(presence_count / 2)
      half:   spawn_count = ceil(presence_count / 2)
    Spawn spawn_count natives in that territory (HP = default_native_hp from BalanceConfig)
    Log: "ASSIMILATION: Spawned {spawn_count} Native(s) at {territory} (presence={count}, mode={mode})"
```

**For the sim bot:** Choose the territory with the most invaders adjacent to it. If tied, choose the territory with the most presence (maximizes spawn count). This is a reasonable heuristic — the bot places natives where they'll fight.

### Step 3: Gate conversion behind upgrade

When `Gating.IsActive("assimilation_u1")` (the upgrade):

**Timing: Resolution** (after the final tide, as currently implemented).

```
At Resolution:
  For each territory where:
    - Root has presence >= 2
    - Territory has natives >= 2
    - Territory has invaders
  Convert floor(min(presence, natives) / 2) invaders → natives
    - Target weakest invaders first
    - Converted native HP = max(1, invader.MaxHp / 2)
    - Log: "ASSIMILATION UPGRADE: Converted {invader} → Native at {territory}"
```

This is the existing D42 conversion logic, just gated behind the upgrade. The base spawn mechanic now builds the native army the upgrade needs.

### Step 4: Create sim profiles

**Profile A — Linear (1 native per presence):**
```json
{
  "name": "b6-linear — spawn count = presence in territory",
  "seeds": "1-500",
  "warden": "root",
  "balance_overrides": {
    "assimilation_spawn_mode": "linear"
  }
}
```

**Profile B — Scaled (1 + floor(presence/2)):**
```json
{
  "name": "b6-scaled — spawn count = 1 + floor(presence/2)",
  "seeds": "1-500",
  "warden": "root",
  "balance_overrides": {
    "assimilation_spawn_mode": "scaled"
  }
}
```

**Profile C — Half (ceil(presence/2)):**
```json
{
  "name": "b6-half — spawn count = ceil(presence/2)",
  "seeds": "1-500",
  "warden": "root",
  "balance_overrides": {
    "assimilation_spawn_mode": "half"
  }
}
```

Run all three on ALL FOUR encounters: standard, scouts, siege, elite.
That's 12 sim runs total (3 formulas × 4 encounters).

### Step 5: Analyze

Compare against B5 targets:

| Encounter | B5 Target Clean% | B5 Target Breach% |
|-----------|-------------------|-------------------|
| standard | 52% | 9.6% |
| scouts | 50.6% | 3.2% |
| siege | 43.6% | 5.2% |
| elite | ~30% | ~5% |

Also check in verbose logs:
- How many natives are alive at each tide? (Are they accumulating or dying as fast as they spawn?)
- Which territory does the bot choose most often? (Validates the targeting heuristic)
- Does Provocation fire more often now? (Natives counter-attacking means Provocation is working)
- Is there a critical-mass problem? (One territory with 10+ natives steamrolling everything)

### Step 6: Decision tree

**If "scaled" hits B5 targets across most encounters:** Ship it. Best balance between wide and tall reward.

**If "linear" is needed to hit targets:** Natives from presence=3 territories (3/tide) might snowball. Check verbose logs for critical mass. If it snowballs, add a native cap per territory (e.g., max 4 natives per territory).

**If "half" is too weak and "scaled" overshoots:** Try a custom formula — e.g., `min(presence, 2)` (cap at 2 natives regardless of presence). Add as a fourth mode if needed.

**If ALL three formulas produce 0% clean:** The problem isn't spawn count — it's something else (bot targeting, tide timing, Network passive interaction). Check whether Network Fear/Slow are actually firing by examining verbose logs.

**If any formula exceeds 70% clean:** Too many natives. Add `assimilation_native_cap` (max natives per territory from Assimilation) as a knob and test with cap=3 and cap=4.

## What NOT to change
- Network Fear / Network Slow mechanics — leave unchanged, they reward wide play and that's correct
- Provocation — stays as pool passive, do not move to base
- Ember balance — B1/B4 are shipped and working, don't touch
- Encounter configs — board state stays warden-neutral
- Other Root passives (Dormancy, Rest Growth) — unchanged
