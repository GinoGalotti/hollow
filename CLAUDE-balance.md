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

---

## B5: Encounter — siege +2 Marchers/wave, elite +2 Marchers/wave — 2026-03-24

**Background:** Post-D41 sim (bot greedy targeting) showed siege at 62.4% Clean / 0.6% Breach — far too safe. Testing +2 extra Marchers per wave on scouts, siege, and elite (not standard/tutorial).

**Results (500 seeds each, Root warden):**

| Encounter | +1 (B2) | +2 (B5 candidate) | Verdict |
|-----------|---------|-------------------|---------|
| scouts | 50.6% / 3.2% | 26.4% / 8.2% | ❌ Too hard — Outrider swarm already brutal |
| siege | 62.4% / 0.6% | 43.6% / 5.2% | ✅ Meaningful pressure, fits penultimate role |
| elite | ~30% / ~5% | ~30% / ~4% | ≈ No change — bottlenecked by starting corruption |

**Shipped:** `AddB2Marchers(waves, count: 2)` for siege and elite. Scouts reverted to count: 1.

**Final per-encounter invader counts:**
- standard: +1 Marcher/wave (tutorial)
- scouts: +1 Marcher/wave (50.6% Clean — Outriders already provide pressure)
- siege: **+2 Marchers/wave** (43.6% Clean / 5.2% Breach — harder penultimate encounter)
- elite: **+2 Marchers/wave** (≈30% Clean — elite difficulty driven by starting corruption + escalation)

**Note:** All B5 sims use bot greedy threshold targeting (AutoResolveAll with null target). Real players choosing targets deliberately expected to achieve +5–10pp Clean. Sim numbers are a floor, not a ceiling.

---

## B6: Root — Passive redesign needed after D42 mechanic changes (OPEN — re-sims needed with RootTallStrategy)

**Status: Bot blindness resolved (RootTallStrategy shipped) — re-run sims before design decision — 2026-03-24**

---

### The Problem (D42 collapse)

D42 redesigned Root's Assimilation from adjacent-territory to same-territory (≥2 presence + ≥2 natives + invaders in same territory). This caused complete balance collapse:

| Encounter | B5 target | D42 result | Delta |
|-----------|-----------|------------|-------|
| standard | 52% / 9.6% | **0% / 28%** | −52pp clean, +18pp breach |
| scouts | 50.6% / 3.2% | **0% / 34.4%** | −50pp clean, +31pp breach |
| siege | 43.6% / 5.2% | **0% / 54%** | −44pp clean, +49pp breach |
| elite | ~30% / ~5% | **0% / 36%** | −30pp clean, +31pp breach |

Root cause: arrival territories (A1/A2/A3) have 0 natives; inner territories (M1/M2/I1) start with 2 each but die before Resolution. The three-way coincidence (≥2 presence + ≥2 natives + invaders in same territory) almost never occurs.

---

### What Was Tried (B6 implementation — 2026-03-24)

Implemented Option A from D43: **split Assimilation into base spawn + upgraded conversion.**

**Code changes shipped (684 tests, all passing):**
- `BalanceConfig.AssimilationSpawnThreshold = 3` — new knob; configurable via sim profiles
- `RootAbility.OnResolution` redesigned:
  - **Base** (always active): for each territory with `presence >= threshold`, spawn 1 native (HP=2)
  - **Upgraded** (`assimilation_u1`): AFTER spawn pass, also run D42 conversion logic (≥2 presence + ≥2 natives + invaders → convert weakest invaders)
- `data/wardens/root.json` assimilation description updated
- `SIM_REFERENCE.md` Natives section updated with `assimilation_spawn_threshold` knob
- 11 unit tests updated/added in `RootFullEncounterTest.cs`

**Sim profiles created:** `b6-t{2,3}-{standard,scouts,siege,elite}.json` (8 profiles, all include B2/B5 marcher counts)

---

### Sim Results (500 seeds × 8 runs — 2026-03-24)

Ran threshold=3 and threshold=2 on all four encounters:

