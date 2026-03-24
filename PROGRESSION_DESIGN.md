# Hollow Wardens — Progression System Design Decisions

These are DESIGN DECISIONS for discussion, not implementation specs. Once
approved, they'll be added to CLAUDE-decisions.md and specced for Claude Code.

---

## D37: Post-Encounter Rewards — Warden-Neutral Tier System

**Problem:** Tying rewards to Clean/Weathered/Breach penalizes wardens that
naturally run Weathered (Ember on most encounters). Ember would always get
worse rewards than Root despite playing optimally.

**Decision:** Reward tiers are based on **performance relative to the warden's
expected range per encounter**, not on absolute Clean/Weathered/Breach.

| Tier | Criteria | Rewards |
|------|----------|---------|
| Tier 1 (Exceeded) | Outcome above expected | 3 draft choices + 1 upgrade token + 1 card removal |
| Tier 2 (Met) | Outcome within expected | 2 draft choices + 1 upgrade token |
| Tier 3 (Barely survived) | Outcome below expected | 2 draft choices + choose: 1 token OR heal 2 weave |

**Tier thresholds are warden × encounter specific, stored in EncounterConfig:**

```json
"reward_tiers": {
    "root": {
        "tier1": { "min_result": "clean" },
        "tier2": { "min_result": "weathered" },
        "tier3": { "min_result": "breach" }
    },
    "ember": {
        "tier1": { "min_result": "clean" },
        "tier2": { "min_result": "weathered", "max_weave_loss": 5 },
        "tier3": { "min_result": "breach" }
    }
}
```

For encounters where Ember is naturally Weathered (siege), the tier thresholds
shift: Tier 1 = Weathered with weave > 16, Tier 2 = Weathered with weave ≤ 16.

**Draft pool:** Cards offered scale with encounter progression:
- Encounter 1 reward: Dormant + Awakened cards
- Encounter 2 reward: Awakened cards
- Encounter 3 reward: Awakened + Ancient cards
- Encounter 4 reward: Ancient cards

**Card removal:** Tier 1 reward includes optional card removal (thin the deck).
This is powerful in a roguelike — faster cycling, less chaff. Only available
on the best outcome.

---

## D38: Card Upgrade System — Gloomhaven Pipe Model

**Problem:** Universal "+1 to any card" is impossible to balance. A "+1 damage
to all invaders" upgrade is game-breaking. Need designer control over what
each card can upgrade.

**Decision:** Each card has 0-2 pre-defined **upgrade slots** in the JSON. Each
slot specifies exactly what changes. Players spend 1 upgrade token per slot.

### Upgrade Slot Schema

```json
{
    "id": "ember_001",
    "name": "Flame Burst",
    "elements": ["Ash", "Ash"],
    "top": { "type": "DamageInvaders", "value": 2, "range": 1 },
    "bottom": { "type": "DamageInvaders", "value": 4, "range": 1 },
    "upgrades": [
        {
            "id": "ember_001_u1",
            "slot": "top",
            "field": "value",
            "from": 2,
            "to": 3,
            "cost": 1,
            "description_key": "UPGRADE_EMBER_001_U1"
        },
        {
            "id": "ember_001_u2",
            "slot": "elements",
            "action": "add",
            "element": "Shadow",
            "cost": 1,
            "description_key": "UPGRADE_EMBER_001_U2"
        }
    ]
}
```

### Upgrade Types

| Type | What it does | Example |
|------|-------------|---------|
| **value_bump** | +N to effect value | DI×2 → DI×3 |
| **range_bump** | +1 to effect range | Range 1 → Range 2 |
| **add_element** | Add 1 element to card | [Ash, Ash] → [Ash, Ash, Shadow] |
| **add_secondary** | Add secondary effect to top | Top: DI×2 → DI×2 + GF×1 |
| **upgrade_secondary** | Improve existing secondary | Secondary: GF×2 → GF×3 |

### Universal Upgrade

Every card has at least 1 slot: **add_element**. This is always useful
(feeds thresholds) but never game-breaking. Some cards additionally get a
value or range upgrade — but only where the designer has balanced it.

### Cards WITHOUT Value Upgrades

Cards that are already strong or have board-wide effects should NOT get
value bumps. Their only upgrade is add_element:
- Any "damage all invaders" effect (T3-like power level)
- ReduceCorruption on cards that also have damage secondary
- GenerateFear on already-high-value cards

