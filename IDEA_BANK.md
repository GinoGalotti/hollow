# Hollow Wardens — Idea Bank (Untested)

> Brainstormed effects, verbs, passives, and warden concepts. Nothing here
> is committed. Pull ideas into the Core Loop Redesign doc when they've been
> validated through paper prototyping.
>
> Revision: 1

---

## Verb Taxonomy

### Universal Verbs (every warden gets these on their starting cards)

| Verb | What it does | Scales with |
|------|-------------|-------------|
| DamageInvaders | Deal N damage to invaders in territory | Presence (amplification) |
| ReduceCorruption | Remove N corruption from territory | — |
| PlacePresence | Place N presence tokens in territory | — |
| RestoreWeave | Heal N weave | — |
| GenerateFear | Add N to fear pool | Dread level (actions get better) |
| PushInvaders | Move invaders AWAY from territory — player chooses which adjacent territory each goes to (can split) | Defensive — clear a territory |
| PullInvaders | Gather invaders FROM all adjacent territories into target territory | Offensive — stack for AoE |
| SpawnNatives | Create N natives in territory | — |
| MoveNatives | Move N natives to adjacent territory | — |
| ShieldNatives | Give N natives +HP shield | — |
| BoostNatives | Natives in territory deal +N damage on counter-attack | — |
| SlowInvaders | Invaders in territory move -N steps on March | Presence (Network Slow) |

### Warden-Specific Verb Families

Each warden's starting deck uses mostly universal verbs with 1-2 signature
verbs. Draft cards and progression unlock more from their family.

---

## Warden Concepts

### Root — The Network (IMPLEMENTED)
**Fantasy:** "I am the forest. My roots are everywhere."
**Signature verbs:** Presence-scaling effects, adjacency bonuses

| Verb | Effect | Notes |
|------|--------|-------|
| ElementalOffering (PASSIVE) | Once per rest cycle: discard a card from hand (→ top-discard, safe). Add its elements to pool. Costs 1 pair from cycle. Variant: dissolve instead for ×2 elements. | Root-only passive. Timing decision: turn 1 for compound value or wait for safety. |
| PresenceStrike | DI = total presence on board | Scales with wide network |
| RootsSpread | All territories adjacent to presence: +1 to card effects this turn | Rewards wide presence |
| Assimilate | Remove invaders adjacent to presence territories (weakest first) | Existing mechanic |
| NetworkFear | Generate fear based on presence pairs (adjacency counting) | Existing passive |
| Entangle | Target invader can't move for 1 tide; takes double damage from next hit | Control + combo setup |
| DeepNetwork | Presence counts double for adjacency checks this turn | Burst coverage |

### Ember — The Fire (IMPLEMENTED)
**Fantasy:** "A dying fire spirit. Burn everything, including yourself."
**Signature verbs:** Corruption manipulation, self-inflict for power

| Verb | Effect | Notes |
|------|--------|-------|
| CorruptionDetonate | Deal damage to all invaders in territory = territory corruption, then cleanse to 0 | THE Ember fantasy |
| CorruptionHarvest | Generate Fear = total corruption on board | Turns corruption into fear |
| Ignite | Territory is burning for 2 tides: invaders entering take 2 damage | Zone denial |
| AshTrail | +N corruption per presence territory per tide | Existing passive |
| Scorch | Add N corruption to territory + deal N damage to all invaders there | Self-harm + AoE |
| PhoenixFlare | When a card is dissolved, deal 3 damage to random territory | Loss = damage |

### Veil — The Threshold Channeler (CONCEPT)
**Fantasy:** "Raw elemental power, barely contained."
**Signature verbs:** Element manipulation, threshold enhancement

| Verb | Effect | Notes |
|------|--------|-------|
| ElementSurge | Deal damage = current count of [X element] | Rewards element building |
| ThresholdEcho | Next threshold you trigger fires twice | One-shot multiplier |
| Overflow | If threshold already fired this turn, this effect gets ×1.5 | Rewards threshold stacking |
| ElementLock | One element doesn't decay this turn | Sustain thresholds longer |
| Attune | Add 3 of any element to the pool (player choice) | Flexible threshold rushing |
| Cascade | When any T2 fires, also trigger T1 of an adjacent element | Chain reactions |
| Siphon | Remove all elements of one type; deal damage = count removed | Convert elements to damage |

