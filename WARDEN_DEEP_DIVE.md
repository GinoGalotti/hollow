# Hollow Wardens — Warden Design Deep Dive + Simulated Turns

> Working through what makes each warden FUN, not just mechanically different.
> Includes turn-by-turn simulations showing actual player decisions.

---

## What Makes a Turn Fun?

A good turn has ALL of these:

1. **Competing priorities** — "I need to damage AND cleanse AND place presence,
   but I only have 1 pair this turn."
2. **Timing tension** — "Do I play fast (before invaders act) or slow (after,
   but stronger)?"
3. **Spatial puzzle** — "WHERE I target matters as much as what I do."
4. **Bottom risk** — "This bottom is amazing but I might lose this card on rest."
5. **Element planning** — "This pair gives me 4 Ash, which triggers T1. But if
   I save the Ash card for next turn and pair differently, I hit T2 next turn."
6. **Information response** — "The tide preview says March is coming. I need
   to slow invaders BEFORE they move."

If a turn has fewer than 3 of these, it's boring. Let's design cards that
create ALL of them.

---

## ROOT — "The Growing Network"

### Root's Emotional Arc

**Early game (turns 1-2):** Vulnerable. 1 presence on I1. Need to expand
before invaders overwhelm. Every pair is "do I invest in presence or fight
the fire in front of me?"

**Mid game (turns 3-4):** Network coming online. 3-4 presence, reaching
A-row. Passives starting to trigger. Feeling strong. But corruption is
building in territories you haven't reached yet.

**Late game (turns 5+):** Network is wide but some territories are scarred.
Post-rest with fewer cards. Every pair is precious. Riding the momentum
of your network to control the last tides.

### Root's Design Principles

- **Tops are defensive/setup.** Place presence, slow invaders, shield natives.
  These are the "safe" plays — build your network, control the board.
- **Bottoms are offensive/explosive.** Big damage, deep cleanse, fear bursts.
  These are the payoffs — but risk losing the card on rest.
- **Element identity: Root = presence scaling, Mist = fear/information,
  Shadow = native/control.** Mono-Root pairs build presence. Mixed pairs
  give versatile effects.
- **Root's unique advantage: more cards (10) = more flexibility per cycle.**
  Root can afford 1-2 "throwaway" bottom plays where they're OK losing the
  card. Ember can't.

### Root's 10 Cards (Revised for Fun)

| # | Name | Elem | Fast/Slow | Top | Bottom | Design Intent |
|---|------|------|-----------|-----|--------|---------------|
| 1 | **Deep Roots** | Rt,Rt | SLOW | PlacePresence ×2 r1 | PlacePresence ×3 r2 + SpawnNatives ×1 | Core expansion. Bottom is your best presence play + a native bonus. Losing it on rest hurts. |
| 2 | **Grasping Thorns** | Rt,Sh | FAST | DamageInvaders ×2 r1 | DamageInvaders ×4 r1 + PullInvaders ×2 r1 | Damage + pull combo. Bottom pulls THEN damages — stack and kill. Setup card for AoE. |
| 3 | **Earthen Mending** | Rt,Rt | SLOW | ReduceCorruption ×2 r1 | ReduceCorruption ×5 r1 | Pure cleanse. Slow top means corruption from THIS tide's Ravage stays until resolved. Bottom is emergency cleanse but risks losing your best cleanse card. |
| 4 | **Whisper of Growth** | Rt,Mi | FAST | PlacePresence ×1 r2 | RestoreWeave ×3 + PlacePresence ×1 r1 | Flexible. Fast top for emergency presence. Bottom heals AND places — but it's safe enough you might play it as bottom early, then regret losing it later. |
| 5 | **The Forest Remembers** | Mi,Mi | FAST | GenerateFear ×3 | GenerateFear ×6 + SpawnNatives ×1 | Fear engine. Double-Mist = fast threshold building. Bottom is huge fear but risks your fear card. |
| 6 | **Tangled Earth** | Sh,Rt | SLOW | SlowInvaders ×2 r1 + DamageInvaders ×1 r1 | PushInvaders ×3 r2 + ReduceCorruption ×2 r1 | Control. Top slows AND pings. Bottom pushes invaders back AND cleanses. The "I'm losing control of this territory" card. |
| 7 | **Stir the Sleeping** | Mi,Sh | FAST | SpawnNatives ×1 + MoveNatives ×2 | SpawnNatives ×2 + BoostNatives ×2 r1 | Native investment. Fast top moves natives to where they're needed. Bottom creates an army. Losing this = losing your native strategy. |
| 8 | **Ancient Ward** | Sh,Mi | FAST | ShieldNatives ×3 r1 | ShieldNatives ×4 r1 + GenerateFear ×3 | Defensive combo. Protects natives AND generates fear. Both halves are good — agonizing to assign as bottom. |
| 9 | **Reclaim the Wild** | Rt,Rt,Sh | SLOW | DamageInvaders ×3 r1 + ReduceCorruption ×2 r1 | DamageInvaders ×5 r2 + ReduceCorruption ×4 r2 | Finisher. Both halves are amazing. Top is the best top in the deck. Bottom is the best bottom. You'll agonize over which role to give it EVERY SINGLE TURN. |
| 10 | **Roots of Knowing** | Mi,Rt | SLOW | DamageInvaders ×2 r1 (deals +1 per presence in territory) | GenerateFear = total presence on board | Scaling card. Top scales with local presence (2 presence = DI×4). Bottom scales with TOTAL presence (8 presence = GF×8). This is the "build around" card — the more presence you have, the better this gets. |

