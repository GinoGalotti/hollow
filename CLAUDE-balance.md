# Hollow Wardens — Balance Decisions

Balance changes derived from sim testing. Each entry records what changed, why, and the evidence behind it.

Format: **B{n}: {Warden} — {summary}**

---

## B1: Ember — Flame Burst + Conflagration top damage 3 → 2

**Change:** `ember_001` (Flame Burst) and `ember_008` (Conflagration) top `value` reduced from 3 to 2.

**Evidence:** 15 sim configurations tested across 7,500 encounters (Rounds 1–3, seeds 1–500 each). Zero breach in every configuration. Root cause: Ash Trail passive (1 dmg to all invaders in presence territories at Tide start) + DI×3 tops = Outriders (HP 3) die to Ash Trail + one top hit. Invaders cannot survive long enough to Ravage or advance to the Heart regardless of threshold tuning.

**Why this fix:** DI×2 + Ash Trail = 3 damage, which no longer one-shots an Outrider on the first hit. Invaders now need 2 hits to clear, opening a Ravage window. Card nerf is Ember-specific (does not affect global invader stats or other wardens). Sim M (DI×2 tops alone) produced 54% Clean — best result across all tested configs, squarely in the 50–70% target.

**Why not other options tested:**
- Threshold damage nerfs (configs A–K): exhausted direction. Kill count stayed ~22–23 regardless of threshold changes; cards handle invaders independently.
- `invader_hp_bonus: 1` (config L): global buff, harms all wardens. Also produced 0% breach.
- Per-element threshold overrides (config H: Ash T2=1): creates per-element threshold inconsistency — players can't carry knowledge between wardens. Dropped in favour of a card stat change.
- H+M combined (config N): *worse* than M alone (49% Clean vs 54%). Do not stack.

**Remaining limitation:** Sim bot plays optimally. 0% breach in sims is an artifact of perfect play. Expect real players to generate breach through suboptimal targeting. Validate via playtesting.

**Threshold system:** Kept uniform (T1=1, T2=2, T3=3 damage for all elements). No per-warden or per-element overrides shipped.

---

## B2: Root / Encounter — extra_invaders_per_wave: 1 (CONFIRMED, NOT YET SHIPPED TO CODE)

**Change:** Add 1 extra Marcher to A1 per wave option. Originally proposed for `pale_march_standard`; now confirmed across standard, scouts, siege, and elite. Applied via `encounter_overrides: { "extra_invaders_per_wave": 1 }` in SimProfile JSON.

**Original evidence (pale_march_standard):** R-C hit all five targets simultaneously — the only config to do so.

| Config | Clean% | Wthrd% | Breach% | Weave | Heart Dmg |
|--------|--------|--------|---------|-------|-----------|
| Baseline | 95% | 4.2% | 0.8% | 17.85 | 0.99 |
| R-A: No Assimilation | 0% | 99.2% | 0.8% | 17.85 | 0.99 |
| R-B: No Network Slow | 94.8% | 4.2% | 1.0% | 17.75 | 1.02 |
| **R-C: +1/wave (B2)** | **62.6%** | **28.4%** | **9%** | **16.1** | **1.53** |
| R-D: No Assimilation + +1 | 0% | 91% | 9% | 16.1 | 1.53 |

**Expanded to all encounters (500 seeds each):**

| Encounter | Without B2 | With B2 | Assessment |
|-----------|-----------|---------|------------|
| standard | 93% / 5.6% / 1.4% | **56% / 34.6% / 9.4%** | ✅ All 5 targets |
| scouts | 89.6% / 9.2% / 1.2% | **62% / 33.8% / 4.2%** | ✅ All 5 targets |
| siege | 80.8% / 19.2% / 0% | **61.6% / 38% / 0.4%** | ✅ Near targets (breach low) |
| elite | 58% / 40.8% / 1.2% | **33% / 63.2% / 3.8%** | Acceptable for hard E3 |
| frontier | 10.2% / 81.4% / 8.4% | NOT TESTED — skip B2 | Wide board is its own difficulty |

**Key findings:**
- One lever (`extra_invaders_per_wave: 1`) brings Root into target range on ALL encounter types simultaneously.
- Breach rate is driven by invader volume, not by Assimilation or Network Slow.
- Assimilation = board-quality lever (Clean vs Weathered), not a survival lever.
- Network Slow = cosmetic in current encounter design.
- Frontier already at 8.4% breach — B2 would make it unwinnable. Leave as-is; its difficulty comes from the wide board layout.
- Elite B2 (33% clean / 3.8% breach) is below the 50–70% Clean target but appropriate for a penultimate encounter.