### Tracking Upgrades at Runtime

`Card` model gains:
```csharp
public List<string> AppliedUpgradeIds { get; set; } = new();
public bool HasUpgrade(string upgradeId) => AppliedUpgradeIds.Contains(upgradeId);
```

When an upgrade is applied:
1. Modify the card's effect values in memory
2. Add the upgrade ID to `AppliedUpgradeIds`
3. Persist in `BoardCarryover` so upgrades carry across encounters

### SimProfile Support

```json
"warden_overrides": {
    "applied_upgrades": ["ember_001_u1", "root_025_u2"]
}
```

The sim can test "what if the player upgraded Flame Burst AND added Shadow?"
without running a full progression loop.

---

## D39: Passive Upgrade System — Slot Machine

**Problem:** Passive upgrades need to be meaningful but controlled. Can't let
players upgrade any passive in any way.

**Decision:** Each passive has exactly 1 pre-defined upgrade. When spending 2
upgrade tokens, the player is offered 3 choices:

- **Option A:** Upgrade Passive X (pre-defined upgrade)
- **Option B:** Upgrade Passive Y (different passive)
- **Option C:** Permanently unlock Passive Z (a currently gated passive)

Pick exactly one. Already-upgraded passives and already-unlocked passives
are excluded from the pool.

### Root Passive Upgrades

| Passive | Upgrade | Description |
|---------|---------|-------------|
| Network Fear | Cap 4 → 6 per tide | More passive fear generation |
| Dormancy | Dormant cards playable as tops at half value | Dormant isn't fully dead |
| Assimilation | Also removes 1 invader from territory itself (not just adjacent) | Stronger resolution |
| Rest Growth | Place 2 presence on rest (up from 1) | Faster recovery |
| Provocation | Natives deal +1 damage in provoked territories | Stronger native combat |
| Network Slow | Penalty increases to -2 movement (up from -1) | Harder lockdown |

### Ember Passive Upgrades

| Passive | Upgrade | Description |
|---------|---------|-------------|
| Ash Trail | Deals 2 damage (up from 1) to invaders in presence territories | Stronger passive damage |
| Flame Out | Permanently removed cards generate 1 Fear each | Incentivizes bottom play |
| Scorched Earth | Smart cleanse now fully cleanses L2 (not just halve) | Cleaner resolutions |
| Ember Fury | +2 per corrupted territory (up from +1) | Doubled corruption bonus |
| Heat Wave | 3 damage on rest (up from 2) | Stronger rest benefit |
| Controlled Burn | Triggers at 2+ L1 territories (down from 3) | Easier to activate |
| Phoenix Spark | 5 Fear per removal (up from 3) | Bigger fear spikes |

### Passive Unlock (Option C)

Spending 2 tokens to permanently unlock a gated passive means it starts
active in ALL future encounters without needing to hit the element threshold.
This is expensive but removes the "earn your engine" tax for one passive.

### Data Format

```json
"passives": [
    {
        "id": "network_fear",
        "name": "The Web Remembers",
        "upgrade": {
            "id": "network_fear_u1",
            "description_key": "UPGRADE_NETWORK_FEAR",
            "effect": { "network_fear_cap": 6 }
        }
    }
]
```

The `effect` field maps directly to BalanceConfig overrides or ability
parameter changes. When the upgrade is applied, mutate the relevant config.

---

## D40: Weave Decay — Max Weave Shrinks Based on Damage Taken

**Problem:** Wardens heal weave easily during encounters, so weave between
encounters is nearly meaningless. The chain arc simulation confirmed this —
most encounters end at 20/20.

**Decision:** After each encounter, max weave shrinks based on how much damage
the warden absorbed. The spirit is fading — each wound leaves a scar.

| Weave missing at end | Max weave loss |
|---------------------|---------------|
| 0 (ended at max) | 0 — no decay |
| 1-3 missing | -1 max |
| 4-7 missing | -2 max |
| 8+ missing | -3 max |

### Examples Over a 4-Encounter Run

| Run quality | Max weave progression |
|------------|----------------------|
| Perfect | 20 → 20 → 20 → 20 |
| Good | 20 → 19 → 18 → 16 |
| Rough | 20 → 18 → 15 → 12 |
| Death spiral | 20 → 17 → 13 → breach |

### Design Implications

