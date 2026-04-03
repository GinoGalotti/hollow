# Hollow Wardens — Simulation Reference
> Single source of truth for the sim harness, balance knobs, and CLI.
> Updated by the architecture conversation. Read by the balance conversation.
> Last updated: 2026-04-01 (Core Loop Redesign — pairing, dual-discard, terrain, element decay scaling, Root passive, new effect types)

---

## Turn Structure (Core Loop Redesign)

### New Phase Order

```
Plan → Fast → Tide → Slow → Dusk → Elements → Cleanup
```

| Phase | What happens |
|-------|-------------|
| **Plan** | Player (or bot) submits a `CardPair` (top card + bottom card) |
| **Fast** | Top card resolves if `TopTiming == Fast` (before invaders act) |
| **Tide** | Ravage → March → Arrive (unchanged). Invaders act. |
| **Slow** | Top card resolves if `TopTiming == Slow` (after invaders act) |
| **Dusk** | Bottom card resolves |
| **Elements** | Top elements added ×1, bottom elements added ×2, threshold effects resolved |
| **Cleanup** | Top → TopDiscard, Bottom → BottomDiscard, element decay applied |

**Rest turn:** No pair submitted. Tide still runs (invaders act). After Tide: `DeckManager.Rest(rng)` dissolves 2 random cards from BottomDiscard. Player is shown dissolved cards and may reroll each for 2 weave.

### Card Pairing

Each turn the player picks exactly 2 cards: one as **top** (resolves before or after Tide depending on Fast/Slow), one as **bottom** (resolves at Dusk, goes into at-risk pile on Cleanup).

- **Fast top** cards resolve before the Tide — proactive denial/positioning.
- **Slow top** cards resolve after the Tide — reactive response.
- **Bottom** cards always resolve at Dusk; the at-risk pile is dissolved on rest.

### Dual Discard System

| Pile | Cards enter when | Cards exit when |
|------|-----------------|-----------------|
| **TopDiscard** | Card played as top, soak card used | Rest (fully recovered) |
| **BottomDiscard** | Card played as bottom | Rest (2 random dissolved), or drawn back next turn |
| **Dissolved** | 2 random from BottomDiscard on rest, or Reroll victim | Permanent (encounter-scoped) |

**RerollDissolve:** Pay 2 weave to save one dissolved card during the rest screen; a different BottomDiscard card is dissolved instead. One reroll per rest turn.

**Damage Soak:** Before heart damage applies, discard a card from hand → TopDiscard (safe) to block up to 3 damage.

---

## CLI Reference

```bash
# Basic run (defaults to seeds 1-500)
dotnet run --project src/HollowWardens.Sim/ -- --warden root

# Specific seed range
dotnet run --project src/HollowWardens.Sim/ -- --seeds 1-500 --warden ember

# Specific seeds (comma-separated)
dotnet run --project src/HollowWardens.Sim/ -- --seeds 42,100,200 --warden root

# Single seed (backward compat)
dotnet run --project src/HollowWardens.Sim/ -- --seed 42 --warden ember

# With SimProfile (JSON overrides)
dotnet run --project src/HollowWardens.Sim/ -- --profile sim-profiles/my-test.json --output sim-results/my-test/

# Verbose logging (first 5 encounters + any breaches)
dotnet run --project src/HollowWardens.Sim/ -- --seeds 1-500 --warden ember --verbose --output sim-results/test/

# Specific encounter type
dotnet run --project src/HollowWardens.Sim/ -- --seeds 1-500 --warden root --encounter pale_march_siege --output sim-results/siege/

# Output directory (CSV + summary + logs)
dotnet run --project src/HollowWardens.Sim/ -- --seeds 1-500 --warden root --output sim-results/root-baseline/
```

**Flags:**
| Flag | Default | Description |
|------|---------|-------------|
| `--warden` | `root` | Warden ID: `root` or `ember` |
| `--encounter` | `pale_march_standard` | Encounter ID (see Available Encounters) |
| `--seeds` | `1-500` | Range (`1-500`) or list (`42,100,200`) |
| `--seed` | — | Single seed (shorthand for `--seeds N-N`) |
| `--profile` | — | Path to SimProfile JSON file |
| `--output` | `sim-results/` | Output directory for CSV + summary + logs |
| `--verbose` | off | Write per-encounter turn-by-turn logs |
| `--mode` | `single` | Sim mode: `single` (per-encounter) or `chain` (full roguelike run) |
| `--realm` | `realm_1` | Realm ID to use in chain mode |
| `--strategy` | `root_tall` | Strategy: `root_tall`, `root_wide`, `smart`, `optimised`, `telemetry` |
| `--strategy-profile` | — | Path to PlayerProfile JSON (required when `--strategy telemetry`) |
| `--strategy-params` | — | Path to StrategyParams JSON (used with `--strategy optimised`) |
| `--optimise` | off | Enable HillClimber optimizer mode |
| `--optimise-seeds` | `1-100` | Seed range for optimization fitness evaluation |
| `--optimise-iterations` | `200` | Max optimizer iterations |
| `--optimise-output` | — | Path to write optimized StrategyParams JSON |

**Chain mode example:**
```bash
# Full roguelike run sim — 500 seeds through realm_1
dotnet run --project src/HollowWardens.Sim/ -- --mode chain --realm realm_1 --warden root --seeds 1-500 --output sim-results/root-chain/
```

Chain mode output appends `chain-runs.csv` alongside the usual output files.

---

## Available Encounters

| ID | Tier | Tides | Description |
|----|------|-------|-------------|
| `pale_march_standard` | Standard | 6 | Baseline — mixed Marcher/Outrider waves |
| `pale_march_scouts` | Standard | 6 | Outrider-heavy; fast pressure, natives on A-row |
| `pale_march_siege` | Standard | 8 | Ironclad + Pioneer; escalation at T3 and T6 |
| `pale_march_elite` | Elite | 6 | 3 resolution turns; starts with A1/A2/M1 corruption |
| `pale_march_frontier` | Standard | 7 | Wider wave spread across all arrival points |