**Ember note:** B2 applied to standard drops Ember to 19% Clean / 0% Breach (Ember's outcome is board-state based, not weave-based — see B3). B2 on scouts brings Ember to 51.4% Clean ✅. B2 on elite = 4.2% Clean / 0% Breach (very challenging but survivable). Ember never takes weave damage on any encounter, making carryover asymmetric — see B3.

**Shipping plan:**
- Apply `extra_invaders_per_wave: 1` in `EncounterLoader` factory methods for: `CreatePaleMarchStandard()`, `CreatePaleMarchScouts()`, `CreatePaleMarchSiege()`, `CreatePaleMarchElite()`.
- Do NOT apply to `CreatePaleMarchFrontier()`.
- Implementation pending; currently applied via encounter_overrides in sim profiles only.

**Sim bug fixes landed alongside this investigation:**
- `RootAbility.OnResolution` now checks `Gating.IsActive("assimilation")` before firing
- `PassiveGating.ForceLock` now persists against threshold re-unlocks (added `_lockedPassives` set)
- `SimProfileApplier.ApplyEncounterOverrides` now wires `ExtraInvadersPerWave` to wave options

---

## B3: Encounter Progression — Run Arc Analysis (2026-03-22)

**Scope:** Chain simulation of standard(B2) → scouts(B2) → elite(B2), both wardens, using real p25/p50/p75 carryover values extracted from CSV output.

### Encounter Baselines (500 seeds, no B2)

| Encounter | Root Clean% | Root Breach% | Ember Clean% | Ember Breach% | Notes |
|-----------|-------------|--------------|--------------|---------------|-------|
| standard | 93% | 1.4% | 54% | 0% | Root too easy; Ember in range |
| scouts | 89.6% | 1.2% | 73.6% | 0% | Both too easy; Ember especially |
| siege | 80.8% | 0% | 0% | 0% | Root handles Ironclads; Ember always weathered |
| elite | 58% | 1.2% | 5.6% | 0% | Root in range; Ember extreme |
| frontier | 10.2% | 8.4% | 0% | 5.4% | Hard for both; fine as capstone |

### Warden Asymmetry (confirmed, deliberate)

- **Root = anti-tank**: Network Slow + Assimilation hard-counter slow Ironclads. Root's danger zone is `frontier` (wide board, coverage problem).
- **Ember = anti-swarm**: Ash Trail one-shots fragile Outriders. Ember's comfort encounter is `scouts`; danger zone is `siege` (Ironclads absorb Ash Trail).
- Asymmetry emerged with zero intentional tuning. It is a design feature, not a bug.

### Carryover System Behavior

**Root (carryover is real):**
- Exit standard-B2: p25 weave=16, p50=20, p75=20. Dread always=3 (Network Fear fills dread fast).
- 25% of Root players exit E1 damaged. 75% exit at full health.
- Dread/fear carryover: **zero effect on outcomes** (confirmed by chain testing — dread=3 vs dread=1 start produces identical results).
- Weave carryover: **small but real** effect. Starting E2 at weave=16 adds +1.2pp breach vs weave=20 start.

**Ember (carryover is flat):**
- Exit standard-B2: p25/p50/p75 weave all = 20. Ember NEVER takes weave damage on any tested encounter.
- "Weathered" for Ember = **board-state classification** (invaders at I1 at encounter end), NOT heart damage.
- Distinguishing Clean vs Weathered for Ember: Clean runs have more `total_presence_at_end` (+24%) and less `total_fear_generated` (−13%). Presence coverage = clean board; less presence = invaders reach I1.
- Chain is completely flat for Ember — any carryover percentile produces identical E2 and E3 outcomes.

### Full 3-Encounter Chain Arc (B2 on all)

**Root — damage cascades for worst-case players:**
| Encounter | Fresh (no carryover) | p25 arc (accumulated damage) |
|-----------|---------------------|------------------------------|
| E1 standard-B2 | 56% / 9.4% breach | — (is the E1) |
| E2 scouts-B2 | 62% / 4.2% breach | 61.8% / **5.4%** breach |
| E3 elite-B2 | 33% / 3.8% breach | 32.6% / **6.2%** breach |

Root worst-case arc is viable: breach rates escalate (9.4% → 5.4% → 6.2%) but stay within target. Typical players (p50/p75) see identical outcomes to fresh runs because they exit each encounter at weave=20.

**Ember — flat across all runs:**
| Encounter | Any arc |
|-----------|---------|
| E1 standard-B2 | 19% / 0% breach |
| E2 scouts-B2 | 51.4% / 0% breach |
| E3 elite-B2 | 4.2% / 0% breach |

All Ember percentile chains are identical — the chain simulation confirmed no meaningful carryover.

### Open Design Gap: Ember Carryover

Ember's run arc is flat because its "weathered" outcomes (invaders at I1) don't produce weave damage. A future mechanic is needed to create run escalation for Ember:

**Option A:** `heart_damage_multiplier: 1.5` on siege and elite — forces Ember to take real weave damage on hard encounters, creating meaningful carryover into subsequent encounters.

**Option B:** New carryover field: `starting_invaders` — residual invaders at I1 at encounter end carry into next encounter as starting I1 corruption. Architecturally heavier.

**Option C:** Accept asymmetry. Ember's run challenge is cumulative board-state degradation (always weathered) rather than weave attrition. If weave is used for a future mechanic (upgrades, healing), Ember's preserved weave is a design choice, not a gap.

Decision deferred. Playtest first — real players may take weave damage even when the sim bot doesn't.

---

## B4: Ember — Feel fixes (fear from kills, Fury L2, Flame Out 3×) — 2026-03-23

**Background:** Playtest feedback: "I feel I can just spam damage and that's it, it can destroy everything." Three targeted fixes to address the spam-damage feel without touching the core numbers.

### Changes shipped (code live, 646 tests passing):

**1. Fear from card-effect kills (`DamageInvadersEffect.cs`)**
- Before: kills fired `GameEvents.InvaderDefeated` (UI only — no fear generated)
- After: each kill also calls `state.Dread?.OnFearGenerated(1)` + `GameEvents.FearGenerated?.Invoke(1)`
- Rationale: Destroying invaders should feel rewarding. Previously kills generated zero fear — no feedback loop for precise targeting.

**2. Ember Fury requires L2 / Defiled (`EmberFuryHelper.cs`)**
- Before: `CorruptionLevel >= 1` (Tainted = 3+ corruption points, reached after 1 Ash Trail tick)
- After: `CorruptionLevel >= 2` (Defiled = 8+ corruption points, requires real buildup)
- Rationale: At L1, Fury bonus fired on nearly every DamageInvaders card from tide 3 onward with zero player effort. L2 requires the player to actually build heavy corruption in specific territories.

**3. Flame Out 3× elements (`TurnActions.cs`)**
- Before: Ember bottoms used `BottomElementMultiplier` (2×, same as all wardens)
- After: Ember bottoms use 3× element multiplier
- Rationale: "Flame Out" (permanent card burn on bottom play) felt like a pure downside with no upside. 3× elements gives players a real payoff — if you're burning a card for good, it should supercharge your threshold engine.

### Sim evidence (500 seeds, pale_march_standard):

| Metric | Baseline | After B4 | Delta |
|--------|---------|----------|-------|
| Clean | 18% | 26.8% | +8.8pp |
| Weathered | 82% | 73.2% | −8.8pp |
| Breach | 0% | 0% | — |
| Avg fear/encounter | 27.91 | 42.27 | +51% |
| Avg invaders killed | 28.17 | 28.11 | ≈ same |

Fear increase is substantial (+51%) — kills now feed the dread track meaningfully. Clean rate up because Flame Out 3× charges thresholds faster, enabling more threshold ability uses per encounter. Breach still 0% — the root dominance issue is structural (auto-broadcast thresholds).

### Remaining structural problem:

Ember still never loses because elemental thresholds auto-broadcast to ALL presence territories. This is a player-skill problem: no amount of stat tweaking fixes it. The correct fix is **D41: threshold as targeted active ability** (see CLAUDE-decisions.md §D41). Deferred to next session — requires WeaveSystem + TurnManager + UI changes.

### Ruled-out lever:

`invader_hp_bonus: 1` (HP+1 for all invaders): Ember+B2+HP1 sim = 1.6% Clean / 98.4% Weathered / 0% Breach — Ember kills 32 invaders/encounter (vs 28) with zero breach. Adding invaders just gives Ember more targets. Not a useful lever for Ember specifically.

