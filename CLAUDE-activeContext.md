# Hollow Wardens — Active Context

## Current Status
**Phase 6+ active development** — 689 tests passing, functional prototype playable.
Both Root and Ember wardens implemented. Single/Chain encounter selection UI. 5 encounter configs live. B1/B2/B4/B5/D41/D42/D44 shipped. Full string localization migration complete. SQLite crash + localization bug fixed.

**⚠️ BALANCE PENDING — B6 v2 implemented, sims run, design decision needed.** Root Assimilation redesigned to tide-start native spawn (3 formula modes: linear/scaled/half). 12 sim profiles created. Current numbers (root_wide, standard+B2): **0% clean / 66% weathered / 34% breach**. "Clean" is now structurally 0% — old Resolution clear is gone; "Weathered" is the new success state. 34% breach is above the 5–15% target. Design question: is native spawn alone enough, or is tuning needed? See §B6-v2 in CLAUDE-balance.md.

## Phase Completion
| Phase | Description | Tests | Status |
|-------|-------------|-------|--------|
| 1–4 | Data layer, territory graph, card engine, encounter loop | 82 | ✅ Done |
| 5 | Root Warden implementation | +10 | ✅ Done |
| 6a | Pure C# architecture migration (Core/Sim/Tests split) | major rewrite | ✅ Done |
| 6b | Ember Warden + passive gating + sim workbench | +~150 | ✅ Done |
| 6c | Data-driven thresholds (BalanceConfig per-element overrides) | +7 | ✅ Done |
| 6d | Territory targeting for global-range effects | 0 new | ✅ Done |
| 6e | Localization infrastructure (Loc.cs + CSV) | +10 | ✅ Done |
| 6f | Encounter variety: 5 configs, 4 board layouts, 22 levers | +63 | ✅ Done |
| 6g | Balance sim: B1 (Ember nerf) + B2 (Root +1/wave) shipped | 0 new | ✅ Done |
| 6h | String migration (all controllers → Loc.Get) + chain encounter selection UI | +35 | ✅ Done |

## Architecture Summary
| Layer | Path | Notes |
|-------|------|-------|
| Pure C# logic | `src/HollowWardens.Core/` | No Godot deps — all game logic |
| xUnit tests | `src/HollowWardens.Tests/` | `dotnet test`, no Godot needed |
| Godot UI | `hollow_wardens/godot/` | Bridge + view controllers only |
| Sim console | `src/HollowWardens.Sim/` | Balance simulation, `--warden`, `--encounter`, `--profile` |

## Encounter Configs (live in EncounterLoader.cs)

| ID | Tier | Tides | Board | Identity |
|----|------|-------|-------|----------|
| `pale_march_standard` | Standard | 6 | standard | Tutorial — mixed Marcher/Outrider |
| `pale_march_scouts` | Standard | 6 | standard | Outrider-heavy, fast pressure (Ember comfort) |
| `pale_march_siege` | Standard | 8 | standard | Ironclad + Pioneer, dual escalation (Root comfort) |
| `pale_march_elite` | Elite | 6 | standard | Starting corruption A1/A2/M1, hard capstone |
| `pale_march_frontier` | Standard | 7 | wide | 4-row board, coverage challenge for both wardens |

**Confirmed Realm 1 run order:** standard → scouts → siege → elite
(frontier = optional alternate capstone or post-game challenge)

## Balance Status

| Change | File | Status |
|--------|------|--------|
| B1: Ember Flame Burst + Conflagration top 3→2 | `data/wardens/ember.json` | ✅ Shipped |
| B2: +1 Marcher/wave on standard, scouts | `EncounterLoader.cs` `AddB2Marchers` | ✅ Shipped |
| B4: Ember — fear from kills, Fury L2, Flame Out 3× | `DamageInvadersEffect.cs`, `EmberFuryHelper.cs`, `TurnActions.cs` | ✅ Shipped |
| B5: +2 Marchers/wave on siege + elite | `EncounterLoader.cs` `AddB2Marchers(count:2)` | ✅ Shipped |
| **B6: Root passive redesign (v2)** | `RootAbility.cs`, `BalanceConfig.cs` | ⚠️ **Shipped, 34% breach — design decision needed** |

