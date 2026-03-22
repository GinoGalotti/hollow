# Hollow Wardens — Active Context

## Current Status
**Phase 6+ active development** — 478 tests passing, functional prototype playable.
Both Root and Ember wardens implemented. Two-screen warden/encounter selection. 5 encounter configs live. B1/B2 balance shipped and pushed.

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

## Balance Status — B1 + B2 Shipped (commit 6d508ab)

| Change | File | Status |
|--------|------|--------|
| B1: Ember Flame Burst + Conflagration top 3→2 | `data/wardens/ember.json` | ✅ Shipped |
| B2: Root +1 Marcher/wave (`AddB2Marchers`) | `EncounterLoader.cs` | ✅ Shipped |

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

### Phase 6e — Localization Infrastructure
- `src/HollowWardens.Core/Localization/Loc.cs` — `Loc.Get(key)`, `Loc.Get(key, args...)`, fallback to key.
- `data/localization/strings.csv` — 83+ English keys.
- `WardenSelectController.cs` — two-screen flow (warden → encounter), all text uses `Loc.Get()`.

## Open Work / Next Steps
1. **Playtest** with B2 live — validate sim predictions against real play. Real players expected to take weave damage where the bot doesn't.
2. **Ember carryover decision** (defer to post-playtest data):
   - Option A: `heart_damage_multiplier: 1.5` on siege/elite — forces Ember weave damage on hard encounters
   - Option B: `starting_invaders` residual mechanic — I1 invaders carry into next encounter
   - Option C: Accept asymmetry — Ember's challenge is board degradation, not weave attrition
3. **Bulk string migration** — ~165 hardcoded strings in DebugLogController, PhaseIndicatorController, DreadBarController, etc.
4. **Frontier encounter access** — not in main run arc; needs a clear access point (alternate capstone? post-game?).

## Localization Pattern (for new code)
```csharp
using HollowWardens.Core.Localization;
label.Text = Loc.Get("SOME_KEY");
label.Text = Loc.Get("PHASE_TIDE_N", currentTide, totalTides);
// Add key to data/localization/strings.csv: MY_KEY,"My English string"
```
The remaining ~165 hardcoded strings in other view controllers are NOT yet migrated.
