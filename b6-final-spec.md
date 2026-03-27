# B6: Root Redesign — Final Spec

## Read First
- `CLAUDE-decisions.md` §D42, §D43 — problem statement
- `CLAUDE-balance.md` §B6 — sim evidence
- `SIM_REFERENCE.md` — balance knobs, CLI reference
- `src/HollowWardens.Core/Wardens/RootAbility.cs` — current implementation
- `data/wardens/root.json` — passive + card data
- `master.md` §8.1 — Root warden overview

---

## Context

D42 changed Root's Assimilation from adjacent-territory to same-territory (≥2 presence + ≥2 natives + invaders → convert). This collapsed clean rates to 0% across all encounters (breach 28–54%). Root has no tools to build a native army, so the condition never fires.

Sim testing confirmed: Root's identity is wide-presence network control. Tall-bot play (stacking presence ≥3) WORSENS outcomes because it cripples Network Fear/Slow, which need ≥3 presence-territory neighbors to fire. The wide network IS the primary defense.

The redesign has three parts:
1. **Passive restructure** — Provocation becomes base (with targeting), Assimilation becomes pool
2. **Card revisions** — 3 cards gain native/control interaction (out of 10 total)
3. **New balance knobs** — configurable for A/B testing

---

## Part 1: Passive Restructure

### Base Passives (always active, 3 total)

| Id | Name | Type | Effect |
|----|------|------|--------|
| network_fear | The Web Remembers | Base | Generate 1 Fear per invader in a territory whose ≥3 neighbors have Presence (max 3 Fear/tide) |
| dormancy | Nothing Truly Dies | Base | Bottoms go Dormant (inert in deck) instead of permanently removed |
| provocation | Protector's Call | **Base (MOVED FROM POOL)** | See Provocation detail below |

### Pool Passives (player picks 2 of 3 per run)

| Id | Name | Type | Effect |
|----|------|------|--------|
| assimilation | The Land Reclaims | **Pool (MOVED FROM BASE)** | At Tide start, spawn natives in ONE Presence territory (player picks). Spawn count based on formula. Upgrade: also converts invaders at Resolution. |
| rest_growth | Deep Breath | Pool | Place 1 free Presence on any territory with existing Presence when you Rest. Upgrade: place 2. |
| network_slow | Tangled Earth | Pool | Invaders in a territory whose ≥3 neighbors have Presence get −1 Advance movement. Upgrade: −2. |

### The Pool Choice (what it means for the player)
- **Assimilation + rest_growth** = native-army builder. Spawn natives passively + grow presence for more spawning. The "growing forest" path.
- **Assimilation + network_slow** = defensive native play. Slow invaders + replenish natives each tide. Control path.
- **rest_growth + network_slow** = pure network, NO passive native spawning. Rely on starting natives + card-spawned natives only. Aggressive presence-pattern path.

---

## Part 2: Provocation — Targeted Activation

### The Mechanic

