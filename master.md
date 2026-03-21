# Hollow Wardens — Master Document
> Game Design + Technical Architecture  
> Version 0.5 — Working reference for Claude Code sessions

---

## Table of Contents
1. [Game Overview](#1-game-overview)
2. [Core Design](#2-core-design)
3. [Encounter System](#3-encounter-system)
4. [Invader System](#4-invader-system)
5. [Card System](#5-card-system)
6. [Elements System](#6-elements-system)
7. [Natives System](#7-natives-system)
8. [Warden Roster](#8-warden-roster)
9. [Run Structure](#9-run-structure)
10. [Balance Reference](#10-balance-reference)
11. [Godot Architecture](#11-godot-architecture)
12. [Class Definitions](#12-class-definitions)
13. [V1 Scope & Build Order](#13-v1-scope--build-order)
14. [Open Design Questions](#14-open-design-questions)
15. [Localization System](#15-localization-system)
16. [Input Actions](#16-input-actions)

---

## 1. Game Overview

**One-liner:** A card roguelike where you play as a dying ancient spirit defending the last sacred territories of a corrupted world.

**Fantasy:** You are not a hero. You are something ancient trying not to be forgotten. The world is being unmade. You fight not to win — but to endure long enough to matter.

**Inspirations:**
- **Spirit Island** — Presence system (location = power), Fear as a resource, asymmetric spirits, elemental innates
- **Gloomhaven** — Hand exhaustion as stamina, card burning for power spikes, rest as a costly decision
- **Slay the Spire** — Roguelike run structure, card drafting, meta-progression, rarity tiers

**What's original:**
- Split turn (Vigil / Tide / Dusk): you act before *and* after invaders
- Bottom-as-dissolve: playing a card's bottom half is powerful but removes it until next encounter
- Element system: cards carry elemental affinities; hitting thresholds mid-turn triggers bonus effects
- Encounter survival model: timed sieges, not kill-all puzzles
- Breach system: losing slowly rather than dying suddenly
- Natives as active counterattack board entities

---

## 2. Core Design

### 2.1 Turn Structure

Each turn has three phases, always in this order:

```
VIGIL → THE TIDE → DUSK
```

| Phase | Who acts | Cards played | Nature |
|---|---|---|---|
| Vigil | Player | Up to 2 tops | Proactive — setup, positioning, building Fear |
| The Tide | Invaders | — | Fear actions resolve → invaders spawn, advance, ravage |
| Dusk | Player | Up to 1 bottom | Reactive — respond with full information |

**Key principle:** Tops go to discard and return when you rest. Bottoms dissolve — they are gone until the next encounter (or permanently on bosses). This creates two distinct economies on a single card.

**Playing options per turn (not hardcoded — rules can modify):**
- Do nothing (rest — see §2.3)
- Play 1 or 2 tops only (safe, conservative)
- Play 1 bottom only in Dusk (dissolve one card for its powerful effect)
- Play 1–2 tops in Vigil + 1 bottom in Dusk (full offensive turn, highest stamina cost)

### 2.2 Eclipse Events — Flipping the Ratio

Certain encounters or Realm events invert the structure to **2 bottoms + 1 top**. The Tide resolves first. Your Vigil action becomes the disadvantaged one.

This is not a stat modifier — it changes how the entire turn *feels*. Under Eclipse, you're in reactive mode from the start, with limited ability to set up.

**Eclipse sources:**
- Corrupted Zones (territory type)
- The Long Night (Realm event, lasts multiple turns)
- Herald-class Invaders (flip as a Ravage side-effect)

### 2.3 Card Draw — Refill Model

Each turn, at the start of Vigil, draw from your deck until your hand reaches your hand limit (default 5) or the deck is empty. Unplayed cards stay in hand between turns. Played tops go to the discard pile. Played bottoms dissolve (removed from the encounter).

This means your hand degrades gradually as the deck empties. After 3–4 play turns, draws start coming up short. The player can see the Rest approaching and plan for it.

### 2.4 Resting

When your deck is empty (or nearly so), you may **Rest** instead of playing cards:
- Shuffle all discarded cards back into the deck
- **Rest-dissolve:** remove 1 random card from the deck for the rest of this encounter (encounter-only — returns between encounters, like played bottoms)
- The Tide still runs — invaders activate, advance, and arrive uncontested
- You play 0 cards this turn
- Element thresholds still check against carryover pool at Vigil start (see §6.3)

**Rest is costly in three ways:** you lose a play turn, you lose a card to rest-dissolve, and the Tide runs unopposed. But your element engine persists — carryover from the previous turn means threshold effects can still fire during a Rest turn, rewarding players who built strong element pools before Resting.

**Rest-dissolve design intent:** Each Rest taxes the deck by 1 card, creating a natural compression over the encounter. After 2 Rests, the deck is 2 cards thinner. Combined with bottoms played, a 10-card deck might be down to 5–6 cards by the final cycle. This is the stamina drain — the spirit is exhausting itself.

**Rest-dissolve per encounter tier:**
- Standard/Elite: rest-dissolved cards return between encounters (encounter-only loss)
- Boss: rest-dissolved cards are permanently removed (adds to the Boss drain alongside permanently dissolved bottoms)

**Rest is not always voluntary.** When the deck is empty and the hand is too small to act meaningfully, Rest becomes the only option. Aggressive bottom play accelerates this — more bottoms = faster deck depletion = earlier forced Rest = more rest-dissolve tax. The system self-balances against frontloading.

### 2.5 Fear Actions

Accumulating Fear generates **Fear Actions** — hidden effects that resolve at the start of each Tide, before invaders act. Every 5 Fear spent queues one Fear Action drawn from the current Dread Level's pool.

- **Hidden:** queued actions appear as face-down cards. You know how many are coming but not which effects until the Tide starts.
- **Dread-gated:** Dread Levels (every 15 total Fear generated in the run) unlock higher-tier pools — each level dramatically more powerful. When Dread advances, all queued (unrevealed) Fear Actions retroactively upgrade to the new pool.
- **Timing:** Fear Actions resolve at the top of the Tide phase, before Activate/Advance/Arrive
- **Pool:** Global fear deck mixed with adversary-specific fear cards
- **Warden interaction:** Some wardens can preview upcoming fear actions, upgrade them to a higher tier, or choose between multiple options

---

## 3. Encounter System

### 3.1 What an Encounter Is

An encounter is a contained invader wave. Each encounter has:
- A defined invader group (type, count, pattern)
- A fixed number of **Tide steps** (wave duration)
- An entry pattern (spawn locations, movement targets)
- A readable spawn preview: you can see what's coming *one Tide step* before it arrives

**Encounter setup:**
- **Initial wave (Wave 0):** Before the player's first Vigil, a starting wave arrives at A-row positions per the encounter data. The board already has invaders in play when the game begins.
- **Starting Presence:** The Root Warden begins with 1 Presence token placed on I1.

### 3.2 How Encounters End

**Encounters use the survival model, not the kill-all model.**

- The encounter ends when all Tide steps are exhausted
- After the final Tide step, you enter **Resolution turns**
- During Resolution, invaders stop spawning and advancing — they hold positions
- You spend Resolution turns dealing with whatever remains
- **Resolution turn limit by tier:** Standard: 2 turns. Elite: 3 turns. Boss: 1 turn. If invaders remain after Resolution, the encounter is a Breach. Boss Resolution is intentionally brutal — one shot to clean up. Root's Assimilation fires automatically on Resolution start, making that single turn enough for a well-positioned Root player.

### 3.3 Resolution — Warden Flavor

Different Wardens resolve encounters differently, even against identical invader states.

| Warden | Resolution style |
|---|---|
| The Root | Assimilation — Presence tokens absorb adjacent invaders, reducing Corruption in that territory by 1 per invader absorbed |
| The Ember | Destruction — Resolution turns are uncapped damage output |
| The Veil | Repulsion — pushes remaining invaders back toward spawn, reduces Corruption in vacated territories |

### 3.4 Reward Tiers

| Result | Condition | Reward |
|---|---|---|
| **Clean** | No invaders remain in Resolution | Full reward: card draft + Aspect choice + Weave restore |
| **Weathered** | Cleared in 1–2 Resolution turns | Partial: card draft OR Aspect (player chooses) + Weave +1 |
| **Breach** | Invaders remain after Resolution | Minimal: Weave +1 only + Breach effect carries into next encounter |

**Breach effects (carry into next encounter):**
- One territory enters next encounter pre-Tainted
- One invader unit carries over (doesn't reset)
- Next encounter's Escalate clock starts one step ahead

### 3.5 Encounter Tiers and Dissolution Risk

| Tier | Bottom cost | Notes |
|---|---|---|
| **Standard** | Card removed until next encounter | Base cost. Bottoms return between encounters. |
| **Elite** | Card *may* be permanently removed | Unknown at time of playing the bottom — revealed at reward screen |
| **Boss** | All played bottoms permanently removed | Known upfront. Every bottom you play is a conscious sacrifice. |

This creates a clear risk gradient. In Standard encounters you use bottoms more freely. In Elites you hesitate. In Boss fights every bottom is a permanent decision.

---

## 4. Invader System

### 4.1 The Weave — Run Health

The Weave is the spiritual fabric of the Realm. Run ends when Weave hits 0.

- **Starting value:** 20

**Weave drain events:**
- Invader reaches the Sacred Heart: large hit (−3 to −5, scales with invader's remaining HP)
- Desecrated territory (Corruption level 3): −1 per turn passively
- Sacred Site falls: −3
- Certain invader traits drain Weave passively

**Weave restoration:**
- Fear threshold events
- Between-encounter and between-Realm rewards (2–6 Weave depending on performance)

### 4.2 Corruption — Territory Health (Points-Based)

Each territory has a **Corruption Points** counter that advances toward three levels. Levels have meaningful thresholds — level 1 happens easily, level 2 should be avoided, level 3 is lasting and severe.

| Level | Points to reach | Effect | Duration |
|---|---|---|---|
| Clean | 0 | Full resource generation. Normal Presence rules. | — |
| Tainted (1) | 3 points | Reduced resource output. Invaders get bonus action effects. | Resets next encounter |
| Defiled (2) | 5 additional points | No resources. Placing Presence costs 1 extra card. Invaders gain advanced actions. | Persists to next encounter as level 1 |
| Desecrated (3) | 7 additional points | Presence tokens here are removed. −1 Weave per turn passively. | Permanent — persists all encounters |

**Advancing Corruption:** Invader Activate/Ravage actions add corruption points. Invaders outnumbering Natives in a territory at end of turn may also add points (to define during balancing).

**Reducing Corruption:** Cleanse card effects reduce points. Point removal: small cleanse = −1 point. Large cleanse = −3 points. Full cleanse = drop one full level. Desecrated territories are harder to cleanse (require special effects).

**Persistence rules:**
- Level 1 resets to Clean between encounters
- Level 2 persists as Level 1 corruption at the start of next encounter (i.e., 3 points already accumulated when encounter begins)
- Level 3 is fully permanent — territories stay Desecrated for the rest of the run

### 4.3 The Tide — Step Sequence

Every Tide phase follows this exact sequence:

1. **Fear Actions resolve** — any queued Fear Actions fire before invaders act
2. **Activate** — all invaders on the board execute the faction action card (with unit-type modifiers). After Activate, if the action was Ravage or Corrupt, surviving Natives in affected territories counter-attack (player-assigned).
3. **Advance** — existing invaders move toward the Sacred Heart. Base movement: 1 step. The action card may modify movement (e.g., March = +1, Settle = 0). Invaders already in I1 with nowhere to advance **march on the Heart**: deal Weave damage equal to remaining HP (minimum 1). Invaders that entered I1 this Advance step stop — Heart marching starts next Tide (one-turn grace).
4. **Arrive** — new invaders appear at arrival points. Location was previewed last Tide; unit composition is revealed now. Newly arrived units do nothing else this Tide.
5. **Escalate** — every 3 Tides, the faction escalates: a new card is added to the Painful action pool (see §4.4).
6. **Preview** — draw and reveal next Tide's action card from the appropriate pool (per cadence). Next wave's arrival locations revealed. Player enters Vigil with full action knowledge but unknown arrival composition.

**Sub-phase pauses:** The Tide is not a single automated sequence. It pauses for player input:
1. Fear Actions reveal one at a time — player resolves each (with targeting if needed)
2. Player confirms → Activate runs
3. Counter-attack prompt per territory if the action was Ravage or Corrupt — player assigns
4. Player confirms → Advance + Arrive run
5. Preview next action card

**Tide 1 — ramp-up:** The first Tide runs **Advance** and **Arrive** only. Fear Actions, Activate, Native counter-attack, Escalate, and Preview are all skipped. This gives the player one full turn to see the threat positions before invaders start acting. Tide 2 onwards runs the full sequence.

### 4.4 Faction Action System — Two-Pool Model

Each faction has two action pools: **Painful** (actions demanding a player response) and **Easy** (breathing room with minor effects). Each Tide draws from one pool based on the encounter's **cadence rules**.

#### Cadence Rules

Cadence is rule-based by default: each encounter tier defines a `max_painful_streak` and `easy_frequency`. After N consecutive Painful draws, force an Easy draw. This creates a guaranteed rhythm — harder encounters have longer painful streaks, easier ones alternate more frequently.

| Tier | Default cadence | Max painful streak | Feel |
|------|----------------|-------------------|------|
| Standard (early Realm 1) | P-E-P-E-P-E... | 1 | Steady, learnable. Every other Tide is relief. |
| Standard (late Realm 1) | P-P-E-P-P-E... | 2 | Pressure builds. Two hits before a breather. |
| Elite | P-P-P-E-P-P-P-E... | 3 | Relentless. Easy Tides are precious. |
| Boss | Accelerating: starts at 2, increases to 4+ | 2→4 | Opens manageable, compresses into brutality. |

Designers can override the default with a hand-authored pattern array (e.g., `["P","E","P","P","P","E","P"]`) for specific encounters. Some encounters may pair a harder cadence with an easier arrival pattern, or vice versa — this is a deliberate balancing lever.

#### Pool Draw Rules

Within each pool, draw randomly without replacement. When a pool is empty, reshuffle all its cards back in. This creates short deduction windows: after seeing Ravage from the Painful pool, the player knows the remaining Painful cards, but not the order.

Escalation adds cards to the Painful pool, making it larger and less predictable over time.

#### Painful Pool (base)

| Card | Activate effect | Advance modifier | Provokes counter-attack | Notes |
|------|----------------|------------------|------------------------|-------|
| **Ravage** | Deal 2 Corruption to territory. Deal 2 damage as a pool to Natives (auto-maximize kills, targeting lowest HP first). The corruption value IS the native damage pool. | 1 step (normal) | Yes | The primary threat. Most common painful action. |
| **March** | Units at full HP gain Shield 1. Below-full units recover 1 HP. | +1 step (2 total) | No | Dangerous because it heals AND accelerates. |

#### Painful Pool (added via Escalation)

| Card | Activate effect | Advance modifier | Provokes counter-attack | Added at |
|------|----------------|------------------|------------------------|----------|
| **Corrupt** | Deal 1 Corruption + kill 1 Native outright. | 1 step (normal) | Yes | Escalation 1 (Tide ~3–4) |
| **Fortify** | Place fortification token: all units in territory gain Shield 2. | 0 steps (hold) | No | Escalation 2 (Tide ~6–7) |

#### Easy Pool

| Card | Activate effect | Advance modifier | Provokes counter-attack | Notes |
|------|----------------|------------------|------------------------|-------|
| **Rest** | Recover half max HP (round up). | 1 step (normal) | No | Pure recovery. Still advances. |
| **Settle** | Pioneer places Infrastructure if 2+ units present. All units gain Shield 1. | 0 steps (hold) | No | Shield 1 makes them slightly harder to kill, but they don't advance. Even without a Pioneer, units get a small shield. |
| **Regroup** | Arrival-row units return to spawn point. Others hold position. | 0 steps (special) | No | Resets board positioning. Removed from easy pool at Escalation 3 (Boss only). |

#### Escalation Schedule

| Escalation | Trigger | Effect |
|------------|---------|--------|
| 1st | Tide 3–4 | **Corrupt** added to Painful pool (now 3 cards). |
| 2nd | Tide 6–7 | **Fortify** added to Painful pool (now 4 cards). |
| 3rd (Boss only) | Tide 9+ | **Regroup** removed from Easy pool (now 2 cards — relief gets worse). |

#### Preview Timing

At end of each Tide (after Arrive), two things are revealed for the next turn:
- The **action card** for next Tide (drawn from the appropriate pool per cadence)
- The **arrival locations** for next Tide's wave (which A-row points are active)

The player enters Vigil knowing what invaders will do, and where new ones arrive — but NOT the unit composition of the arriving wave. Composition is revealed at Arrive.

### 4.5 Territory Layout — Pyramid (V1: 3-2-1)

The territory map is a pyramid with invaders arriving at the wide end and the Sacred Heart at the apex. Strategic depth: you can sacrifice outer territories to create a chokepoint.

```
[A1] [A2] [A3]   ← Arrival row (3 territories, invaders spawn here)
   [M1] [M2]     ← Middle row (2 territories)
     [I1]        ← Inner row (1 territory)
      [H]        ← Sacred Heart (not a territory — invaders attack it from I1)
```

**Adjacency:**
- A1↔A2, A2↔A3 (arrival row horizontal)
- A1↔M1, A2↔M1, A2↔M2, A3↔M2 (arrival → middle)
- M1↔M2 (middle row horizontal)
- M1↔I1, M2↔I1 (middle → inner)
- I1 → Heart (invaders in I1 can Activate against the Heart)

**Sacred Heart:** Not a territory. Has no Corruption. During the Advance step, invaders in I1 that have been there since before this Tide's Activate **march on the Heart** instead of advancing (nowhere to go), dealing Weave damage equal to their remaining HP (minimum 1). Invaders that entered I1 during this Tide's Advance step stop there — Heart marching starts next Tide (one-turn grace period). Some invader types have multiplied Heart damage (e.g., double HP value).

**Future expansion:** Larger pyramid (4-3-2-1 = 10 territories) for Realm 2+. Wider arrival rows create more strategic variance.

### 4.6 Fear & Dread System

Fear is a resource you generate and spend. Dread is the escalation track that makes Fear Actions more powerful over time. Two distinct systems, deliberately separated.

**Fear** — the resource:
- Generated by card effects, defeating invaders, and Native deaths
- Every 5 Fear spent queues one **Fear Action** (hidden until Tide start)
- Fear generation from combat: Small invader +1, Medium +2, Elite/Infrastructure +3

**Dread** — the escalation track (Dread Level 1–4):
- Tracks *total* Fear generated across the entire run (never decremented, even when Fear is spent)
- Every 15 total Fear advances the Dread Level
- Higher Dread = Fear Actions draw from stronger pools

| Dread Level | Threshold | Fear Action pool quality |
|---|---|---|
| 1 | 0 Fear total | Basic actions (1 damage, push 1 invader) |
| 2 | 15 Fear total | Improved actions (2 damage, push + corrupt reduction) |
| 3 | 30 Fear total | Strong actions (3 damage, rout, weave restore) |
| 4 | 45 Fear total | Powerful actions (AoE damage, multiple routs, fear cascades) |

**Dread Level upgrade — retroactive:** When the Dread Level advances, ALL currently queued (unrevealed) Fear Actions are upgraded to the new level's pool. This means pushing past a Dread threshold mid-encounter upgrades pending actions — a huge incentive to frontload Fear generation. If you have 2 Fear Actions queued at Dread 1 and then hit 15 total Fear, both upgrade to Dread 2 quality before they reveal.

**Fear Action UX:**
- Queued Fear Actions appear as **face-down cards** in the Tide queue area. The player can count them but not see which effects they are.
- At Tide start (before Activate), cards flip face-up one at a time (~0.5s each), revealing and resolving in sequence.
- The Dread track is displayed as a **bar with threshold markers** at 15/30/45, showing current total Fear generated and how much is needed to reach the next Dread Level. Queued Fear Action cards sit above or beside this bar.
- Phase 6 (functional UI): stack of face-down card rectangles with count badge + Dread bar with 4 segmented pips.

### 4.7 Invader Factions

#### V1 — The Pale March (Methodical)

**Identity:** Slow, inevitable, escalating. They march in formation, build infrastructure, and grind territories down through sustained pressure. Their strength is inevitability, not speed.

**Unit Types:**

| Unit | HP | Identity | Activate modifier | Advance modifier |
|------|-----|----------|-------------------|------------------|
| **Marcher** | 3 | Grunt | None — executes action as printed. | Normal (1 step). |
| **Ironclad** | 5 | Heavy | Ravage: +1 Corruption (3 total). Rest: recover to full HP. Corrupt: kills 2 Natives instead of 1. | Moves only every other Advance (alternates move/hold). |
| **Outrider** | 2 | Runner | Ravage: only 1 Corruption, but deals 2 damage to 1 Native first. Rest: doesn't rest — advances 1 instead. Regroup: doesn't regroup — advances 1 instead. | Always +1 movement (2 steps total; 3 on March). |
| **Pioneer** | 2 | Builder | After ANY Activate, if 2+ March units in territory: place 1 Infrastructure. Fortify: fortification also grants +1 Corruption on future Ravage. | Normal (1 step). |

**Infrastructure:** Invader-placed token. All Pale March units in the same territory deal +1 Corruption on Ravage. Infrastructure has 2 HP, can be targeted by damage effects. Destroying Infrastructure generates 1 Fear.

**Adversary-specific fear card:** "March Without End" — all Pale March in Tainted+ territories advance one extra step this Tide.

**Spawn Waves — Randomized Composition:**

Each wave defines 2–3 possible unit compositions. The player sees which arrival points are active (location preview from previous Tide) but unit composition is revealed at Arrive. Encounter data defines wave options with weights:

```json
{
  "wave": 3,
  "arrival_points": ["A1", "A3"],
  "options": [
    { "weight": 40, "units": { "A1": ["ironclad"], "A3": ["marcher"] } },
    { "weight": 35, "units": { "A1": ["outrider", "outrider"], "A3": ["marcher"] } },
    { "weight": 25, "units": { "A1": ["marcher"], "A3": ["pioneer", "outrider"] } }
  ]
}
```

Encounter designers control variance: early waves are mostly Marchers with occasional Outriders, mid-encounter waves introduce Ironclads, late waves bring Pioneers with escorts. The player can't memorize exact compositions across runs.

**Fear generation from Pale March kills:**
- Marcher defeated: +1 Fear
- Outrider defeated: +1 Fear
- Ironclad defeated: +2 Fear
- Pioneer defeated: +1 Fear
- Infrastructure destroyed: +1 Fear

#### Planned — The Scorch (Volatile)

**Identity:** Fast, fragile, explosive. They overwhelm through speed and numbers. Chain-Ravage if territories are undefended. Their easy pool is unusually weak (no Rest card — they never stop).

**Unit types (to design):** Sparks (HP 1, fast), Blazes (HP 2, chain-ravage), Embers (HP 1, spawn more on death)

**Element interaction:** Scorch units add Ash×1 to the element pool when they Arrive. The player can exploit this for Ash thresholds, but Ash T2 has a Corruption cost.

#### Planned — The Hollow (Adaptive)

**Identity:** Adaptive, parasitic. They grow stronger near Desecrated territories. Every card in their action deck is aggressive — including their "easy" pool, which is merely less aggressive. Self-destructive: Consume destroys their own Infrastructure for HP and Weave damage.

**Unit types (to design):** Husks (HP 2, basic), Feeders (HP 3, scale with Corruption), Devourers (HP 4, Consume specialists)

**Element interaction:** Hollow units add Void×1 when in Desecrated territories.

---

## 5. Card System

### 5.1 Card Anatomy

Every card has two halves and an element set. The bottom half is the dissolve action.

```
┌────────────────────────────────────┐
│  [Elements]  ◈ Root  ◈ Mist        │  ← 1–3 elements shown as icons
│                                    │
│  Card Name                         │  ← Cinzel-Bold
│  ─────────────────────────────     │
│  [TOP — VIGIL ACTION]              │  ← Goes to DISCARD when played
│  Played during Vigil phase.        │     Returns on Rest
│  Proactive, setup-oriented.        │
│  ─────────────────────────────     │
│  [BOTTOM — DUSK ACTION]  ◈ DISSOLVE│  ← DISSOLVES when played
│  Played during Dusk phase.         │     Gone until next encounter
│  Reactive, stronger, played after  │     (or permanently on Boss)
│  seeing the Tide.                  │
└────────────────────────────────────┘
```

**Bottom doubling of elements:** When you play the bottom half of a card, the elements on *that card* count double toward threshold this turn. A card with [Root, Mist] contributes Root×2 + Mist×2 when its bottom is played.

### 5.2 Top Actions (Vigil)
Design space: setup, positioning, resource generation, defensive preparation. These are safe, repeatable effects.

Examples:
- Place 1 Presence in range 1
- Reduce Corruption by 1 anywhere
- Generate 3 Fear
- Restore 1 Weave
- Awaken 1 Dormant card (Root-specific)

### 5.3 Bottom Actions (Dusk — Dissolve)
Design space: stronger reactive effects. The best bottoms respond to what the Tide *just did*. They cost the card until next encounter, so they must justify the sacrifice.

Examples:
- Reduce Corruption by 3 in range 2 (big reactive cleanse)
- Generate 8 Fear (threshold push)
- Damage all Invaders in range 2 for 3 (wide defensive strike)
- Restore 3 Weave + Place 1 Presence anywhere
- Awaken ALL dormant cards (Root — explosive recovery)

**Default bottom (safety valve):** Cards without a strong thematic bottom get a weak default: *Place 1 Presence, range 1.* This is rarely worth dissolving for, but available in an emergency. In practice, well-designed cards should have a meaningful bottom — the interesting question is *when* the sacrifice is worth it.

### 5.4 Deckbuilding Layer

- **Starting deck:** 10 cards (baseline, varies by warden). Fixed per warden per run. Provides identity and early strategy.
- **Draft pool:** 15–20 additional cards per warden across rarity tiers. Available to draft between encounters.
- **Shared pool:** "Divine" cards (warden-agnostic, thematic name TBD) available in the draft pool regardless of warden.
- **Rarity tiers:** Dormant (common), Awakened (uncommon), Ancient (rare). Starting deck is all Dormant-tier.

### 5.5 Card Rarity Tiers

| Tier | Name | Design role |
|---|---|---|
| Common | Dormant | Baseline effects. Reliable, predictable. Starting deck material. |
| Uncommon | Awakened | Synergistic, stronger, slightly conditional. Draft material. |
| Rare | Ancient | Build-defining. Powerful bottoms, unique effects, high risk-reward. |

### 5.6 Card Upgrades (Future)

Two upgrade paths per card (to design post-V1):
- **Enhance:** Add a secondary effect (e.g., add "and generate 1 Fear" to a cleanse card)
- **Upgrade:** Predefined stronger version of the card (bigger values, better range, additional condition)

---

## 6. Elements System

### 6.1 The Six Elements

| Element | Thematic identity | Root warden affinity |
|---|---|---|
| **Root** 🌿 | Earth, growth, network, stability | Primary |
| **Mist** 🌫 | Water, memory, reach, healing | Secondary |
| **Shadow** 🌑 | Darkness, fear, hidden power | Tertiary |
| **Ash** 🔥 | Fire, destruction, transformation | Rare |
| **Gale** 💨 | Wind, movement, disruption | Rare |
| **Void** ⚫ | Decay, entropy, corruption | Rare |

Each card carries 1–3 element icons. Most well-balanced cards carry 2. Cards that are under the power curve on effects may carry 3 elements to compensate — element fuel as a balancing lever.

### 6.2 Accumulating Elements

Elements accumulate in a pool during your turn as you play cards:
- Playing a top: add that card's elements to the pool ×1
- Playing a bottom: add that card's elements to the pool ×2 (bottom doubles)
- Invaders arriving with elements: certain invader types bring their own elements to the pool (e.g., Scorch brings Ash on arrival, which the player can use to trigger fire-threshold events)

**Example turn (turn 3 of encounter, with carryover):**
1. Start of turn: carryover from last turn → Root×2, Mist×1 (decayed from Root×3, Mist×2)
2. Play top of Card A [Root, Mist] → pool: Root×3, Mist×2
3. Play top of Card B [Root, Root] → pool: Root×5, Mist×2 → Root Tier 1 (4) triggers! Place 1 Presence at range 1.
4. Play bottom of Card C [Mist, Mist] → pool adds Mist×4 → pool: Root×5, Mist×6 — no new Mist threshold yet (Tier 1 is 4, triggers!)
5. End of turn: decay −1 each → Root×4, Mist×5. Next turn starts with strong carryover.

### 6.3 Thresholds — Universal Effects

Thresholds are **universal** — every element does the same thing regardless of which warden triggers it. Wardens differentiate by how easily they *reach* thresholds (via affinity), not by what the thresholds do. Each threshold tier can trigger **once per turn**, checked after each card play. This means Tier 1 can fire in Vigil and Tier 2 can fire in Dusk on the same turn — bottoms accelerate you into higher tiers.

**Threshold resolution — player-confirmed:** Thresholds do not auto-resolve. When a threshold triggers, the threshold button **lights up** in the element tracker UI. Targeted thresholds (e.g., Place Presence, Reduce Corruption in a territory) enter targeting mode when clicked — the player selects a valid target. Untargeted thresholds (e.g., Restore Weave, Generate Fear) show an OK button to confirm. A triggered threshold may be delayed until the other phase — for example, bank a Vigil-triggered threshold to use in Dusk. Unresolved thresholds are lost at end of turn. All three tiers (T1/T2/T3) are always visible with their descriptions in the UI — the player always knows what they're working toward. Future design space: some thresholds may be phase-restricted (e.g., "Vigil only" or "Dusk only") for balancing purposes.

**Rest turns:** At the start of a Rest turn's Vigil, thresholds check against the element carryover pool (reduced by 1 from last turn's end). No cards are played, so no new elements enter — but if the carryover is still above a threshold, that threshold fires. This rewards building a strong element engine before Resting: a player at Root×5 who Rests still has Root×4 after decay, triggering Root Tier 1 (free Presence placement) even on their "empty" turn.

| Element | Tier 1 (4) | Tier 2 (7) | Tier 3 (11) |
|---|---|---|---|
| **Root** 🌿 | Place 1 Presence at range 1 | Reduce Corruption by 3 in one territory with Presence | Place 2 Presence anywhere + reduce Corruption by 2 in each |
| **Mist** 🌫 | Restore 1 Weave | Return 1 card from discard to hand | Restore 3 Weave + return all discarded cards to hand (full Rest effect without losing your turn) |
| **Shadow** 🌑 | Generate 2 Fear | Next Fear Action draws from one Dread Level higher | Generate 5 Fear + preview and choose between 2 Fear Actions for the next queue |
| **Ash** 🔥 | Deal 1 damage to all invaders in one territory | Deal 2 damage to all invaders in one territory; that territory gains 1 Corruption point | Deal 3 damage to ALL invaders on the board; each affected territory gains 1 Corruption |
| **Gale** 💨 | Push 1 invader one territory toward spawn | Push all invaders in one territory toward spawn | Push all invaders on the board one territory toward spawn + they skip their next Advance |
| **Void** ⚫ | Deal 1 damage to lowest-HP invader on board | All invaders take 1 damage | All invaders take 2 damage; invaders killed by this don't generate Corruption on death |

**Design notes:**
- Tier 1 (4): The "passive" — a warden with primary affinity hits this by turn 3 through consistent top play. For off-affinity wardens, requires dedicated draft picks or a bottom play.
- Tier 2 (7): The "commitment" — even with primary affinity, this takes 5+ turns of pure consistency OR a bottom spike. This is where bottoms become element fuel, not just powerful effects.
- Tier 3 (11): The "build-around" — near-impossible from tops alone within a standard 5-turn encounter. Requires sustained play + bottom spikes + possibly element-linger effects. Fires 0–1 times per encounter in a deck built for it.

**Future: Adversary element interaction (post-V1)**
- *Adversary element presence:* Certain factions add elements to the pool on spawn (e.g., Scorch brings Ash×1 per arriving unit). Players can use these, but threshold effects with downsides (Ash corruption) may fire unwantedly.
- *Adversary element thresholds:* Adversary-specific effects that trigger against the player if certain elements are too high (e.g., "If Ash ≥ 7, The Hollow gains +1 Corruption dealt this Tide"). Printed on encounter card as visible information.

Encounters can also have their own elemental threshold events (specific to that encounter's challenge card or adversary type).

### 6.4 Element Decay — Reduce by 1

At the end of each turn, each element count **reduces by 1** (minimum 0):
- Pool after turn: Root×5, Mist×3, Shadow×1
- Start of next turn: Root×4, Mist×2, Shadow×0

This creates a rising-floor engine. Consistent play of the same element builds a pool that accumulates over turns — rewarding dedication to an element strategy. A warden generating Root×2 per turn from tops reaches a steady state of Root×4–5 by mid-encounter, keeping Tier 1 active as a passive.

**Engine-building example (consistent Root×2 per turn from tops):**

| Turn | Carryover | + Tops | = Pool | Decay | End |
|------|-----------|--------|--------|-------|-----|
| 1 | 0 | 2 | 2 | −1 | 1 |
| 2 | 1 | 2 | 3 | −1 | 2 |
| 3 | 2 | 2 | 4 ← Tier 1 | −1 | 3 |
| 4 | 3 | 2 | 5 | −1 | 4 |
| 5 | 4 | 2 | 6 | −1 | 5 |

**Bottom spike example (turn 3, double-Root bottom = +4 instead of +2):**
Turn 3: carryover 2 + bottom 4 = **6** → Tier 1 fires, close to Tier 2. End: 5.
Turn 4: 5 + 2 = **7** → Tier 2 fires. End: 6.

**Rest penalty:** Resting generates no elements but decay still applies. A rest turn costs 1 point off every element — engine builders must weigh rest timing carefully.

Cards or effects that say "elements linger" prevent the decay for specific elements that turn.

### 6.5 Display

- Element icons shown on each card (1–3 icons, small, bottom-left of card)
- Active element pool shown on the HUD as a counter bar (one per element, shows current value)
- Threshold markers at 4, 7, and 11 shown on each counter so players can see how close they are
- Previous turn's value shown in muted colour behind current value (shows the −1 decay)
- When a threshold triggers, brief flash on the counter + effect resolves immediately

---

## 7. Natives System

### 7.1 What Natives Are

Natives are the indigenous inhabitants of the sacred territories. They're not passive — they actively fight back when protected. They are the board's counterattack layer and a key resource for certain warden playstyles.

### 7.2 Native Stats (V1 baseline)

| Unit | HP | Damage | Notes |
|---|---|---|---|
| Native | 2 | 3 | Knows the land; hits hard but fragile |
| Invader (grunt) | 3 | 2 corruption | More durable but less efficient in combat |

Natives deal *damage* (HP reduction) to invaders, not corruption. This distinction matters: invaders can be damaged without corruption advancing.

### 7.3 Native Behavior

- **Spawn:** Each territory starts with a fixed number of Natives defined in EncounterData. **Default baseline:** 0–1 on A-row (entry territories — most will die before you can protect them, max 1–2), 2 on M-row and I-row. This gives ~6 Natives total on a 3-2-1 pyramid. Encounter data and between-encounter events can override these defaults (e.g., a harder encounter starts with 1 per M-territory; a blessed territory event grants an extra Native on I1).
- **Take damage (invader → native):** During Activate, invader damage to Natives is **auto-assigned to maximize kills**. The system distributes damage to kill as many Natives as possible (targeting lowest HP first, allocating exactly enough to kill each before moving to the next). Corruption from Ravage hits the territory simultaneously — Natives take HP damage and the territory takes Corruption points in the same step.
- **Counter-attack (native → invader):** Natives only counter-attack when the invader action this Tide was **Ravage** or **Corrupt**. On all other actions (Rest, Settle, March, Regroup, Fortify), Natives stay passive. When counter-attack triggers, surviving Natives pool their damage and the **player assigns it** freely across invaders in that territory — focus fire, split, or skip entirely (assign 0). **Future:** `Arouse` effects (warden abilities, element thresholds) can force Natives to counter-attack on non-damage Tides.
- **Death:** A Native reduced to 0 HP is removed from the board. Its death generates 1 Fear.

### 7.4 Protecting Natives

Cards that interact with Natives:
- **Shield Natives** — give all Natives in range a temporary HP buffer (requires damage above threshold to kill)
- **Boost Natives** — increase Native damage for this turn
- **Heal Natives** — restore HP to Natives in range
- **Entice** (future warden mechanic) — mark Natives with a special token; certain warden effects trigger off marked Natives

### 7.5 Board Token System (Future-Proofed)

Natives are the first board token type. The system is designed to support others:

| Token type | Effect | Status |
|---|---|---|
| Native | Active counterattack unit | V1 |
| Bramble | Slows/blocks invader movement | Planned |
| Dangerous Terrain | Increases damage dealt to invaders in territory | Planned |
| Animal | Territory-level passive effect | Planned |
| Richness | Empowers invader Activate OR native damage, depending on control | Planned |
| Infrastructure | Invader-placed: defends invaders, enables special actions | V1 (invader side) |

All token types use a base `BoardToken` class with type, HP (optional), and `OnTidePhase()` hook.

---

## 8. Warden Roster

### 8.1 The Root (V1 — Starter)
**Archetype:** Tank / Control  
**Playstyle:** Spreads Presence slowly, passively generates Fear via network. Difficult to exhaust. Wins by blanketing the board and outlasting.

**Element affinity:** Root (heavy), Mist (medium), Shadow (light)

**Presence mechanic:** Root Presence generates passive Fear each turn equal to the number of *adjacent* Presence tokens (network effect — directed edges, each pair counted twice). Spreading wide is as valuable as spreading fast.

**Starting deck:** 10 cards, all Dormant rarity. Heavy Corruption reduction, moderate Fear generation, some Presence placement, one Weave recovery. Full 28-card pool includes 10 starting + 18 more across Dormant/Awakened/Ancient rarities available as draft rewards.

**Dissolution (bottom) — Dormancy:**  
When Root plays the bottom of a card, the card enters a **Dormant** state rather than being fully removed. It goes to the **discard pile** (not the draw pile) — the player won't encounter it again until the next Rest shuffles discards back into the deck. While dormant, it is inert (cannot be played — top or bottom — until Awakened). On Boss encounters, double-dissolving a Dormant card removes it permanently.

**Rest-dissolve — also Dormancy:** When Root rests, the rest-dissolved card also goes dormant (not removed), but it **stays in the draw pile** (already shuffled in with the rest of the discards during Rest). Bottom-played dormant cards go to the discard pile and re-enter the deck on the next Rest; rest-dissolved dormant cards are already in the freshly-shuffled deck. In both cases the card is inert dead weight until Awakened. Awaken effects clean up both types. Root's deck never actually shrinks — it fills with dead draws instead.

*Design intent: The Root's power accumulates. Even its sacrifice state is a resource to manage — Dormant cards are fuel for Awaken effects.*

**Resolution style:** Assimilation — at end of encounter, for each Presence territory, all invaders in all adjacent territories are removed. Each removed invader reduces Corruption in that territory by 1 point.

### 8.2 The Ember (Planned)
**Archetype:** Burst / Aggressive  
**Element affinity:** Ash (heavy), Shadow (medium), Gale (light)

**Dissolution (bottom) — Fear Pulse:** When Ember plays a bottom, it generates a Fear pulse equal to the card's cost before the card is removed. Higher-cost cards = bigger fear spike on sacrifice.

**Resolution style:** Destruction — pure damage output in Resolution turns.

### 8.3 The Veil (Planned)
**Archetype:** Control / Disruption  
**Element affinity:** Mist (heavy), Shadow (medium), Void (light)

**Dissolution (bottom) — Disruption:** Instead of the usual bottom effect, may choose to delay one Invader group's Advance this Tide. Reach vs. disruption choice.

**Resolution style:** Repulsion — pushes remaining invaders toward spawn, reduces Corruption.

### 8.4 Future Warden Concepts

- **Volcano Warden:** Accumulates enemies via terrain manipulation, then blasts regions with explosive presence-sacrifice effects. Threshold-heavy Ash/Gale build.
- **Teacher Warden:** Empowers Natives via knowledge tokens. Boosts Native stats and enables new Native behaviors. Mist/Root build.
- **Seducer Warden:** Converts and lures invaders into temporary allies. Void/Shadow build.
- **Weakener Warden:** Applies status effects (Weaken, Slow, Expose, Brittle) to let Natives do the killing. Void/Gale build.
- **Pusher Warden:** Creates powerful territory clusters, herds invaders into them, retaliates with massive area effects. Gale/Root build.
- **Seer Warden:** Sees invader arrivals one turn early (not which type, just where). Can preview fear actions or choose between multiple drawn fear cards. Mist/Shadow build.
- **Enticing Warden:** Marks Natives with entice tokens; two enticed Natives in one territory create a new, weaker Native that grows over a turn. Cooldown prevents abuse. Root/Mist build.

---

## 9. Run Structure

### 9.1 Overview

```
Realm 1               Realm 2               Realm 3
[Enc][Enc][Enc][Boss] [Enc][Enc][Enc][Boss] [Enc][Enc][Enc][Final Boss]
          ↑                       ↑
     Realm reward            Realm reward
   (full card draft,        (full card draft,
    Aspect, Weave restore)   Aspect, Weave restore)
```

Each Realm: 3 standard/elite encounters + 1 boss.

### 9.2 Zone Difficulty Progression

| Zone | Factions | Starting state | Territory layout |
|---|---|---|---|
| Realm 1 | 1 faction (Pale March) | All territories Clean | 3-2-1 pyramid |
| Realm 2 | 2 factions | Some territories pre-Tainted (3 pts) | 4-3-2-1 pyramid (10 territories) |
| Realm 3 | 2–3 factions | Some territories pre-Defiled (8 pts) | 4-3-2-1 pyramid + side route |

### 9.3 Between-Encounter Rewards

| Performance | Rewards |
|---|---|
| Clean | Card draft (1 of 3, any rarity) + Aspect upgrade (1 of 2) + Weave +3 |
| Weathered | Card draft OR Aspect (player chooses) + Weave +1 |
| Breach | Weave +1 only + Breach effect carries forward |

### 9.4 Between-Realm Rewards

- Card draft: 1 of 4 options (wider pool, includes Ancient-tier)
- Aspect upgrade: 1 of 3 options
- Weave restore: 4–6 (scales with Fear generated in Realm)
- Remove Corruption from 1 territory (points drop to 0 in that territory)

### 9.5 Meta Progression

Unlocks happen between runs:
- New Wardens (clear runs with current Warden)
- New Realm types (map shapes, faction mixes)
- New Aspects (appear in future runs' upgrade pools)
- Warden Aspects/Variants (alternate starting decks — similar to Spirit Island aspects)

---

## 10. Balance Reference

*Working targets — all subject to playtesting*

### 10.1 Hand Size and Deck Size

| Warden | Starting deck | Hand limit | Notes |
|---|---|---|---|
| The Root | 10 | 5 | Baseline. Dormancy keeps cards in deck (as dead draws) rather than removing. |
| The Ember | 8 | 5 | Smaller deck = faster cycling, more Rests, more rest-dissolves. Matches burst identity. |
| The Veil | 10 | 6 | Larger hand = more options per turn, slower cycling, fewer Rests. Matches control identity. |

**Refill model:** At the start of each Vigil, draw from deck until hand = hand limit (or deck empty). Unplayed cards stay in hand. Played tops → discard. Played bottoms → dissolved.

**Deck growth across a run:** Players draft 1 card per encounter reward (Clean/Weathered). Expect 1–4 new cards per Realm. By Realm 3, deck size is roughly 13–16.

### 10.2 Turn Economy (Refill + Rest-Dissolve)

**Three resource drains per encounter:**
- **Tops (discard):** Return on Rest. The cycling resource. Managed by hand limit and deck size.
- **Bottoms (dissolve):** Return between encounters (permanent on Boss). The sacrifice resource.
- **Rest-dissolve:** 1 random card removed per Rest (encounter-only on Standard/Elite; permanent on Boss). The stamina tax.

**Rest rate target (10-card deck, hand 5, playing 2 tops/turn):**

| Turn | Hand | Refill from deck | Deck remaining |
|------|------|-----------------|----------------|
| 1 | 5 | 0 (full) | 5 |
| 2 | 3 | 2 | 3 |
| 3 | 3 | 2 | 1 |
| 4 | 3 | 1 (short draw) | 0 |
| 5 | **Rest** | Shuffle 8 discard → deck. Dissolve 1 random. Deck: 7. Refill 5. | 2 |

**Result: 4 play turns per cycle, Rest on turn 5.** With 1 bottom played per cycle, Rest hits on turn 4 instead (3 play turns). Aggressive bottom play compresses the cycle, accelerating rest-dissolve. This is the self-balancing anti-frontload mechanic.

**Bottom budget per encounter (Standard, 6–7 Tides):**
- Starting deck: 10 cards, 10 bottoms available
- Conservative play: 1–2 bottoms. 2 Rests. End deck: 6–7 cards.
- Aggressive play: 3–4 bottoms. 2–3 Rests. End deck: 3–5 cards. Resolution turns are scraped-together.
- Boss: every bottom and every rest-dissolve is permanent. Players face a real choice between using power now vs. preserving their run deck.

**Element engine during Rest:** Carryover pool (decayed by −1 from last turn) persists. If above a threshold, the threshold fires during the Rest turn. This rewards building elements before Resting — a Rest turn with Root×4 carryover still places 1 free Presence.

### 10.3 Encounter Length

| Tier | Tides | Est. play turns | Est. Rests | Est. total player turns |
|------|-------|----------------|------------|------------------------|
| Standard | 5–7 | 4–6 | 1–2 | 6–8 |
| Elite | 8–10 | 6–8 | 2–3 | 9–11 |
| Boss | 12–15 | 9–12 | 3–4 | 13–16 |

### 10.4 Weave Economy

- Starting Weave: 20
- Target Weave at end of Realm 1: 14–17
- Target Weave at end of Realm 2: 8–14
- Target Weave at start of Realm 3: 6–12
- Sacred Heart hit: −3 to −5 per invader

### 10.5 Fear & Dread Economy

- Fear generation per Standard encounter: 8–12 (2–3 Fear Actions triggered at 5 each)
- Dread Level 1 → 2 transition (15 Fear total): expect mid-Realm 1
- Dread Level 2 → 3 transition (30 Fear total): expect mid-Realm 2
- Fear Actions per encounter average: 2–3 in Standard, 4–6 in Boss
- Dread upgrade timing: pushing past a Dread threshold upgrades all queued Fear Actions retroactively — incentivizes frontloading Fear generation to hit Dread 2 before queued actions reveal

---

## 11. Godot Architecture

### 11.1 Engine Version
Godot **4.6.1** — **.NET version** (C#). All architecture targets Godot 4 C# patterns.  
Use `.cs` files for all scripts. Do **not** mix GDScript and C# in the same project.  
Recommended IDE: **JetBrains Rider** or **VS Code** with the C# Dev Kit extension.  
Executable: `D:\Downloads\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe`

### 11.2 Project Structure

```
hollow_wardens/
├── project.godot
├── scenes/
│   ├── game/
│   │   ├── Game.tscn
│   │   ├── Encounter.tscn
│   │   └── RealmMap.tscn
│   ├── entities/
│   │   ├── Card.tscn
│   │   ├── InvaderUnit.tscn
│   │   ├── Territory.tscn
│   │   ├── PresenceToken.tscn
│   │   └── NativeUnit.tscn          ← NEW
│   ├── ui/
│   │   ├── Hand.tscn
│   │   ├── TideQueue.tscn
│   │   ├── WeaveBar.tscn
│   │   ├── FearCounter.tscn
│   │   ├── ElementTracker.tscn      ← NEW
│   │   ├── EncounterResult.tscn
│   │   └── RealmReward.tscn
│   └── menus/
│       ├── MainMenu.tscn
│       └── RunStart.tscn
├── scripts/
│   ├── core/
│   │   ├── GameState.cs
│   │   ├── EncounterManager.cs
│   │   ├── TurnManager.cs
│   │   ├── TideExecutor.cs
│   │   ├── RewardManager.cs
│   │   ├── ElementTracker.cs        ← NEW
│   │   └── FearActionDeck.cs        ← NEW
│   ├── data/
│   │   ├── CardData.cs              ← MODIFIED (add Elements[], rename DuskEffect)
│   │   ├── CardEffect.cs            ← MODIFIED (add new EffectTypes)
│   │   ├── InvaderData.cs
│   │   ├── InvaderActionCard.cs     ← NEW (invader activate action pool)
│   │   ├── FearActionData.cs        ← NEW
│   │   ├── TerritoryData.cs         ← MODIFIED (points-based corruption)
│   │   ├── EncounterData.cs         ← MODIFIED (pyramid layout, native count)
│   │   ├── WardenData.cs
│   │   └── BoardToken.cs            ← NEW (abstract base for board tokens)
│   ├── entities/
│   │   ├── Card.cs
│   │   ├── Deck.cs
│   │   ├── Hand.cs
│   │   ├── InvaderUnit.cs           ← MODIFIED (activate action card pool)
│   │   ├── NativeUnit.cs            ← NEW
│   │   ├── Territory.cs             ← MODIFIED (points corruption, token list)
│   │   └── PresenceToken.cs
│   ├── wardens/
│   │   ├── Warden.cs
│   │   ├── WardenRoot.cs            ← MODIFIED (dormancy in new bottom model)
│   │   ├── WardenEmber.cs
│   │   └── WardenVeil.cs
│   └── ui/
│       ├── HandUI.cs
│       ├── WeaveBarUI.cs
│       ├── FearCounterUI.cs
│       ├── ElementTrackerUI.cs      ← NEW
│       └── TideQueueUI.cs
├── resources/
│   ├── cards/
│   │   ├── root/                    ← 12 starting cards + 18 draft pool cards
│   │   └── shared/
│   ├── fear_actions/                ← NEW
│   │   ├── global/
│   │   └── pale_march/
│   ├── encounters/
│   ├── invaders/
│   └── wardens/
└── assets/
    ├── art/kenney/
    ├── audio/
    ├── fonts/
    └── hollow_wardens_theme.tres
```

### 11.3 Signal Architecture

New and modified signals:

```csharp
// ElementTracker.cs — NEW
[Signal] public delegate void ElementAddedEventHandler(Element element, int newTotal);
[Signal] public delegate void ElementThresholdReachedEventHandler(Element element, int threshold);
[Signal] public delegate void ElementsDecayedEventHandler();

// FearActionDeck.cs — NEW
[Signal] public delegate void FearActionQueuedEventHandler(FearActionData action);
[Signal] public delegate void FearActionRevealedEventHandler(FearActionData action);
[Signal] public delegate void DreadLevelAdvancedEventHandler(int newLevel);

// Territory — MODIFIED
[Signal] public delegate void CorruptionPointsChangedEventHandler(Territory territory, int newPoints, int level);
[Signal] public delegate void CorruptionLevelChangedEventHandler(Territory territory, int newLevel);

// NativeUnit — NEW
[Signal] public delegate void NativeDefeatedEventHandler(NativeUnit native, Territory territory);
[Signal] public delegate void NativeCounterAttackedEventHandler(NativeUnit native, InvaderUnit target, int damage);

// CardEngine / TurnManager — MODIFIED
// CardDissolved is now emitted when a bottom is played (not when explicit dissolve action)
[Signal] public delegate void BottomPlayedEventHandler(CardData card, EncounterData.EncounterTier tier);
// CardPermanentlyRemoved still emitted on Boss or double-dissolve (Root dormant)
```

### 11.4 Autoloads (Singletons)

- `GameState` — run state, Weave, Fear totals, Dread Level
- `EventBus` — global signal relay
- `ElementTracker` — NEW: current element pool, decay, threshold checking
- `FearActionDeck` — NEW: fear action pool management, level tracking

### 11.5 Typography & Art Assets

*(unchanged from v0.4 — see §9.5 in previous version)*

**Card.tscn node structure (updated for elements + new anatomy):**
```
PanelContainer          ← Kenney card frame
└── VBoxContainer
    ├── HBoxContainer   element icons      ← 1–3 small icons (16px each)
    ├── Label           card name          — Cinzel-Bold, 16px
    ├── TextureRect     card art           — placeholder ColorRect
    ├── HSeparator
    ├── Label           top text (vigil)   — IM Fell English, 12px
    ├── HSeparator
    └── Label           bottom text (dusk) — IM Fell English Italic + dissolve marker
```

### 11.6 Card Data Authoring (Revised Schema)

```json
{
  "warden": "root",
  "version": "2.0",
  "cards": [
    {
      "num": "001",
      "id": "root_001",
      "name": "Tendrils of Reclamation",
      "rarity": "dormant",
      "starting": true,
      "elements": ["Root", "Mist"],
      "top":    { "type": "ReduceCorruption", "value": 1, "range": 1, "desc": "Reduce Corruption by 1 point in range 1" },
      "bottom": { "type": "ReduceCorruption", "value": 3, "range": 2, "desc": "Reduce Corruption by 3 points in range 2 and restore 1 Weave" },
      "design_note": "Starter cleanse card. Top is safe and repeatable. Bottom is worth dissolving when a territory is close to Tainted and you need Weave back."
    }
  ]
}
```

**Schema changes from v1.0:**
- `"vigil"` → `"top"` (renamed)
- `"dusk"` → `"bottom"` (renamed, this IS the dissolve action)
- `"dissolve"` → **removed** (bottom IS the dissolve)
- Added `"rarity"`: `"dormant"` | `"awakened"` | `"ancient"`
- Added `"starting"`: `true` for starting deck cards, `false` for draft pool only
- Added `"elements"`: array of element names (1–3)

**Valid EffectType strings (updated):**

| String | Notes |
|---|---|
| `PlacePresence` | Requires target territory, range applies |
| `MovePresence` | Stubbed Phase 5 |
| `GenerateFear` | No target needed |
| `ReduceCorruption` | Value = corruption POINTS removed |
| `Purify` | Remove one full corruption level |
| `DamageInvaders` | Requires target territory, range applies |
| `PushInvaders` | Push invaders toward spawn |
| `RoutInvaders` | Stubbed Phase 5 |
| `RestoreWeave` | No target needed |
| `PredictTide` | Show next Tide spawns — stubbed |
| `Conditional` | Requires EffectCondition |
| `Custom` | Warden-specific |
| `AwakeDormant` | Root only. Value=0 = all dormant. |
| `DamageNatives` | NEW — invader side only |
| `ShieldNatives` | NEW — protect natives in range |
| `BoostNatives` | NEW — increase native damage this turn |
| `HealNatives` | NEW — restore HP to natives in range |
| `WeakenInvaders` | NEW — reduce invader damage/action power |
| `SlowInvaders` | NEW — reduce invader movement |
| `ExposeInvaders` | NEW — invaders take increased damage |
| `BrittleInvaders` | NEW — reduce invader Shield value |

### 11.7 Seeded Randomness & Action Log

All randomness in an encounter flows through a single **`GameRandom`** instance seeded at encounter start. The seed is logged alongside the action log and is exportable for replay.

**Seed:** A single integer, set when the encounter begins. All RNG calls (wave composition, cadence draws, rest-dissolve selection) use this shared instance. Same seed + same actions = identical game state.

**Action Log:** Every player action is recorded with:
- Turn number and phase (Vigil / Dusk)
- Action type (PlayTop, PlayBottom, Rest, Confirm)
- Card ID (where applicable)
- Target territory (where applicable)

The log is exportable as a compact string: `seed:actions`. This string fully describes the game state from encounter start — sufficient for bug reporting and future undo support (truncate log to N entries, replay from seed).

**Export:** Press **P** to print the full seed + action sequence to the output console.

**Future: Undo support.** The action log is the foundation. Undo = truncate the log by 1 entry, replay from seed. Not V1 scope, but the architecture supports it.

### 11.8 Debug Overlay

A toggleable full-screen event log for playtesting. Press **D** to show/hide.

**Logged events (color-coded by type):**
- Card plays (top and bottom)
- Element changes and threshold triggers
- Fear generation and Fear Action reveals
- Tide steps (Activate, Advance, Arrive, Escalate)
- Combat: invader damage to Natives, Native counter-attacks
- Territory Corruption changes
- Targeting mode entered/resolved

**Display:** Maximum 200 entries, auto-scrolling to the latest. Events are color-coded so the designer can visually distinguish card play, combat, element activity, and system events at a glance.

**Purpose:** Playtest debugging and design validation. Lets the designer see exactly what the system is doing on each step — essential for verifying that damage models, tide ramp-up, and threshold resolution are behaving as intended.

---

## 12. Class Definitions

### CardData (Resource) — MODIFIED
```csharp
[GlobalClass]
public partial class CardData : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string CardName { get; set; }
    [Export] public string WardenId { get; set; }
    [Export] public CardEffect TopEffect { get; set; }      // Vigil — goes to discard
    [Export] public CardEffect BottomEffect { get; set; }   // Dusk — dissolves when played
    [Export] public Element[] Elements { get; set; }        // 1–3 elements
    [Export] public CardRarity Rarity { get; set; }
    [Export] public bool IsStartingCard { get; set; }
    [Export] public bool IsDormant { get; set; } = false;   // Root runtime state
    [Export] public Texture2D ArtTexture { get; set; }
    // Localization keys
    [Export] public string CardNameKey { get; set; }
    [Export] public string TopDescKey { get; set; }
    [Export] public string BottomDescKey { get; set; }
}

public enum CardRarity { Dormant, Awakened, Ancient }
public enum Element { Root, Mist, Shadow, Ash, Gale, Void }
```

### ElementTracker (Singleton) — NEW
```csharp
public partial class ElementTracker : Node
{
    public static ElementTracker Instance { get; private set; }

    private Dictionary<Element, int> _pool = new();

    [Signal] public delegate void ElementThresholdReachedEventHandler(Element element, int threshold);
    [Signal] public delegate void ElementsDecayedEventHandler();

    public void AddElements(Element[] elements, int multiplier = 1)
    {
        foreach (var e in elements)
        {
            _pool[e] = _pool.GetValueOrDefault(e) + multiplier;
            CheckThresholds(e);
        }
    }

    public void DecayAtTurnEnd()
    {
        foreach (var key in _pool.Keys.ToList())
            _pool[key] = _pool[key] / 2; // halve, round down
        EmitSignal(SignalName.ElementsDecayed);
    }

    private void CheckThresholds(Element e)
    {
        int val = _pool.GetValueOrDefault(e);
        foreach (int threshold in new[] { 2, 3, 5 })
            if (val == threshold)
                EmitSignal(SignalName.ElementThresholdReached, (int)e, threshold);
    }
}
```

### TerritoryState — MODIFIED
```csharp
public class TerritoryState
{
    public string Id { get; set; }
    public int CorruptionPoints { get; set; } = 0;  // Raw points
    public int CorruptionLevel => CorruptionPoints switch
    {
        < 3 => 0,                    // Clean
        < 8 => 1,                    // Tainted (3–7 pts)
        < 15 => 2,                   // Defiled (8–14 pts)
        _ => 3                       // Desecrated (15+ pts)
    };
    public int PresenceCount { get; set; } = 0;
    public List<InvaderUnit> InvaderUnits { get; set; } = new();
    public List<NativeUnit> NativeUnits { get; set; } = new();      // NEW
    public List<BoardToken> Tokens { get; set; } = new();           // NEW
    public bool IsSacredSite { get; set; } = false;
    public bool IsEntryPoint { get; set; } = false;
    public bool IsDefended => PresenceCount > 0;

    public void AddCorruption(int points) { CorruptionPoints += points; }
    public void ReduceCorruption(int points) { CorruptionPoints = Math.Max(0, CorruptionPoints - points); }
    public void PurifyLevel() { /* drop one full level */ }
}
```

### NativeUnit — NEW
```csharp
public partial class NativeUnit : Node
{
    public int MaxHp { get; set; } = 2;
    public int CurrentHp { get; set; }
    public int Damage { get; set; } = 3;
    public bool IsShielded { get; set; } = false;
    public int ShieldValue { get; set; } = 0;

    [Signal] public delegate void NativeDefeatedEventHandler();

    public void TakeDamage(int amount)
    {
        CurrentHp -= amount;
        if (CurrentHp <= 0) EmitSignal(SignalName.NativeDefeated);
    }

    public int CounterAttack() => CurrentHp > 0 ? Damage : 0;
}
```

### FearActionDeck — NEW
```csharp
public partial class FearActionDeck : Node
{
    public static FearActionDeck Instance { get; private set; }

    public int DreadLevel { get; private set; } = 1;
    public int TotalFearGenerated { get; private set; } = 0;

    private List<FearActionData>[] _levelPools;  // indexed by DreadLevel - 1
    private Queue<FearActionData> _revealQueue = new();

    [Signal] public delegate void FearActionQueuedEventHandler(FearActionData action);
    [Signal] public delegate void FearActionRevealedEventHandler(FearActionData action);
    [Signal] public delegate void DreadLevelAdvancedEventHandler(int newLevel);

    public void OnFearGenerated(int amount)
    {
        TotalFearGenerated += amount;
        CheckLevelAdvance();
        // Every 5 fear spent → queue an action
    }

    public void RevealQueuedActions()  // called at start of Tide, before Spawn
    {
        while (_revealQueue.Count > 0)
        {
            var action = _revealQueue.Dequeue();
            EmitSignal(SignalName.FearActionRevealed, action);
            // Execute action
        }
    }

    private void CheckLevelAdvance()
    {
        int newLevel = TotalFearGenerated / 15 + 1;
        if (newLevel > DreadLevel && newLevel <= 4)
        {
            DreadLevel = newLevel;
            EmitSignal(SignalName.DreadLevelAdvanced, DreadLevel);
        }
    }
}
```

### TerritoryGraph — MODIFIED (Pyramid)
```csharp
public static class TerritoryGraph
{
    // V1: 3-2-1 pyramid
    private static readonly Dictionary<string, List<string>> _adjacency = new()
    {
        ["A1"] = new() { "A2", "M1" },
        ["A2"] = new() { "A1", "A3", "M1", "M2" },
        ["A3"] = new() { "A2", "M2" },
        ["M1"] = new() { "A1", "A2", "M2", "I1" },
        ["M2"] = new() { "A2", "A3", "M1", "I1" },
        ["I1"] = new() { "M1", "M2" },  // Can attack Heart from here
    };

    public static IReadOnlyList<string> GetNeighbors(string territoryId)
        => _adjacency.TryGetValue(territoryId, out var neighbors) ? neighbors : Array.Empty<string>();

    public static bool IsAdjacent(string a, string b)
        => _adjacency.TryGetValue(a, out var n) && n.Contains(b);

    public static bool CanAttackHeart(string territoryId) => territoryId == "I1";
}
```

---

## 13. V1 Scope & Build Order

### V1 Target
One Warden (The Root), 10-card starting deck + 18-card draft pool, 3-2-1 pyramid territory layout, The Pale March only, no meta-progression. Core loop must feel good before expanding.

### Build Order

**Phase 1 — Data Layer** ✅ Complete  
**Phase 2 — Turn Engine** ✅ Complete  
**Phase 3 — Card Engine** ✅ Complete  
**Phase 4 — Encounter Loop** ✅ Complete  
**Phase 5 — Root Warden** ✅ Complete (111/111 tests)

**Phase 5.5 — Architecture Migration** ✅ Complete
- [x] Update `CardData.cs`: add `Element[]`, `CardRarity`, `IsStartingCard`; rename `DuskEffect` → `BottomEffect`; remove `DissolveEffect`
- [x] Update `CardEffect.cs`: add new EffectType values (native interactions, status effects, element threshold effects)
- [x] Update `TurnManager.cs`: refill draw model (draw to hand limit each Vigil); bottom play = dissolve; rest-dissolve (remove 1 random card per Rest)
- [x] Replace `TerritoryGraph` with pyramid (3-2-1)
- [x] Update `TerritoryState`: `CorruptionPoints` + level thresholds (3/8/15)
- [x] Add `ElementTracker` singleton: 6 elements, reduce-by-1 decay, threshold checks at 4/7/11, once-per-tier-per-turn triggering, phase-flexible resolution
- [x] Add `NativeUnit` entity
- [x] Add `FearActionDeck` singleton + `FearActionData` resource
- [x] Add `BoardToken` abstract base
- [x] Add `InvaderActionCard` resource (pool tag, activate effect, advance modifier)
- [x] Add `CadenceManager` (rule-based: max_painful_streak + easy_frequency, with hand-authored pattern override)
- [x] Add `SpawnWaveOption` (weighted composition arrays per wave)
- [x] Add 4 Pale March unit-type subclasses: Marcher, Ironclad, Outrider, Pioneer
- [x] Rewrite `TideExecutor`: Fear Actions → Activate → Natives counter-attack → Advance → Arrive → Escalate → Preview
- [x] Rewrite Root cards JSON (v2.0 schema, 10 starting + 18 draft — move 2 cards from starting to draft)
- [x] Update `generate-cards.py` for new schema
- [x] Update test suite (many existing tests will need rewriting — see CLAUDE-migration.md)

**Phase 6 — UI (Functional, Not Pretty)** ← CURRENT
- [ ] `Card.tscn` — updated node structure (element icons + top/bottom labels)
- [ ] Hand display
- [ ] Territory grid display (pyramid layout)
- [ ] Element tracker HUD (6 element counters)
- [ ] Weave bar + Fear counter + Dread Level indicator
- [ ] Tide preview
- [ ] Phase indicator
- [ ] Fear action reveal animation (hidden → revealed at Tide start)
- [ ] Reward screen

**Phase 7 — First Playtest**
Run through 3 encounters with The Root. Validate:
- Is bottom-as-dissolve creating real decisions?
- Are element thresholds triggering often enough to matter?
- Is the pyramid map creating interesting spatial decisions?
- Is Native counter-attack meaningful?
- Does the refill model create visible deck depletion and natural Rest timing?
- Does rest-dissolve feel like a fair tax or too punishing?
- Is 10 cards the right starting deck size for 4 play turns before Rest?
- Does the two-pool action cadence create the right pain/relief rhythm?
- Are unit-type modifiers readable at a glance?

---

## 14. Open Design Questions

1. ~~**Element threshold effects**~~ — **RESOLVED (D20).** Universal thresholds at 4/7/11. See §6.3 for full table.

2. ~~**Resolution turn count**~~ — **RESOLVED.** Standard: 2 turns. Elite: 3 turns. Boss: 1 turn. See §3.2.

3. ~~**Corruption persistence**~~ — **CONFIRMED.** Level 2 persists as 3 points (Level 1) at next encounter start. Territory is fragile — one Ravage pushes to 5, two Ravages to 7 (one point from Defiled again). Intended as a scar, not a death sentence.

4. ~~**Bottom budget feel**~~ — **RESOLVED (D22).** Refill draw model (draw to hand limit each turn, not draw-5). Starting deck 10 cards (down from 12). Rest-dissolve removes 1 random card per Rest (encounter-only on Standard/Elite, permanent on Boss). Self-balances against frontloading: more bottoms = faster deck depletion = earlier Rest = more rest-dissolve tax. See §2.3, §2.4, §10.2.

5. ~~**Fear Action reveal UX**~~ — **RESOLVED.** Face-down cards in Tide queue area, flip face-up one at a time at Tide start. Dread track displayed as a bar with threshold markers at 15/30/45 showing progress to next Dread Level. See §4.6.

6. ~~**Native spawn count**~~ — **RESOLVED (D23).** Default: 0–1 on A-row, 2 on M/I-row (~6 total). Overridable per encounter via EncounterData. Between-encounter events can also modify Native counts. See §7.3.

7. ~~**Invader activate action deck**~~ — **RESOLVED (D21).** Two-pool system (Painful/Easy) with rule-based cadence, unit-type modifiers, randomized wave composition. See §4.4 and §4.7.

8. ~~**Element decay rate**~~ — **RESOLVED (D20).** Reduce by 1 per turn (not halving). Creates engine-building: consistent element play builds a rising floor. See §6.4 for model and math.

9. ~~**Fear Level vs Fear actions**~~ — **RESOLVED.** Renamed to **Dread Level** (Dread 1–4). Fear = the resource you generate and spend. Dread = the escalation track (total Fear generated, advances every 15). When Dread advances, all queued Fear Actions retroactively upgrade to the new pool. See §4.6.

10. ~~**Invader action preview**~~ — **RESOLVED (D21).** Action card revealed at end of previous Tide (alongside arrival locations). Player enters Vigil with full action knowledge. Arrival unit composition remains hidden until Arrive step. See §4.4 preview timing.

---

## 15. Localization System

*(unchanged from v0.4 — schema same, new keys needed for elements, natives, fear actions)*

New key patterns:
```
ELEMENT_ROOT / MIST / SHADOW / ASH / GALE / VOID
ELEMENT_THRESHOLD_{ELEMENT}_{N}   — e.g. ELEMENT_THRESHOLD_ROOT_2
NATIVE_COUNTER_ATTACK
NATIVE_DEFEATED
DREAD_LEVEL_{N}
FEAR_ACTION_{ID}_DESC
INVADER_ACTION_{FACTION}_{NUM}_DESC
```

---

## 16. Input Actions

*(unchanged from v0.4)*

Same 9 custom actions. Territory grid navigation maps to pyramid layout:
- Row navigation (`ui_navigate_up/down`): A-row ↔ M-row ↔ I-row
- Column navigation (`ui_navigate_left/right`): within each row
- `game_confirm`: select focused territory