### Terraform — The Land Shaper (CONCEPT)
**Fantasy:** "I reshape the battlefield itself."
**Signature verbs:** Terrain manipulation, obstacle placement, territory modification

| Verb | Effect | Notes |
|------|--------|-------|
| Terraform | Change territory terrain type (Plains→Forest, Ruins→Fertile, etc.) | Works with dynamic terrain system |
| Leyline | Connect two territories for 2 tides: effects targeting one also hit the other | Spatial combo |
| SacredGround | Territory becomes Sacred (immune to corruption past L1) for 3 tides | Temporary protection |
| RaiseWall | Place obstacle between two adjacent territories: invaders can't move through for 2 tides | Pathing control |
| FertileGround | Plains territory becomes Fertile (natives +1 HP, +1 damage) | Invest in territory |
| Sinkhole | Territory: invaders here take 2 damage at tide start, for 2 tides | Trap — combo with Pull |
| Overgrow | Spawn 1 native in every territory with presence | Board-wide spawn |
| Restore | Scorched/Blighted/Ruins → immediately becomes Plains (skip timer) | Emergency terrain repair |

### Uprising — The Native Commander (CONCEPT)
**Fantasy:** "The land's people fight back. I lead them."
**Signature verbs:** Native specialization, army building, infrastructure

| Verb | Effect | Notes |
|------|--------|-------|
| NativeRally | All natives on board deal their counter-attack NOW | Burst damage via army |
| Specialize | Native becomes Champion (HP ×2, damage ×2) or Scout (+2 range, reveals) | Permanent upgrade |
| BuildOutpost | Place native infrastructure on territory: +1 native spawned on rest tides | Economy building |
| WarCry | Natives in territory and all adjacent: +2 damage this tide | AoE native buff |
| Recruit | Kill an invader, spawn a native in its place | Convert enemies to allies |
| ShieldWall | Natives in territory absorb ALL ravage damage (corruption goes to native HP) | Natives = shields |
| Ambush | If natives outnumber invaders in territory, deal 2× damage on counter-attack | Rewards positioning |

### Weave — The Card Manipulator (CONCEPT)
**Fantasy:** "The threads of fate bend to my will."
**Signature verbs:** Card recursion, pair manipulation, deck shaping

| Verb | Effect | Notes |
|------|--------|-------|
| Recall | Return 1 card from bottom-discard to hand | Cheat the rest economy |
| Amplify | Your paired card's TOP effect triggers twice | Double a top — this card is the bottom |
| Echo | At end of turn, repeat this card's top effect | Self-doubling top |
| Duplicate | Create temp copy of paired card: both go to discard, copy vanishes at encounter end | Extra card for 1 cycle |
| Resonance | If both paired cards share an element, both effects get +2 value | Rewards mono-element pairing |
| Foresight | Look at next 3 tide actions; you may reorder 1 pair of adjacent tides | Information + manipulation |
| TriplePlay | This turn: play 3 cards (top, bottom, and a second top from a third card) | Massive tempo burst |
| Preserve | When this card would be dissolved by rest, it isn't. (Once per rest.) | Self-protecting card |
| Transmute | Dissolve a card from hand; add its elements ×3 to pool | Convert stamina to elements |
| Resonate | This card's top effect repeats at the start of your NEXT turn automatically | Persistent echo |

---

## Cross-Warden Passives ("Relics")

Small passives available to ANY warden through events, merchants, or
progression. These create build archetypes and "busted combo" runs.

### Damage / Combat

| Passive | Effect | Combos with |
|---------|--------|------------|
| Thornwall | When invaders Ravage a territory with presence, they take 1 damage | Root (wide presence) |
| Scavenger | When an invader dies, gain 1 element of your primary type | Damage-focused wardens |
| Predator | First invader killed each turn: kill deals double fear | Fear builds |
| Brittle Touch | First damage you deal each turn applies "Brittle" to target (next hit kills) | Weak damage cards become assassins |

### Elements / Thresholds

| Passive | Effect | Combos with |
|---------|--------|------------|
| Elementalist | Element decay reduced by 1 (min 0) | Threshold builds, Veil |
| Overcharge | SLOW tops become FAST but deal -1 value | Speed-focused builds |
| Resonant Core | When any threshold fires, gain 1 element of each of your secondary elements | Multi-threshold builds |
| Awakening | When you hit T2, recover 1 card from dissolved pile | Threshold + stamina |

