# Hollow Wardens — Active Context

## Current Status
**Phase 6+ active development** — 646 tests passing, functional prototype playable.
Both Root and Ember wardens implemented. Single/Chain encounter selection UI. 5 encounter configs live. B1/B2/B4 balance shipped. Full string localization migration complete. SQLite crash + localization bug fixed.

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

## Balance Status — B1 + B2 + B4 Shipped

| Change | File | Status |
|--------|------|--------|
| B1: Ember Flame Burst + Conflagration top 3→2 | `data/wardens/ember.json` | ✅ Shipped |
| B2: Root +1 Marcher/wave (`AddB2Marchers`) | `EncounterLoader.cs` | ✅ Shipped |
| B4: Ember — fear from kills, Fury L2, Flame Out 3× | `DamageInvadersEffect.cs`, `EmberFuryHelper.cs`, `TurnActions.cs` | ✅ Shipped |

**B2 applied to:** standard, scouts, siege, elite. **NOT frontier** (wide board is its own difficulty).

**Sim-validated balance targets:** Clean 50–70%, Weathered 20–35%, Breach 5–15%

**Root B2 results (500 seeds each):**
| Encounter | Clean% | Breach% | Status |
|-----------|--------|---------|--------|
| standard | 56% | 9.4% | ✅ |
| scouts | 62% | 4.2% | ✅ |
| siege | 61.6% | 0.4% | ✅ |
| elite | 33% | 3.8% | Hard E3 — acceptable |

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

### High Priority
1. **D41: Elemental threshold → active targeted ability** — WeaveSystem sets PendingThreshold flag instead of auto-firing; TurnManager exposes `UseThreshold(territoryId)` free action; threshold effects receive TargetInfo; UI shows ready indicator + territory click to activate. T3 broadcasts to all presence (current behavior). See CLAUDE-decisions.md §D41.
   - This is the root fix for Ember's 0% breach dominance — stat tweaks cannot fix it because the problem is structural (auto-broadcast to all territories).

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