At the start of each tide, the player selects which presence territories have Provocation active this tide. Natives in selected territories counter-attack on every invader action (Activate). Natives in non-selected territories do NOT counter-attack (they're dormant — the forest is quiet there).

**Rate limit:** In each activated territory, only `provocation_natives_per_presence` natives can be aroused per invader action. With the default of 1, a territory with 1 presence and 4 natives still only gets 1 native counter-attacking per action. A territory with 2 presence gets 2 natives counter-attacking.

**Territory limit:** The player can activate up to `provocation_territory_limit` territories per tide. This is the core tactical decision — which territories are your kill zones?

### Balance Knobs

| Key | Default | Description |
|-----|---------|-------------|
| `provocation_territory_limit` | 0 | Max territories with active Provocation per tide. 0 = unlimited (all presence territories). |
| `provocation_natives_per_presence` | 1 | Max natives that counter-attack per invader action, per presence token in territory |

**Testing plan:**
- Start with `provocation_territory_limit: 0` (all territories, simplest version) to establish baseline
- Then test with `provocation_territory_limit: 2` and `provocation_territory_limit: 3` to see if limiting creates better tension
- `provocation_natives_per_presence: 1` throughout — this prevents stacked-presence territories from becoming invulnerable kill zones

### Sim Bot Heuristic

When `provocation_territory_limit` requires a choice, the bot should select territories with:
1. The most invaders present (maximize counter-attacks this tide)
2. Tiebreak: territories closest to the Heart (protect the high-value zone)
3. Tiebreak: territories with the most natives (maximize damage output)

### Why Targeting Matters

Without targeting, Provocation on all presence territories means every M-row and I-row territory is an automatic kill zone. Invaders walk in, natives counter-attack, invaders die. No player decision needed. With targeting:
- 6 territories have presence but you can only activate 2 → "Do I protect M1 (Ironclads approaching) or A2 (new wave arriving)?"
- A territory you DON'T activate lets invaders advance through — you sacrificed that position to defend elsewhere
- If the invader wave splits across A1, A2, A3 and you can only activate 2, one lane gets through. That's tension.

---

## Part 3: Assimilation — Pool Passive with Presence-Scaled Spawning

### The Mechanic

At Tide start (before Vigil), if the player picked Assimilation as a pool passive and it's unlocked:
1. Player picks ONE territory where Root has presence
2. Spawn natives there based on a formula tied to presence count

### Spawn Formulas (configurable for A/B testing)

| Formula key | Name | Presence=1 | Presence=2 | Presence=3 |
|-------------|------|------------|------------|------------|
| `linear` | Full | 1 | 2 | 3 |
| `scaled` | Scaled | 1 | 2 | 2 |
| `half` | Half | 1 | 1 | 2 |

### Balance Knobs

| Key | Default | Description |
|-----|---------|-------------|
| `assimilation_spawn_mode` | `scaled` | Formula: `linear` (=presence), `scaled` (=1+floor(presence/2)), `half` (=ceil(presence/2)) |

### Wide vs Tall Reward

This formula creates the wide-vs-tall choice:
- **Wide Root** (1 presence in 5 territories): picks a different territory each tide, spawns 1 native. Slow but distributed — feeds Provocation across the board.
- **Tall Root** (3 presence in 2 territories): picks the stacked territory, spawns 2-3 natives. Concentrated — builds toward the conversion upgrade.
- **Decision every tide:** "Do I reinforce M1 (under siege) or seed natives at A3 (building for the future)?"

### Upgrade: Conversion at Resolution

When `assimilation_u1` is unlocked:

At Resolution, for each territory with ≥2 presence AND ≥2 natives AND invaders:
- Convert `floor(min(presence, natives) / 2)` invaders → natives
- Target weakest invaders first
- Converted native HP = max(1, invader.MaxHp / 2)

This is the existing D42 conversion logic, gated behind the upgrade. The base spawn mechanic builds the native army the upgrade needs.

### Sim Bot Heuristic

Choose territory with the most invaders adjacent (anticipating where natives will be needed next tide). Tiebreak: most presence (maximizes spawn count).

---

## Part 4: Card Revisions (3 of 10 cards changed)

Root's identity is presence patterns and corruption management. Natives are a secondary tool, not the main event. Only 3 cards are revised — the other 7 stay exactly as they are.

### New Effect Types to Implement

| Effect | Parameters | Description |
|--------|------------|-------------|
| `MoveNatives` | count, range | Move up to {count} natives from one territory to an adjacent territory within {range} steps. Player picks source territory, natives move to one adjacent territory. |
| `SpawnNatives` | count | Spawn {count} natives in a target territory that has Root presence. |
| `PushInvaders` | count, range | Push up to {count} invaders in a territory one step toward their spawn row. Not movement — instant repositioning. Does not interact with Network Slow. |

### Changed Cards

**005 — The Forest Remembers**
| | Before | After |
|---|--------|-------|
| Elements | Root, Shadow | Root, Shadow |
| Top | GenerateFear(2) | **MoveNatives(2, range 1)** |
| Bottom | DamageInvaders(4) | DamageInvaders(4) — unchanged |

*Tradeoff:* Player loses a fear-generation top and gains native positioning. Moving 2 natives from I1 toward A-row costs the turn's fear income. The bottom stays as Root's primary damage tool.

**006 — Spreading Growth**
| | Before | After |
|---|--------|-------|
| Elements | Root, Root | Root, Root |
| Top | PlacePresence(1) range 1 | PlacePresence(1) range 1 — unchanged |
| Bottom | PlacePresence(2) | **SpawnNatives(2) in a Presence territory** |

*Tradeoff:* Old bottom (PlacePresence 2) was rarely worth going dormant for since the top already places 1. New bottom spawns 2 natives as burst reinforcement at the cost of the card going dormant. Decision: "Are 2 natives worth a dead draw until I rest?"

**025 — Grasping Roots**
| | Before | After |
|---|--------|-------|
| Elements | Root, Root | Root, Root |
| Top | DamageInvaders(2) range 1 | DamageInvaders(2) range 1 — unchanged |
| Bottom | DamageInvaders(3) | **PushInvaders(2, range 1) + ReduceCorruption(1) in that territory** |

*Tradeoff:* Root had two damage bottoms (005 at 4, 025 at 3) which was redundant. New bottom pushes 2 invaders back toward spawn and cleanses 1 corruption. Fits Root's control identity — "I don't kill you, I make you go away and clean up after."

### Unchanged Cards (7 total)

| # | Card | Top | Bottom | Role |
|---|------|-----|--------|------|
| 001 | Tendrils of Reclamation | ReduceCorruption(2) | ReduceCorruption(5) | Core cleanse |
| 002 | Deep Roots | PlacePresence(1) | PlacePresence(1) anywhere | Core presence |
| 003 | Earthen Mending | ReduceCorruption(2) | ReduceCorruption(4) | Heavy cleanse |
| 004 | Shiver of the Ancient | GenerateFear(3) | GenerateFear(7) | Fear engine |
| 007 | Healing Earth | RestoreWeave(1) | RestoreWeave(3) | Weave recovery |
| 008 | Stir the Sleeping | AwakeDormant(1) | AwakeDormant(0) | Dormancy management |
| 009 | Living Wall | PlacePresence(1) | DamageInvaders(5) | Emergency damage |

### Deck Composition Summary
- **Presence:** 002, 006 top, 009 top (3 cards)
- **Cleanse:** 001, 003 (2 cards — corruption management stays independent of natives)
- **Fear:** 004 (1 dedicated fear card; 005 top traded for MoveNatives)
- **Damage:** 005 bottom (4), 009 bottom (5), 025 top (2) (3 damage options)
- **Weave:** 007 (1 card)
- **Dormancy:** 008 (1 card)
- **Native interaction:** 005 top (move), 006 bottom (spawn), 025 bottom (push invaders + cleanse)

That's 3 out of 10 cards with native/control interaction. Root stays a presence-pattern warden with natives as tactical seasoning.

---

## Part 5: Implementation Plan

### Step 1: Passive restructure
1. In `root.json`: set `provocation` to `"pool": false` (base), set `assimilation` to `"pool": true` (pool)
2. Add to BalanceConfig: `provocation_territory_limit` (int, default 0 = unlimited), `provocation_natives_per_presence` (int, default 1), `assimilation_spawn_mode` (string, default "scaled")
3. Update `RootAbility.cs`:
   - Provocation: at tide start, if `provocation_territory_limit > 0`, bot selects N territories; natives in those territories counter-attack with the per-presence rate limit
   - Assimilation: at tide start, if active (pool + unlocked), bot picks 1 presence territory, spawns natives per formula
4. Update `PassiveGating` defaults to reflect new base/pool assignment
5. Add all new knobs to `SIM_REFERENCE.md`

### Step 2: New card effects
1. Implement `MoveNativesEffect` — source territory selection, count limit, range check, move natives
2. Implement `SpawnNativesEffect` — target territory must have Root presence, spawn N natives at default HP
3. Implement `PushInvadersEffect` — target territory selection, push N invaders one step toward spawn row, then ReduceCorruption in same territory
4. Wire all three into `EffectResolver` / effect dispatch
5. Write xUnit tests for each effect

### Step 3: Update card data
1. Update `root.json` cards 005, 006, 025 with new effect types and values
2. Keep elements unchanged on all three cards

### Step 4: Update sim bot
1. Add scoring for `MoveNatives`: score = invader_count_in_adjacent_territories × native_count_in_source
2. Add scoring for `SpawnNatives`: score = presence_in_territory × (territory_has_invaders_adjacent ? 2 : 1)
3. Add scoring for `PushInvaders`: score = invaders_in_territory × proximity_to_heart (M-row invaders score higher than A-row)
4. Provocation territory selection: pick territories with most invaders, tiebreak by proximity to Heart

### Step 5: Sim runs — Phase A (baseline with all-territories Provocation)

Run with `provocation_territory_limit: 0` (unlimited) to establish baseline.
Run with Assimilation as pool pick, `assimilation_spawn_mode: "scaled"`.

**Profiles (500 seeds each, Root warden, B2/B5 marcher counts):**

```json
{
  "name": "b6-baseline-scaled",
  "seeds": "1-500",
  "warden": "root",
  "warden_overrides": {
    "force_passives": ["assimilation"]
  },
  "balance_overrides": {
    "assimilation_spawn_mode": "scaled",
    "provocation_territory_limit": 0
  }
}
```

Run on all 4 encounters: standard, scouts, siege, elite. (4 runs)

Also run WITHOUT Assimilation (rest_growth + network_slow) to test the non-native path:

```json
{
  "name": "b6-no-assimilation",
  "seeds": "1-500",
  "warden": "root",
  "warden_overrides": {
    "force_passives": ["rest_growth", "network_slow"],
    "lock_passives": ["assimilation"]
  },
  "balance_overrides": {
    "provocation_territory_limit": 0
  }
}
```

Run on standard + scouts. (2 runs, 6 total for Phase A)

### Step 6: Sim runs — Phase B (Provocation territory limiting)

If Phase A shows Provocation is too dominant (breach < 5%, or invaders never survive M-row), test limits:

```json
{
  "name": "b6-provocation-limit-2",
  "seeds": "1-500",
  "warden": "root",
  "warden_overrides": {
    "force_passives": ["assimilation"]
  },
  "balance_overrides": {
    "assimilation_spawn_mode": "scaled",
    "provocation_territory_limit": 2
  }
}
```

Run limit=2 and limit=3 on standard. (2 runs)

### Step 7: Sim runs — Phase C (spawn formula comparison)

If Phase A/B find the right Provocation setting, compare spawn formulas:

Run `assimilation_spawn_mode`: "linear", "scaled", "half" on standard. (3 runs)

### Step 8: Analysis

Compare all results against B5 targets:

| Encounter | B5 Target Clean% | B5 Target Breach% |
|-----------|-------------------|-------------------|
| standard | 52% | 9.6% |
| scouts | 50.6% | 3.2% |
| siege | 43.6% | 5.2% |
| elite | ~30% | ~5% |

**What to check in verbose logs:**
- Is Provocation firing? How many counter-attacks per tide?
- Are natives accumulating (Assimilation) or depleting faster than they spawn?
- Does the bot use MoveNatives / SpawnNatives / PushInvaders? How often vs other cards?
- What's the Clean win path? (Conversion upgrade? Raw damage? Or still 0% Clean?)
- Does the bot choose different Provocation territories each tide, or always the same ones?

---

## Part 6: Decision Tree After Sims

**If Phase A (unlimited Provocation + Assimilation) hits targets:** Ship it. Provocation territory targeting becomes a future difficulty lever, not a launch requirement.

**If Phase A is too easy (breach < 3%):** Provocation is too strong with no limit. Move to Phase B and test limits. Limit=2 is the expected sweet spot — player protects 2 of 5-6 presence territories, rest are exposed.

**If Phase A is still too hard (breach > 20%):** Card damage and native spawning aren't enough. Consider: increase spawn count (try "linear" formula), or increase native HP (`default_native_hp: 3`), or reduce B2 marcher count on the worst encounter.

**If no-Assimilation run (rest_growth + network_slow) has acceptable breach (< 15%):** Great — Assimilation is a meaningful choice, not a requirement. Both paths are viable.

**If no-Assimilation run breaches > 25%:** Assimilation is mandatory, not a choice. Consider buffing card-level native tools (increase SpawnNatives count on 006 bottom from 2 to 3) to make the non-Assimilation path viable.

---

## What NOT to Change
- Network Fear / Network Slow mechanics — unchanged, they reward wide play
- Ember balance — B1/B4 shipped and working
- Encounter configs — board state stays warden-neutral
- Dormancy mechanic — flagged as undercosted but out of scope for B6
- Other Root cards (001, 002, 003, 004, 007, 008, 009) — unchanged
- Draft pool cards — future phase, not B6

## Future Design Space (Post-B6)
- NativeDamage card (natives deal damage — draft pool, Awakened rarity, for Assimilation builds)
- ProtectNatives card (native immunity for a tide — draft pool, for heavy native strategies)
- Native-scaled cleanse card (cleanse corruption = native count in territory — draft pool)
- Dormancy cost rework (making bottoms more costly for Root to prevent spam)
- Provocation territory targeting as a UI feature (player selects on a map before each tide)