**Healing weave mid-encounter protects your future.** "I'm at 17/20, should I
heal or deal damage?" becomes "if I end at 17 I lose 1 max. If I take 2 more
hits and end at 15, I lose 2 max. The heal is worth more."

**Rest stop healing changes meaning.** Rest stop "heal 3 weave" becomes
"restore 1 max weave" — precious in late game. Going from 14 max to 15 max
is the difference between surviving 2 heart hits and 3.

**Ember's flat weave arc becomes a feature.** Ember never takes weave damage
(confirmed by sim). So Ember's max weave never decays. This is Ember's
hidden advantage — it enters late encounters at full max. Root's advantage is
board cleanliness (Clean wins). Ember's advantage is weave preservation.
The asymmetry resolves the B3 carryover gap without any new mechanics.

### Implementation

```csharp
public static int CalculateMaxWeaveLoss(int maxWeave, int currentWeave)
{
    int missing = maxWeave - currentWeave;
    return missing switch
    {
        0 => 0,
        <= 3 => 1,
        <= 7 => 2,
        _ => 3
    };
}
```

Applied in `BoardCarryover.ExtractCarryover()`:
```csharp
carryover.MaxWeave = state.Balance.MaxWeave
    - CalculateMaxWeaveLoss(state.Balance.MaxWeave, state.Weave.CurrentWeave);
carryover.FinalWeave = Math.Min(carryover.MaxWeave, state.Weave.CurrentWeave);
```

---

## D41: Between-Encounter Events — 6 Types

### Event Type 1: Choice Dilemma
Two options with different tradeoffs. See both before choosing.

**Examples:**
- "The Whispering Grove": (A) Gain 1 Ancient card but 2 territories start at
  L1 corruption. (B) Cleanse all carryover corruption but dissolve 1 card.
- "The Burnt Offering": (A) Upgrade 1 passive for free but lose 3 max weave.
  (B) Gain 2 upgrade tokens but hand limit -1 for next encounter.
- "The Forgotten Shrine": (A) Start next encounter with primary element at 5.
  (B) Start with 2 presence on M-row but no M-row natives.

### Event Type 2: Elemental Sacrifice
Dissolve cards to meet an element threshold. Cards contribute elements ×2
(like bottoms). If you meet the threshold, gain a powerful reward. If not,
you just lost the cards.

**Examples:**
- "The Root Gate": Need 6 Root. Reward: Permanent Rest Growth unlock.
- "The Ash Crucible": Need 8 Ash. Reward: Ember Fury scales +2/territory.
- "The Shadow Veil": Need 5 Shadow. Reward: Preview all fear actions next encounter.

**Design tension:** Weak cards have few elements. Strong cards have many
but you need them. Dissolving permanently thins your deck — powerful in a
roguelike (faster cycling) but risky (fewer options).

### Event Type 3: Rest Stop
Choose ONE:
- Heal 3 weave (current, not max)
- Restore 1 max weave (precious late-game)
- Upgrade 1 card (spend 1 token)
- Remove 1 card permanently (deck thinning)
- Recover 1 dissolved card (returns a bottom from a previous encounter)

### Event Type 4: Merchant / Draft
Choose 1 of 4 cards. Rarity scales with progression:
- After E1: Dormant + Awakened pool
- After E2: Awakened pool
- After E3: Awakened + Ancient pool

Optional: Pay 1 weave to reroll (see 4 new options). Weave cost makes
rerolling a real decision — especially with max weave decaying.

### Event Type 5: Corruption Event (Forced)
Bad thing happens. You mitigate, you don't avoid.

**Examples:**
- "The Pale March Advances": 2 random territories start next encounter at L1.
  Spend 1 token to reduce to 1 territory.
- "The Land Forgets": Lose 1 random native next encounter. Spend 1 token to
  protect them.
- "Eclipse Approaching": Next encounter has Eclipse on Tides 4-5. No mitigation.

### Event Type 6: Warden-Specific
Unique to each warden's identity.

**Root:**
- "The Deep Network": Place 1 permanent presence that persists across all
  future encounters. Choose the territory.
- "Ancient Memory": Recover a permanently dissolved card AND upgrade it.

**Ember:**
- "Controlled Detonation": Start next encounter with all territories at L1
  (3 corruption). Ember Fury immediately online.
- "Phoenix Rebirth": Recover ALL permanently dissolved cards but lose 5 max
  weave. The fire rises from its ashes.