### What Makes Root's Cards Fun

**Card 9 (Reclaim the Wild) is the "impossible choice" card.** Its top is
DI×3 + RC×2 — that's the best top in the deck. Its bottom is DI×5 + RC×4
— that's the best bottom too. Every turn you draw it, you agonize: "Do I
use its incredible top (safe) or its incredible bottom (risky)?" This one
card creates decision tension all by itself.

**Card 10 (Roots of Knowing) is the "engine" card.** Early game with 1
presence: top is DI×3, bottom is GF×1. Weak. Late game with 6 presence:
top is DI×8, bottom is GF×6. Incredible. This card REWARDS your presence
investment. It makes the early-game "invest in presence vs fight fires"
decision matter because your engine card gets better.

**Card 2 (Grasping Thorns) is the "combo setup" card.** Bottom PULLS then
damages. You pull invaders from 2 adjacent territories, THEN deal 4 damage.
If you pulled 3 invaders with 3 HP each, you kill one and wound two. Next
turn, a DI×2 top finishes them. The pull creates a TWO-TURN PLAN.

**Cards 5 + 7 (Forest Remembers + Stir the Sleeping) are the "native
engine" pair.** Pair them: Forest Remembers top (GF×3 fast) + Stir the
Sleeping bottom (SpawnNatives ×2 + BoostNatives ×2). You generate fear
AND build a native army. But you risk losing Stir on rest — if you do,
your native strategy collapses.

---

## EMBER — "The Controlled Burn"

### Ember's Emotional Arc

**Early game (turns 1-2):** Ember starts fires. Ash Trail corrupts your
own territories. It feels chaotic and dangerous. "Am I hurting myself?"

**Mid game (turns 3-4):** The corruption you created IS your weapon.
CorruptionDetonate does massive AoE. Ember Fury bonus damage kicks in.
The self-harm was the investment. Feeling powerful and reckless.

**Late game (turns 4+):** Only 6 cards left after rest. Every pair is
desperate. But the board is on fire — Scorched territories deal passive
damage, corruption is your fuel. Either you ride the wave to victory or
it consumes you.

### Ember's Design Principles

- **Tops are fast and aggressive.** Ember acts BEFORE invaders. Hit first,
  ask questions later.
- **Bottoms are self-destructive but massive.** Corruption, AoE, huge damage.
  The bottom IS the risk — not just losing the card, but the effect ITSELF
  is dangerous (adding corruption).
- **Element identity: Ash = damage/corruption, Shadow = control/fear,
  Gale = mobility/speed.** Mono-Ash pairs are explosive. Mixed pairs
  are safer.
- **Ember's unique constraint: 8 cards = tight cycles.** Every bottom
  matters more because the rest pool is smaller (4 bottoms, 2 die = 50%).

### Ember's 8 Cards (Revised for Fun)