### Corruption / Territory

| Passive | Effect | Combos with |
|---------|--------|------------|
| Wildfire | When corruption reaches L1, deal 2 damage to all invaders there | Ember (self-corruption = AoE) |
| Restoration | When you reduce corruption by 3+, restore 1 weave | Cleanse builds |
| Sanctify | You can "overcharge" ReduceCorruption: excess cleanse on L0 territory becomes Fear | Cleanse → fear conversion |
| Blighted Roots | Your presence can exist in L2 territories (normally blocked) | Aggressive positioning |

### Cards / Economy

| Passive | Effect | Combos with |
|---------|--------|------------|
| Sacrificial | When a card is dissolved, generate 3 fear | Small hand builds, aggressive bottoming |
| Recycler | Cards you play as TOP have a 25% chance to not go to discard (stay in hand) | Stamina extension |
| Hoarder | +1 hand size (permanent, stacks) | Draft reward; more pairs per cycle |
| Efficient | Rest only dissolves 1 bottom instead of 2 | Massive stamina boost; premium relic |

### Natives

| Passive | Effect | Combos with |
|---------|--------|------------|
| Warden of the Wild | Natives regenerate 1 HP at start of each tide | Native investment payoff |
| Rallying Cry | When you spawn a native, all natives in that territory deal 1 damage | Spawn = AoE trigger |
| Symbiosis | Territories with both presence AND natives: card effects +1 | Rewards co-location |
| Deep Network | Presence counts double for adjacency checks | Root (network effects) |

### Fear / Dread

| Passive | Effect | Combos with |
|---------|--------|------------|
| Dread Feeder | Fear generation +1 per Dread Level (at Dread 3: +3 to all fear) | Fear builds, late game |
| Panic Cascade | When a Fear Action fires, adjacent territories get +1 corruption | Fear → corruption → Ember |
| Intimidation | Invaders in territories with 3+ fear deal -1 Ravage corruption | Defensive fear |

---

## Scaling Effects (Engine Pieces)

Effects that get stronger based on game state. These are the "build around"
cards that create divergent strategies.

| Effect | Scales with | Fantasy |
|--------|------------|---------|
| PresenceStrike (DI = presence count) | Total presence | "My roots ARE the weapon" |
| ElementSurge (DI = element count) | Element pool | "Pure elemental power" |
| CorruptionDetonate (DI = corruption, then cleanse) | Territory corruption | "Controlled explosion" |
| NativeRally (all natives counter-attack NOW) | Native army size | "The uprising" |
| DreadEcho (repeat last fear action) | Dread level / actions | "They're terrified" |
| SacrificialBurst (DI = cards dissolved this encounter) | Cards lost | "Every death feeds me" |
| ThresholdCascade (all thresholds fire again) | Element pool | "Everything triggers" |

These should be rare — draft cards, event rewards, or late-game progression.
Getting one warps your entire strategy for the run.

---

## Condition / Status Effects

Persistent effects on invaders, territories, or the warden. Create combo
setups ("apply X, then exploit X").

### On Invaders

| Condition | Effect | Duration | Setup → Payoff |
|-----------|--------|----------|---------------|
| Entangled | Can't move on March | 1 tide | Entangle → then kill at leisure |
| Brittle | Next damage kills regardless of HP | Until triggered | Brittle → any damage card |
| Marked | Takes +2 from next damage source | Until triggered | Mark → big hit |
| Feared | Doesn't Ravage this tide | 1 tide | Fear card → buy time |

### On Territories

| Condition | Effect | Duration | Setup → Payoff |
|-----------|--------|----------|---------------|
| Burning | Invaders entering take 2 damage | 2 tides | Ignite → invaders walk into damage |
| Verdant | Natives here: +1 HP regen, +1 damage | 2 tides | Verdant → native army powered up |
| Warded | Corruption gain -2 here | 2 tides | Ward → Ravage doesn't corrupt |
| Leylined | Effects here also hit linked territory | 2 tides | Leyline → double coverage |
| Sacred | Can't be corrupted past L1 | 3 tides | Sacred → safe zone |

### On Warden

| Condition | Effect | Duration | Setup → Payoff |
|-----------|--------|----------|---------------|
| Resonant | Next threshold fires twice | Until threshold fires | Build elements → double T2 |
| Focused | Next card top: +3 value | 1 turn | Focus → big top |
| Rooted | Can't place presence, but all effects +2 | 1 turn | Tradeoff: power vs mobility |

