# Hollow Wardens — Core Loop Redesign (Living Document)

> This is a LIVING DESIGN DOCUMENT. Not a spec. We iterate here until the
> design feels right, THEN write the spec. Version this document.
>
> Revision: 1 — Initial pairing system + card lifecycle

---

## The Problem

Decisions aren't meaningful. The current Vigil (2 tops) → Tide → Dusk
(1 bottom) structure has no tension:
- No resource constraint on plays (you always play 2+1)
- No cost to playing (tops are free, bottoms dissolve but there's no urgency)
- No timing decisions (everything resolves in fixed order)
- Board is too small for spatial reasoning
- Elements accumulate passively — player doesn't control threshold timing

## The Reference Point

**Gloomhaven's card lifecycle:**
1. Pick 2 cards from hand → play top of one, bottom of other
2. Both cards go to **discard** (not lost — recoverable)
3. When hand is empty (or you choose early): **Rest** — recover all
   discarded cards EXCEPT one, which is **permanently lost** (your choice)
4. Some powerful effects are tagged **LOSS** — card goes straight to lost
   pile, never enters discard (can't be recovered)
5. At any time: **burn a card from hand** to negate ALL damage from one
   source. Card goes to lost pile. Your hand IS your health buffer.

This creates a natural timer: 10 cards = 5 pairs → rest (lose 1) → 4 pairs
→ rest (lose 1) → 3 pairs → rest (lose 1)... The encounter gets harder
as your options shrink. Brilliant.

---

## Proposed Card Lifecycle for Hollow Wardens

### Turn Structure

Each turn:
1. **PLAN:** Pick 2 cards from hand. Assign one as TOP, one as BOTTOM.
2. **FAST PHASE:** If the TOP card is tagged FAST, resolve it now.
3. **TIDE:** Invaders act (Ravage, March, Arrive — same as current).
4. **SLOW PHASE:** If the TOP card is tagged SLOW, resolve it now.
5. **DUSK:** Resolve the BOTTOM card.
6. **ELEMENTS:** Both cards contribute elements. Top ×1, Bottom ×2.
   Thresholds check and fire. Decay applies.
7. **CLEANUP:** Both cards go to their respective piles (see below).

### Card Destinations After Play

| Played as | Where it goes |
|-----------|--------------|
| **Top** | → Top discard pile (always safe, recovered on rest for free) |
| **Bottom** | → Bottom discard pile (AT RISK — 2 random cards dissolved on rest) |

**The core tension of every turn:** tops are safe, bottoms are risky. Every
card assignment is a bet. "Am I OK potentially losing this card? Its bottom
effect is strong, but if the rest kills it randomly, I'm stuck without it."

### Rest Mechanic

When your hand is empty (or you choose to rest early):

**Rest takes your entire turn. No card play. Tide still happens.**

Then:
1. **Recover ALL top-discard cards to hand** (free, always safe)
2. **2 RANDOM cards from the bottom-discard pile are dissolved** (gone for
   the encounter). Player sees which 2 were picked.
3. **For each dissolved card: pay 2 weave to REROLL** (that card is saved,
   but a NEW random card from the remaining bottom-discard pile is dissolved
   instead). You can reroll multiple times but each reroll costs 2 weave and
   the replacement is random — you might get an even worse result.
4. **Total cards dissolved is ALWAYS 2.** No way to save both. Weave buys
   lottery tickets, not guaranteed saves.
5. **Remaining bottom-discard cards return to hand**

### Why Reroll (Not Save) Works

**Always losing 2 maintains stamina pressure.** The encounter clock ticks
at exactly the same rate regardless of weave. Rich players don't outlast
poor players — they just control WHICH cards die.

**Rerolling is gambling.** You pay 2 weave and a random replacement is
picked. Your best card might get picked again. Or a different valuable
card might die instead. The more bottoms in the pile, the better your
odds — another reason to push for full 5-turn cycles.

**Weave becomes decision currency, not just health.** "I'm at 16 weave.
Deep Roots was randomly dissolved. Do I pay 2 to reroll? The other 3
bottoms are Grasping Roots, Entangling Vines, and Healing Earth. There's
a 33% chance of losing Grasping Roots (bad), 33% Entangling Vines (OK),
33% Healing Earth (fine). Worth the gamble."

### Rest Timing (unchanged — still a sliding scale)

| Rest after N turns | Bottoms at risk | Cards lost (no saves) | Saves cost |
|-------------------|----------------|----------------------|------------|
| 5 turns | 5 | 2 (40% rate) | 0-4 weave |
| 4 turns | 4 | 2 (50% rate) | 0-4 weave |
| 3 turns | 3 | 2 (67% rate) | 0-4 weave |
| 2 turns | 2 | 2 (100% rate) | 0-4 weave |

Early rest is still punishing — losing 2 of 3 bottoms is devastating
regardless of buy-backs.

### Damage Soak (unchanged)

When the heart takes damage (invaders at I1):

**Discard a card from HAND to prevent up to 3 heart damage.**

Soaked card goes to top discard (safe on rest). But it costs a card from
hand this cycle = fewer pairs before rest.

### Card Loss Summary

There is ONE way cards leave your hand permanently during an encounter:

- **Random rest dissolution** — 2 random bottoms die per rest, buy-back for
  2 weave each

Cards that are dissolved are gone for the encounter. They return to your
full deck for the next encounter (progression cards are permanent).

### On BURN (Deferred)

BURN was originally proposed as a second loss mechanic: powerful bottoms
that guarantee dissolution on play. **Cutting it for now.** The random
rest loss already creates sufficient bottom tension. If playtesting shows
bottoms feel too safe, we can add BURN to 1-2 "ultimate" cards per warden
as a targeted fix.

The design space for BURN is: cards where you'd WANT to guarantee the loss
to avoid the randomness. "I'd rather burn Conflagration on purpose for DI×6
than risk losing Deep Roots randomly on rest." That's interesting but adds
cognitive overhead. Revisit after prototyping the base system.

---

## Card Design for Pairing

Every card needs BOTH a top and a bottom that are independently interesting.
The pairing decision comes from: "which card's top do I want more, and which
card's bottom am I willing to use?"

### Design Principles

1. **Tops and bottoms should want different things.** A card with DI top and
   DI bottom has no pairing tension — you always pair it with something else
   for both effects. Better: DI top + PlacePresence bottom.

2. **Tops should be weaker than bottoms.** This creates the "save for later
   vs use now" tension. If a top is stronger than its bottom, you always
   play it as top and there's no decision.

3. **BURN bottoms should be 2-3× the normal bottom value.** The cost is real
   (card is dissolved), so the effect must be worth it.

4. **Element pairing matters.** Cards with 2 of the same element are
   "threshold builders." Cards with mixed elements are "versatile." Pairing
   two threshold builders of the same element rushes T2/T3.

5. **Range on tops vs bottoms.** Tops might have shorter range (you need
   presence nearby). Bottoms might have longer range (the extra power
   compensates for positional constraints).

### Root — Redesigned 10-Card Hand

| # | Name | Elements | Top (fast/slow) | Bottom | Pairing Role |
|---|------|----------|-----------------|--------|-------------|
| 1 | Deep Roots | Root, Root | SLOW: PlacePresence ×2 r1 | PlacePresence ×4 r2 | Presence builder |
| 2 | Tendrils of Reclamation | Root, Mist | FAST: ReduceCorruption ×2 r1 | ReduceCorruption ×4 r1 | Cleanse |
| 3 | Grasping Roots | Root, Shadow | SLOW: DamageInvaders ×2 r1 | DamageInvaders ×5 r1 | Damage |
| 4 | Healing Earth | Root, Root | SLOW: RestoreWeave ×2 | RestoreWeave ×5 | Sustain |
| 5 | The Forest Remembers | Mist, Mist | FAST: GenerateFear ×2 | GenerateFear ×6 | Fear engine |
| 6 | Earthen Mending | Root, Mist | FAST: ReduceCorruption ×1 r1 | ReduceCorruption ×6 r2 | Deep cleanse |
| 7 | Stir the Sleeping | Mist, Shadow | FAST: SpawnNatives ×1 | SpawnNatives ×3 + MoveNatives ×2 | Native army |
| 8 | Entangling Vines | Shadow, Root | SLOW: SlowInvaders ×2 r1 | SlowInvaders ×3 r2 + DamageInvaders ×1 r2 | Control |
| 9 | Ancient Ward | Shadow, Mist | FAST: ShieldNatives ×2 | ShieldNatives ×4 + GenerateFear ×3 | Defense |
| 10 | Reclaim the Wild | Root, Root, Shadow | SLOW: DamageInvaders ×3 r1 + ReduceCorruption ×1 | DamageInvaders ×6 r2 + ReduceCorruption ×3 r2 | Finisher |

**Bottoms are 2-3× top values.** Every bottom is worth playing — but every
bottom risks that card on rest. The stronger the bottom, the more painful
the random loss.

**Cards you DREAD losing to random rest dissolution:**
- Deep Roots (your main presence card)
- Reclaim the Wild (your finisher — best damage + cleanse combo)
- The Forest Remembers (your fear engine)

**Cards you're "OK" losing:**
- Healing Earth (sustain — less needed if board is clean)
- Entangling Vines (control — situational)

**This gradient of card value creates the rest tension.** You played all 5
as bottoms. 2 die randomly. You PRAY it's not Deep Roots and Reclaim.

### Ember — Redesigned 8-Card Hand

| # | Name | Elements | Top (fast/slow) | Bottom (normal/BURN) | Pairing Role |
|---|------|----------|-----------------|---------------------|-------------|
| 1 | Flame Burst | Ash, Ash | FAST: DamageInvaders ×2 r1 | DamageInvaders ×4 r1 | Core damage |
| 2 | Kindle | Ash, Gale | SLOW: PlacePresence ×1 r1 | PlacePresence ×2 r2 | Positioning |
| 3 | Burning Ground | Ash, Shadow | SLOW: DamageInvaders ×2 r0 (all in territory) | BURN: DamageInvaders ×3 r0 (all) + AddCorruption ×2 | AoE + self-corruption |
| 4 | Smoke Screen | Shadow, Gale | FAST: GenerateFear ×2 | GenerateFear ×5 | Fear |
| 5 | Stoke the Fire | Ash, Ash | FAST: ReduceCorruption ×2 r1 | BURN: ReduceCorruption ×5 r1 | Cleanse burst |
| 6 | Ember Spread | Ash, Shadow | SLOW: PlacePresence ×1 | BURN: PlacePresence ×3 r2 + AddCorruption ×1 (each territory) | Risky expansion |
| 7 | Heat Shimmer | Ash, Gale | FAST: RestoreWeave ×1 | RestoreWeave ×3 + GenerateFear ×2 | Sustain |
| 8 | Conflagration | Ash, Ash, Shadow | SLOW: DamageInvaders ×3 r1 | BURN: DamageInvaders ×6 r2 | Finisher |

**Ember's identity in pairing:** Ember's BURN bottoms are extremely powerful
but also self-destructive (AddCorruption). Ember WANTS to burn cards — each
burn feeds Scorched Earth (resolution damage = total corruption). But burning
cards means fewer turns before rest, and Ember's 8-card hand is already tight.

Ember's death spiral is corruption: burn cards → gain corruption → fewer
cards → can't cleanse → more corruption → Scorched Earth is massive but
your hand is empty.

---

## How the Board Changes

### Territory Terrain — Dynamic, Not Static

**Most territories start as Plains (no special effect).** Only 10-20% of
the board has terrain at encounter start. Terrain CHANGES during the game
based on player actions, invader actions, corruption, and events.

### Terrain Types

Terrain affects EVERYONE — player AND invaders. Every terrain is a trade-off.

| Terrain | Player bonus | Invader bonus | Transforms when |
|---------|-------------|---------------|-----------------|
| Plains | None | None | Default state |
| Forest | All damage effects +1 | Ravage corruption +1 | Burns to Scorched at corruption L2 |
| Mountain | Fear generation +2 | Invader counter-attacks deal +1 to natives | Crumbles to Ruins at corruption L3 |
| Wetland | Corruption thresholds +2 (slower to corrupt) | Invaders heal 1 HP on Rest tides | Dries to Plains if adjacent to Scorched |
| Sacred | Can't be corrupted past L1 | Invaders that Settle here desecrate it (→ Blighted) | Blighted if invaders Settle |
| Scorched | Invaders entering take 2 damage | Natives can't spawn; presence costs +1 | Recovers to Plains after 3 clean tides |
| Blighted | — (no player bonus) | +1 corruption per tide automatically; all card effects -1 | Returns to Plains when cleansed to 0 |
| Ruins | Draft cards played here: effect ×1.5 | Invaders resting here gain +2 HP | Can be Terraformed by card effects |
| Fertile | Natives: +1 HP, +1 damage | Invaders: +1 HP on arrival | Trampled to Plains when 3+ invaders present |

**Why trade-offs matter:** Forest is the best damage territory, but invaders
also hit harder there. Do you WANT to fight in the Forest? If you're Root
with DI×2 (+1 Forest = DI×3), yes. If invaders are Ravaging (2 corruption
+1 Forest = 3 corruption), suddenly the Forest is dangerous. The SAME
terrain is good and bad depending on the situation.

**Mountain is a fear engine** but natives die faster there (invaders deal
+1 to counter-attacks). Building a native army on a Mountain is risky —
they take extra damage. But the fear bonus is huge.

**Wetland is the safe zone** — corruption builds slowly. But invaders heal
here on Rest tides, so they're harder to kill. Do you let invaders camp
in the Wetland (they heal but don't corrupt fast) or push them into Plains
(no healing but faster corruption)?

### Dynamic Terrain Rules

Terrain changes happen automatically based on game state — no special code
per terrain type, just data-driven triggers checked at Tide end:

**Corruption-driven:**
- Forest + corruption L2+ → Scorched (the corruption burns the forest)
- Any territory at corruption L3 → Blighted (the land itself is sick)
- Blighted + corruption cleansed to 0 → Plains (healed but scarred)
- Scorched + 3 tides at corruption 0 → Plains (slowly recovers)

**Invader-driven:**
- Invaders Settle on Sacred → Blighted (desecration)
- 3+ invaders in Fertile territory → Plains (trampled)
- Pioneer infrastructure on any terrain → terrain preserved but Ravage +1

**Player-driven (via card effects):**
- Terraform verb: change terrain type (warden-specific)
- Powerful effects can create Sacred ground temporarily
- ReduceCorruption on Scorched at 0 corruption: starts recovery timer

**Event-driven (between encounters):**
- "Monsoon Season" — 2 random Plains become Wetland
- "The Pale March Burns" — 2 random Forests become Scorched
- "Sacred Awakening" — 1 random Plains becomes Sacred
- "Blight Spreads" — all territories adjacent to Blighted gain +2 corruption

**This means the board TELLS A STORY.** Encounter 1: a Forest gets corrupted
to L2, turns Scorched. Encounter 2: the Scorched territory recovers to
Plains but the adjacent Wetland dried out. By encounter 3 the board looks
completely different from where it started. Each run's map evolves uniquely.

### Terrain in Data

```json
{
    "id": "A1",
    "row": "arrival",
    "terrain": "plains",
    "terrain_timer": null,
    "adjacency": ["A2", "M1"]
}
```

`terrain_timer` tracks recovery states (Scorched → Plains in 3 tides, etc.).
All terrain transition rules are data-driven:

```json
"terrain_transitions": [
    { "from": "forest", "trigger": "corruption_level_2", "to": "scorched" },
    { "from": "any", "trigger": "corruption_level_3", "to": "blighted" },
    { "from": "blighted", "trigger": "corruption_zero", "to": "plains" },
    { "from": "scorched", "trigger": "no_corruption_3_tides", "to": "plains" },
    { "from": "sacred", "trigger": "invader_settle", "to": "blighted" },
    { "from": "fertile", "trigger": "invaders_3_plus", "to": "plains" }
]
```

### Board Terrain Presets

Each encounter defines initial terrain. Same layout, different presets:

| Preset | Distribution | Encounter feel |
|--------|-------------|---------------|
| `all_plains` | 100% Plains | Blank canvas — terrain emerges from play |
| `standard_mixed` | 2 Forest, 1 Mountain, rest Plains | Baseline variety |
| `frontier_wild` | 2 Forest, 1 Wetland, 1 Sacred (heart), rest Plains | Lush and fragile |
| `blighted_land` | 2 Blighted, 1 Scorched, rest Plains | Post-corruption — hard start |
| `sacred_grove` | 2 Sacred, 2 Forest, rest Plains | Easy but fragile if desecrated |

### Push and Pull (Spatial Verbs)

**Push:** Move invaders AWAY from a territory. Player chooses which adjacent
territory each invader moves to. Can split invaders across different
neighbors. Defensive — "clear this territory."

**Pull:** Gather invaders FROM adjacent territories into one territory.
All invaders in neighboring territories move to the pull target. Offensive
— "stack them up for an AoE."

**Why pull is powerful:**
- Pull 4 invaders into A1 → CorruptionDetonate hits all 4
- Pull into territory with 3 natives → counter-attack hits all pulled invaders
- Pull into Burning territory → each pulled invader takes 2 arrival damage
- Pull invaders OFF the heart → buy a turn before Ravage
- Pull + DamageAll = efficient multi-kill

**Push vs Pull identity:**
- Push = defensive, reactive ("get them away from the heart")
- Pull = offensive, setup ("stack them up for the kill")
- Root prefers push (control, keep invaders away from presence network)
- Ember prefers pull (stack into corrupted territory, detonate)

### Bigger Boards for Real Spatial Decisions

Standard encounter: 3-2-1 board (6 territories) with varied terrain.
Siege encounter: 4-3-2-1 board (10 territories).
Elite encounter: 3-2-2-1 twin peaks with split paths.

With dynamic terrain, each encounter's identity is: board shape + initial
terrain preset + wave composition + escalation schedule. "Pale March Siege"
is "10 territories, standard_mixed terrain, Ironclad-heavy waves, 8 tides"
— and by tide 4 the Forests might be Scorched and the board looks completely
different.

---

## Stamina Math

See "Rest Mechanic" section above for full stamina calculations per warden.

**Summary:**
- Root (10 cards): rests once per 8-tide encounter. Comfortable.
- Ember (8 cards): barely survives 8 tides with 1 rest. Tight.
- Damage soaks accelerate exhaustion (card from hand = fewer pairs this cycle).
- The "lose 2 random bottoms on rest" mechanic means every bottom assignment is a gamble.

---

## Roguelike Progression (Revised)

Between encounters, your hand doesn't grow. It transforms.

### Progression Actions (pick 1-2 based on reward tier)

1. **Replace a card:** Swap one card for a draft card. Hand size stays same.
   The new card might have different elements, different top/bottom split,
   different BURN status. This changes your pairing options.

2. **Upgrade a card:** Apply a pipe upgrade (value bump, range bump, add
   element, toggle normal→BURN or BURN→normal). The upgraded card plays
   the same role but better.

3. **Upgrade a passive:** Make your passive abilities stronger (same as D39).

4. **Remove a card:** Hand size shrinks by 1. Dangerous but powerful — fewer
   cards means every pair is better (no weak filler). But you rest sooner.
   Deck thinning is a high-risk, high-reward strategy.

5. **Add a card (RARE):** Hand size +1. Major reward — only from Tier 1
   wins or special events. More cards = more pairs per cycle = more stamina.

### BURN Toggle as Upgrade

One upgrade type: toggle a normal bottom to BURN (with increased value) or
a BURN bottom to normal (with decreased value).

"Flame Burst bottom is DamageInvaders ×4 (normal). Upgrade: convert to BURN,
becomes DamageInvaders ×7. More damage, but playing it costs the card."

This is a real choice. More power vs more stamina.

### Event Integration

Events work the same as before (D41) but with pairing-aware options:

- "The Whispering Grove: (A) Gain a BURN card with a powerful bottom. (B)
  Convert one of your BURN bottoms to normal (lose power, gain stamina)."
- "The Ash Crucible: Sacrifice cards until you reach 8 Ash. Reward: one of
  your cards gains a second element of your choice."

---

## Open Questions

### Q1: Should there be an initiative system?

In Gloomhaven, each card has an initiative number. The card you play as TOP
determines your initiative. Lower = faster. This matters in co-op (who acts
first) but in solo it only matters for "act before or after invaders."

Our Fast/Slow split already handles this. Fast tops resolve before tide,
slow tops after. No need for numerical initiative in a solo game.

**Decision: No initiative numbers. Fast/Slow is sufficient.**

### Q2: How many turns per tide?

Currently 1 turn per tide (Vigil + Tide + Dusk = 1 turn). With pairing,
should there be 2 turns per tide?

If 1 turn per tide: 6 tides = 6 turns. Root uses 6 of 10 cards = 3 pairs
before even needing to think about rest. Too comfortable.

If 2 turns per tide: 6 tides = 12 turns. Root burns through nearly all
stamina. Tight. But 12 turns might feel long.

**Proposed: 1 turn per tide, but tides increase.** Standard has 8-10 tides
instead of 6. This gives the stamina math room to breathe while the tide
count drives difficulty.

### Q3: What about Vigil play limit?

The old system had "play 2 tops in Vigil." With pairing, you play exactly
1 top + 1 bottom per turn. Do we ever allow playing 2 pairs per turn?

**Proposed: No. 1 pair per turn, always.** Simplicity. The decision is WHICH
pair, not how many. If we need more player power, increase card values.

### Q4: Rest timing — forced or voluntary?

**Decision: Both.** You MUST rest when your hand is empty (0 cards). You CAN
rest voluntarily with cards in hand — but the 2-bottom-loss rule makes early
rest painful (67-100% loss rate with few bottoms played).

Resting takes your entire turn. No card play. The tide still happens. This
makes rest timing a real decision: "Rest now during a Rest tide (safe) or
push one more turn and rest during a Ravage (dangerous)?"

### Q5: Elements from dissolved cards?

When a card is dissolved (via BURN or rest), should its elements burst?
Like a final gasp of elemental energy?

**Proposed: Yes. Dissolved cards release their elements ×2.** This means
BURN bottoms have a hidden benefit — they feed thresholds. A BURN card
with [Ash, Ash] releases 4 Ash elements when dissolved, potentially
triggering Ash T1.

This creates a tension: do you BURN a card partly for the element burst,
even if the bottom effect isn't needed right now?

### Q5: Elements from dissolved cards?

When a card is dissolved (via rest), should its elements burst?

**Proposed: Yes. Dissolved cards release their elements ×2.** This means
losing a card has a silver lining — it feeds thresholds. An [Ash, Ash] card
dissolving releases 4 Ash, potentially triggering Ash T1.

This creates a tension: sometimes you're "OK" losing a card because the
element burst is what you needed. Especially for mono-element cards.

### Q6: Why do some wardens have more cards?

Root has 10 cards. Ember has 8. The asymmetry creates different play feels:

**Root (10 cards) — Consistent but individually weaker.**
- 5 pairs per cycle = more turns per rest. Comfortable pacing.
- Individual card values are LOWER than Ember's (DI×2 tops vs Ember's DI×2
  but Ember has fewer cards to cycle through).
- Root's strength: coverage, flexibility, redundancy. Losing 1 card on rest
  is annoying but survivable — Root has alternatives.
- Root's weakness: no single card solves a crisis. Root grinds, doesn't burst.

**Ember (8 cards) — Explosive but fragile.**
- 4 pairs per cycle = tighter. Every pair counts.
- Individual card values are HIGHER. Ember's bottoms hit harder.
- Ember's strength: burst power. Wildfire bottom can clear a board.
- Ember's weakness: losing 1 card on rest is 12.5% of the deck. Losing the
  wrong card is devastating.

**Root-specific passive: Elemental Offering.** Once per rest cycle, Root may
discard a card from hand (as if played as top — goes to top-discard, safe
on rest) to add that card's elements to the pool. The card is NOT played —
no top effect resolves. You just sacrifice the card for its elements.

This costs 1 pair from your cycle (hand shrinks by 1 = one fewer pair
before rest). But it frontloads element building.

**The timing decision is the fun part:**
- **Turn 1 sacrifice:** Maximum compound value. The elements you gain turn 1
  persist (minus decay) through turns 2-5. If you sacrifice a [Root, Root]
  card turn 1, you gain 2 Root that compound across the cycle. You hit T1
  faster, which means more threshold fires across more turns.
- **Turn 3 sacrifice:** Safer. You've seen the board develop. Maybe you
  needed that card's top on turn 2 but didn't. Now you know it's expendable.
  But the element gain only benefits turns 4-5 before rest.
- **Never sacrifice:** Keep all 5 pairs. Sometimes the extra pair is worth
  more than the elements. Especially if you're facing heavy pressure and
  need every card's effects.

**Variant to playtest: Dissolve for ×2 elements.** Instead of top-discard
(safe), the card goes to dissolved pile (gone for encounter). You get
the card's elements ×2 instead of ×1. Higher reward, real cost. A [Root,
Root] card dissolved = 4 Root elements. That could instantly trigger T1.

**Synergy with tier-scaling decay:** The Elemental Offering is impactful
early but decays faster at higher tiers. Sacrifice a [Root, Root] card
turn 1 → gain 2 Root → push to T1 (4+) → decay is now 2/turn instead of 1.
The elements you gained are burning off faster. By turn 3-4, the sacrifice
boost has mostly decayed. This is self-balancing: the offering gives a
SPIKE, not permanent advantage. You still need to play matching-element
pairs to sustain the tier. The offering buys you 1-2 extra turns of
threshold fires — which can be decisive, but isn't free.

The dissolve variant (×2 elements) is even more dramatic: instant T1 or
near-T2, but the higher tier means 2-3 decay/turn, so it crashes back
within 2-3 turns. A [Root, Root] dissolve → 4 Root → T1 instantly → decay
of 2/turn → back below T1 in 2 turns unless you feed it. High risk,
high reward, self-correcting.

**Rest pain asymmetry:**

Root's rests should be MORE painful to offset the stamina advantage:
- Root loses 2 of 5 bottoms (40%) — standard
- But Root's element decay DOUBLES during rest (all elements lose 2 extra)
- This means Root loses threshold progress on rest. Resting is a setback.

Ember's rests should be LESS painful:
- Ember loses 2 of 4 bottoms (50%) — already harsher by percentage
- But Ember's elements DON'T decay extra during rest
- Ember preserves threshold momentum through rest cycles
- This rewards Ember for aggressive element building before resting

### Q7: BURN — Revisit Later

BURN (guaranteed dissolution on play) is deferred. Random rest loss already
creates bottom tension. If playtesting shows:
- Bottoms feel too safe → add BURN to 1-2 ultimate cards
- Players want a "heroic sacrifice" moment → BURN is the answer
- The random loss is sufficient → don't add BURN

BURN's design space: cards where you'd WANT guaranteed loss to avoid the
randomness. "I'll BURN Conflagration for DI×6 rather than risk losing
Deep Roots randomly." Interesting but needs the base system first.

### Q8: How do thresholds interact with pairing?

Elements accumulate from both the top card (×1) and bottom card (×2).
Thresholds check at the end of each turn (after Dusk).

If Ash T1 = 4 and you pair two [Ash, Ash] cards: top contributes 2 Ash,
bottom contributes 4 Ash = 6 total. You hit T1 (4) and are close to T2 (7).

**Threshold effects: player always chooses target.** Threshold effects enter
targeting mode. The player picks which territory takes the threshold
damage/effect. This makes thresholds feel earned, not automatic.

### Q9: Element Decay — Scales With Tier (RESOLVED)

**Problem:** Flat decay (1 per turn) means T3 is easy to sustain once reached.
Element building becomes "rush T3, keep it forever." No tension in maintenance.

**Decision: Decay scales with current tier level.**

| Current tier | Decay per turn | Effect |
|-------------|---------------|--------|
| Below T1 (0-3) | 1 per turn | Easy to build, slow drain |
| At T1 (4-6) | 2 per turn | Moderate upkeep — need to keep playing matching elements |
| At T2 (7-10) | 3 per turn | Hard to maintain — need dedicated element building |
| At T3 (11+) | 4 per turn | Nearly impossible to sustain — T3 is a SPIKE not a state |

**Why this works:**

- **T1 is reachable and sustainable.** One matching pair per turn feeds 3-6
  elements. Decay of 2 means net +1 to +4 per turn. T1 is your "cruising
  altitude."

- **T2 is achievable but costly.** You need dedicated mono-element pairs to
  push from 7 to 10 while losing 3/turn. It means sacrificing optimal
  pairings (best top + best bottom) for element-building pairings (matching
  elements). The COST of T2 is suboptimal card plays.

- **T3 is a spike.** You build to 11, T3 fires, deals massive damage, then
  elements crash back toward T2/T1. You can't stay at T3. It's a burst, not
  a state. This is Spirit Island's "major power" feeling — it fires once
  and it's glorious.

**Multi-element wardens vs mono-element wardens:**

Root has [Root, Mist, Shadow]. Building Root to T3 means playing lots of
Root-element cards. But that means NOT playing Shadow or Mist cards. Root
has to choose: wide element coverage (T1 in multiple elements) or deep
single-element focus (T2/T3 in one).

Ember has [Ash, Ash, Shadow, Gale] — heavily Ash-weighted. Ember naturally
rushes Ash thresholds. Getting Ash T2 is easy for Ember. Getting Shadow T1
is a deliberate investment.

**The future Threshold Warden (Veil) would have passives that reduce decay
or allow multi-element threshold maintenance.** That's their whole identity.

### Q10: Root's Rest Pain (RESOLVED)

Root's extra cards (10 vs 8) give a stamina advantage. To balance:

**Root's elements decay by an extra 2 during rest (all elements).**

This means resting is a threshold setback for Root. If Root had Ash at 5
(T1), after rest it drops by the normal decay + 2 extra = potentially
falling below T1.

Ember's elements do NOT take extra decay on rest. Ember preserves
threshold momentum through rest cycles.

This creates an incentive for Root to delay rest (protect thresholds) vs
the incentive to rest early (better reroll odds on bottoms).

---

## Next Steps

1. **Paper prototype:** Play through 4 turns of Root with 10 cards above.
   Pick pairs, resolve effects, track hand/discard/dissolved. See if it
   feels good.

2. **If yes:** Write the Core Loop Redesign Spec. Changes to TurnManager,
   CardSystem, EncounterState. The sim needs to pair cards instead of
   playing tops/bottoms separately.

3. **If no:** Identify what's flat and iterate this document.

---

## Revision History

| Rev | Date | Changes |
|-----|------|---------|
| 1 | 2026-03-22 | Initial pairing system + card lifecycle + card designs |
| 2 | 2026-03-22 | Random rest loss + weave reroll (not save). Removed BURN for now. |
| 3 | 2026-03-22 | Dynamic terrain (trade-offs, transforms based on corruption/events). Push scatters, Pull gathers for AoE. |
| 4 | 2026-03-22 | Terrain trade-offs (bonuses affect BOTH sides). Element decay scales with tier. Root rest pain (extra element decay). Root Elemental Offering passive. Warden hand size asymmetry resolved. |
| 5 | 2026-03-22 | Elemental Offering refined to passive (once per rest cycle). Safe vs dissolve variants to playtest. Tier-decay naturally balances the offering spike. |
