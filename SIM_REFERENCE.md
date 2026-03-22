# Hollow Wardens — Simulation Reference
> Single source of truth for the sim harness, balance knobs, and CLI.
> Updated by the architecture conversation. Read by the balance conversation.
> Last updated: 2026-03-22

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
| `network_fear_cap` | 4 | Max passive fear generated per tide by Root |

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

### Natives
| Key | Default | Description |
|-----|---------|-------------|
| `default_native_hp` | 2 | Native HP |
| `default_native_damage` | 3 | Native counter-attack damage |

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
- Base passives: Network Fear, Dormancy, Assimilation
- Unlockable: Rest Growth (Root T1), Provocation (Root T2), Network Slow (Shadow T1)

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

## Known Balance Status

### Encounter Baseline Matrix (500 seeds each, no B2 overrides)

| Encounter | Root Clean% | Root Breach% | Ember Clean% | Ember Breach% |
|-----------|-------------|--------------|--------------|---------------|
| `pale_march_standard` | 93% | 1.4% | 54% | 0% |
| `pale_march_scouts` | 89.6% | 1.2% | 73.6% | 0% |
| `pale_march_siege` | 80.8% | 0% | 0% | 0% |
| `pale_march_elite` | 58% | 1.2% | 5.6% | 0% |
| `pale_march_frontier` | 10.2% | 8.4% | 0% | 5.4% |

### B2 (+1 invader/wave) — Confirmed config, not yet in EncounterLoader code

| Encounter | Root B2 Clean% | Root B2 Breach% | Ember B2 Clean% | Ember B2 Breach% |
|-----------|----------------|-----------------|-----------------|------------------|
| `pale_march_standard` | **56%** ✅ | **9.4%** ✅ | 19% | 0% |
| `pale_march_scouts` | **62%** ✅ | **4.2%** ✅ | **51.4%** ✅ | 0% |
| `pale_march_siege` | **61.6%** ✅ | 0.4% | 0% | 0% |
| `pale_march_elite` | 33% | 3.8% | 4.2% | 0% |
| `pale_march_frontier` | NOT TESTED | — | NOT TESTED | — |

Apply B2 to: standard, scouts, siege, elite. **Do not apply to frontier** (already 8.4% breach for Root; wide board is its own difficulty).

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

### Shipped Changes

- **B1 (shipped):** `ember_001` + `ember_008` top damage 3 → 2 in `data/wardens/ember.json`.
- **B2 (pending):** Apply `extra_invaders_per_wave: 1` in `EncounterLoader` for standard, scouts, siege, elite.

**Remaining gap:** Sim bot plays optimally. 0% breach for Ember is an artifact of perfect play. Validate real-player breach rates via playtesting. Ember run arc flatness needs design decision — see CLAUDE-balance.md §B3.