---

## Combo Examples (Theoretical)

### "The Corruption Engine" (Ember + Wildfire + CorruptionDetonate)
1. Ash Trail adds corruption to all presence territories each tide
2. Wildfire passive triggers at L1: 2 damage to all invaders
3. Build corruption to L2 on a key territory (Forest → Scorched!)
4. Play CorruptionDetonate bottom: deal 8 damage AoE + cleanse to 0
5. Territory is clean, invaders are dead, restart the cycle
6. Bonus: the Scorched terrain now deals 2 damage to invaders entering

### "The Kill Zone" (Any warden + PullInvaders + AoE)
1. Invaders are spread across A1, A2, A3 (1-2 each)
2. Play PullInvaders targeting A2: all invaders from A1 and A3 gather at A2
3. Now A2 has 4-5 invaders stacked
4. Next turn: pair DamageAll top with any bottom → wipe the stack
5. With Ember: Pull into corrupted territory → Ash Trail + detonate
6. With Root: Pull into territory with 3 natives → counter-attack hits all

### "The Fear Machine" (Root + Scavenger + Dread Feeder)
1. Scavenger: each kill gives +1 Root element
2. Root threshold T1 fires: deals damage, which kills more invaders
3. More kills → more elements → more thresholds
4. Dread Feeder: fear generation +1 per Dread Level
5. By Dread 3: every fear card generates +3 extra → fear actions cascade

### "The Immortal Network" (Root + Deep Network + Thornwall + Efficient)
1. Deep Network: presence adjacency doubled → Network Slow covers everywhere
2. Thornwall: invaders take 1 damage when they Ravage near presence
3. Efficient: only lose 1 card per rest instead of 2
4. Root plays 15+ turns without running out → slow grind, but invaders
   can barely move and take passive damage every tide

### "The Glass Cannon" (Ember + Sacrificial + Awakening)
1. Sacrificial: dissolved cards generate 3 fear
2. Awakening: hitting T2 recovers a dissolved card
3. Ember plays aggressive bottoms, cards get dissolved
4. Each dissolution = 3 fear → Dread climbs fast
5. When Ash T2 fires → recover a card → play it again → dissolve again
6. Loop: play → dissolve → fear → threshold → recover → repeat

### "Land Shaper" (Terraform warden + FertileGround + Leyline)
1. Terraform key territories to Forest (+1 to all effects)
2. FertileGround on the Forest: now +2 to all effects permanently
3. Leyline connects Forest to an adjacent territory
4. Every card targeting either territory gets +2 AND hits both
5. Two territories become one super-zone

---

## Multiplayer / Warden Aid (Stretch Goal)

Even in single-player, some form of warden synergy could exist:

**Option A: Aid Cards** — Events or merchants offer 1 card from another
warden's pool. "The Ember's dying spark grants you Corruption Detonate."
Root deck with one Ember card = new combo line.

**Option B: Allied Passive** — At run start or via event, choose 1 passive
from another warden's pool (base level, not upgraded). "Root gains Ash
Trail" — now presence territories gain corruption, enabling Ember-style
strategies on a presence-focused warden.

**Option C: True Co-op** — Two players, two wardens, shared board. Each
plays their own pairs. Thresholds are shared (both contribute elements).
One player's presence helps the other's adjacency. Massive design space
but massive complexity. STRETCH GOAL.

---

## Open Questions

1. How many cross-warden passives should exist at launch? 8-12 feels right.
2. Should scaling effects (PresenceStrike etc.) be on starting cards or
   draft-only? Draft-only preserves the "I found the engine piece" feeling.
3. How do conditions interact with the pairing system? Can a top apply
   Entangle and a bottom exploit it in the same turn? Probably yes — the
   top resolves first (Fast phase), Entangle is applied, bottom resolves
   (Dusk), benefits from Entangle.
4. Should cross-warden passives be offered as a choice of 3 (like upgrades)
   or found at fixed map locations?
5. Terrain types: how many is enough? 4-5 feels right for launch. More can
   be added per-encounter.

---

## Revision History

| Rev | Date | Changes |
|-----|------|---------|
| 1 | 2026-03-22 | Initial brainstorm — verbs, wardens, passives, combos |