**Extra Marchers per encounter:** standard=+1, scouts=+1, siege=+2, elite=+2. NOT frontier.

**Sim-validated balance targets:** Clean 50–70%, Weathered 20–35%, Breach 5–15%

**B6 v2 sim results (tide-start spawn, Provocation locked, root_wide, 100 seeds — 2026-03-24):**
| Formula | Clean% | Weathered% | Breach% | Avg natives killed |
|---------|--------|------------|---------|-------------------|
| linear (standard+B2) | 0% | 66% | **34%** | 1.11 |
| scaled (standard+B2) | 0% | 66% | **34%** | 1.11 |
| half (standard+B2) | 0% | ≈66% | **34%** | ≈1.07 |

All three formulas give **identical results with wide play** (presence=1 per territory ⇒ all formulas = 1 native). Formulas differentiate only with tall stacking (presence ≥ 2). "Clean" is 0% by design — old Resolution invader-clear is gone; invaders always survive to end. **Weathered = survived. Breach = lost.**

**Pre-D42 sim results (B5, old mechanics — STALE):**
| Encounter | Clean% | Breach% | Status |
|-----------|--------|---------|--------|
| standard | 52% | 9.6% | ✅ was in range |
| scouts | 50.6% | 3.2% | ✅ was in range |
| siege | 43.6% | 5.2% | ✅ was in range |
| elite | ~30% | ~5% | acceptable capstone |

**D42 sim results (broken mechanics — pre-B6):**
| Encounter | Clean% | Breach% | Delta vs target |
|-----------|--------|---------|-----------------|
| standard | **0%** | **28%** | −52pp clean, +18pp breach |
| scouts | **0%** | **34.4%** | −52pp clean, +29pp breach |
| siege | **0%** | **54%** | −43pp clean, +49pp breach |
| elite | **0%** | **36%** | −30pp clean, +31pp breach |

**B6 sim results (2-tier assimilation shipped, T2 vs T3 threshold — 500 seeds each):**
| Encounter | T3 Clean%/Breach% | T2 Clean%/Breach% | B5 target |
|-----------|-------------------|-------------------|-----------|
| standard  | 0% / 36.4%        | 0% / 36.4%        | 52% / 9.6% |
| scouts    | 0% / 50.6%        | 0% / 50.6%        | 50.6% / 3.2% |
| siege     | 0% / 60.8%        | 0% / 60.8%        | 43.6% / 5.2% |
| elite     | 0% / 57%          | 0% / 57%          | ~30% / ~5% |

T2=T3 identical means spawn threshold is irrelevant — spawn fires 0 times. Bot never stacks presence.

**Warden asymmetry (emergent, not tuned):**
- Root = anti-tank: strong vs Ironclads/siege, challenged by frontier's wide board
- Ember = anti-swarm: strong vs Outriders/scouts, challenged by siege (Ironclads absorb Ash Trail)
- Asymmetry emerged from passive mechanics alone — treat as a design feature.

## Chain Arc (confirmed)
Standard(B2) → Scouts(B2) → Elite(B2) is a coherent difficulty arc:
- **Root worst-case:** breach escalates 9.4% → 5.4% → 6.2% (manageable; most players exit at weave=20)
- **Ember chain:** flat — Ember never takes weave damage; carryover mechanism is an open design gap
- **Dread/fear carryover:** zero gameplay effect (confirmed via chain sim)
- **Weave carryover:** small but real (+1.2pp breach per encounter in damaged arc; affects ~25% of Root players)

## Recently Completed Work

### Session 2026-03-24 — B6 v2 redesign: tide-start native spawn (D45)
- **B6 v2 implemented:** `RootAbility.OnTideStart` — picks best presence territory (most adj invaders), spawns natives per formula. `RootAbility.OnResolution` now only handles the `assimilation_u1` upgrade. `BalanceConfig.AssimilationSpawnThreshold` removed; replaced with `AssimilationSpawnMode` string (`"linear"/"scaled"/"half"`). `SimProfileApplier.ApplyBalanceOverrides` gained string-type handling.
- **12 sim profiles created:** `sim-profiles/b6-{linear,scaled,half}-{standard,scouts,siege,elite}.json`
- **8 unit tests updated** in `RootFullEncounterTest.cs` (all 3 formula modes + spawn-without-invaders case). 689 tests passing.
- **Sim results (root_wide, standard+B2, 100 seeds):** 0% clean / 66% weathered / **34% breach**. All three formulas identical with wide play (presence=1 ⇒ all formulas = 1 native). Clean is 0% by design — old Resolution invader-clear is gone.
- **False lead noted:** Provocation (pool passive) was briefly force-enabled in profiles but reverted — not the design intent. With Provocation forced: 15% breach (root_wide). Without: 34% breach.
- **Design pending:** 34% breach is above target; see CLAUDE-balance.md §B6-v2.