### Data Format

Events stored as JSON in `data/events/`:
```json
{
    "id": "whispering_grove",
    "type": "choice",
    "name_key": "EVENT_WHISPERING_GROVE",
    "description_key": "EVENT_WHISPERING_GROVE_DESC",
    "options": [
        {
            "label_key": "EVENT_WHISPERING_GROVE_A",
            "effects": [
                { "type": "add_card", "rarity": "ancient", "count": 1 },
                { "type": "add_corruption", "territories": ["random", "random"], "amount": 3 }
            ]
        },
        {
            "label_key": "EVENT_WHISPERING_GROVE_B",
            "effects": [
                { "type": "cleanse_carryover" },
                { "type": "dissolve_card", "count": 1, "choice": "player" }
            ]
        }
    ],
    "warden_filter": null
}
```

---

## D42: Run Map — Branching Path

**Decision:** Slay the Spire-style branching map. Player sees the full map
and chooses their path. Some paths are safer, some are riskier with better
rewards.

### Realm 1 Structure

```
[E1: Standard]
        │
    ┌───┼───┐
    ▼   ▼   ▼
   [E] [R] [E]     ← Event / Rest / Event
    │   │   │
    └───┼───┘
        ▼
[E2: Scouts or Siege]  ← varies by path
        │
    ┌───┼───┐
    ▼   ▼   ▼
   [M] [E] [C]     ← Merchant / Event / Corruption (forced)
    │   │   │
    └───┼───┘
        ▼
[E3: Elite]
        │
    ┌───┴───┐
    ▼       ▼
   [R]     [E]     ← Rest (safe) / Event (risky)
    │       │
    └───┬───┘
        ▼
[E4: Frontier]      ← capstone (optional — map could end at E3)
```

**Node types:**
- **E** = Random event (types 1, 2, 5, or 6)
- **R** = Rest stop (type 3)
- **M** = Merchant/draft (type 4)
- **C** = Corruption event (type 5, always forced)