CLI flag: `--encounter <id>`. SimProfile field: `"encounter": "<id>"`. Defaults to `pale_march_standard`.

---

## SimProfile JSON Format

```json
{
  "name": "description of this test",
  "seeds": "1-500",
  "warden": "ember",
  "encounter": "pale_march_siege",
  "warden_overrides": {
    "hand_limit": 6,
    "add_cards": ["root_015"],
    "remove_cards": ["root_001"],
    "upgrade_cards": {
      "root_025": { "top": { "value": 3 } }
    },
    "force_passives": ["network_slow"],
    "lock_passives": ["rest_growth"],
    "starting_elements": { "Root": 3 }
  },
  "encounter_overrides": {
    "tide_count": 8,
    "starting_corruption": { "A1": 5, "M1": 3 },
    "native_spawns": { "M1": 3 }
  },
  "balance_overrides": {
    "element_decay_per_turn": 2,
    "threshold_t3_damage": 2,
    "element_overrides": {
      "Ash": { "tier3_threshold": 14, "t3_damage": 2 }
    }
  },
  "board_carryover": {
    "starting_weave": 15,
    "starting_corruption": { "A1": 3, "M1": 5 },
    "dread_level": 2,
    "total_fear": 18,
    "removed_cards": ["root_003"]
  }
}
```

All fields are optional. Omitted fields use defaults.

### Chain mode additions

```json
{
  "mode": "chain",
  "realm": "realm_1",
  "chain_overrides": {
    "starting_max_weave": 18,
    "starting_tokens": 1,
    "force_encounters": ["pale_march_standard", "pale_march_scouts", "pale_march_elite"],
    "disable_events": false,
    "bot_config": "data/sim/bot_chain_config.json"
  }
}
```

| `chain_overrides` field | Type | Description |
|-------------------------|------|-------------|
| `starting_max_weave` | int | Override max weave at run start (default 20) |
| `starting_tokens` | int | Starting upgrade tokens (default 0) |
| `force_encounters` | string[] | Override realm encounter sequence |
| `disable_events` | bool | Skip all event nodes (default false) |
| `bot_config` | string | Path to BotChainConfig JSON |

---

## Chain Output Format

Chain mode writes `chain-runs.csv` in the output directory. Columns:

| Column | Description |
|--------|-------------|
| `seed` | Run seed |
| `stages_completed` | How many encounters were finished |
| `encounter_results` | Comma-separated result per stage (clean/weathered/breach) |
| `final_weave` | Weave at run end |
| `final_max_weave` | Max weave at run end (can decay via events) |
| `tokens_earned` | Total upgrade tokens accumulated |
| `events_resolved` | Number of event nodes resolved |
| `full_clear` | 1 if all required stages completed, else 0 |

---

## Bot Chain Config (`data/sim/bot_chain_config.json`)

Controls bot decisions between encounters:

```json
{
  "draft_priority": ["DamageInvaders", "PlacePresence", "ReduceCorruption", "GenerateFear", "RestoreWeave"],
  "upgrade_priority": "highest_value_damage_card",
  "passive_upgrade_over_unlock": true,
  "event_strategy": "safe",
  "path_strategy": "balanced",
  "rest_stop_strategy": {
    "heal_threshold_percent": 80,
    "prefer_max_weave_heal_below": 16,
    "otherwise": "upgrade_card"
  }
}
```

| Field | Values | Description |
|-------|--------|-------------|
| `event_strategy` | `"safe"` | Prefer heal options; default option 0 otherwise |
| `path_strategy` | `"balanced"`, `"first"`, `"last"` | Node selection heuristic |
| `rest_stop_strategy.heal_threshold_percent` | int | Heal current weave if below this % of max |
| `rest_stop_strategy.prefer_max_weave_heal_below` | int | Prefer max weave heal if max < this value |

---

## Terrain System

### Terrain Types

| Terrain | Key Modifier | Trade-off |
|---------|-------------|-----------|
| **Plains** | Neutral | Default terrain |
| **Forest** | +1 player damage, +1 invader Ravage corruption | Bonus damage but corruption risk |
| **Mountain** | +2 fear on kill, +1 invader counter-attack | Fear engine but tougher fights |
| **Wetland** | +2 corruption threshold | Harder to corrupt, invaders rest-heal 1 |
| **Sacred** | Max corruption L1 | Corruption-immune above level 1 |
| **Scorched** | Invaders take 2 entry damage, no native spawn | Punishing to enter; blocks natives |
| **Blighted** | Auto-corrupts per tide | Net-negative; cleanse to escape |
| **Ruins** | No modifier (stub) | Transition target for Mountain |
| **Fertile** | No modifier | Transition target; collapses to Plains with 3+ invaders |

### Terrain Transitions

| From | Trigger | To |
|------|---------|-----|
| Forest | Corruption reaches L2 | Scorched |
| Mountain | Corruption reaches L3 | Ruins |
| Sacred | Invader Settles | Blighted |
| Blighted | Fully cleansed | Plains |
| Scorched | 3 clean tides (no corruption) | Plains |
| Fertile | 3+ invaders present | Plains |

Transitions are checked at tide end via `TerrainTransitions.CheckTransitions`.

### Terrain SimProfile Overrides

Set terrain in encounter config:
```json
"encounter_overrides": {
  "terrain_preset": "scorched_frontier",
  "terrain_overrides": {
    "A1": "Scorched",
    "M1": "Forest"
  }
}
```

Preset file: `data/terrain_presets.json`.

---

## Element Decay Scaling

Element decay now scales with current tier (configurable per balance key):

| Tier | Default Decay/turn |
|------|--------------------|
| Below T1 | 1 |
| At T1 | 2 |
| At T2 | 3 |
| At T3 | 4 |

### New Balance Keys — Element Decay