### Session 2026-03-24 — RootTallStrategy + multi-strategy bot system (Layer 1+2)
- **`RootTallStrategy`** new class (`src/HollowWardens.Core/Run/`): spreads to `spreadTarget` (3) territories first, then stacks toward `stackTarget` (3) presence per territory before expanding further. `ChooseRestGrowthTarget` targets the territory closest to the stack threshold. Now the **default bot for Root** in all sim modes.
- **`"strategy"` field in `SimProfile`** — profiles can specify `"strategy": "root_tall"`, `"root_wide"`, or `"ember_aggressive"`. CLI `--strategy` still takes precedence.
- **`BuildStrategy` in `Program.cs`** updated: named strategies registered, Root now defaults to `RootTallStrategy` (was `BotStrategy`).
- **`ChainSimulator`**, **`TelemetryBotWrapper`**, **`VerboseLogger`** all updated for `RootTallStrategy`.
- **`hill-climber-bot-spec.md`** written — full spec for Layer 3 (adaptive bot, for Opus).
- 687 tests pass (0 failures).

### Session 2026-03-24 — B6 Root passive redesign (shipped + sim failed)
- **B6 design:** Split Assimilation into two tiers — base (presence ≥ threshold → spawn 1 native at Resolution) + upgraded `assimilation_u1` (≥2 presence + ≥2 natives → convert invaders to natives). Code shipped in `RootAbility.OnResolution`, `BalanceConfig.AssimilationSpawnThreshold`, 11 tests updated in `RootFullEncounterTest.cs`.
- **8 sim profiles created** (`sim-profiles/b6-t{2,3}-{standard,scouts,siege,elite}.json`) testing threshold=2 vs threshold=3 across all 4 encounters, 500 seeds each.
- **Result: 0% Clean across all 8 profiles.** T2=T3 identical — threshold irrelevant because spawn fires 0 times.
- **Root cause:** Sim bot spreads presence wide (1 token/territory) to maximize Network Fear. Assimilation requires tall presence (≥2–3 in one territory), which the bot actively avoids. Mechanic was never tested against its design prerequisite.
- **B6 is worse than D42:** Standard breach rate 28% (D42) → 36.4% (B6). D42 occasionally fired due to accidental ≥2 presence coincidence; B6 spawn fires zero times.
- **CLAUDE-balance.md updated** with full B6 story, root cause analysis, and 3 design options for Opus.
- **Decision needed:** Opus design review required before next code change.

### Session 2026-03-24 — D41 threshold redesign + B5 balance pass
- **D41 shipped:** Complete threshold effect redesign across all 6 elements. Root T1↔T2 swapped, Mist T2/T3 return discard→draw, Ash T3 presence-scaled, Gale T1/T2 stack-toward-most-populated, Void T1=3 dmg, T2 hits natives, T3 kills generate Fear. `IDeckManager.ReturnDiscardToDraw` added. 679 tests passing.
- **B5 shipped:** `AddB2Marchers` refactored to take `count` param. Siege and elite bumped to +2 Marchers/wave. Scouts stays +1 (Outrider swarm already brutal — +2 tanks to 26%). Siege: 62.4%→43.6% Clean (was too safe). Elite: ~30% unchanged (bottlenecked by starting corruption).
- **D42 design confirmed:** Passives currently dimmed (not hidden) when locked. `PassivePanelController` needs update when run-level locked passives exist.