| # | Name | Elem | Fast/Slow | Top | Bottom | Design Intent |
|---|------|------|-----------|-----|--------|---------------|
| 1 | **Flame Burst** | As,As | FAST | DamageInvaders ×2 r1 | DamageInvaders ×5 r1 | Bread and butter. Fast damage before invaders act. Bottom is big single-target burst. Simple, reliable. |
| 2 | **Kindle** | As,Ga | FAST | PlacePresence ×1 r2 | PlacePresence ×2 r2 + Ignite territory (2 dmg to entering invaders, 2 tides) | Presence + zone control. Bottom creates a burning zone — invaders walking in take damage. Losing this means no more fire traps. |
| 3 | **Burning Ground** | As,Sh | SLOW | DamageInvaders ×1 r0 (all in territory) | DamageInvaders ×2 r0 (all) + AddCorruption ×2 to territory | The Ember dilemma card. Bottom does AoE damage BUT adds corruption to your own territory. Worth it to clear 4 invaders? But now that territory is closer to Scorched. |
| 4 | **Smoke Screen** | Sh,Ga | FAST | GenerateFear ×2 + SlowInvaders ×1 r1 | GenerateFear ×5 + PushInvaders ×2 r1 | Defensive Ember. Fast fear + slow. Bottom is a big fear burst + push. Your "I need to buy time" card. |
| 5 | **Stoke the Fire** | As,As | SLOW | ReduceCorruption ×2 r1 | ReduceCorruption ×4 r1 + DamageInvaders ×2 r1 | Ember's cleanse. Slow — corruption from Ravage stays until resolved. Bottom cleanses AND damages. This is precious — Ember needs cleanse and losing it on rest is devastating. |
| 6 | **Ember Spread** | As,Sh | FAST | PlacePresence ×1 r1 | PlacePresence ×2 r2 + AddCorruption ×1 to each territory where presence placed | Risky expansion. Bottom places 2 presence but corrupts both territories. Ash Trail will then trigger on more territories. Accelerates Ember's engine but also the corruption spiral. |
| 7 | **Wildfire** | As,As,Ga | SLOW | PullInvaders ×3 r1 | CorruptionDetonate: deal damage = corruption to ALL invaders in territory, then cleanse to 0 | THE EMBER CARD. Top PULLS invaders (Slow — happens after tide). Bottom DETONATES the corruption as AoE. THE two-turn combo: Turn 1 top = pull invaders into corrupted territory. Turn 2 bottom = detonate. But losing Wildfire on rest ends your entire strategy. |
| 8 | **Dying Light** | As,Sh,Ga | FAST | RestoreWeave ×2 + GenerateFear ×2 | RestoreWeave ×4 + GenerateFear ×4 + if weave ≤ 10: DamageInvaders ×3 to all territories with presence | Comeback card. Fast heal + fear. Bottom heals, fears, and if you're hurting (≤10 weave), also does board-wide damage. Losing this card when you're at low weave is a death sentence. Getting desperate = this card gets BETTER. |

### What Makes Ember's Cards Fun

**Card 7 (Wildfire) is Ember's identity card.** The top PULLS invaders
(stack them up). The bottom DETONATES corruption as damage then cleanses.
The TWO-TURN COMBO: Turn 1: Wildfire top (pull invaders into corrupted
territory). Turn 2: Wildfire bottom (detonate — deal 8+ damage to all
stacked invaders, cleanse the territory). If you lose Wildfire on rest,
your entire strategy collapses. You'll ALWAYS play it as top first, then
agonize about when to use the bottom.

**Card 3 (Burning Ground) embodies Ember's dilemma.** Bottom does AoE ×2
to ALL invaders in the territory — great! But also adds 2 corruption. If
the territory was at 1 corruption, now it's at 3 (L1). If it had a Forest,
two more corruption and it BURNS (L2 = Forest→Scorched). Ember is literally
setting the world on fire to save it.

**Card 8 (Dying Light) is the comeback card.** When Ember is at full health,
it's just decent (heal 4 + fear 4). When Ember is at ≤10 weave (desperate),
it ALSO deals 3 damage to every territory with presence. The worse things
get, the stronger this card becomes. Ember doesn't die quietly.