| Key | Default | Description |
|-----|---------|-------------|
| `element_decay_below_t1` | 1 | Decay when below T1 threshold |
| `element_decay_at_t1` | 2 | Decay when at or above T1 |
| `element_decay_at_t2` | 3 | Decay when at or above T2 |
| `element_decay_at_t3` | 4 | Decay when at or above T3 |
| `root_rest_extra_decay` | 2 | Extra decay Root suffers on rest turns |

---

## Root Passive — Elemental Offering

**Once per rest cycle:** Discard a card from hand to TopDiscard (safe). Add that card's elements ×1 to pool. No effect resolves. Flag `RootOfferingUsedThisCycle` resets on rest.

Triggered via `GameBridge.UseElementalOffering(cardId)` in Godot or `RootAbility.UseElementalOffering(card, state)` in Core.

### New Balance Key

| Key | Default | Description |
|-----|---------|-------------|
| `root_offering_element_multiplier` | 1 | Element multiplier for offering card (default ×1) |

---

## New Effect Types

| Type | Description | `value` field |
|------|-------------|---------------|
| `PullInvaders` | Pull up to N invaders from adjacent territories into target | Max count (0 = all) |
| `AddCorruption` | Add N corruption to target territory | Points to add |
| `CorruptionDetonate` | Damage all invaders = corruption × value, then cleanse all corruption | Damage multiplier |

### Pull Behavior

`PullInvadersEffect` gathers invaders from all adjacent territories into the target. Invaders entering a **Scorched** territory immediately take entry damage (2). The effect respects `_count` (limited pull) or pulls all if `value = 0`.

---

## Bot Strategy — Pairing System

### PairingBotStrategy

New `PairingBotStrategy` class evaluates all N×(N-1) card pair orientations per turn.