### Session 2026-03-23 — Bug fixes + B4 Ember feel
- **SQLite crash fix:** `Microsoft.Data.Sqlite` moved from Core to Sim. GameBridge now uses `NullSink` instead of SQLiteSink. This also fixed the cascading localization bug (Loc.Load() never ran because SQLiteSink threw before it could).
- **B4 shipped:** 3 targeted Ember feel fixes (646 tests passing):
  1. `DamageInvadersEffect.cs`: kills now generate 1 fear via `Dread.OnFearGenerated` + `FearGenerated` event
  2. `EmberFuryHelper.cs`: Fury requires L2 (Defiled, 8+ corruption) instead of L1 (Tainted, 3+)
  3. `TurnActions.cs`: Ember bottoms use 3× element multiplier (Flame Out upside)
- **Sim evidence:** Clean 18%→26.8%, fear/encounter 27.91→42.27 (+51%). Breach stays 0%.
- **D41 design confirmed:** Threshold targeting as active ability is the structural fix for Ember dominance. Recorded in CLAUDE-decisions.md.

### Phase 6g — Balance: B1 + B2
- **B1 (Ember nerf):** top damage 3→2 on Flame Burst + Conflagration. Prevents Ash Trail + DI×3 one-shotting Outriders (HP 3) before they can act. 15 configs tested across 7,500 encounters.
- **B2 (Root fix):** `AddB2Marchers()` private helper in `EncounterLoader.cs` adds 1 Marcher to A1 in every `SpawnWaveOption`. Applied to 4 factory methods. One lever fixes Root on all encounter types simultaneously.
- **B3 (chain arc):** Full 3-encounter run arc simulated at p25/p50/p75 carryover. Ember carryover gap identified. Full findings in `CLAUDE-balance.md`.
- **Warden asymmetry confirmed** as emergent design feature from passive mechanics alone.
- **Evidence:** `CLAUDE-balance.md` (B1/B2/B3) and `SIM_REFERENCE.md` (lever reference, single source of truth).

### Phase 6f — Encounter Variety System
- **5 encounter configs** in `EncounterLoader.cs` with `Create(id)` dispatcher.
- **4 board layouts** in `TerritoryGraph.cs`: standard (3-2-1), wide (4-3-2-1), narrow (2-1-1), twin_peaks (3-2-2-1 with M1↔M2 not adjacent).
- **22 encounter levers** on `EncounterConfig`: surge_tides, extra_invaders_per_wave, invader_corruption_scaling, invader_advance_bonus, presence_placement_corruption_cost, corruption_spread, blight_pulse_interval, native_erosion_per_tide, etc.
- **Board carryover** (`BoardCarryover.cs`): starting_weave, starting_corruption, dread_level, total_fear, removed_cards.
- **63 sim profiles** covering baselines, B2 experiments, chain arc simulations.
- **63 new tests** across EncounterVarietyTests, EncounterLeverTests, BoardLayoutTests, BoardCarryoverTests.

### Phase 6h — String Migration + Chain Encounter Selection UI
- **String migration complete:** All view controllers now use `Loc.Get()`. Loc.cs updated to process `\n` escape sequences from CSV values.
- **strings.csv:** 119+ keys (added PHASE_VIGIL_N, DECK_COUNTS, BTN_BACK, BTN_PLAY_TOP_RES, BTN_SKIP_DMG, LABEL_REVEALED, LABEL_NEXT, LABEL_NO_CARD, LABEL_NONE, CA_NO_DAMAGE, CA_DMG_N, FEAR_CONFIRM_BTN, chain UI keys).
- **WardenSelectController:** Redesigned as playtesting tool with Single mode (pick any encounter) and Chain mode (configure 3 slots: E1/E2/Capstone, each with tab buttons for all 5 encounters, then Start Chain).
- **GameBridge chain support:** `ChainEncounterIds[]`, `ChainIndex`, `HasNextInChain`, `ContinueChain()`. After each chain encounter ends, `ChainAdvanceReady` signal fires. `ContinueChain()` resets loop state, applies carryover via `EncounterRunner.ApplyCarryover()`, rebuilds encounter.
- **ChainContinueController** embedded in WardenSelectController: shows result + carryover summary + continue button between chain encounters.
- **35 new tests** in LocalizationTests.cs (new key coverage, `\n` escape test) + EncounterSelectionTests.cs (all 5 encounter IDs valid, chain slots valid, carryover extract stable).