**Card 5 (Stoke the Fire) is the card Ember CAN'T LOSE.** It's the only
real cleanse card. Bottom is ReduceCorruption ×4 + DI×2. If rest
dissolves it, Ember has no way to manage corruption. Every turn Stoke is
in the bottom-discard pile, you're sweating.

---

## TERRAIN IN ACTION

### How Terrain Creates Board Puzzles

Terrain isn't just stat buffs. It creates BOARD READING — looking at the
map and seeing opportunities and threats.

**Scenario: Standard board, mixed terrain**

```
        [I1: Sacred]
       /            \
   [M1: Forest]   [M2: Plains]
   /    \           /    \
[A1: Plains] [A2: Mountain] [A3: Wetland]
```

Turn 1 setup: 3 Marchers arriving (1 per A-row territory). Root has 1
presence on I1.

**The board TELLS you what to do:**
- A2 is Mountain — presence there generates +1 Fear per tide. But that's
  far from I1.
- M1 is Forest — card effects there get +1. If you place presence on M1,
  your damage cards hit harder there.
- A3 is Wetland — corruption builds slower (thresholds +2). Invaders that
  Ravage here need 5 corruption to reach L1, not 3. Safe-ish for now.
- I1 is Sacred — can't corrupt past L1. Your heart is protected... unless
  invaders Settle here (Sacred → Blighted, which auto-corrupts each tide).

**Where do you place first presence?** M1 (Forest, amplifies damage) or
M2 (Plains, but covers A2 and A3)? Or A2 (Mountain, fear engine, but
exposed to invaders)?

THAT'S a decision. The board itself creates the puzzle.

### How Terrain Evolves During an Encounter

**Turn 1:** Invaders Ravage on A1 (Plains). 1 Marcher = 2 corruption. A1
is now at 2 corruption. Plains stays Plains.

**Turn 3:** A1 now at 5 corruption (multiple Ravages). L1 (Tainted).
Still Plains — no terrain change.

**Turn 4:** You play Burning Ground bottom on A1 (DI×2 all + AddCorruption
×2). A1 is now at 7 corruption. L2 (Defiled). If A1 were Forest, it would
BURN to Scorched. But it's Plains — nothing happens. The Forest on M1 is
safe... for now.

**Turn 5:** Invaders March from A1 to M1. The Forest territory. Invaders
Ravage M1 = 3 corruption. M1 Forest at 3 corruption = L1. One more Ravage
and it hits 6 = L2 = Forest → Scorched. You need to cleanse M1 or lose
your +1 damage amplification.

**This creates urgent spatial priorities.** "I can't let M1 reach L2 or
I lose my Forest. But A2's Mountain is generating fear I need. And A3's
Wetland is the only territory NOT getting corrupted. Where do I spend my
pair this turn?"

### Terrain-Specific Card Interactions

**Pull + Scorched:** Pull invaders into a Scorched territory — they each
take 2 damage on entry. Pull 3 invaders = 6 free damage.

**PlacePresence + Mountain:** Presence on Mountain = +1 Fear per tide.
With 2 presence = +2 Fear per tide. Root's Network Fear + Mountain presence
= massive passive fear generation.

**CorruptionDetonate + any terrain:** Detonating 8 corruption on a Forest
territory: deals 8 damage AoE, then cleanses to 0. But the Forest already
turned Scorched at L2 (6 corruption). So after detonation, the territory
is Scorched (from the corruption) but at 0 corruption (from the cleanse).
Scorched deals 2 damage to entering invaders — the detonation left a scar
that now helps you.

**DamageAll + Forest:** DamageInvaders ×2 r0 (all in territory) in a
Forest = ×3. With 2 presence (amplification) = ×4 to all invaders. One
card clears the territory.

---

## SIMULATED ENCOUNTER: Root vs Pale March Standard

### Setup
- Root, 10 cards, 1 presence on I1 (Sacred)
- Board: I1 Sacred, M1 Forest, M2 Plains, A1 Plains, A2 Mountain, A3 Wetland
- 8 tides. Wave 1: 2 Marchers (A1, A2) + 1 Outrider (A3)
- Tide 1: Ravage

### Turn 1 — "Where do I invest?"