**Score formula per orientation:**
```
FastTopBonus (if card is Fast) + TopValue×2 + BottomValue×3 + ElementSynergy - RiskPenalty
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `HighValueThreshold` | 5 | Bottom values above this incur risk penalty |
| `FastTopBonus` | 10 | Score bonus for Fast cards played as top |
| `RestHandThreshold` | 2 | Hand size at or below which bot rests |
| `RerollValueThreshold` | 8 | Dissolved card bottom value above which bot rerolls |
| `RerollWeaveThreshold` | 6 | Minimum weave to trigger reroll |

**Element synergy:** +2 per element shared between top and bottom cards (builds toward thresholds).

**Rest decision:** `ShouldRest()` → rest when non-dormant hand ≤ 2 cards (default).

**Reroll decision:** `ShouldReroll()` → reroll if `dissolved.BottomEffect.Value > 8 && weave > 6`.

**Offering decision (Root):** `ShouldUseOffering()` → use if card has ≥2 primary elements and offering not yet used this cycle.

Strategy flag: `--strategy pairing` (use with `PairingBotStrategy`). Not yet wired into sim CLI — use `smart` for near-optimal play.

---

## Balance Overrides — Complete Key Reference

### Presence
| Key | Default | Description |
|-----|---------|-------------|
| `max_presence_per_territory` | 3 | Max presence tokens per territory |
| `amplification_per_presence` | 1 | Bonus per presence token on territorial effects |
| `amplification_cap` | unlimited | Max amplification bonus |

### Network Fear (Root)
| Key | Default | Description |
|-----|---------|-------------|
| `network_fear_cap` | 3 | Max passive fear generated per tide by Root. Triggers via cluster presence ≥ 3 (self + neighbors) |

### Sacrifice
| Key | Default | Description |
|-----|---------|-------------|
| `sacrifice_presence_cost` | 1 | Presence removed per sacrifice |
| `sacrifice_corruption_cleanse` | 3 | Corruption cleansed per sacrifice |

### Invaders
| Key | Default | Description |
|-----|---------|-------------|
| `invader_hp_bonus` | 0 | Added to all invader BaseHp on creation |
| `base_ravage_corruption` | 2 | Base corruption per Ravage action |
| `corruption_rate_multiplier` | 1.0 | Multiplier on total Ravage corruption |

### Weave
| Key | Default | Description |
|-----|---------|-------------|
| `max_weave` | 20 | Maximum weave (health) |
| `starting_weave` | 20 | Weave at encounter start |

### Corruption Thresholds
| Key | Default | Description |
|-----|---------|-------------|
| `corruption_level1_threshold` | 3 | Points for Tainted |
| `corruption_level2_threshold` | 8 | Points for Defiled |
| `corruption_level3_threshold` | 15 | Points for Desecrated |

### Element Thresholds (Global)
| Key | Default | Description |
|-----|---------|-------------|
| `element_tier1_threshold` | 4 | Element count to trigger T1 |
| `element_tier2_threshold` | 7 | Element count to trigger T2 |
| `element_tier3_threshold` | 11 | Element count to trigger T3 |
| `element_decay_per_turn` | 1 | Elements removed per element per turn |
| `top_element_multiplier` | 1 | Element multiplier for top plays |
| `bottom_element_multiplier` | 2 | Element multiplier for bottom plays |

### Threshold Damage (Global)
| Key | Default | Description |
|-----|---------|-------------|
| `threshold_t1_damage` | 1 | Damage dealt by any T1 effect |
| `threshold_t2_damage` | 2 | Damage dealt by any T2 effect |
| `threshold_t3_damage` | 3 | Damage dealt by any T3 effect |
| `threshold_t2_corruption` | 1 | Corruption added by T2 effects |
| `threshold_t3_corruption` | 0 | Corruption added by T3 effects |

### Per-Element Threshold Overrides
| Key | Default | Description |
|-----|---------|-------------|
| `element_overrides` | `{}` | Per-element config (see below) |

Each element override can set:
`tier1_threshold`, `tier2_threshold`, `tier3_threshold`,
`t1_damage`, `t2_damage`, `t3_damage`, `t2_corruption`, `t3_corruption`

Omitted fields fall back to global. Example:
```json
"element_overrides": {
  "Ash": { "tier3_threshold": 14, "t3_damage": 2 },
  "Root": { "tier1_threshold": 3 }
}
```

### Fear / Dread
| Key | Default | Description |
|-----|---------|-------------|
| `fear_per_action` | 5 | Fear spent to queue 1 fear action |
| `dread_threshold1` | 15 | Total fear for Dread Level 2 |
| `dread_threshold2` | 30 | Total fear for Dread Level 3 |
| `dread_threshold3` | 45 | Total fear for Dread Level 4 |

### Terrain
| Key | Default | Description |
|-----|---------|-------------|
| `element_decay_below_t1` | 1 | Element decay when below T1 |
| `element_decay_at_t1` | 2 | Element decay at T1 |
| `element_decay_at_t2` | 3 | Element decay at T2 |
| `element_decay_at_t3` | 4 | Element decay at T3 |
| `root_rest_extra_decay` | 2 | Extra elements Root loses on rest |
| `scorched_entry_damage` | 2 | Damage invaders take entering Scorched territory |

### Natives / Assimilation
| Key | Default | Description |
|-----|---------|-------------|
| `default_native_hp` | 2 | Native HP |
| `default_native_damage` | 3 | Native counter-attack damage |
| `assimilation_spawn_mode` | `scaled` | Tide-start native spawn formula: `linear` (=presence), `scaled` (=1+floor(presence/2)), `half` (=ceil(presence/2)) |
| `provocation_territory_limit` | 0 | Max presence territories with Provocation per tide (0 = unlimited) |
| `provocation_natives_per_presence` | 1 | Max natives counter-attacking per invader action per presence |

### Cards / Turns
| Key | Default | Description |
|-----|---------|-------------|
| `vigil_play_limit` | 2 | Max tops in Vigil |
| `dusk_play_limit` | 1 | Max bottoms in Dusk |

---

## Warden Overrides Reference

| Key | Type | Description |
|-----|------|-------------|
| `hand_limit` | int | Override hand size |
| `add_cards` | string[] | Card IDs to promote to starting deck |
| `remove_cards` | string[] | Card IDs to demote from starting deck |
| `upgrade_cards` | object | Per-card effect overrides |
| `force_passives` | string[] | Force-unlock passives at start |
| `lock_passives` | string[] | Force-lock passives |
| `starting_elements` | object | Element counts at encounter start |
| `starting_presence` | object | Override starting presence |

---

## Encounter Overrides Reference

### Base Overrides
| Key | Type | Description |
|-----|------|-------------|
| `tide_count` | int | Number of tides |
| `starting_corruption` | object | Territory → corruption points at start |
| `native_spawns` | object | Territory → native count override |
| `extra_invaders_per_wave` | int | Extra invaders per wave |
| `escalation_schedule` | array | Custom escalation entries |
| `board_layout` | string | Override board layout: `standard`, `wide`, `narrow`, `twin_peaks` |

### Easy Tier Levers
| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `element_decay_override` | int | (balance default=1) | Override element decay per turn |
| `starting_elements` | object | `{}` | Elements to add at encounter start, e.g. `{"Root": 3}` |
| `threshold_damage_bonus` | int | 0 | Added to T1/T2/T3 threshold damage |
| `vigil_play_limit_override` | int | (balance default=2) | Override max tops per Vigil |
| `dusk_play_limit_override` | int | (balance default=1) | Override max bottoms per Dusk |
| `hand_limit_override` | int | (warden default) | Override hand size for this encounter |
| `native_hp_override` | int | (balance default=2) | Override native HP |
| `native_damage_override` | int | (balance default=3) | Override native counter-attack damage |
| `fear_multiplier` | float | 1.0 | Multiply all fear generation (0.5 = half, 2.0 = double) |
| `heart_damage_multiplier` | float | 1.0 | Invaders at I1 deal ×N weave damage |

### Medium Tier Levers
| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `invader_corruption_scaling` | bool | false | Invaders gain +1 HP per L1+ territory on arrival |
| `invader_arrival_shield` | int | 0 | All invaders spawn with N shield |
| `invader_regen_on_rest` | int | 0 | Invaders heal N HP on Rest turns (capped at MaxHp) |
| `invader_advance_bonus` | int | 0 | All invaders move N extra steps per Advance (applied before Network Slow) |
| `surge_tides` | int[] | `[]` | Tides where double waves spawn, e.g. `[3, 5]` |
| `starting_infrastructure` | object | `{}` | Territory → infrastructure token count at start |
| `presence_placement_corruption_cost` | int | 0 | Placing Presence adds N corruption to that territory |

### Hard Tier Levers
| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `corruption_spread` | int | 0 | At Tide end, L1+ territories spread N corruption to a random adjacent L0 territory |
| `sacred_territories` | string[] | `[]` | Territory IDs immune to all corruption gain |
| `native_erosion_per_tide` | int | 0 | All natives lose N HP at Tide end; killed natives fire NativeDefeated |
| `blight_pulse_interval` | int | 0 | Every N tides, a random territory gains +3 corruption |
| `eclipse_tides` | int[] | `[]` | (Stub) Tides where phase order is inverted — not yet implemented |

---

## Board Layouts

| Layout ID | Territories | Arrival Row | Description |
|-----------|-------------|-------------|-------------|
| `standard` | 6 | A1–A3 (3) | Default 3-2-1 pyramid |
| `wide` | 10 | A1–A4 (4) | Broader front with Bridge row (4-3-2-1) |
| `narrow` | 4 | A1–A2 (2) | Compact 2-1-1 layout, tight defense |
| `twin_peaks` | 8 | A1–A3 (3) | 3-2-2-1 with two separate paths (M1↔M2 not adjacent) |

Set in `encounter_overrides.board_layout` or in `EncounterConfig.BoardLayout` (C# code).

**Wide layout** (10 territories):
```
A1  A2  A3  A4
  M1  M2  M3
    B1  B2
      I1