### Phase 6e — Localization Infrastructure
- `src/HollowWardens.Core/Localization/Loc.cs` — `Loc.Get(key)`, `Loc.Get(key, args...)`, fallback to key. `\n` in CSV processed to real newline.
- `data/localization/strings.csv` — 119+ English keys (all UI strings migrated).

## Open Work / Next Steps

### High Priority (Blocking)
1. **B6: Design decision on breach rate** — B6 v2 (tide-start spawn, 3 formula modes) is implemented (689 tests). Sims show **34% breach** with root_wide on standard+B2, with natives spawning but only counter-attacking on Ravage/Corrupt action cards (Provocation is a pool passive). This is above the 5–15% target. Key question: is native spawn without Provocation sufficient, or does the encounter need re-tuning? Options:
   - Reduce invader pressure (remove B2 on standard since old Resolution clear is gone)
   - Tune formula to spawn more natives (but with wide play all formulas give same = 1/tide)
   - Accept 34% breach as a new harder baseline (Weathered=66% is survivable)
   - See §B6-v2 in CLAUDE-balance.md

2. **Bot strategy system (L3 pending Opus)** — Layer 1+2 shipped: `RootTallStrategy`, `"strategy"` field in SimProfile. Layer 3 (hill-climber adaptive bot) specced in `hill-climber-bot-spec.md` — hand this to Opus for design + implementation.

### High Priority
2. **D42: Hide locked passives in encounter UI** — `PassivePanelController` currently shows all passives, dimming locked ones at 50% opacity. D42 requires *hiding* run-level locked passives entirely. Currently all passives are threshold-gated (no run-level locked passives exist yet) so no visible change until run-level progression is added. Design confirmed in CLAUDE-decisions.md §D42.
3. **Sim bot targeting quality** — `AutoResolveAll` uses greedy null-target fallbacks. Improving targeting heuristics would give more accurate sim numbers (currently a floor, not realistic midpoint).

### Medium Priority
2. **Playtest** with B4 live — now that kills generate fear and Ember Fury requires L2, does combat feel more deliberate?
3. **Ember carryover decision** (defer to post-playtest data):
   - Option A: `heart_damage_multiplier: 1.5` on siege/elite — forces Ember weave damage on hard encounters
   - Option B: `starting_invaders` residual mechanic — I1 invaders carry into next encounter
   - Option C: Accept asymmetry — Ember's challenge is board degradation, not weave attrition
4. **Dev console wiring** — most console commands are parsed but dispatch to "coming soon" stubs in `DevConsole.DispatchCommand`. Wired commands: `/help`, `/add_presence`, `/kill_all`, `/export`, `/run_info` (console UI); plus `/set_weave`, `/set_corruption`, `/give_tokens`, `/end_encounter` (via GameBridge.ExecuteConsoleCommand, called from smoke tests). Remaining stubs: `/set_max_weave`, `/set_element`, `/set_dread`, `/spawn`, `/add_card`, `/upgrade_card`, `/unlock_passive`, `/upgrade_passive`, `/trigger_event`, `/skip_tide`, `/encounter`, `/restart`.

### Pending Tests (needs writing)
- **WardenSelectController input-blocking regression** — After `LaunchEncounter()` / `StartWithWarden()` is called, `WardenSelectController.Visible` must be `false`. The bug was that the full-screen `overlay` Control (added in `_Ready()` with `FullRect` anchors, `MouseFilter=Stop`) stayed active after the game started, swallowing all card-click events. Can't use xUnit (Godot node). Options: (a) Godot integration test scene that calls `StartWithWarden` and asserts `WardenSelect.Visible == false`; (b) a code-convention check that warns if any `CanvasLayer` child of the `Game` scene root has a FullRect child Control without a `Visible = false` guard in a `LaunchEncounter`-style method.

## Localization Pattern (for new code)
```csharp
using HollowWardens.Core.Localization;
label.Text = Loc.Get("SOME_KEY");
label.Text = Loc.Get("PHASE_TIDE_N", currentTide, totalTides);
// Add key to data/localization/strings.csv: MY_KEY,"My English string"
// Use \n in CSV values for embedded newlines (processed by Loc.cs loader)
```