**Hand:** All 10 cards.
**Tide preview:** Ravage → March → Arrive → Ravage

**The situation:** 3 invaders on A-row. Ravage is coming — each will
add 2 corruption to their territory. I have 1 presence on I1 (Sacred).
I can't reach A-row yet.

**Priority analysis:**
- Damage? Can't reach A-row from I1 (range 1 effects don't reach A-row)
- Presence? Need to expand to M1 or M2 to reach threats
- Slow? Can't slow from I1 — need presence adjacent to invaders
- Fear? I can generate fear but it doesn't solve the immediate problem

**The pair I'm considering:**

*Option A:* Deep Roots top (SLOW: PlacePresence ×2 r1) + Whisper of Growth
bottom (RestoreWeave ×3 + PlacePresence ×1 r1)

Result: Place 2 presence (M1, M2) from top. Bottom gives +3 weave (wasted —
already at 20) and +1 presence. Total: 3 presence placed. Great expansion.
But Whisper is now in bottom-discard (risky).
Elements: [Root, Root] + [Root, Mist] = 2 Root + 2 Mist (bottom ×2).
4 Root + 2 Mist. Root T1 fires (4 needed)! Threshold deals 1 damage to...
where? I need to target a territory — A1 has a Marcher.

*Option B:* Whisper of Growth top (FAST: PlacePresence ×1 r2) + Grasping
Thorns bottom (DI×4 r1 + PullInvaders ×2)

Result: Fast presence on M1 (before invaders Ravage). Then after Ravage,
the bottom activates... but wait, I placed on M1, which is adjacent to A1
and A2. The bottom's DI×4 can hit A1 or A2, and the pull drags invaders
from adjacent territories. I pull 2 invaders from A2 and A3 into A1, then
deal 4 damage to the Marcher on A1 (kills it). A1 now has 3 invaders but
1 is dead.
But Grasping Thorns is in the bottom-discard pile. If I lose it on rest,
I lose my pull + damage combo card.
Elements: [Root, Mist] + [Root, Shadow ×2] = 1 Root + 1 Mist + 2 Root +
2 Shadow = 3 Root + 1 Mist + 2 Shadow. No thresholds yet.

*Option C:* The Forest Remembers top (FAST: GenerateFear ×3) + Deep Roots
bottom (PlacePresence ×3 r2 + SpawnNatives ×1)

Result: Fast fear gives me 3 fear before invaders act (toward first fear
action at 5). Bottom places 3 presence with range 2 (I can reach A-row from
I1!) and spawns a native. Huge expansion. But Deep Roots is my best presence
card — losing it on rest cripples my network for cycle 2.
Elements: [Mist ×2] + [Root ×2 ×2] = 2 Mist + 4 Root. Root T1 fires!

**I go with Option C.** The expansion is too important. Deep Roots in the
bottom-discard is scary but I need the network NOW. Fear is a nice bonus.
Root T1 fires — I target the threshold damage at A2's Marcher.

**After Tide 1 (Ravage):**
- A1: 1 Marcher ravages → +2 corruption. A1 at 2 corruption.
- A2: 1 Marcher ravages (wounded from T1) → +2 corruption. A2 at 2 corruption.
- A3: 1 Outrider ravages → +1 corruption. A3 at 1 corruption. (Wetland
  threshold is 5 not 3, so still L0.)
- My presence: I1 + M1 + M2 + one more at A-row (range 2 from I1).
  I placed at A1 to contest the Marcher there.
- I spawned 1 native on... A1 (to counter-attack the Marcher next turn).

**Board state after Turn 1:**
```
        [I1: Sacred, 1 pres]
       /                     \
   [M1: Forest, 1 pres]   [M2: Plains, 1 pres]
   /         \                /          \
[A1: 2 corr   [A2: Mountain  [A3: Wetland
 1 pres        2 corr          1 corr
 1 native      1 Marcher       1 Outrider
 1 Marcher]    (wounded)]       ]
```

### Turn 2 — "The March is Coming"

**Hand:** 8 cards left (played 2 last turn).
**Tide preview:** March → Arrive → Ravage → Rest

**The situation:** March is coming. The Marcher on A2 will march to M1
(Forest). The Outrider on A3 will march to M2 (and then M1 next tide
because Outriders move +1). If I don't slow or kill them, M1 Forest gets
invaded.