```

**Narrow layout** (4 territories):
```
A1  A2
  M1
  I1
```

**Twin Peaks layout** (8 territories):
```
A1  A2  A3
  M1    M2    ← M1 and M2 are NOT adjacent
  B1    B2    ← separate paths to I1
      I1
```

---

## Example Profiles

Three example profiles ship in `sim-profiles/`:

### `example-nightmare.json`
Worst-case stress test: corruption spreads every tide, surge waves at T3+T5,
invaders gain HP as corruption grows, and natives are extremely fragile.
```bash
dotnet run --project src/HollowWardens.Sim/ -- --profile sim-profiles/example-nightmare.json
```

### `example-ember-haunted.json`
Haunted ground variant for Ember: placing Presence costs 2 corruption, blight
pulses every 2 tides, and fear generation is boosted 50%.
```bash
dotnet run --project src/HollowWardens.Sim/ -- --profile sim-profiles/example-ember-haunted.json
```

### `example-twin-peaks.json`
Root on the twin-peaks layout: two separate invader paths converge at I1,
and all natives erode 1 HP per tide.
```bash
dotnet run --project src/HollowWardens.Sim/ -- --profile sim-profiles/example-twin-peaks.json
```

---

## Board Carryover Overrides Reference

Simulates a campaign's carry-forward state from a prior encounter. Applied after deck and state initialization.

| Key | Type | Description |
|-----|------|-------------|
| `starting_weave` | int | Weave at encounter start (replaces config default) |
| `starting_corruption` | object | Territory → corruption points pre-applied |
| `dread_level` | int | Starting dread level (1–4) |
| `total_fear` | int | Fear already accumulated (used to advance dread) |
| `removed_cards` | string[] | Card IDs permanently removed from deck |

**Priority:** Board carryover overrides run after encounter and warden overrides but before the first tide. `starting_corruption` in board carryover is additive with any `starting_corruption` in encounter overrides.

---

## Output Files

| File | Description |
|------|-------------|
| `summary.txt` | Aggregate stats |
| `encounters.csv` | One row per encounter |
| `per-tide.csv` | One row per tide per encounter |
| `logs/encounter_{seed}.txt` | Verbose turn-by-turn log |
| `telemetry.db` | SQLite telemetry database (sim runs; always written) |

---

## Telemetry

### Overview

Every sim run writes a SQLite database (`telemetry.db`) alongside the CSV output. In-game sessions write to `{OS user data dir}/telemetry/hollow_wardens.db`. These can be aggregated into a `PlayerProfile` for replay-accurate bot simulations.

### Aggregate Telemetry

```bash
# Aggregate a telemetry DB into a PlayerProfile JSON
dotnet run --project src/HollowWardens.Sim/ -- --aggregate-telemetry sim-results/my-run/telemetry.db --output player_profile.json