| Encounter | T3 Clean% / Breach% | T2 Clean% / Breach% | B5 Target |
|-----------|--------------------|--------------------|-----------|
| standard | **0% / 36.4%** | **0% / 36.4%** | 52% / 9.6% |
| scouts | **0% / 50.6%** | **0% / 50.6%** | 50.6% / 3.2% |
| siege | **0% / 60.8%** | **0% / 60.8%** | 43.6% / 5.2% |
| elite | **0% / 57%** | **0% / 57%** | ~30% / ~5% |

**Key observations:**
- **0% clean across all 8 runs.** No improvement over D42.
- **T2 = T3 in every case (identical to the decimal point for scouts/elite/siege).** The threshold has zero effect.
- **Breach rates worse than D42** — standard went from 28% (D42) to 36.4% (B6). B6 is actively worse.
- **Avg natives killed: 0.18–0.62** — essentially the pre-seeded natives dying to combat. No spawned natives are being created.

---

### Root Cause of B6 Failure

**The bot never stacks presence.** The sim bot optimizes for Network Fear coverage (wide presence: 1 token per territory). With total presence averaging 9–10 at encounter end spread across 6 territories, no territory ever hits threshold=2, let alone threshold=3. The spawn never fires.

This explains why:
1. T2 and T3 produce identical results — both thresholds are unreachable with the bot's wide strategy
2. Breach is worse than D42 — D42 conversion occasionally fired when the bot accidentally had ≥2 presence + ≥2 natives coincide; B6 spawn never fires at all, so zero Assimilation benefit of any kind

**This is a bot measurement problem, not a threshold tuning problem.** The mechanic requires a *tall* presence playstyle (stack 2–3 tokens in one territory). The bot plays *wide* (spread 1 token everywhere for Network Fear/Slow). The sim cannot measure this mechanic until either:
(a) the bot is updated to prefer stacking presence when playing Root, or
(b) the mechanic is redesigned to work with wide play

---

### Open Design Questions (for Opus)

The mechanic was designed around the hypothesis that "stack presence → grow natives → tension loop." The sim reveals this doesn't work at all with an unmodified bot. Three paths forward:

**Option A — threshold=1 (any presence spawns)**
Change `AssimilationSpawnThreshold` to 1: every territory with ANY presence spawns 1 native at Resolution. With the bot's wide play, this fires in 3–4 territories per tide from Tide 3. At B2 difficulty (13–17 invaders at mid-encounter), 3–4 extra HP=2 natives/tide may or may not be enough. Quick to test (one profile change, no code change). Risk: may be too powerful (>70% clean) if it fires everywhere.

**Option B — revert to Option B from D43 (adjacent-territory)**
Restore the pre-D42 adjacent-territory mechanic as base: "presence in territory X → converts/affects invaders in territories adjacent to X." Pre-D42 + B2 achieved 62.6% clean on standard. This was the proven working design before D42 changed it. Requires rewriting `OnResolution` again.

**Option C — bot update + keep current mechanic**
Teach the bot to stack presence at 1–2 key territories (M1/M2) rather than spreading. Properly measures the tall-play identity. Probably reveals the mechanic works if played correctly, but adds significant scope and changes bot behavior for all future warden testing.

**⚠️ Option C has been implemented (D44 — 2026-03-24).** `RootTallStrategy` ships as the new default Root bot. Spreads to 3 territories then stacks toward threshold=3. B6 sims should be re-run to get valid baseline numbers before choosing between Options A and B.

**The deeper question Opus should answer:** Is "native-synergy warden who stacks presence" the RIGHT design identity for Root at all? If the answer is yes, Option C was correct (re-run sims and tune). If the answer is "Root should naturally work with any presence spread," Options A or B are still available.

---

### Additional Context for Opus

**What Root's pre-D42 Assimilation did:** Presence in territory X → during Resolution, invaders in territories *adjacent* to X were converted to natives. This meant a single presence token at M1 would convert invaders at A1, A2, and I1. The bot (which places presence at M1/M2/I1) would naturally trigger Assimilation at all arrival territories. The mechanic worked WITH the bot's existing behavior, not against it.

**What B2 R-A showed:** Removing pre-D42 Assimilation entirely = 0% Clean / 99.2% Weathered / 0.8% Breach. Assimilation was the entire source of clean wins. Root without Assimilation is a zero-clean warden.