**E2 encounter varies by path:** Left path leads to Scouts (Ember's comfort).
Right path leads to Siege (Root's comfort). Middle path is random. This
creates warden-specific route optimization.

### Map Node Data

```json
{
    "realm_id": "realm_1",
    "nodes": [
        { "id": "e1", "type": "encounter", "encounter_id": "pale_march_standard", "row": 0 },
        { "id": "n1", "type": "event", "row": 1, "column": 0 },
        { "id": "n2", "type": "rest", "row": 1, "column": 1 },
        { "id": "n3", "type": "event", "row": 1, "column": 2 },
        { "id": "e2a", "type": "encounter", "encounter_id": "pale_march_scouts", "row": 2 },
        { "id": "e2b", "type": "encounter", "encounter_id": "pale_march_siege", "row": 2 }
    ],
    "edges": [
        { "from": "e1", "to": ["n1", "n2", "n3"] },
        { "from": "n1", "to": ["e2a"] },
        { "from": "n2", "to": ["e2a", "e2b"] },
        { "from": "n3", "to": ["e2b"] }
    ]
}
```

### Frontier's Role

Frontier (wide board) is the optional capstone / post-game challenge. It's
not in the main run arc. Accessible if the player beats E3 (Elite) with a
Clean or Tier 1 result. This gives advanced players something to chase
without forcing casual players into an unwinnable encounter.

---

## Summary of New Systems

| System | Core Files | Depends On |
|--------|-----------|------------|
| Main menu | MainMenuController.cs, GameBridge | Warden/encounter/run selection |
| Reward tiers | RewardCalculator.cs, EncounterConfig | Sim data for thresholds |
| Card upgrades | Card.cs, WardenData upgrade slots, JSON schema | Upgrade token economy |
| Passive upgrades | PassiveGating.cs, WardenData passive upgrades | Upgrade token economy |
| Weave decay | BoardCarryover.cs, WeaveSystem.cs | Already built |
| Events | EventData.cs, EventRunner.cs, data/events/*.json | Map system |
| Map / Full Run | RealmMap.cs, RunState.cs, data/realms/*.json | Event + reward systems |
| Dev console | DevConsole.cs | All systems (it's a debug tool) |
| Sim chain mode | ChainSimulator.cs | Carryover + rewards + events |

---

## D43: Main Menu — Warden + Mode Selection

**Decision:** The main menu has three layers: Warden → Mode → Encounter.

### Flow

```
┌─────────────────────────────┐
│     HOLLOW WARDENS          │
│                             │
│  [The Root]   [The Ember]   │
└─────────────────────────────┘
          │ click
          ▼
┌─────────────────────────────┐
│  Choose Mode                │
│                             │
│  [Full Run]                 │  ← map with 3-4 encounters + events
│  [Single Encounter]         │  ← current behavior (pick encounter)
│  [Practice]                 │  ← any encounter, starting config
└─────────────────────────────┘
          │
          ▼ (if Single Encounter or Practice)
┌─────────────────────────────┐
│  Choose Encounter           │
│                             │
│  [Standard] [Scouts]        │
│  [Siege]    [Elite]         │
│  [Frontier]                 │
└─────────────────────────────┘
```

### Mode Descriptions

**Full Run:** The real game. Map with branching paths, 3-4 encounters
connected by events. Carryover between encounters. Weave decay. Card
drafting and upgrades between encounters. Ends with a score/summary.

**Single Encounter:** Current behavior. Pick any encounter, play it once.
No carryover, no rewards. Good for testing and learning encounter types.

**Practice:** Like Single Encounter but with configurable starting state.
Choose starting weave, corruption, elements, extra cards, unlocked passives.
Uses a simplified SimProfile-like UI. Good for testing specific scenarios.

### Implementation

**MODIFY `hollow_wardens/godot/views/WardenSelectController.cs`**

Replace the current 2-screen flow (warden → encounter) with a 3-screen flow
(warden → mode → encounter/map).

**Add to `GameBridge.cs`:**
```csharp
public static string SelectedMode { get; set; } = "single"; // "full_run", "single", "practice"
```

**Full Run mode** starts the map system (D42). **Single Encounter** and
**Practice** start `BuildEncounter()` directly.

**Practice mode** shows a config panel before starting:
- Starting weave slider (1-20)
- Starting corruption per territory (dropdowns)
- Starting elements (sliders per element)
- Extra cards to add (checkboxes from draft pool)
- Passives to force-unlock (checkboxes)
- This is essentially a visual SimProfile editor

---

## D44: Developer Console

**Decision:** Press backtick (`) to open an in-game command console. Text
input field at the bottom of the screen, output log above it. Commands
modify game state in real-time for testing.

### Command Format

```
/command arg1 arg2 ...
```

### Command List

**Encounter Control:**
| Command | Args | Effect |
|---------|------|--------|
| `/encounter` | `<id>` | Immediately start a specific encounter |
| `/restart` | — | Restart current encounter with same seed |
| `/seed` | `<n>` | Set seed for next encounter |
| `/skip_tide` | — | Skip to next tide (auto-resolve current) |
| `/end_encounter` | `[clean\|weathered\|breach]` | Force-end with result |
| `/set_tide` | `<n>` | Jump to specific tide number |

**State Manipulation:**
| Command | Args | Effect |
|---------|------|--------|
| `/set_weave` | `<n>` | Set current weave |
| `/set_max_weave` | `<n>` | Set max weave |
| `/set_corruption` | `<territory> <points>` | Set corruption on territory |
| `/add_presence` | `<territory> [count]` | Place presence |
| `/remove_presence` | `<territory> [count]` | Remove presence |
| `/set_element` | `<element> <count>` | Set element pool |
| `/set_dread` | `<level>` | Set dread level |
| `/add_fear` | `<amount>` | Generate fear |

**Card / Deck:**
| Command | Args | Effect |
|---------|------|--------|
| `/add_card` | `<card_id>` | Add card to hand |
| `/remove_card` | `<card_id>` | Remove card from deck |
| `/upgrade_card` | `<card_id> <upgrade_id>` | Apply specific upgrade |
| `/list_cards` | — | Print current deck contents |
| `/list_upgrades` | `<card_id>` | Print available upgrades for card |

**Invaders:**
| Command | Args | Effect |
|---------|------|--------|
| `/spawn` | `<type> <territory>` | Spawn invader |
| `/kill_all` | — | Kill all invaders on board |
| `/kill` | `<territory>` | Kill all invaders in territory |

**Passives:**
| Command | Args | Effect |
|---------|------|--------|
| `/unlock_passive` | `<id>` | Force-unlock passive |
| `/lock_passive` | `<id>` | Force-lock passive |
| `/upgrade_passive` | `<id>` | Apply passive upgrade |
| `/list_passives` | — | Print all passives + status |

**Events (Full Run mode):**
| Command | Args | Effect |
|---------|------|--------|
| `/trigger_event` | `<event_id>` | Trigger specific event |
| `/list_events` | — | Print all event IDs |
| `/give_tokens` | `<n>` | Add upgrade tokens |
| `/give_card` | `<card_id>` | Add card to run deck |

**Run State:**
| Command | Args | Effect |
|---------|------|--------|
| `/run_info` | — | Print full run state (weave, max weave, dread, cards, upgrades, tokens) |
| `/set_tokens` | `<n>` | Set upgrade token count |
| `/advance_map` | `<node_id>` | Jump to specific map node |

**Meta:**
| Command | Args | Effect |
|---------|------|--------|
| `/help` | — | Print all commands |
| `/help` | `<command>` | Print detailed help for one command |
| `/export` | — | Print current state as a SimProfile JSON |
| `/import` | `<json_string>` | Apply a SimProfile to current state |

### Implementation

**NEW FILE `hollow_wardens/godot/views/DevConsole.cs`**

- Inherits `CanvasLayer` (overlay on top of everything)
- Toggle with backtick key
- `LineEdit` input at bottom, `RichTextLabel` output scrollback above
- Parses commands, dispatches to `GameBridge` methods
- All commands print feedback to the output log
- Commands that modify state call `EmitSignal(SignalName.EncounterReady)`
  to refresh all view controllers

**NEW FILE `src/HollowWardens.Core/Debug/CommandParser.cs`**

Pure C# command parsing (no Godot dependency). Takes a command string,
returns a `CommandResult` with the action to take. This keeps the logic
testable.

```csharp
public record CommandResult(
    string Command,
    string[] Args,
    bool IsValid,
    string? ErrorMessage
);

public static class CommandParser
{
    public static CommandResult Parse(string input) { ... }
}
```

The Godot layer (`DevConsole.cs`) calls `CommandParser.Parse()` then
dispatches to `GameBridge` methods based on the command.

---

## D45: Sim Chain Mode — Full Run Simulation

**Decision:** The sim can simulate a full run (multiple encounters + random
rewards + random events) to test the complete progression loop.

### CLI

```bash
# Simulate 200 full runs
dotnet run --project src/HollowWardens.Sim/ -- --mode chain --seeds 1-200 --warden root

# Chain with specific realm map
dotnet run --project src/HollowWardens.Sim/ -- --mode chain --seeds 1-200 --warden ember --realm realm_1

# Chain with SimProfile overrides (applied to ALL encounters in chain)
dotnet run --project src/HollowWardens.Sim/ -- --mode chain --seeds 1-200 --warden root --profile sim-profiles/chain-test.json
```

Default `--mode` is `single` (current behavior). `chain` runs a full
multi-encounter sequence.

### Chain Simulation Flow

For each seed:

1. **Initialize run state:** full deck, 20 max weave, 0 dread, 0 tokens
2. **Encounter 1:** Standard(B2). Run with BotStrategy. Record result.
3. **Post-E1 reward:** Based on result tier, randomly select cards from
   draft pool. Bot auto-picks highest-value card (simple heuristic).
   Award upgrade tokens.
4. **Post-E1 event:** Randomly select 1 event from the pool. Bot makes
   the "safe" choice (Option B in dilemmas, skip in sacrifices, heal in
   rest stops).
5. **Apply carryover:** Extract from E1, apply weave decay, apply event
   effects.
6. **Encounter 2:** Scouts or Siege (random per seed). Run with carryover.
7. **Post-E2 reward + event:** Same flow.
8. **Encounter 3:** Elite. Run with accumulated carryover.
9. **Record full run result:** survived all 3? Final weave? Total tokens
   spent? Cards drafted?

### Bot Behavior in Chain Mode

The bot needs simple heuristics for between-encounter decisions:

**Card draft:** Pick the card with the highest priority score:
- DamageInvaders cards: priority 100
- PlacePresence cards: priority 80
- ReduceCorruption cards: priority 60
- GenerateFear cards: priority 40
- RestoreWeave cards: priority 20

**Card upgrade:** Upgrade the highest-value damage card first. Then
presence cards. Then cleanse.

**Passive upgrade:** Always pick the upgrade over the unlock (upgrades
improve existing power, unlocks add power that needs threshold investment).

**Event choices:**
- Dilemmas: Pick the option that doesn't cost weave or cards
- Elemental sacrifice: Skip (don't sacrifice cards)
- Rest stop: Heal weave if below 80% max, otherwise upgrade a card
- Merchant: Draft the highest-priority card
- Corruption event: Spend token to mitigate if available

These are deliberately **average-player** heuristics, not optimal play.
The chain sim represents a typical player, not a min-maxer.

### Chain Output

**summary.txt additions for chain mode:**
```
=== HOLLOW WARDENS CHAIN SIMULATION — 200 runs ===
Warden: root | Realm: realm_1 | Encounters: standard → scouts → elite

RUN OUTCOMES:
  Full clear (all 3 survived): 180 (90%)
  Failed at E1: 5 (2.5%)
  Failed at E2: 8 (4%)
  Failed at E3: 7 (3.5%)

PROGRESSION:
  Avg cards drafted: 2.4
  Avg cards upgraded: 1.1
  Avg passive upgrades: 0.3
  Avg tokens earned: 3.2
  Avg tokens spent: 2.8

WEAVE ARC:
  Avg max weave after E1: 19.6
  Avg max weave after E2: 18.8
  Avg max weave after E3: 17.2
  Avg final weave: 14.8

ENCOUNTER RESULTS:
  E1: 56% clean / 34.6% weathered / 9.4% breach
  E2: 58% clean / 36% weathered / 6% breach
  E3: 35% clean / 58% weathered / 7% breach
```

**chain-runs.csv:** One row per run with columns: seed, e1_result,
e1_final_weave, e1_reward_tier, e2_encounter, e2_result, e2_final_weave,
e3_result, e3_final_weave, max_weave_at_end, cards_drafted, cards_upgraded,
tokens_earned, tokens_spent, events_encountered.

### SimProfile for Chain Mode

```json
{
    "name": "chain test — extra hard",
    "mode": "chain",
    "seeds": "1-200",
    "warden": "ember",
    "realm": "realm_1",
    "balance_overrides": {
        "element_decay_per_turn": 2
    },
    "chain_overrides": {
        "starting_max_weave": 18,
        "starting_tokens": 2,
        "force_e2_encounter": "pale_march_siege",
        "disable_events": false,
        "bot_draft_strategy": "damage_priority"
    }
}
```

`chain_overrides` lets you test specific chain scenarios: "what if the
player starts with 2 tokens and faces siege as E2?"

---

## Implementation Priority (Revised)

This is a large system. Build in layers, each independently useful:

### Layer 1: Core Progression (no UI needed, sim-testable)
1. **Weave decay** in `ExtractCarryover` — 1 method change
2. **Card upgrade schema** — add `upgrades` array to warden JSONs
3. **Reward tiers** — add tier config to EncounterConfig + RewardCalculator
4. **Passive upgrade data** — add upgrade definitions to warden JSONs
5. **RunState.cs** — tracks tokens, upgrades, drafted cards across encounters
6. **Sim chain mode** — `--mode chain` with bot heuristics

After Layer 1: run chain sims to validate the progression math works.

### Layer 2: Events (data-driven, sim-testable)
7. **Event data format** — JSON schema + EventData.cs
8. **EventRunner.cs** — resolves event effects on RunState
9. **3-4 events per type** — enough to test variety
10. **Chain sim with events** — bot makes safe choices

After Layer 2: run chain sims with events to see if they create meaningful
run variance.

### Layer 3: Godot Integration
11. **Main menu** (D43) — warden → mode → encounter flow
12. **Reward screen** — post-encounter card draft + upgrade UI
13. **Event screen** — choice presentation + resolution
14. **Map screen** — branching path display + navigation
15. **Upgrade UI** — card upgrade selection + passive slot machine
16. **Practice mode** — config panel before encounter

### Layer 4: Dev Console
17. **DevConsole.cs** — command input + output
18. **CommandParser.cs** — pure C# parsing (testable)
19. **Wire all commands** to GameBridge
20. **Export/import** — dump state as SimProfile JSON

### Layer 5: Polish
21. Localization for all new strings
22. Encounter-specific reward tier thresholds (from sim data)
23. Event pool balancing (run chain sims to find broken events)
24. Map layout variations for replay value