**Priority analysis:**
- Slow invaders to prevent march into M1? Tangled Earth top (SLOW: Slow ×2 + DI×1). But SLOW means it resolves AFTER the march. Too late!
- Kill A2 Marcher before it marches? Need FAST damage. Grasping Thorns top (FAST: DI×2). I have presence on M1 which is adjacent to A2. Can target A2. Marcher has 4 HP, wounded (took 1 from threshold). DI×2 from M1 with Forest bonus (+1) = DI×3. Marcher has 3 HP left. KILLS IT.
- But if I use Grasping Thorns as top, I need another card as bottom. What bottom do I want to play?

**Options:**

*Option A:* Grasping Thorns top (FAST: DI×2+1 Forest = DI×3, kills A2 Marcher) + Stir the Sleeping bottom (SpawnNatives ×2 + BoostNatives ×2)

Kill the immediate threat fast. Spawn 2 more natives. Boost existing natives.
A1 now has 3 natives — next Ravage, they counter-attack the Marcher for
3 damage each = 9 damage → dead Marcher. A1 is self-defending.
But Stir the Sleeping in bottom-discard = risk losing native strategy.

*Option B:* Grasping Thorns top (kill A2 Marcher) + Earthen Mending bottom (RC×5 r1)

Kill threat + deep cleanse. A1 is at 2 corruption — bottom cleanses to 0.
A2 also at 2 — but range 1, can I reach? M1 has presence, adjacent to A2.
Yes. Cleanse A2 to 0.
Earthen Mending in bottom-discard — it's my deep cleanse card.

*Option C:* Tangled Earth top (SLOW: Slow ×2 + DI×1) + Ancient Ward bottom
(Shield ×4 + GF×3)

Don't kill the marcher — slow it instead. It doesn't march this tide.
Shield the native on A1 (survives Ravage). Generate 3 fear. Slower but
preserves board state. BUT: Slow is SLOW phase — it resolves AFTER the
march. The Marcher already moved to M1 before the slow applies.

**Tangled Earth doesn't work here because it's SLOW.** This is exactly the
Fast/Slow tension — I NEED fast damage to kill before March resolves. The
slow control card is useless for prevention.

**I go with Option A.** Kill the threat, invest in natives.

Elements: [Root, Shadow] + [Mist, Shadow ×2] = 1 Root + 1 Shadow + 2 Mist
+ 2 Shadow = 1 Root + 2 Mist + 3 Shadow. Shadow at 3 (T1 needs 4). Close.

**After March:** A2 Marcher is dead (killed fast). Outrider moves A3→M2→M1
(+1 movement). M1 Forest now has 1 Outrider. Arrive: 2 new Marchers on
A1 and A3.

**Board after Turn 2:**
```
        [I1: Sacred, 1 pres]
       /                     \
   [M1: Forest, 1 pres    [M2: Plains, 1 pres]
    1 Outrider]            /          \
   /         \          [A2: Mountain  [A3: Wetland
[A1: 2 corr   0 corr]     1 Marcher
 1 pres                     (new)]
 3 natives (boosted!)
 1 Marcher (original)
 1 Marcher (new)]
```

**Decision I'm already thinking about for Turn 3:** The Outrider on M1
(Forest) will Ravage next tide. 1 Outrider = 1 corruption. M1 Forest at 1
corruption = still L0. But if I don't clear it, subsequent Ravages push M1
toward L2 = Forest → Scorched. I'm protecting my +1 damage bonus.

Meanwhile A1 has 2 Marchers + 3 boosted natives. If Marchers Ravage, A1
takes 4 corruption (currently at 2 → total 6 = L2). BUT my 3 natives
counter-attack: 3 damage × 3 (boosted to 5?) = enough to kill both
Marchers. The native investment from Turn 2 pays off on Turn 3.

**THIS is the fun.** Turn 2's bottom play (SpawnNatives + Boost) directly
creates Turn 3's board advantage. Decisions chain across turns.

---

## SIMULATED ENCOUNTER: Ember vs Pale March Scouts