**What "natives killed" measures:** At 0.62 avg for standard, this is almost entirely the 6 pre-seeded natives (2 each at M1/M2/I1) dying to invader combat. B6 spawning is contributing essentially nothing.

---

## B6-v2: Root — Tide-start native spawn (redesigned mechanic — 2026-03-24)

**Status: Implemented, 34% breach — design decision pending**

### The Redesign (D45)

Replaced the old B6 Resolution-based spawn with a **tide-start spawn** approach. On every tide start, Root picks ONE presence territory and spawns natives there. Formula is configurable:

| Mode | Formula | Presence=1 | Presence=2 | Presence=3 |
|------|---------|-----------|-----------|-----------|
| `linear` | = presence | 1 | 2 | 3 |
| `scaled` | 1 + floor(p/2) | 1 | 2 | 2 |
| `half` | ceil(p/2) | 1 | 1 | 2 |

**Design intent:** Wide player (1 presence × 5 territories) gets 1 native/tide at the chosen territory. Tall player (3 presence × 1 territory) gets 2–3 natives/tide there. The tradeoff is real every tide. Spawned natives can counter-attack that same tide if Presence Provocation is active (pool passive).

**Code changes (689 tests):**
- `RootAbility.OnTideStart` — new method: picks territory with most adjacent invaders (tie-break: most presence), spawns per formula
- `RootAbility.OnResolution` — now only handles `assimilation_u1` upgrade (invader conversion); base spawn moved to tide-start
- `BalanceConfig.AssimilationSpawnMode` — string knob: `"linear"` / `"scaled"` / `"half"` (default `"scaled"`)
- `BalanceConfig.AssimilationSpawnThreshold` — **removed**; no longer needed
- `SimProfileApplier.ApplyBalanceOverrides` — added string-type handling (was missing before)
- **12 new sim profiles:** `sim-profiles/b6-{linear,scaled,half}-{standard,scouts,siege,elite}.json`
- **8 updated unit tests** in `RootFullEncounterTest.cs` covering all 3 formula modes + spawn-without-invaders case

### Sim Results (root_wide, standard+B2, 100 seeds — 2026-03-24)

| Formula | Clean% | Weathered% | Breach% | Avg natives killed |
|---------|--------|------------|---------|-------------------|
| linear | 0% | 66% | **34%** | 1.11 |
| scaled | 0% | 66% | **34%** | 1.11 |
| half | 0% | 66% | **34%** | ~1.07 |

**All three formulas produce identical results with wide play.** Because root_wide places 1 presence per territory, all three formulas output 1 native: `1+floor(1/2) = ceil(1/2) = 1`. Formula differentiation only appears with tall stacking (presence ≥ 2).

### Why 0% Clean Is Expected

The old `OnResolution` cleared ALL invaders adjacent to presence territories — very powerful, gave Clean trivially. That mechanic is gone. With the new B6, invaders survive to the end of the encounter in all realistic scenarios. **"Weathered" (survived, invaders alive) is now the success state for Root.** Clean would require clearing every alive invader AND all corruption, which the native army can't achieve on Ravage/Corrupt cards alone.

### Provocation Note (false lead — 2026-03-24)

During analysis, `force_passives: ["presence_provocation"]` was briefly added to all 12 sim profiles (then reverted). With Provocation forced:
- standard+B2, root_wide: 0% clean / 85% weathered / **15% breach**
- standard+B2, root_tall: 0% clean / 76% weathered / **24% breach**

Provocation (counter-attack on all Activate steps, not just Ravage) substantially helps. But Provocation is a **pool passive** — it starts locked and must be earned as a run reward. The user confirmed this is intentional; native spawn should work as a base mechanic without requiring Provocation.

The Provocation profiles were reverted. The 34% breach numbers above are the correct B6-v2 baseline.

### Open Question

34% breach (standard+B2, root_wide) is above the 5–15% target. Possible directions:
- **Remove B2 from standard** — old assimilation was the B2 counterweight; with it gone, standard may not need +1 invader/wave
- **Accept 34% for now** — playtest data will show if real players do better (they likely do; bot plays suboptimally)
- **Formula tuning** — only matters for tall play; wide play is always 1 native/tide regardless of formula