# Filter to a specific game version prefix (e.g. "0.8" matches "0.8.0+5")
dotnet run --project src/HollowWardens.Sim/ -- --aggregate-telemetry hollow_wardens.db --version-filter 0.8 --output player_profile.json
```

The command exits immediately after writing the JSON. No encounter simulation is run.

### Telemetry-Driven Strategy

```bash
# Run sim using a telemetry-derived player profile instead of the hardcoded bot
dotnet run --project src/HollowWardens.Sim/ -- --warden root --strategy telemetry --strategy-profile player_profile.json --seeds 1-500
```

| Flag | Default | Description |
|------|---------|-------------|
| `--strategy` | `root_tall` | `root_tall` (heuristic), `root_wide` (greedy), `smart` (parameterized), `optimised` (tuned), `telemetry` (profile) |
| `--strategy-profile` | — | Path to PlayerProfile JSON (required for `--strategy telemetry`) |
| `--aggregate-telemetry` | — | Path to telemetry DB to aggregate; writes JSON to `--output` then exits |
| `--version-filter` | — | Version prefix filter applied to aggregated records (e.g. `"0.8"`) |

### Database Schema

Five tables are written. All rows include `game_version`, `balance_hash`, `schema_version`, and `source` columns.

#### `runs`
| Column | Type | Description |
|--------|------|-------------|
| `run_id` | TEXT | UUID for the run |
| `warden_id` | TEXT | `root` or `ember` |
| `mode` | TEXT | `single`, `chain_sim`, or `player` |
| `realm_id` | TEXT | Realm played |
| `seed` | INTEGER | RNG seed |
| `result` | TEXT | `complete` or `failed` |
| `final_weave` | INTEGER | Weave at run end |
| `final_max_weave` | INTEGER | Max weave at run end |
| `total_fear` | INTEGER | Cumulative fear generated |

#### `encounters`
| Column | Type | Description |
|--------|------|-------------|
| `run_id` | TEXT | Parent run UUID |
| `encounter_id` | TEXT | Encounter config ID |
| `board_layout` | TEXT | Board layout used |
| `result` | TEXT | `clean`, `weathered`, or `breach` |
| `final_weave` | INTEGER | Weave at encounter end |
| `tides_completed` | INTEGER | Tides run |
| `invaders_killed` | INTEGER | Invaders defeated |
| `fear_generated` | INTEGER | Fear generated this encounter |
| `heart_damage_events` | INTEGER | Invaders that reached heart row |
| `peak_corruption` | INTEGER | Peak corruption across all territories |
| `sacrifices` | INTEGER | Presence sacrificed |
| `reward_tier` | TEXT | Reward tier earned (if tracked) |
| `elements_json` | TEXT | JSON snapshot of element counts at end |

#### `decisions`
| Column | Type | Description |
|--------|------|-------------|
| `run_id` | TEXT | Parent run UUID |
| `encounter_id` | TEXT | Current encounter |
| `tide` | INTEGER | Tide number when decision was made |
| `type` | TEXT | `card_play`, `rest`, `targeting`, `draft`, `upgrade` |
| `chosen` | TEXT | ID of chosen card or territory |
| `chosen_detail` | TEXT | Effect type (for targeting decisions) |
| `card_half` | TEXT | `top` or `bottom` |
| `target_territory` | TEXT | Territory targeted |
| `options_json` | TEXT | JSON array of available options |
| `reason` | TEXT | Bot strategy reason string |
| `weave_at_decision` | INTEGER | Current weave |
| `game_version` | TEXT | Game version string |

#### `tide_snapshots`
| Column | Type | Description |
|--------|------|-------------|
| `run_id` | TEXT | Parent run UUID |
| `encounter_id` | TEXT | Current encounter |
| `tide` | INTEGER | Tide number |
| `invaders_arrived` | INTEGER | Invaders that arrived this tide |
| `invaders_killed` | INTEGER | Invaders killed this tide |
| `weave_after` | INTEGER | Weave at tide end |
| `elements_json` | TEXT | Element counts at tide end |

#### `events`
| Column | Type | Description |
|--------|------|-------------|
| `run_id` | TEXT | Parent run UUID |
| `event_id` | TEXT | Event definition ID |
| `event_type` | TEXT | `choice`, `rest`, `corruption`, etc. |
| `option_chosen` | INTEGER | Index of option selected |
| `effects_json` | TEXT | Effects applied |
| `weave_before` | INTEGER | Weave before event |
| `weave_after` | INTEGER | Weave after event |
| `tokens_before` | INTEGER | Tokens before event |
| `tokens_after` | INTEGER | Tokens after event |

### Example Queries

```sql
-- Win rate by warden and encounter
SELECT e.encounter_id, r.warden_id,
       COUNT(*) AS total,
       SUM(CASE WHEN e.result = 'clean' THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS clean_pct,
       SUM(CASE WHEN e.result = 'breach' THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS breach_pct
FROM encounters e JOIN runs r USING (run_id)
GROUP BY e.encounter_id, r.warden_id;

-- Most-played cards (top plays only)
SELECT chosen, COUNT(*) AS plays
FROM decisions
WHERE type = 'card_play' AND card_half = 'top'
GROUP BY chosen ORDER BY plays DESC;

-- Voluntary rest rate (had cards available but rested)
SELECT
    SUM(CASE WHEN type = 'rest' AND options_json != '[]' THEN 1 ELSE 0 END) * 1.0
    / NULLIF(COUNT(*), 0) AS voluntary_rest_rate
FROM decisions WHERE type IN ('card_play', 'rest');

-- Average weave at which a specific card is played
SELECT chosen, ROUND(AVG(weave_at_decision), 1) AS avg_weave
FROM decisions WHERE type = 'card_play'
GROUP BY chosen ORDER BY avg_weave;

-- Element distribution at encounter end (parse JSON in Python/pandas)
SELECT encounter_id, elements_json FROM encounters WHERE result = 'breach';
```

### Player Profile JSON Format

Produced by `--aggregate-telemetry`. Consumed by `--strategy telemetry`.

```json
{
  "name": "",
  "source": "telemetry",
  "sample_size": 1200,
  "game_version_filter": "0.8",
  "card_play_distribution": {
    "root_001": 0.18,
    "root_002": 0.12
  },
  "targeting_preference": {
    "DamageInvaders": "most_invaded",
    "ReduceCorruption": "highest_corruption"
  },
  "bottom_play_rate": 0.42,
  "rest_timing": {
    "forced_rest_pct": 0.15,
    "voluntary_rest_pct": 0.08,
    "avg_cards_in_hand_at_rest": 2.3
  },
  "draft_preferences": {
    "root_015": 0.31,
    "root_022": 0.24
  },
  "upgrade_preferences": {
    "root_001": 0.45
  },
  "event_risk_tolerance": 0.6
}
```

All fields are optional when hand-authoring a profile. Missing fields cause the `TelemetryDrivenStrategy` to fall back to `BotStrategy` heuristics.

---

## Strategy System

### Three-Tier Measurement Model

| Tier | Strategy Flag | Class | Description |
|------|---------------|-------|-------------|
| Floor | `root_wide` | `RootTallStrategy` (greedy mode) | Greedy heuristic — establishes minimum performance baseline |
| Heuristic | `root_tall` | `RootTallStrategy` | Presence-stacking heuristic with fear/cleanse priorities |
| Near-ceiling | `smart` | `ParameterizedBotStrategy` | 68-parameter scoring engine — near-optimal play |
| Tuned | `optimised` | `ParameterizedBotStrategy` | Uses HillClimber-optimized params from `--strategy-params` |
| Telemetry | `telemetry` | `TelemetryDrivenStrategy` | Replays from PlayerProfile (see Telemetry section) |

### ParameterizedBotStrategy

Scoring-based strategy with 68 perturbable parameters controlling:

- **Phase transition** — early/late split at configurable tide (default: tide 3)
- **Effect priorities** — 12 priority ranks (early + late for: Damage, Fear, Cleanse, Presence, Weave, Passive Unlock, Spawn Natives, Move Natives)
- **Urgency thresholds** — when to switch from priority ordering to emergency response (heart threat, low weave, high corruption)
- **Bottom play weights** — 7 weights controlling when bottom plays are preferred
- **Targeting** — 16 params for kill preference, row targeting, cleanse strategies, threshold-specific targeting, provocation selection

Default parameters per warden are in `src/HollowWardens.Core/Run/StrategyDefaults.cs`.

### HillClimber Optimizer

Momentum-biased hill climbing optimizer. Evaluates breach% + clean% + average weave across a seed range.

```bash
# Optimize Root params over 200 iterations
dotnet run --project src/HollowWardens.Sim/ -- --warden root --optimise --optimise-seeds 1-100 --optimise-iterations 200 --optimise-output sim-params/root-optimised.json

# Run sim with optimized params
dotnet run --project src/HollowWardens.Sim/ -- --warden root --strategy optimised --strategy-params sim-params/root-optimised.json --seeds 1-500
```

Optimizer features:
- 70% recent-param-bias, 30% random exploration
- Periodic shakes every 60 iterations (4 random params perturbed ±1–3)
- Early convergence detection (window=40, min improvement=0.5)

### SimProfile Strategy Fields

```json
{
  "strategy": "smart",
  "strategy_params_path": "sim-params/root-optimised.json"
}
```

---

## Combo Testing Scripts

```bash
sim-profiles/scripts/combo-cards.sh root 100 42      # All draft card pairs
sim-profiles/scripts/combo-passives.sh root 100 42    # Passive combinations
sim-profiles/scripts/sweep-balance.sh root max_presence_per_territory 1 5 200 42
sim-profiles/scripts/compare.sh sim-results/a/ sim-results/b/
```

---

## Current Warden Stats

### Root
- Elements: Root / Mist / Shadow
- Starting deck: 10 cards, hand limit 5
- Starting presence: I1 × 1
- Base passives: Presence Provocation, Dormancy, Rest Growth
- Pool passives (unlockable): Network Fear, Network Slow, Assimilation

### Ember
- Elements: Ash / Shadow / Gale
- Starting deck: 8 cards, hand limit 5
- Starting presence: I1 × 1
- Base passives: Ash Trail, Flame Out, Scorched Earth
- Unlockable: Ember Fury (Ash T1), Heat Wave (Ash T2), Controlled Burn (Shadow T1), Phoenix Spark (Gale T1)
- Special: Presence tolerates Defiled (L3 blocks, not L2)

---

## Invader Stats (Pale March)

| Unit | HP | Ravage Corruption | Special |
|------|----|--------------------|---------|
| Marcher | 4 | 2 | Baseline |
| Outrider | 3 | 1 | +1 movement, never rests |
| Ironclad | 5 | 3 | +1 corruption on Ravage, alternating movement |
| Pioneer | 4 | 2 | Places infrastructure |

---

## Threshold Behavior

Each element threshold tier fires **exactly once per turn** (not per phase,
not per card play). The `_firedThisTurn` HashSet in ElementSystem is tested.
T1+T2+T3 each firing once = 6 combined damage per turn when all three are active.

Thresholds reset at Vigil start. Do NOT reset between Vigil and Dusk.
Rest turns check thresholds against carryover elements.

---

## Event System

Events fire at `event`, `rest`, `merchant`, and `corruption` nodes in the realm map.
Event definitions live in `data/events/*.json`. Each event has:

```json
{
  "id": "whispering_grove",
  "type": "choice",
  "name_key": "EVENT_WHISPERING_GROVE",
  "description_key": "EVENT_WHISPERING_GROVE_DESC",
  "warden_filter": null,
  "tags": ["stage_1", "stage_2"],
  "rarity": "common",
  "options": [
    {
      "label_key": "EVENT_WHISPERING_GROVE_A",
      "description_key": "EVENT_WHISPERING_GROVE_A_DESC",
      "effects": [
        { "type": "add_corruption", "value": 2 },
        { "type": "add_tokens", "value": 1 }
      ]
    }
  ]
}
```

**Event types:** `choice`, `sacrifice`, `rest`, `corruption`, `merchant`

**Tags used for filtering:**
- Stage tags: `stage_1`, `stage_2`, `stage_3`, `low_weave`
- Node tags: `rest`, `merchant`, `corruption`
- Warden tags: set `warden_filter: "root"` to restrict to one warden

**RunEffect types supported in event options:**

| Effect type | Description |
|-------------|-------------|
| `heal_weave` | Restore current weave |
| `heal_max_weave` | Restore max weave |
| `reduce_max_weave` | Reduce max weave permanently |
| `add_tokens` | Add upgrade tokens |
| `remove_tokens` | Remove upgrade tokens |
| `add_corruption` | Add corruption carryover to a territory |
| `cleanse_carryover` | Remove corruption carryover |
| `dissolve_card` | Permanently remove a card from deck |
| `add_card` | Add a card to the deck |
| `recover_card` | Un-remove a dissolved card |
| `unlock_passive` | Unlock a passive ability |
| `upgrade_passive` | Apply passive upgrade |
| `upgrade_card` | Apply card upgrade |
| `set_elements` | Set starting element counts |
| `set_balance` | Override a BalanceConfig field |
| `modify_hand_limit` | Change hand limit by ±N |

---

## Reward Tier Config

Reward tiers are defined in `data/rewards/reward_tiers.json`:

```json
{
  "tier1": { "draft_choices": 3, "upgrade_tokens": 1, "can_remove_card": true, "can_choose_heal": false, "draft_pool_tag": "tier1" },
  "tier2": { "draft_choices": 2, "upgrade_tokens": 1, "can_remove_card": false, "can_choose_heal": false, "draft_pool_tag": "tier2" },
  "tier3": { "draft_choices": 2, "upgrade_tokens": 0, "can_remove_card": false, "can_choose_heal": true, "draft_pool_tag": "tier3" }
}
```

**Tier assignment** (per encounter, per warden — defined in encounter config):
- **Tier 1 (Clean Victory):** Full clear + weave ≥ threshold %
- **Tier 2 (Weathered):** Survived, weave below threshold
- **Tier 3 (Breach):** Invaders reached heart row

---

## Draft Pool Config (`data/rewards/draft_pools.json`)

Controls which cards are offered as draft choices by rarity:

```json
{
  "default": {
    "stage_1": { "tier1": ["uncommon", "common"], "tier2": ["common"], "tier3": ["common"] },
    "stage_2": { "tier1": ["rare", "uncommon"], "tier2": ["uncommon", "common"], "tier3": ["common"] },
    "stage_3": { "tier1": ["rare"], "tier2": ["rare", "uncommon"], "tier3": ["uncommon"] }
  }
}
```

Non-starting cards are filtered by their `rarity` field to match the allowed rarities for the current stage and reward tier.

---

## Developer Console Commands

Toggle with backtick (`` ` ``). Available commands:

| Command | Args | Description |
|---------|------|-------------|
| `/help` | `[cmd]` | List all commands or help for one |
| `/encounter` | `<id>` | Start a specific encounter |
| `/restart` | — | Restart current encounter with same seed |
| `/set_weave` | `<n>` | Set current weave to n |
| `/set_max_weave` | `<n>` | Set max weave to n |
| `/set_corruption` | `<territory> <pts>` | Set corruption on territory |
| `/add_presence` | `<territory> [n]` | Place n presence tokens (default 1) |
| `/set_element` | `<element> <count>` | Set element pool |
| `/set_dread` | `<level>` | Set dread level |
| `/spawn` | `<type> <territory>` | Spawn an invader |
| `/kill_all` | — | Remove all invaders |
| `/add_card` | `<card_id>` | Add card to hand |
| `/upgrade_card` | `<card_id> <upgrade_id>` | Apply card upgrade |
| `/unlock_passive` | `<id>` | Force-unlock passive |
| `/upgrade_passive` | `<id>` | Apply passive upgrade |
| `/give_tokens` | `<n>` | Add upgrade tokens |
| `/trigger_event` | `<event_id>` | Trigger a named event |
| `/export` | — | Print encounter state summary |
| `/run_info` | — | Print current run state |
| `/skip_tide` | — | Auto-resolve current tide |
| `/end_encounter` | `[result]` | Force-end (clean/weathered/breach) |

---

## Known Balance Status

### Encounter Baseline Matrix (500 seeds each, `root_tall` heuristic, no B2 overrides)

| Encounter | Root Clean% | Root Breach% | Ember Clean% | Ember Breach% |
|-----------|-------------|--------------|--------------|---------------|
| `pale_march_standard` | 93% | 1.4% | 54% | 0% |
| `pale_march_scouts` | 89.6% | 1.2% | 73.6% | 0% |
| `pale_march_siege` | 80.8% | 0% | 0% | 0% |
| `pale_march_elite` | 58% | 1.2% | 5.6% | 0% |
| `pale_march_frontier` | 10.2% | 8.4% | 0% | 5.4% |

### B2 (+1 invader/wave) — Shipped

| Encounter | Root B2 Clean% | Root B2 Breach% | Ember B2 Clean% | Ember B2 Breach% |
|-----------|----------------|-----------------|-----------------|------------------|
| `pale_march_standard` | **56%** ✅ | **9.4%** ✅ | 19% | 0% |
| `pale_march_scouts` | **62%** ✅ | **4.2%** ✅ | **51.4%** ✅ | 0% |
| `pale_march_siege` | **61.6%** ✅ | 0.4% | 0% | 0% |
| `pale_march_elite` | 33% | 3.8% | 4.2% | 0% |
| `pale_march_frontier` | NOT TESTED | — | NOT TESTED | — |

B2 applied to: standard, scouts, siege, elite. **Not applied to frontier** (already 8.4% breach for Root; wide board is its own difficulty).

### B6 — Root Passive Redesign (shipped)

Presence Provocation moved to base passive, Assimilation moved to pool (unlockable). Assimilation now spawns natives at tide start (not resolution). Three spawn formulas configurable via `assimilation_spawn_mode`:
- `linear` — spawn count = presence count
- `scaled` — spawn count = 1 + floor(presence/2) (default)
- `half` — spawn count = ceil(presence/2)

Network Fear and Network Slow use **cluster presence ≥ 3** formula: `clusterPresence = territory.PresenceCount + sum(neighbor.PresenceCount)`. Triggers when total ≥ 3.

### Warden Character Identity (emergent, no tuning)

- **Root = anti-tank**: Comfort on siege (80.8% clean); danger on frontier (10.2% clean, 8.4% breach).
- **Ember = anti-swarm**: Comfort on scouts (73.6% clean); challenge on siege (0% clean). Ember NEVER takes weave damage — "weathered" means invaders reached I1, not heart HP loss.

### 3-Encounter Chain Arc (standard-B2 → scouts-B2 → elite-B2)

**Root worst-case arc (accumulated damage, p25 percentile):**
E1: 56% clean / 9.4% breach → E2: 61.8% clean / 5.4% breach → E3: 32.6% clean / 6.2% breach

**Root typical arc (p50/p75):** Identical to fresh standalone results at every step — p50 exits each encounter at weave=20.

**Ember:** Completely flat across all percentiles — Ember never takes weave damage, chain produces no escalation.

### Carryover System Notes

- **Dread/fear carryover: zero effect** on outcomes (confirmed across all chain tests).
- **Weave carryover: small but real** (+1.2pp breach in E2, +2.4pp in E3 vs fresh baseline for worst-case arc).
- **No corruption carryover** on any standard/scouts/elite run — corruption is cleansed before encounter ends.
- **No card removal** in any tested encounter — boss bottom mechanic not triggered at current difficulty.

### Balance Targets (tiered breach %)

| Tier | Target Breach % | Measured Against |
|------|-----------------|------------------|
| Easy encounters | ~5% | `smart` bot |
| Hard / elite encounters | 15–20% | `smart` bot |
| No upgrades / unimproved deck | up to 30% | `smart` bot |

Targets are iterative — will be refined with playtesting.

### Shipped Balance Changes

- **B1 (shipped):** `ember_001` + `ember_008` top damage 3 → 2 in `data/wardens/ember.json`.
- **B2 (shipped):** `extra_invaders_per_wave: 1` applied in EncounterLoader for standard, scouts, siege, elite.
- **B4/B5 (shipped):** Chain arc balancing complete.
- **B6 (shipped):** Root passive redesign — Provocation base, Assimilation pool, tide-start spawn.

**Remaining gap:** Sim bot plays optimally. 0% breach for Ember is an artifact of perfect play. Validate real-player breach rates via playtesting. Ember run arc flatness needs design decision — see CLAUDE-balance.md §B3.