### Setup
- Ember, 8 cards, 1 presence on I1
- Board: I1 Plains, M1 Forest, M2 Plains, A1 Plains, A2 Plains, A3 Wetland
- 6 tides. Scouts: Outrider-heavy. Wave 1: 3 Outriders (A1, A2, A3)
- Tide 1: Ravage

### Turn 1 — "Set the world on fire"

**Hand:** All 8 cards.
**Tide preview:** Ravage → March → Ravage → March

**The situation:** 3 Outriders. HP 3 each. Fast and fragile. They'll Ravage
for 1 corruption each (light damage) then march FAST (2 steps because
Outrider +1 movement). They'll reach the heart by tide 3 if I don't act.

Ash Trail passive: at Tide start, each territory with Ember presence gains
+1 corruption. I1 will gain 1 corruption. I'm corrupting my own heart.

**Priority: I need presence on M-row to intercept. And I need to kill
Outriders fast — 3 HP means most tops can wound or kill them.**

**Options:**

*Option A:* Flame Burst top (FAST: DI×2, kill an Outrider on A-row?) —
wait, range 1 from I1 can only reach M1/M2. Can't hit A-row from I1.

No good. I can't damage A-row without M-row presence.

*Option B:* Kindle top (FAST: PlacePresence ×1 r2) + Flame Burst bottom
(DI×5 r1)

Fast presence on M1. Now I can reach A1 and A2. Bottom: DI×5 on A1 — kills
the Outrider (HP 3). But I also get Ash Trail corruption: I1 gains +1
corruption AND M1 gains +1 corruption (presence territories).
But wait — Kindle's bottom is PlacePresence ×2 r2 + Ignite. If I used
Kindle as BOTTOM instead... I place 2 presence AND create a fire zone.

*Option C:* Kindle top (FAST: Presence ×1 r2 on M1) + Burning Ground bottom
(SLOW: DI×2 all in territory + AddCorruption ×2)

Place presence on M1 fast. After Ravage, the Slow bottom hits... but Burning
Ground targets a territory with all invaders. A1 has 1 Outrider. DI×2 all
on A1 wounds the Outrider (3→1 HP). Plus adds 2 corruption to A1.

Not efficient. Outrider still alive.

*Option D:* Ember Spread top (FAST: Presence ×1 r1 on M1) + Stoke the Fire
bottom (RC×4 + DI×2)

Presence on M1. Bottom: cleanse corruption (I1 has +1 from Ash Trail → clean
it) and deal 2 damage to an Outrider on A1 or A2 (range 1 from M1). Outrider
at 1 HP after.

Hmm, but do I NEED cleanse turn 1? I1 is Sacred — can't corrupt past L1.
It takes 3 corruption to reach L1. I have time.

*Option E:* Smoke Screen top (FAST: GF×2 + SlowInvaders ×1 r1) + Flame
Burst bottom (DI×5)

Fast fear + slow 1 Outrider (prevent it from marching 2 spaces). Then DI×5
kills the Outrider on A1. I don't expand my presence but I eliminate a
threat and slow another.

**I go with Option B (Kindle top + Flame Burst bottom).** Place presence
on M1 fast. DI×5 kills A1 Outrider. Elements: [Ash, Gale] + [Ash ×2, ×2]
= 1 Ash + 1 Gale + 4 Ash = 5 Ash + 1 Gale. Ash T1 fires! (threshold at 4).

Ash T1 deals... I target A2 Outrider. 1 damage. A2 Outrider at 2 HP.

**After Tide 1 (Ravage):** A2 Outrider ravages (1 corruption on A2). A3
Outrider ravages (1 corruption on A3, Wetland = still far from L1). Ash
Trail: I1 +1 corruption, M1 +1 corruption (Forest at 1 corruption now).

**Turn 1 result:** 1 Outrider dead. 1 wounded. 1 untouched. Presence on
M1 (but M1 Forest is getting corrupted from Ash Trail). I1 at 1 corruption.

**The Ember tension is ALREADY visible:** Ash Trail is corrupting my own
territories. M1 Forest will burn if it hits L2 (6 corruption). I need to
either cleanse M1 or accept losing the Forest bonus. Ember's self-harm is
the price of its aggression.

### Turn 2 — "They're marching"

**Hand:** 6 cards left.
**Tide:** March → Arrive

