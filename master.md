# Hollow Wardens — Master Document
> Game Design + Technical Architecture  
> Version 0.4 — Working reference for Claude Code sessions

---

## Table of Contents
1. [Game Overview](#1-game-overview)
2. [Core Design](#2-core-design)
3. [Encounter System](#3-encounter-system)
4. [Invader System](#4-invader-system)
5. [Card System](#5-card-system)
6. [Warden Roster](#6-warden-roster)
7. [Run Structure](#7-run-structure)
8. [Balance Reference](#8-balance-reference)
9. [Godot Architecture](#9-godot-architecture)
10. [Class Definitions](#10-class-definitions)
11. [V1 Scope & Build Order](#11-v1-scope--build-order)
12. [Open Design Questions](#12-open-design-questions)

---

## 1. Game Overview

**One-liner:** A card roguelike where you play as a dying ancient spirit defending the last sacred territories of a corrupted world.

**Fantasy:** You are not a hero. You are something ancient trying not to be forgotten. The world is being unmade. You fight not to win — but to endure long enough to matter.

**Inspirations:**
- **Spirit Island** — Presence system (location = power), Fear as a resource, asymmetric spirit identities
- **Gloomhaven** — Hand exhaustion as stamina, card burning for power spikes, rest as a costly decision
- **Slay the Spire** — Roguelike run structure, card drafting, meta-progression

**What's original:**
- Split turn (Vigil / Tide / Dusk): you act before *and* after invaders
- Dissolution: burn cards for territorial reach, with escalating permanence based on encounter type
- Encounter survival model: timed sieges, not kill-all puzzles
- Breach system: losing slowly rather than dying suddenly

---

## 2. Core Design

### 2.1 Turn Structure

Each turn has three phases, always in this order:

```
VIGIL → THE TIDE → DUSK
```

| Phase | Who acts | Cards played | Nature |
|---|---|---|---|
| Vigil | Player | 2 top cards | Proactive — setup, positioning, building Fear |
| The Tide | Invaders | — | Invaders spawn, advance, ravage |
| Dusk | Player | 1 bottom card | Reactive — respond with full information |

**Why 2 tops + 1 bottom:** Bottoms are stronger because they're played with information. The 2:1 ratio creates tension — you always want to play your powerful bottom effect, but you're spending two cards setting it up. The bottom justifies its existence by being worth at least two tops in the right situation.

**Dusk timing is the core strategic layer.** Knowing what the Tide did before playing your bottom card creates a completely different decision space from Gloomhaven, where all cards are committed simultaneously.

### 2.2 Eclipse Events — Flipping the Ratio

Certain encounters or Realm events invert the structure to **2 bottoms + 1 top**. The Tide resolves first. Your Vigil action becomes the disadvantaged one.

This is not a stat modifier — it changes how the entire turn *feels*. Under Eclipse, you're in reactive mode from the start, with limited ability to set up. Reserved for late-zone encounters and special event types.

**Eclipse sources:**
- Corrupted Zones (territory type)
- The Long Night (Realm event, lasts multiple turns)
- Herald-class Invaders (flip as a Ravage side-effect)

### 2.3 Resting

At any point during your turn, instead of playing cards, you may **Rest**:
- Recover full discard pile to hand
- The Tide still runs — invaders advance uncontested
- You play 0 cards this turn

Rest is always costly. The game is balanced so that an uncontested Tide is genuinely dangerous.

---

## 3. Encounter System

### 3.1 What an Encounter Is

An encounter is a contained invader wave — equivalent to a room in Slay the Spire. Each encounter has:
- A defined invader group (type, count, pattern)
- A fixed number of **Tide steps** (the wave duration)
- An entry pattern (spawn locations, movement targets)
- A readable spawn preview: you can see what's coming *one Tide step* before it arrives

### 3.2 How Encounters End

**Encounters use the survival model, not the kill-all model.**

- The encounter ends when all Tide steps are exhausted
- After the final Tide step, you enter **Resolution turns**
- During Resolution, invaders stop spawning and advancing — they only hold their positions
- You spend Resolution turns dealing with whatever remains: destroy them, push them back, or absorb their presence
- The number of Resolution turns you needed, and how far invaders penetrated, determines your reward tier

**Resolution turn limit:** TBD during balancing, but likely 2–3 turns maximum. If invaders remain after Resolution, the encounter is a Breach.

### 3.3 Resolution — Warden Flavor

Different Wardens resolve encounters differently, even against identical invader states. This is where identity expresses itself:

| Warden | Resolution style |
|---|---|
| The Root | Assimilates — presence tokens absorb adjacent invaders, converting them to neutral terrain |
| The Ember | Destroys — Resolution turns are pure damage output, burning what remains |
| The Veil | Repels — pushes remaining invaders back toward spawn points, reduces Corruption |

Resolution flavor is mechanical, not cosmetic — assimilation vs. destruction vs. repulsion each interact differently with Corruption tracks and Weave recovery.

### 3.4 Reward Tiers

Rewards scale based on encounter performance:

| Result | Condition | Reward |
|---|---|---|
| **Clean** | No invaders in Resolution | Full reward: card draft + Aspect choice + Weave restore |
| **Weathered** | 1–2 invaders cleared in Resolution | Partial: card draft OR Aspect (not both) |
| **Breach** | Invaders remain after Resolution | Minimal: Weave restore only + Breach effect carries forward |

**Breach effects (carry into next encounter):**
- One territory enters next encounter pre-Tainted
- One invader unit carries over (doesn't reset)
- Next encounter's Escalate clock starts one step ahead

Breach is losing slowly, not dying. The run continues but gets harder.

### 3.5 Encounter Tiers

| Tier | Dissolution cost | Notes |
|---|---|---|
| **Standard** | Card removed until next encounter | Base cost, recovers between encounters |
| **Elite** | Card *may* be permanently removed | Unknown at time of Dissolution — revealed at reward screen |
| **Boss** | All dissolved cards permanently removed | Known upfront. Dissolution is a conscious sacrifice |

This creates a clear risk gradient. Against standards you Dissolve freely. Against Elites you hesitate. Against Bosses you choose deliberately.

---

## 4. Invader System

### 4.1 The Weave — Run Health

The Weave is the spiritual fabric of the Realm. It's the run-level health bar.

- **Starting value:** 20
- **Run ends:** when Weave hits 0

**Weave drain events:**
- Invader Ravages undefended territory: −1
- Sacred Site falls: −3
- Desecrated territory (Corruption 3): −1 per turn passively
- Certain invader traits drain Weave passively

**Weave restoration:**
- Fear threshold events can restore Weave (see Fear system)
- Between-encounter rewards restore 2–4 Weave

### 4.2 Corruption — Territory Health

Each territory has a Corruption track: 0 → 1 → 2 → 3.

| State | Value | Effect |
|---|---|---|
| Clean | 0 | Full resource generation. Normal Presence rules. |
| Tainted | 1 | Reduced resource output. Invaders +1 move speed in this territory. |
| Defiled | 2 | No resources. Placing Presence here costs 1 additional card. |
| Desecrated | 3 | Presence tokens here are removed. −1 Weave per turn passively. |

**Advancing Corruption:** Invaders in an undefended territory Ravage it — Corruption +1.  
**Reducing Corruption:** Purification card effects. Expensive, powerful, high priority.  
**Desecrated is not instant death** — it's a compounding liability. Let too many territories hit 3 and you lose the attrition war.

### 4.3 The Tide — Step Sequence

Every Tide phase follows this exact sequence:

1. **Spawn** — New invaders arrive at entry points. Pattern was visible previous turn.
2. **Advance** — Invaders move toward Presence tokens and Sacred Sites per faction rules.
3. **Ravage** — Invaders in undefended territories: Corruption +1, Weave damage if applicable. Defended territories are contested but not Ravaged.
4. **Escalate** — Every 3 turns, the faction escalates: adds unit type or behavior modifier. This is the encounter timer independent of The Weave.

### 4.4 Fear System

Fear is generated by card effects and is the primary counter-resource to Weave drain.

Fear is a run-level counter. It accumulates across turns. Hitting thresholds triggers effects and then resets to 0.

| Threshold | Effect |
|---|---|
| 5 | Invaders hesitate — skip Advance step this Tide |
| 12 | Restore 2 Weave. Rout one Invader group. |
| 20 | Dread event — major faction-specific effect (unique per faction) |

Fear generation is not optional — it's how you fight back against the Weave clock. Cards that generate Fear need to feel weighty, not incidental.

### 4.5 Invader Factions (V1 + Planned)

**V1 — The Pale March** (Methodical)
- High HP, slow movers
- Extremely predictable spawn patterns — telegraph everything
- Hard to stop once in place
- Passive Weave drain scales with their presence count
- Dread event: The Long Silence — all territories with Pale March present lose 1 Corruption resistance (Defiled acts as Desecrated) for 3 turns

**Planned — The Scorch** (Volatile)
- Fast movers, low HP
- Chain-Ravage: after Ravaging a territory, immediately move into the next and Ravage again
- Can devastate 3 territories in a single Tide if unchecked
- Dread event: Conflagration — all Tainted territories immediately become Defiled

**Planned — The Hollow** (Adaptive)
- Grow stronger near Desecrated territories
- Near-ignorable early, terrifying late
- Reward you for losing ground — punish accumulated Corruption
- Dread event: Unraveling — all Desecrated territories expand Corruption to adjacent territories

---

## 5. Card System

### 5.1 Card Anatomy

Every card has three layers:

```
┌──────────────────────────────┐
│  [TOP — VIGIL ACTION]        │
│  Played during Vigil phase   │
│  Proactive, setup-oriented   │
├──────────────────────────────┤
│  [BOTTOM — DUSK ACTION]      │
│  Played during Dusk phase    │
│  Reactive, stronger, info-   │
│  gated (played after Tide)   │
├──────────────────────────────┤
│  ◈ DISSOLVE EFFECT           │
│  Sacrifice this card for     │
│  a burst of Presence reach   │
│  Cost: card removed per      │
│  encounter tier rules        │
└──────────────────────────────┘
```

### 5.2 Vigil Actions (Top)
Design space: setup, positioning, resource generation, defensive preparation

Examples:
- Place 1 Presence in range. Generate 2 Fear.
- Move 1 Presence token up to 2 territories.
- Reduce Corruption by 1 in one adjacent territory.
- Generate 4 Fear.
- Predict: look at next Tide's spawn pattern.

### 5.3 Dusk Actions (Bottom)
Design space: reactive power, threshold effects, counter-attacks, consequence amplification

The best bottoms react to what the Tide *just did*. They should feel like they were written for the situation you're now in.

Examples:
- Destroy all Invaders in one territory with 3+ Fear tokens on it.
- If 2+ territories were Ravaged this Tide: restore 1 Corruption level in each.
- Push all Invaders in range back 1 territory (toward spawn).
- If a Sacred Site was threatened this Tide: generate 6 Fear.
- Deal damage to all Invaders that Ravaged this turn.

### 5.4 Dissolution

**Default Dissolution (all Wardens):**  
Sacrifice this card. Place 1 Presence on any territory in range, bypassing normal distance limits. Card is removed until next encounter (or permanently, per encounter tier).

**Why this is different from Gloomhaven burns:**  
- GH: burn a card *for its specific printed burn effect*  
- HW: burn any card *for territorial reach*  
- The power is spatial, not textual  
- The cost is endurance, not just one card's future use

**Warden-specific Dissolution modifications** — see Warden Roster section.

### 5.5 Deckbuilding Layer

Between encounters (at reward screens), you draft Power cards:
- Choose 1 from 3 options
- Cards are permanent additions for the rest of the run
- Dissolution does not affect your deck composition — it only costs you the card until the next encounter (or permanently on Elites/Bosses)
- These are entirely separate systems and do not compete

Between Realms, you may also access Aspect upgrades (passive identity modifiers) and have a wider card selection.

---

## 6. Warden Roster

### 6.1 The Root (V1 — Starter)
**Archetype:** Tank / Control  
**Playstyle:** Spreads Presence slowly, passively generates resources. Difficult to exhaust. Wins by blanketing the board over time.

**Presence mechanic:** Root Presence generates passive Fear each turn equal to the number of adjacent Presence tokens (network effect). Spreading wide is as valuable as spreading fast.

**Starting deck:** Heavy on Purification (Corruption reduction), moderate Fear generation, few direct damage effects. Survives by denying Corruption, not by killing.

**Dissolution — Dormancy:**  
Instead of removing the card until next encounter, it enters a Dormant state — still in deck, still drawn, but inert. Cannot be played until Awakened by a specific card effect. Dormant cards can still be Dissolved again (paying nothing, since they're already spent).  
*Design intent: The Root's power accumulates. Even its sacrifice state is a resource to manage.*

**Resolution style:** Assimilation — Presence tokens absorb adjacent invaders at end of encounter. Absorbed invaders reduce Corruption in that territory by 1.

### 6.2 The Ember (Planned)
**Archetype:** Burst / Aggressive  
**Playstyle:** High damage, volatile. Burns through cards faster than any other Warden. Every turn is a risk calculation.

**Presence mechanic:** Ember Presence in a territory generates bonus damage when cards affect that territory, but Ember Presence burns out after 3 turns and must be re-placed.

**Dissolution — Fear Pulse:**  
When dissolving, generates a Fear pulse equal to the card's cost before it's removed. The sacrifice fuels a spike of dread.  
*Design intent: The Ember turns its desperation into power. High-cost cards become high-value sacrifices.*

**Resolution style:** Destruction — straight damage output. Resolution turns are uncapped damage turns.

### 6.3 The Veil (Planned)
**Archetype:** Control / Disruption  
**Playstyle:** Manipulates Invader movement and timing. Can delay The Tide partially. High skill ceiling, low margin for error.

**Presence mechanic:** The Veil can place Presence tokens face-down (concealed). Invaders cannot target concealed Presence for their Advance logic. Revealing a concealed Presence is a Vigil action.

**Dissolution — Disruption:**  
Instead of placing Presence, you may delay one Invader group's Advance action this Tide. You choose: territorial reach OR tactical disruption.  
*Design intent: The Veil's burn is always a choice between two meaningful power types.*

**Resolution style:** Repulsion — pushes remaining invaders back toward spawn points. Reduces Corruption in vacated territories.

---

## 7. Run Structure

### 7.1 Overview

```
Realm 1               Realm 2               Realm 3
[Enc][Enc][Enc][Boss] [Enc][Enc][Enc][Boss] [Enc][Enc][Enc][Final Boss]
          ↑                       ↑
     Realm reward            Realm reward
   (full card draft,        (full card draft,
    Aspect, Weave restore)   Aspect, Weave restore)
```

Each Realm contains 3 standard/elite encounters + 1 boss.  
Between encounters: small rewards (see Encounter Reward Tiers).  
Between Realms: full rewards (card draft, Aspect upgrade, significant Weave restore).

### 7.2 Zone Difficulty Progression

| Zone | Factions | Starting state | Special |
|---|---|---|---|
| Realm 1 | 1 faction (Pale March) | All territories Clean | Tutorial pacing, no Eclipse |
| Realm 2 | 2 factions | Some territories pre-Tainted | Eclipse events possible |
| Realm 3 | 2–3 factions | Some territories pre-Defiled | The Hollow appears if Corruption is high |

### 7.3 Between-Encounter Rewards

| Performance | Rewards |
|---|---|
| Clean | Card draft (1 of 3) + Aspect upgrade (1 of 2) + Weave +3 |
| Weathered | Card draft OR Aspect upgrade (player chooses) + Weave +1 |
| Breach | Weave +1 only + Breach carries forward |

### 7.4 Between-Realm Rewards

Always granted regardless of performance (Realm boss must be cleared):
- Card draft: 1 of 4 options (wider pool)
- Aspect upgrade: 1 of 3 options
- Weave restore: 4–6 (scales with Fear generated in Realm)
- Remove Corruption from 1 territory
- Heal 1 territory from Tainted → Clean

### 7.5 Meta Progression

Unlocks happen *between runs*, not during:
- New **Wardens** (unlock by completing runs with current Warden)
- New **Realm types** (map shapes, faction mixes, special rules)
- New **Aspects** (appear in future runs' upgrade pools)
- New **card variants** (alternate cards for existing Wardens, unlocked by specific achievements)

The card pool per Warden is fixed. Variety comes from drafting, not unlocking.

---

## 8. Balance Reference

*Working targets — all subject to playtesting*

### 8.1 Hand Size

| Warden | Starting hand | Max hand |
|---|---|---|
| The Root | 6 | 10 |
| The Ember | 5 | 8 |
| The Veil | 7 | 9 |

### 8.2 Turn Economy

- Standard turn: 2 tops + 1 bottom = 3 cards spent
- Rest turn: 0 cards played, full discard recovered (Tide still resolves — rest is costly)
- Dissolution: 1 additional card removed until next encounter
- A hand of 7 with no rests: 2 full turns before forced rest (7 → 4 → 1, can't play a third turn)
- A hand of 10 (max, post-reward): 3 full turns between rests (10 → 7 → 4 → 1)

**Rest rate target:** ~1 rest per 2–3 play turns. With starting hand 7: 33% of turns are rests. With hand 10: 25%. Card rewards directly reduce rest pressure — a meaningful, legible upgrade.

**Cycle math (starting deck 30 cards, hand 7):**
- Pattern: P, P, Rest, P, P, Rest... (every 3rd turn is rest)
- Each cycle through the hand = 6 cards played + 1 rest turn
- 30-card deck → never fully cycled in a single encounter (see §8.3)
- Dormancy (Root): dormant cards in hand reduce effective play cards without consuming a full rest slot — creates passive stamina pressure

### 8.3 Encounter Length

Target turn counts (Tide steps = total turns including rests):

| Tier | Tide steps | Est. play turns | Est. rest turns | Cards played |
|------|-----------|-----------------|-----------------|--------------|
| Standard | 5–6 | 3–4 | 1–2 | 9–12 |
| Elite | ~10 | 6–7 | 3–4 | 18–21 |
| Boss | 12–13 | 8–9 | 4–5 | 24–27 |

Boss fights nearly exhaust the starting 30-card deck. With reward cards (hand up to 10), the boss cycle extends: more turns between rests, more cards played total — the late-game deck feels meaningfully larger.

Resolution turns: up to 3 (Standard), up to 2 (Elite), 1 (Boss — bosses are designed to be survived, not cleaned up).

### 8.4 Weave Economy

- Starting Weave: 20
- Target Weave at end of Realm 1: 14–17 (some damage taken)
- Target Weave at end of Realm 2: 8–14 (meaningful pressure)
- Target Weave at start of Realm 3: 6–12 (survival mode)
- Average Weave drain per standard encounter (no breach): 2–4
- Average Weave drain per breach: 5–8

### 8.5 Fear Economy

- Target Fear generation per encounter: 6–10 (hitting threshold 1 most encounters)
- Threshold 2 (12 Fear): should be achievable once per Realm on good runs
- Threshold 3 (20 Fear): boss fights or exceptional Fear-focused builds

---

## 9. Godot Architecture

### 9.1 Engine Version
Godot **4.6.1** — **.NET version** (C#). All architecture targets Godot 4 C# patterns.
Use `.cs` files for all scripts. Do **not** mix GDScript and C# in the same project.
Recommended IDE: **JetBrains Rider** or **VS Code** with the C# Dev Kit extension.
Executable: `D:\Downloads\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe`

### 9.2 Project Structure

```
hollow_wardens/
├── project.godot
├── scenes/
│   ├── game/
│   │   ├── Game.tscn              # Root game scene
│   │   ├── Encounter.tscn         # Single encounter scene
│   │   └── RealmMap.tscn          # Between-encounter map/navigation
│   ├── entities/
│   │   ├── Card.tscn
│   │   ├── InvaderUnit.tscn
│   │   ├── Territory.tscn
│   │   └── PresenceToken.tscn
│   ├── ui/
│   │   ├── Hand.tscn              # Player hand display
│   │   ├── TideQueue.tscn         # Preview of next Tide step
│   │   ├── WeaveBar.tscn
│   │   ├── FearCounter.tscn
│   │   ├── EncounterResult.tscn   # Post-encounter reward screen
│   │   └── RealmReward.tscn      # Between-Realm reward screen
│   └── menus/
│       ├── MainMenu.tscn
│       └── RunStart.tscn          # Warden select
├── scripts/
│   ├── core/
│   │   ├── GameState.cs           # Singleton — run state
│   │   ├── EncounterManager.cs    # Encounter loop controller
│   │   ├── TurnManager.cs         # Phase sequencing
│   │   ├── TideExecutor.cs        # Invader AI execution
│   │   └── RewardManager.cs       # Post-encounter rewards
│   ├── data/
│   │   ├── CardData.cs            # Resource class for card definitions
│   │   ├── InvaderData.cs         # Resource class for invader types
│   │   ├── TerritoryData.cs       # Resource class for territory state
│   │   ├── EncounterData.cs       # Resource class for encounter definitions
│   │   └── WardenData.cs          # Resource class for warden definitions
│   ├── entities/
│   │   ├── Card.cs
│   │   ├── Deck.cs
│   │   ├── Hand.cs
│   │   ├── InvaderUnit.cs
│   │   ├── Territory.cs
│   │   └── PresenceToken.cs
│   ├── wardens/
│   │   ├── Warden.cs              # Base class
│   │   ├── WardenRoot.cs
│   │   ├── WardenEmber.cs         # Planned
│   │   └── WardenVeil.cs          # Planned
│   └── ui/
│       ├── HandUI.cs
│       ├── WeaveBarUI.cs
│       ├── FearCounterUI.cs
│       └── TideQueueUI.cs
├── resources/
│   ├── cards/
│   │   ├── root/                  # Root's 30 card definitions (.tres)
│   │   └── shared/                # Shared/drafted cards
│   ├── encounters/
│   │   ├── realm1/
│   │   ├── realm2/
│   │   └── realm3/
│   ├── invaders/
│   │   ├── pale_march.tres
│   │   ├── scorch.tres
│   │   └── hollow.tres
│   └── wardens/
│       └── root.tres
└── assets/
    ├── art/
    │   └── kenney/                # Kenney placeholder art (see §9.5)
    ├── audio/
    ├── fonts/
    │   ├── Cinzel-Regular.ttf     # Headings, card names, phase indicators
    │   ├── Cinzel-Bold.ttf
    │   ├── static/                # Cinzel weight variants
    │   ├── IMFellEnglish-Regular.ttf  # Body text, card descriptions
    │   └── IMFellEnglish-Italic.ttf
    └── hollow_wardens_theme.tres  # Project-wide Godot Theme (set in Project Settings)
```

### 9.3 Signal Architecture

Key signals that connect systems (use Godot signals, not direct calls between managers).  
In Godot 4 C#, signals are declared as delegate types with `[Signal]`:

```csharp
// TurnManager.cs
[Signal] public delegate void PhaseChangedEventHandler(TurnPhase phase);
[Signal] public delegate void TurnStartedEventHandler(int turnNumber);
[Signal] public delegate void TurnEndedEventHandler(int turnNumber);

// EncounterManager.cs
[Signal] public delegate void EncounterStartedEventHandler(EncounterData encounterData);
[Signal] public delegate void EncounterEndedEventHandler(EncounterResult result);
[Signal] public delegate void TideStepCompletedEventHandler(int stepNumber);
[Signal] public delegate void ResolutionPhaseStartedEventHandler(int turnsRemaining);
[Signal] public delegate void BreachOccurredEventHandler(int severity);

// GameState.cs
[Signal] public delegate void WeaveChangedEventHandler(int newValue, int delta);
[Signal] public delegate void FearChangedEventHandler(int newValue, int delta);
[Signal] public delegate void FearThresholdReachedEventHandler(int threshold);

// Card signals (on CardManager or EventBus)
[Signal] public delegate void CardPlayedEventHandler(CardData card, TurnManager.TurnPhase phase);
[Signal] public delegate void CardDissolvedEventHandler(CardData card, EncounterData.EncounterTier tier);
[Signal] public delegate void CardPermanentlyRemovedEventHandler(CardData card);

// Territory signals
[Signal] public delegate void CorruptionChangedEventHandler(Territory territory, int newLevel);
[Signal] public delegate void TerritoryDesecratedEventHandler(Territory territory);
[Signal] public delegate void PresencePlacedEventHandler(Territory territory, Warden warden);

// Invader signals
[Signal] public delegate void InvaderSpawnedEventHandler(InvaderUnit unit, Territory territory);
[Signal] public delegate void InvaderAdvancedEventHandler(InvaderUnit unit, Territory from, Territory to);
[Signal] public delegate void InvaderRavagedEventHandler(InvaderUnit unit, Territory territory);
[Signal] public delegate void InvaderDefeatedEventHandler(InvaderUnit unit);
```

Emitting and connecting signals in C#:
```csharp
// Emit
EmitSignal(SignalName.WeaveChanged, newValue, delta);

// Connect (lambda)
gameState.WeaveChanged += (newValue, delta) => UpdateWeaveBar(newValue);

// Connect (method)
gameState.WeaveChanged += OnWeaveChanged;
```

### 9.4 Autoloads (Singletons)

Registered in **Project > Project Settings > Autoload**. In C#, access them via:

```csharp
// Option A — direct node path (verbose but explicit)
var gameState = GetNode<GameState>("/root/GameState");

// Option B — static instance pattern (cleaner, set up in _Ready)
// In GameState.cs:
public static GameState Instance { get; private set; }
public override void _Ready() => Instance = this;

// Then anywhere:
GameState.Instance.Weave -= 2;
```

**Autoload list:**
- `GameState` — persists across scenes, holds full run state (Weave, Fear, deck, territories)
- `EventBus` — global signal relay for signals that don't belong to one specific node

### 9.5 Typography & Art Assets

**Fonts** (already imported in `assets/fonts/`):

| Font | Weight | Use |
|---|---|---|
| Cinzel | Bold | Card names, headings, phase indicator |
| Cinzel | Regular / SemiBold | Secondary labels, UI chrome |
| IM Fell English | Regular | Card effect descriptions (body text) |
| IM Fell English | Italic | Dissolve text, flavour labels |

Cinzel is unreadable below ~14px — never use it for dense body text. IM Fell English is the pairing for anything small.

**Project Theme:** `assets/hollow_wardens_theme.tres` is registered as the custom theme in Project Settings. Set fonts and styles here once — do not override per-node except in exceptional cases.

**Placeholder Art (Kenney — kenney.nl, free):**

| Pack | Use |
|---|---|
| Card Kit | Card frame `PanelContainer` background (9-patch StyleBoxTexture) |
| Hexagon Kit / Board Game Pack | Territory tile placeholders |
| UI Pack (RPG or Space variant) | Buttons, bars, panel chrome |
| Game Icons | Corruption pips, Fear counter icon, Weave bar icon |

Files go in `assets/art/kenney/`. Import as-is; Godot auto-imports PNGs.

**9-patch setup for card frames:**
Select the Kenney card frame PNG → Inspector → Import tab → set **Repeat** to Disabled. Then in the node using it as a `StyleBoxTexture`, set the four margin values to match the border width of the PNG (e.g. 16px border → all margins = 16). This prevents corners from stretching.

**Card.tscn node structure** (Phase 6 reference):

```
PanelContainer          ← Kenney card frame as StyleBoxTexture
└── VBoxContainer
    ├── Label           card name      — Cinzel-Bold, 16px
    ├── TextureRect     card art       — placeholder ColorRect until art exists
    ├── HSeparator
    ├── Label           vigil text     — Cinzel-Regular, 12px (or IM Fell English)
    ├── HSeparator
    ├── Label           dusk text      — same
    └── Label           dissolve text  — IM Fell English Italic, tertiary colour
```

This scene is the first thing to build in Phase 6 — everything else (Hand display, reward screen) follows the same pattern.

### 9.6 Card Data Authoring

Card definitions live in two layers: **design data** (editable) and **Godot resources** (generated).

```
data/
├── cards-root.tres          ← Root warden card definitions (source of truth)
├── cards-ember.json         ← Ember (when created)
└── cards-veil.json          ← Veil (when created)

hollow_wardens/resources/cards/
├── root/   root_001.tres … root_030.tres   ← auto-generated, do not edit
├── ember/  (generated when ember JSON exists)
└── veil/   (generated when veil JSON exists)

cards-catalog.html           ← browsable card viewer (auto-updated)
tools/generate-cards.py      ← migration script
```

**Rule: never edit `.tres` files by hand. Always edit `data/cards-{warden}.json` then run the script.**

#### Card JSON schema

```json
{
  "warden": "root",
  "version": "1.0",
  "cards": [
    {
      "num":     "001",
      "id":      "root_001",
      "name":    "Card Name",
      "cost":    0,
      "vigil":   { "type": "ReduceCorruption", "value": 1, "range": 1, "desc": "..." },
      "dusk":    { "type": "GenerateFear",     "value": 2, "range": 0, "desc": "..." },
      "dissolve": null,
      "design_note": "Balance notes and open questions."
    }
  ]
}
```

- `"dissolve": null` → uses the engine default (PlacePresence 1, range bypasses check)
- Custom dissolve overrides with any `EffectType` and specific value/range
- `design_note` is display-only — shown in the catalog viewer, not written to .tres

#### Running the migration

```bash
python tools/generate-cards.py           # regenerate ALL wardens
python tools/generate-cards.py root      # regenerate root only
python tools/generate-cards.py root ember  # explicit list
```

This rewrites all `.tres` files and updates the embedded JSON in `cards-catalog.html`.
Open `cards-catalog.html` in any browser to review cards (works offline, no server needed).

#### Adding a new Warden's cards

1. Create `data/cards-{warden}.json` (copy root as template, change `"warden"` field)
2. Run `python tools/generate-cards.py {warden}`
3. New `.tres` files appear in `hollow_wardens/resources/cards/{warden}/`
4. Catalog HTML gains a warden filter entry for the new warden

#### Valid `EffectType` strings

| String | Enum | Notes |
|---|---|---|
| `PlacePresence` | 0 | Requires target territory, range applies |
| `MovePresence` | 1 | Requires target territory, range applies |
| `GenerateFear` | 2 | No target needed |
| `ReduceCorruption` | 3 | Requires target territory, range applies |
| `Purify` | 4 | Requires target territory |
| `DamageInvaders` | 5 | Requires target territory, range applies |
| `PushInvaders` | 6 | Requires target territory |
| `RoutInvaders` | 7 | Requires target territory — **stubbed, Phase 6** |
| `RestoreWeave` | 8 | No target needed |
| `PredictTide` | 9 | No target needed — **stubbed** |
| `Conditional` | 10 | Threshold/if-then — needs EffectCondition |
| `Custom` | 11 | Warden-specific, resolved in Warden subclass |
| `AwakeDormant` | 12 | Root only. Value=0 means all dormant cards. |

---

## 10. Class Definitions

### CardData (Resource)
```csharp
// CardData.cs — define card definitions as .tres resources in Godot editor
using Godot;

[GlobalClass]
public partial class CardData : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string CardName { get; set; }
    [Export] public string WardenId { get; set; }    // "root", "ember", "veil", "" (shared)
    [Export] public CardEffect VigilEffect { get; set; }    // Top action
    [Export] public CardEffect DuskEffect { get; set; }     // Bottom action
    [Export] public CardEffect DissolveEffect { get; set; } // Default: place presence bypassing range
    [Export] public int Cost { get; set; }                  // Used for Ember's Fear pulse on dissolve
    [Export] public bool IsDormant { get; set; } = false;   // Root-specific state
    [Export] public Texture2D ArtTexture { get; set; }
}
```

### CardEffect (Resource)
```csharp
// CardEffect.cs
using Godot;

[GlobalClass]
public partial class CardEffect : Resource
{
    public enum EffectType
    {
        PlacePresence, MovePresence,
        GenerateFear,
        ReduceCorruption, Purify,
        DamageInvaders, PushInvaders, RoutInvaders,
        RestoreWeave,
        PredictTide,
        Conditional,    // Threshold/if-then effects — needs EffectCondition
        Custom          // Warden-specific, resolved in Warden subclass
    }

    [Export] public EffectType Type { get; set; }
    [Export] public int Value { get; set; }
    [Export] public int Range { get; set; }             // Territory steps from nearest Presence
    [Export] public EffectCondition Condition { get; set; } // Optional, for Dusk threshold effects
    [Export] public string Description { get; set; }
}
```

### EncounterData (Resource)
```csharp
// EncounterData.cs
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EncounterData : Resource
{
    public enum EncounterTier { Standard, Elite, Boss }

    [Export] public string Id { get; set; }
    [Export] public EncounterTier Tier { get; set; }
    [Export] public InvaderData Faction { get; set; }
    [Export] public int TideSteps { get; set; }         // Total Tide steps before Resolution
    [Export] public int ResolutionTurns { get; set; }   // Max Resolution turns allowed
    [Export] public Array<SpawnEvent> SpawnPattern { get; set; }
    [Export] public Array<EscalateEvent> EscalationSchedule { get; set; }
    [Export] public bool IsEclipse { get; set; } = false;
    [Export] public Dictionary StartingCorruption { get; set; } // territoryId → corruptionLevel
}
```

### TerritoryState
```csharp
// TerritoryState.cs — plain C# class, not a Godot Resource (lives in GameState)
using System.Collections.Generic;

public class TerritoryState
{
    public string Id { get; set; }
    public int Corruption { get; set; } = 0;       // 0–3
    public int PresenceCount { get; set; } = 0;
    public List<InvaderUnit> InvaderUnits { get; set; } = new();
    public bool IsSacredSite { get; set; } = false;
    public bool IsEntryPoint { get; set; } = false;

    public bool IsDefended => PresenceCount > 0;

    public void Ravage()
    {
        Corruption = Math.Min(Corruption + 1, 3);
        // GameState.Instance will emit CorruptionChanged signal
    }
}
```

### Warden (Base Class)
```csharp
// Warden.cs
using Godot;
using System.Collections.Generic;

public partial class Warden : Node
{
    [Export] public WardenData WardenData { get; set; }

    public Hand Hand { get; protected set; }
    public Deck Deck { get; protected set; }
    public List<CardData> Discard { get; } = new();
    public List<CardData> DissolvedThisEncounter { get; } = new();
    public List<CardData> PermanentlyRemoved { get; } = new();

    // Override in subclasses for Warden-specific Dissolution behavior
    public virtual void OnDissolve(CardData card) { }

    // Called at start of Resolution phase — override for Warden-specific resolution style
    public virtual void OnResolutionStart(List<TerritoryState> territories) { }

    // Override to apply passive presence bonuses (e.g. Root's network Fear)
    public virtual Dictionary<string, int> GetPresenceBonus(TerritoryState territory)
        => new();
}
```

### GameState (Singleton / Autoload)
```csharp
// GameState.cs
using Godot;
using System.Collections.Generic;

public partial class GameState : Node
{
    public static GameState Instance { get; private set; }

    // Run-level state
    public int Weave { get; private set; } = 20;
    public int Fear { get; private set; } = 0;
    public int RunTurn { get; private set; } = 0;
    public int CurrentRealm { get; private set; } = 1;
    public Warden CurrentWarden { get; set; }
    public Dictionary<string, TerritoryState> Territories { get; } = new();
    public List<InvaderUnit> ActiveInvaders { get; } = new();

    // Encounter-level state
    public EncounterData.EncounterTier EncounterTier { get; set; }
    public int TideStep { get; set; } = 0;
    public int BreachCount { get; set; } = 0;

    // Signals
    [Signal] public delegate void WeaveChangedEventHandler(int newValue, int delta);
    [Signal] public delegate void FearChangedEventHandler(int newValue, int delta);
    [Signal] public delegate void FearThresholdReachedEventHandler(int threshold);

    public override void _Ready() => Instance = this;

    public void ModifyWeave(int delta)
    {
        Weave = Math.Max(0, Weave + delta);
        EmitSignal(SignalName.WeaveChanged, Weave, delta);
    }

    public void ModifyFear(int delta)
    {
        Fear += delta;
        EmitSignal(SignalName.FearChanged, Fear, delta);
        CheckFearThresholds();
    }

    private void CheckFearThresholds()
    {
        foreach (int threshold in new[] { 5, 12, 20 })
        {
            if (Fear >= threshold)
            {
                EmitSignal(SignalName.FearThresholdReached, threshold);
                Fear -= threshold; // Reset after threshold hit
                break;
            }
        }
    }

    public TerritoryState GetTerritory(string id) => Territories[id];
    public bool IsRunOver() => Weave <= 0;
}
```

### TurnManager
```csharp
// TurnManager.cs
using Godot;

public partial class TurnManager : Node
{
    public enum TurnPhase { Vigil, Tide, Dusk, Resolution }

    [Signal] public delegate void PhaseChangedEventHandler(TurnPhase phase);
    [Signal] public delegate void TurnStartedEventHandler(int turnNumber);
    [Signal] public delegate void TurnEndedEventHandler(int turnNumber);

    public TurnPhase CurrentPhase { get; private set; }
    public int CardsPlayedVigil { get; private set; } = 0;
    public int CardsPlayedDusk { get; private set; } = 0;
    public bool IsEclipse { get; set; } = false;

    public int VigilLimit => IsEclipse ? 1 : 2;  // Eclipse flips the ratio
    public int DuskLimit => IsEclipse ? 2 : 1;

    public void StartTurn() { /* ... */ }
    public void EndVigil() { /* transition to Tide */ }
    public void EndTide() { /* transition to Dusk */ }
    public void EndDusk() { /* transition to next Vigil or Resolution */ }

    public bool CanPlayCard(TurnPhase phase) =>
        phase == TurnPhase.Vigil
            ? CardsPlayedVigil < VigilLimit
            : CardsPlayedDusk < DuskLimit;

    public void PlayerRest()
    {
        // Skip turn — recover discard, Tide still runs
        GameState.Instance.CurrentWarden.RecoverDiscard();
    }
}
```

### TideExecutor
```csharp
// TideExecutor.cs
using Godot;
using System.Threading.Tasks;

public partial class TideExecutor : Node
{
    public EncounterData EncounterData { get; set; }

    [Signal] public delegate void TideStepCompletedEventHandler(int step);

    public async Task ExecuteTideStep(int step)
    {
        await SpawnPhase(step);
        await AdvancePhase();
        await RavagePhase();
        await EscalatePhase(step);
        EmitSignal(SignalName.TideStepCompleted, step);
    }

    private async Task SpawnPhase(int step) { /* spawn per pattern */ await Task.CompletedTask; }
    private async Task AdvancePhase() { /* pathfind toward Presence/Sacred Sites */ await Task.CompletedTask; }
    private async Task RavagePhase() { /* check defended, apply Corruption + Weave damage */ await Task.CompletedTask; }
    private async Task EscalatePhase(int step) { /* check escalation schedule */ await Task.CompletedTask; }
}
```

---

## 11. V1 Scope & Build Order

### V1 Target
One Warden (The Root), 30 cards, 3-encounter Realm (2 standard + 1 boss), The Pale March only, no meta-progression. Core loop must feel good before expanding.

### Build Order

**Phase 1 — Data Layer** ✅ Complete (41 tests)
- [x] CardData, CardEffect, EffectCondition resources
- [x] EncounterData, SpawnEvent, EscalateEvent resources
- [x] TerritoryState model
- [x] InvaderData for Pale March
- [x] WardenData for The Root
- [x] GameState singleton (weave, fear, territories)
- [x] EventBus autoload
- [x] Deck, Hand entity classes
- [x] Headless test runner (`scenes/tests/TestRunner.tscn`)

**Phase 2 — Turn Engine** ✅ Complete
- [x] TurnManager with phase sequencing
- [x] TideExecutor (spawn, advance, ravage)
- [x] Territory grid (hardcoded 3×3, 9 territories, TerritoryGraph)
- [x] Corruption system
- [x] Weave drain and Fear counter

**Phase 3 — Card Engine** ✅ Complete
- [x] CardEngine: TryPlayCard with phase limits
- [x] Effect resolution (PlacePresence, GenerateFear, ReduceCorruption, DamageInvaders, RestoreWeave)
- [x] Dissolution logic (route to DissolvedThisEncounter or PermanentlyRemoved per tier)
- [x] Rest mechanic (TurnManager.PlayerRest)

**Phase 4 — Encounter Loop** ✅ Complete
- [x] EncounterManager (start, run Tide steps, Resolution, end)
- [x] Resolution turn logic
- [x] Breach detection
- [x] Reward tier evaluation (Clean / Weathered / Breach)
- [x] Encounter-end state cleanup (dissolved cards per tier: Standard/Elite/Boss)

**Phase 5 — Root Warden** ✅ Complete (111/111 tests)
- [x] AwakeDormant effect type (CardEffect.EffectType = 12)
- [x] Dormancy Dissolution (OnDissolve override — first dissolve = dormant, second = permanent)
- [x] Network Fear (OnTideStart — adjacency passive)
- [x] Assimilation Resolution style (OnResolutionStart)
- [x] 30 starting cards as .tres resources (generated from data/cards-root.json)
- [x] Card data tooling: data/ folder + tools/generate-cards.py + cards-catalog.html

**Phase 6 — UI (Functional, Not Pretty)**
- [ ] `Card.tscn` — PanelContainer with VBox (see §9.5 for node structure + font spec)
- [ ] Hand display (cards visible, playable)
- [ ] Territory grid display (Corruption state, Presence, Invaders)
- [ ] Weave bar + Fear counter
- [ ] Tide preview (next step's spawn pattern)
- [ ] Phase indicator (Vigil / Tide / Dusk)
- [ ] Reward screen
- Note: fonts and theme already in place (see §9.5); Kenney placeholders go in `assets/art/kenney/`

**Phase 7 — First Playtest**
- Run through 3 encounters with The Root
- Identify broken balance, missing feedback, unclear rules
- Iterate before building more content

---

## 12. Open Design Questions

Issues to resolve through playtesting or further design discussion:

1. **Invader group vs. individual targeting** — Do cards target single invader units, or invader groups per territory? Likely per-territory for simplicity, but individual targeting creates more interesting decisions. Start per-territory, revisit.

2. **Resolution turn count** — 2–3 turns currently estimated. Needs playtesting to feel neither too generous (always clean) nor too punishing (always breach).

3. **Presence placement rules** — What defines "range"? Number of territory steps from nearest Presence token. Needs a concrete number for V1. Starting proposal: range = 1 (adjacent territories only) without card/Aspect upgrades.

4. **Territory grid shape** — V1 uses a hardcoded 9-territory grid. Shape (linear, branching, hub-and-spoke) significantly affects play feel. Decision deferred until Phase 2.

5. **Elite dissolution reveal** — The moment of "this card is permanently removed" needs UX design. Too abrupt = frustrating. Too telegraphed = removes the tension. Options: audio sting + special animation at reward screen; or a "Devoured" trait visible on the Elite before encounter starts (knowing the risk, not the outcome).

6. **Fear reset mechanics** — Does Fear reset to 0 after each threshold, or accumulate? Current design: resets after each threshold trigger. Means hitting threshold 1 five times is better than hitting threshold 3 once. May create perverse incentives. Review after first playtest.

7. **Pale March passive Weave drain** — How much? Current proposal: 1 Weave per turn per 3+ Pale March units on the board simultaneously. Needs tuning.

8. **Deck size vs. hand size vs. encounter length** — With 30-card starting deck, hand of 7, and 3 cards/turn: players rest every 2 play turns (~33% rest rate). Boss fights at 12–13 turns consume ~24–27 cards total — almost the full deck. Questions to resolve through playtesting:
   - Does 30 feel too many cards to absorb for a new player? Should starting deck be smaller (12–15)?
   - Is 33% rest rate correct? Does the invader pacing need to account for rest turns being "free" damage turns for the board?
   - With max hand 10 (post-reward), rests drop to 25% of turns — does this power curve feel right for a boss fight?

9. **Card variety / anti-stagnation** — Risk: players always play the same top 2 + bottom regardless of situation (optimal rotation found early, no reason to deviate). Potential mitigations:
   - Situational cards that are bad unless triggered (e.g., fear-threshold cards, adjacency-required damage)
   - Hand diversity via draw mechanics (draw N, keep M — but loses stamina/rest feel)
   - Forced dormancy as a stamina substitute: instead of resting, one card in hand becomes Dormant (Root-specific? or general mechanic?) — costs playable cards without full rest, keeps Tide active
   - Deck thinning pressure: the deck naturally narrows via dissolution — old stagnant cards leave, new cards from rewards add variety

10. **Invader action speed relative to rest turns** — If players rest every 3rd turn, invaders act uncontested that often. Does The Tide need a slower cadence for Standard encounters, or is uncontested Tide damage the natural punishment for resting? Consider: Tide acts only on turns 2, 4, 6... (every other turn) in Standard; every turn in Elite/Boss.

11. **Fear and Corruption interaction** — *[To address in next session]* How do Fear thresholds interact with Corruption ticks? Can Fear generation outpace Corruption spread? What happens when both hit critical values simultaneously?

---

## 13. Localization System

### Architecture

`CardData`, `WardenData`, `InvaderData`, `CardEffect`, `EffectCondition`, and `EscalateEvent` are all `Resource` subclasses (not `Node`). Resources cannot call `Tr()`. The convention is:

- **Resources store translation keys** — e.g. `CardNameKey = "CARD_ROOT_001_NAME"`
- **UI Nodes do the lookup** — `label.Text = Tr(card.CardNameKey)` at display time
- `TranslationServer.Translate("KEY")` is available as a static fallback where needed

All player-visible strings MUST go through this system. No hardcoded text on any Label.

### Key naming convention

```
CARD_{WARDEN}_{NUM}_NAME          — card name
CARD_{WARDEN}_{NUM}_VIGIL_DESC    — vigil effect description
CARD_{WARDEN}_{NUM}_DUSK_DESC     — dusk effect description
CARD_{WARDEN}_{NUM}_DISSOLVE_DESC — dissolve effect description (only when non-default)

WARDEN_{ID}_NAME / _ARCHETYPE / _DISSOLVE_DESC / _RESOLUTION_DESC
INVADER_{ID}_NAME / _DREAD_DESC
ESCALATE_{FACTION}_{NUM}_DESC

UI_PHASE_VIGIL / TIDE / DUSK / RESOLUTION
UI_HUD_WEAVE / FEAR / TURN
UI_ACTION_CONFIRM / CANCEL / REST / END_PHASE / INFO
UI_TIER_STANDARD / ELITE / BOSS
UI_REWARD_CLEAN / WEATHERED / BREACH
UI_MENU_START / SETTINGS / QUIT
UI_TERRITORY_E1 .. UI_TERRITORY_SS  (9 territory labels)
```

Rule: ALL_CAPS, underscores only. Card keys are derived deterministically from `warden_id + num` by the generation script.

### CSV file

`hollow_wardens/locale/translations.csv` — registered in `project.godot` under `[internationalization]`. Godot auto-imports `.csv` → `.translation`.

- First row: `keys,en` (header)
- Hand-authored UI strings come first
- Card strings are auto-generated in a sentinel block: `# BEGIN CARDS DATA` / `# END CARDS DATA`
- Run `python tools/generate-cards.py` to regenerate the card block

### Adding a new language

1. Add a new column to `translations.csv`: `keys,en,fr`
2. Fill in translated values for each row
3. Re-import in the Godot editor (or run `godot --headless --import`)
4. The game will auto-detect the OS locale or allow manual override via `TranslationServer.SetLocale()`

### Card pipeline integration

`generate-cards.py` derives translation keys from `warden_id` + `card["num"]`, populates `CardNameKey` and `DescriptionKey` in `.tres` files, and regenerates the card block in `translations.csv` from `card["name"]` and `card["vigil/dusk/dissolve"]["desc"]` fields. The JSON source of truth is never modified.

---

## 14. Input Actions

### Action vocabulary (9 actions)

| Action | Purpose | Keyboard | Controller |
|--------|---------|----------|-----------|
| `ui_navigate_left` | Prev card / focus left | Left arrow | D-pad Left / L-stick |
| `ui_navigate_right` | Next card / focus right | Right arrow | D-pad Right / L-stick |
| `ui_navigate_up` | Territory / menu up | Up arrow | D-pad Up / L-stick |
| `ui_navigate_down` | Territory / menu down | Down arrow | D-pad Down / L-stick |
| `game_confirm` | Select card / confirm target | Enter / Z | A / Cross |
| `game_cancel` | Deselect / back | Escape / X | B / Circle |
| `game_rest` | Pass turn / recover discard | R | Y / Triangle |
| `game_end_phase` | End Vigil or Dusk | Space | X / Square |
| `game_toggle_info` | Card/territory detail overlay | Tab / I | LB / L1 |

Custom actions (not reusing Godot's built-in `ui_left` etc.) so game navigation in HandView and TerritoryMapView can be handled independently of Control focus navigation.

### Territory grid navigation

The 3×3 territory grid maps directly to D-pad:
- `ui_navigate_up/down` — move between rows (E-row → M-row → S-row)
- `ui_navigate_left/right` — move within row (col 1 → col 2 → col 3)
- `game_confirm` — select the focused territory

### Phase 6 UI contracts

These rules are enforced during Phase 6 implementation:

- All interactive Controls: `FocusMode = All`
- HandView: consume `ui_navigate_left/right` to cycle cards
- TerritoryMapView: consume `ui_navigate_*` to move 3×3 grid focus
- Modals: trap `game_cancel` to dismiss
- No hardcoded text on any Label — always `Tr(someKey)` or `TranslationServer.Translate(key)`