**The situation:** A2 Outrider (2 HP) marches to M1 (2 steps with +1
movement). A3 Outrider (3 HP) marches to M2 (2 steps). New wave arrives.

I need to: kill the A2 Outrider before it reaches M1 (FAST effect needed),
AND deal with A3 Outrider heading to M2 (no presence there).

**Option A:** Wildfire top (SLOW: PullInvaders ×3) — SLOW, too late for
march prevention. But after march, I pull invaders from M2 into M1. Then
next turn I can AoE them.

**Option B:** Smoke Screen top (FAST: GF×2 + SlowInvaders ×1) — Slow 1
Outrider (A2). It only marches 1 step instead of 2. Ends up on M1 next
turn instead of this turn.

**Wait — Outrider's +1 movement. Normal march = 1 step. Outrider = 2 steps.
Slow ×1 reduces by 1 = back to 1 step. A2 Outrider goes A2→M1 (1 step).
Still reaches M1! Just doesn't reach I1.**

Actually that's fine — M1 has my presence. I can damage it there.

**Option C:** Burning Ground top (SLOW: DI×1 all in territory) — after
March, Outriders are on M1 and M2. DI×1 all in M1 (with Forest +1 = DI×2)
kills the 2HP Outrider. Then pair with... Stoke bottom (RC×4 + DI×2) to
cleanse M1 AND deal 2 damage to M2 Outrider.

**I go with Option C.** Burning Ground top (Slow: DI×2 all in M1 w/Forest
bonus) + Stoke the Fire bottom (RC×4 on M1 + DI×2 on M2).

After March: A2 Outrider on M1 (2 HP). A3 Outrider on M2 (3 HP).
Slow phase: DI×2 all on M1 → kills A2 Outrider.
Dusk: RC×4 on M1 (cleanse Ash Trail corruption). DI×2 on M2 → wounds A3
Outrider (3→1 HP).

Elements: [Ash, Shadow] + [Ash ×2, ×2] = 1 Ash + 1 Shadow + 4 Ash = 5 Ash
+ 1 Shadow. Still at Ash 5 (T1 already fired. Need 7 for T2.)

**But Stoke the Fire is in the bottom-discard.** My only real cleanse card.
If rest dissolves it, Ember has no way to manage the corruption Ash Trail
keeps adding. I'm already nervous.

---

## KEY TAKEAWAYS FROM THE SIMULATIONS

### What Felt Good
1. **Fast/Slow timing mattered on EVERY turn.** "I need FAST damage before
   March" was a real constraint that eliminated cards from consideration.
2. **Bottom risk created real tension.** Deep Roots, Grasping Thorns, Stoke
   the Fire — losing any of these on rest would change the game.
3. **Two-turn planning emerged naturally.** Root's Turn 2 natives paid off
   on Turn 3. Ember's Wildfire top sets up Wildfire bottom next turn.
4. **Terrain created spatial priorities.** "Protect M1 Forest" was a real
   objective that competed with "kill invaders."
5. **Element thresholds felt earned.** Choosing pairs that build Ash or Root
   elements was a real consideration, not just a bonus.

### What Needs Testing
1. **Is 10 cards too many for Root?** By turn 3, Root still has 6 cards in
   hand. Lots of options. Maybe too comfortable?
2. **Is the Forest bonus (+1) enough to care about?** Maybe it should be +2,
   or maybe terrain should have more dramatic effects.
3. **Are bottoms risky ENOUGH?** With 5 bottoms in the pool, losing 2 is 40%.
   Might feel "probably fine" rather than "genuinely scary." Maybe lose 2 of
   every 4 (50%) would be scarier. Needs playtesting.
4. **Corruption Detonate feels like it should be Ember's signature moment.**
   Is it powerful enough? 8 corruption = 8 AoE damage + full cleanse. That's
   massive. But building to 8 corruption risks terrain transformation. The
   tension is good but needs testing.
5. **Elements might accumulate too slowly or too fast with pairing.** Each
   pair generates 1-2 unique elements from top (×1) and 2-4 from bottom (×2).
   T1 at 4 might fire turn 1 with the right pair. T2 at 7 might fire turn 3.
   Is that too fast? Decay of 1/turn should keep it in check.
