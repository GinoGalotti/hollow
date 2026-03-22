# Hollow Wardens — Architecture Decisions

## Phase 5.5: Architecture Migration (New Design Decisions)

### D12: Bottom-as-dissolve replaces the old three-layer card model
**Decision:** Cards have two halves: Top (goes to discard, returns on Rest) and Bottom (dissolves when played — gone until next encounter, or permanently on Boss). The old explicit "Dissolve" action (a separate third layer) is removed.
**Rationale:** The old model had three independent choices per card (play top / play bottom / dissolve). The new model makes every bottom play a meaningful sacrifice decision without adding a third action type. Tops are repeatable; bottoms are finite per encounter. This gives each card two clear economies rather than three.
**Trade-off:** Removes the "dissolve any card for Presence reach" emergency escape valve. Cards no longer have a universal emergency use. Bottoms must now be designed with a default weak effect for cards where the primary bottom isn't great (see D9 obsolescence note below).
**Impact on existing decisions:**
- D1 (Root dormancy on Boss): Root's dormancy now triggers when Root *plays a bottom*, not when explicitly dissolving. First bottom play = Dormant (inert but in deck). Playing a Dormant card's bottom = permanent removal. Boss tier behavior preserved.
- D2 (signal deferred until after OnDissolve): Signal now named `BottomPlayed` rather than `CardDissolved`. Timing contract preserved — signals emit after OnBottom fires.
- D9 (DissolveEffect=null defaults): OBSOLETE. No longer needed. Every card's bottom IS the dissolve effect. Weak bottoms are explicitly designed, not defaulted.

### D13: Six elements (Root, Mist, Shadow, Ash, Gale, Void) — committed up front
**Decision:** Ship with exactly 6 elements from the start. Do not start with 4 and expand.
**Rationale:** Adding elements post-launch requires rebalancing every card, every encounter threshold event, and every warden. Committing to 6 now is cheaper even though the initial design cost is higher. 4 elements would be simpler to balance but creates a known rebalancing cliff.
**Elements and warden affinities:**
- Root: Root (heavy), Mist (medium), Shadow (light)
- Ember: Ash (heavy), Shadow (medium), Gale (light)
- Veil: Mist (heavy), Shadow (medium), Void (light)
**Decay rule:** Element pool reduces by 1 per element at end of each turn (minimum 0). This creates engine-building potential — consistent play of one element builds a rising floor across turns. See D20 for full rationale and math.
**Bottom doubles:** Playing a card's bottom adds its elements ×2 to the pool. So a [Root, Mist] bottom contributes Root×2, Mist×2.
**Display:** Element counts shown in HUD as 6 labeled counters. Threshold markers on each counter. Decayed values from previous turn shown muted behind current.

### D14: Pyramid territory layout (3-2-1) for V1; designed to scale
**Decision:** V1 uses a 6-territory pyramid (A1, A2, A3 arrival row; M1, M2 middle row; I1 inner) with the Sacred Heart as a non-territory target accessible from I1. Realm 2+ expands to 4-3-2-1 (10 territories).
**Rationale:** Replaces the 3×3 grid (D8). The pyramid gives natural chokepoints, a clear pressure direction, and strategic depth (sacrifice arrival row vs. defend the funnel). It also fits the thematic fantasy better than a neutral grid.
**Sacred Heart:** Not a territory; not subject to Corruption. Invaders in I1 can Activate against it, dealing Weave damage equal to their remaining HP (minimum 1). Some invader types have multiplied Heart damage.
**Trade-off:** Complete rewrite of TerritoryGraph.cs (previously a hardcoded 3×3 dict). All territory-related tests need rewriting. Pyramid layout also affects pathfinding logic in TideExecutor.

### D15: Corruption is a points-based system with three level thresholds
**Decision:** Territories track `CorruptionPoints` (an integer). Level thresholds: Level 1 at 3 points, Level 2 at 8 points (5 more), Level 3 at 15 points (7 more). `CorruptionLevel` is a computed property, not stored.
**Rationale:** Replaces discrete 0–3 corruption levels. The points system gives granular control — a territory can be "almost Tainted" (2 points) without being Tainted. This rewards precise cleansing (reduce 3 points = prevent level-up) and makes corruption feel like a resource to manage rather than a cliff.
**Persistence:**
- Level 0 → Level 1: resets to 0 (Clean) between encounters
- Level 2 (8–14 pts): persists as Level 1 (3 points) at next encounter start
- Level 3 (15+ pts): fully permanent — stays Desecrated for the rest of the run
**CorruptionLevel computed property:**
```csharp
public int CorruptionLevel => CorruptionPoints switch
{
    < 3 => 0,    // Clean
    < 8 => 1,    // Tainted
    < 15 => 2,   // Defiled
    _ => 3       // Desecrated
};
```
**Trade-off:** Existing tests on corruption advancement need rewriting. `ReduceCorruption` effect now reduces *points* (value = points removed), not levels. `Purify` effect removes one full level.

### D16: Natives are active board entities with HP, damage, and counter-attack behavior — TIMING + DAMAGE UPDATED
**Decision:** Each territory starts with 0–2 Native units (defined in EncounterData). Natives have HP=2, Damage=3. Damage model is **asymmetric**: invader damage to Natives auto-assigns to maximize kills (system-controlled); Native counter-attack damage is **player-assigned** (player distributes the damage pool across invaders). Natives die at 0 HP and generate 1 Fear.
**Timing update (D21):** Counter-attack now happens after Activate, before Advance. This means Natives can weaken or kill invaders before they move deeper into the pyramid.
**Damage model update (D23):**
- *Invader → Native:* System auto-assigns, targeting lowest HP first, allocating exactly enough to kill each before moving on. Maximizes kills. No player decision — invaders are a machine.
- *Native → Invader:* Player assigns the pooled damage freely. Focus fire, split, or any combination. An auto-assign mode (lowest HP first) is available as an implementation toggle for faster play.
- *Corruption simultaneous:* During Ravage, Native HP damage and territory Corruption happen in the same step.
**Rationale:** The game needed an active counterattack layer that rewards defensive play without requiring the warden to directly kill everything. Natives create a reason to protect territories even when you can't be everywhere. Their asymmetric stats (fragile but hit hard) make protecting them feel worthwhile. Player-assigned counter-attack adds tactical depth without slowing the invader phase.
**V1 spawn rule:** Default baseline: 0–1 Natives on entry territories (A-row), 2 on M-row and I-row (~6 total). Overridable per encounter via EncounterData. Between-encounter events can modify Native counts (e.g., blessing adds +1 Native to a territory, corruption event removes Natives from a territory).
**Trade-off:** Adds a new entity type and counter-attack phase to TideExecutor. Requires NativeUnit.cs, native-related signals, and EncounterData changes (native spawn count per territory). Player-assigned counter-attack requires UI for damage distribution (click invaders to assign damage, or drag-and-drop).

### D17: Fear actions are hidden, queued, and revealed at Tide start — UPDATED (Dread rename + retroactive upgrade)
**Decision:** Every 5 Fear spent queues one Fear Action drawn from the current Dread Level pool. The action is hidden until the start of the next Tide (before Activate). Dread Level (1–4) is determined by total Fear generated across the run (level advances every 15 total Fear). Pool is mixed: global cards + adversary-specific cards. **Retroactive upgrade:** when Dread Level advances, all queued (unrevealed) Fear Actions upgrade to the new level's pool.
**Naming:** "Fear Level" renamed to "Dread Level" to separate the escalation track from the Fear resource. Fear = what you generate and spend. Dread = the cumulative escalation (Dread 1–4).
**UX:** Queued Fear Actions displayed as face-down cards in the Tide queue area. At Tide start, cards flip face-up one at a time (~0.5s each) and resolve in sequence. Dread track displayed as a bar with threshold markers at 15/30/45, showing current total Fear generated and progress to next Dread Level.
**Rationale:** Hidden reveal at Tide start matches the fantasy (the spirit's dread manifests unpredictably) and creates genuine tension. Retroactive upgrade incentivizes frontloading Fear generation to push past a Dread threshold before queued actions reveal — a strategic depth layer. Adversary-specific cards give factions identity beyond their movement patterns.
**Timing:** Fear actions resolve before Activate (they can kill invaders before they act). This rewards fear generation as a proactive defense.
**Trade-off:** Requires FearActionDeck singleton, FearActionData resource, DreadLevel tracker (total Fear generated, never decremented — separate from Fear spent), and face-down card UI with flip animation.

### D18: Starting deck is 10 cards (Root baseline); full warden pool is 28 cards across rarities — UPDATED by D22
**Decision:** Root's starting deck is 10 Dormant-rarity cards. The full Root card pool is 28 cards (10 starting + 18 draft across Dormant/Awakened/Ancient rarities). Starting deck size is warden-specific: 10 is the baseline (Root, Veil), some wardens may have fewer (Ember: 8) to match their cycling identity. Starting deck is fixed per warden — no per-run customization in V1.
**Rationale:** 10 cards with the refill model (D22) gives 4 play turns before Rest — the right rhythm. 12 was too generous, allowing 5–6 play turns per cycle and making bottoms feel too free. Deck grows +1 per encounter reward; expect 13–16 by Realm 3.
**Trade-off:** Root cards JSON (cards-root.json) needs 2 cards moved from `starting: true` to `starting: false`. Currently has 12 starting cards; must be reduced to 10.

### D19: BoardToken abstract base class future-proofs non-Native territory tokens
**Decision:** NativeUnit and Infrastructure share a `BoardToken` base class with: `TokenType` enum, optional HP, and an `OnTidePhase(TidePhase phase)` virtual method. This lets TideExecutor iterate over `Territory.Tokens` without caring about specific types.
**Rationale:** Gino has identified many future token types (Brambles, Dangerous Terrain, Animals, Richness, Infrastructure). Building the abstraction now avoids retrofitting Territory to support each new type. The alternative — adding a field per token type — doesn't scale.
**V1 token types implemented:** NativeUnit, InfrastructureToken (invader-placed, stubs for effects).

### D20: Universal element thresholds at 4/7/11 with reduce-by-1 decay
**Decision:** Element thresholds are universal (same effects regardless of warden). Three tiers at 4, 7, and 11 of a single element. Decay model changed from halving to reduce-by-1 per element at end of each turn. Each threshold tier triggers once per turn, checked after each card play.
**Rationale:**
- *Universal thresholds:* Wardens differentiate by how easily they reach thresholds (via affinity), not what thresholds do. Reduces learning cost (6 elements × 3 tiers = 18 effects to learn, not 18 × N wardens). Makes elements a shared language.
- *Reduce-by-1 vs halving:* Halving kills momentum — a warden generating 2/turn oscillates between 2–3 and never builds. Reduce-by-1 creates a rising floor: 2/turn reaches steady state of 4–5 by turn 3–4, keeping Tier 1 active. Bottom plays spike above the floor to reach Tier 2. This rewards consistent element commitment and makes bottoms serve as element fuel in addition to their card effect.
- *4/7/11 thresholds:* 4 is reachable from tops alone by turn 3 for primary affinity (the "passive"). 7 requires 5+ turns of consistency or a bottom spike (the "commitment"). 11 is near-impossible without sustained play + bottom spikes (the "build-around"), firing 0–1 times per standard encounter.
- *Once-per-tier-per-turn trigger:* Prevents abuse from element-heavy turns while allowing both Vigil and Dusk to trigger different tiers. A bottom in Dusk can fire Tier 2 after a top fired Tier 1 in Vigil.
**Threshold effects (V1, subject to playtesting):**
- Root: T1=Place 1 Presence range 1, T2=Reduce Corruption 3 in Presence territory, T3=Place 2 Presence anywhere + cleanse 2 each
- Mist: T1=Restore 1 Weave, T2=Return 1 discard to hand, T3=Restore 3 Weave + return all discards (free Rest)
- Shadow: T1=Generate 2 Fear, T2=Next Fear Action from one level higher, T3=Generate 5 Fear + choose between 2 Fear Actions
- Ash: T1=1 damage all invaders in 1 territory, T2=2 damage all invaders in 1 territory + 1 Corruption there, T3=3 damage all invaders board-wide + 1 Corruption each territory
- Gale: T1=Push 1 invader toward spawn, T2=Push all invaders in 1 territory toward spawn, T3=Push all invaders board-wide toward spawn + skip next Advance
- Void: T1=1 damage to lowest-HP invader on board, T2=All invaders take 1 damage, T3=All invaders take 2 damage; kills from this don't generate Corruption
**Trade-off:** Higher thresholds (4/7/11 vs old 2/3/5) mean elements are less impactful on turn 1–2. The game's opening turns feel more like pure card play; element engine kicks in mid-encounter. This is intentional — early turns are about reading the board, mid-to-late turns are about engine payoff.
**Impact on existing decisions:** Supersedes the threshold placeholder in D13. D13's element list and bottom-doubling rule are preserved; only the decay model and threshold values change.
**Future design space:** Adversary element presence (factions contribute elements to pool on spawn) and adversary element thresholds (penalties if player builds too much of certain elements) are designed-forward but not V1 scope.

### D21: Two-pool faction action system with unit-type modifiers, rule-based cadence, and randomized waves
**Decision:** Each faction has two action pools — Painful (threats demanding response) and Easy (breathing room with minor effects). Each Tide draws from one pool based on encounter cadence rules. Within each pool, cards draw randomly without replacement, reshuffling when empty. Unit types within a faction apply modifiers to the faction action card. Spawn wave composition is randomized (2–3 weighted options per wave). Tide sequence renamed and reordered: Fear Actions → Activate → Advance → Arrive → Escalate → Preview.
**Rationale:**
- *Two pools vs single deck:* A single deck with shuffle symbols risks streaks of only painful actions. A fixed cycle (draw 2, reshuffle) becomes too rhythmic. Two pools with cadence rules guarantee a designer-controlled pain/relief pattern while preserving variance within each pool.
- *Rule-based cadence:* Default cadence is defined by `max_painful_streak` and `easy_frequency` per encounter tier. Designers can override with a hand-authored pattern array for specific encounters. Some encounters may pair harder cadence with easier arrivals, or vice versa — a deliberate balancing lever.
- *Unit-type modifiers:* All units of a faction execute the same action card, but unit types modify it (Ironclad deals +1 Corruption on Ravage, Outrider moves +1 on Advance, etc.). This creates readable board states: the player combines "which action" × "which units are where" to predict outcomes.
- *Randomized waves:* Player sees arrival *locations* from previous Tide but unit composition is revealed at Arrive. 2–3 weighted options per wave prevents memorization across runs. Dusk becomes the adaptation phase — you respond to what actually showed up.
- *Activate → Advance → Arrive ordering:* Matches Spirit Island's Ravage → Build → Explore. Existing units attack before moving. New arrivals are inert on their arrival Tide — one full turn cycle to respond.
- *Settle grants Shield 1:* Even when no Pioneer is present or 2+ unit condition isn't met, all units gain Shield 1 on Settle. Easy pool actions are never completely dead turns for invaders.
**Pale March V1 action cards:**
- Painful base: Ravage (2 Corruption + 1 damage to Natives, normal movement), March (Shield/heal + 2-step movement)
- Painful escalation: Corrupt (1 Corruption + kill 1 Native, normal movement), Fortify (Shield 2 token, hold position)
- Easy: Rest (recover half HP, normal movement), Settle (Pioneer builds + all gain Shield 1, hold position), Regroup (arrival row resets, hold position — removed at Escalation 3)
**Pale March V1 unit types:** Marcher (HP 3, baseline), Ironclad (HP 5, heavy/slow), Outrider (HP 2, fast/never stops), Pioneer (HP 2, builds Infrastructure after any action)
**Preview timing:** Action card + arrival locations revealed at end of each Tide. Player enters Vigil knowing what invaders will do and where new ones arrive, but NOT the arriving unit composition.
**Trade-off:** More complex than a single-deck system. Requires: InvaderActionCard resource (with pool tag, activate effect, advance modifier), CadenceManager (rule-based with override), SpawnWaveOption (weighted composition arrays), and 4 unit-type subclasses for the Pale March. TideExecutor rewrite is significant — step ordering changes and Native counter-attack timing moves to after Activate.
**Impact on existing decisions:** Supersedes the old Spawn → Advance → Activate ordering in §4.3. Replaces the single invader-type model in §4.7. D16 (Natives counter-attack) preserved but timing changes: Natives counter-attack after Activate, before Advance.

### D22: Refill draw model, 10-card starting deck, rest-dissolve tax
**Decision:** Card draw uses a refill model: at the start of each Vigil, draw from deck until hand = hand limit (default 5) or deck is empty. Unplayed cards stay in hand. Starting deck reduced from 12 to 10. Each Rest removes 1 random card from the deck (encounter-only on Standard/Elite; permanent on Boss). Element thresholds check against carryover pool at Vigil start even on Rest turns. When a threshold triggers, the player may resolve it immediately or bank it for the other phase (must use by end of turn).
**Rationale:**
- *Refill vs draw-5:* Draw-5 (StS model) resets the hand each turn — the player doesn't feel the deck thinning until it can't fill. Refill creates visible depletion: the hand degrades gradually as the deck empties. The player sees Rest approaching 2 turns ahead and can plan for it (build elements, position defensively).
- *10-card deck:* With refill and 2 tops/turn, 10 cards gives 4 play turns before Rest (turn 5). This is a healthier rhythm than 12 cards (which would give 5–6 play turns — too long between Rests, bottoms feel too free). 10 is the baseline; some wardens may have more or fewer (Ember: 8, faster cycling; Veil: 10 with hand limit 6, slower cycling).
- *Rest-dissolve:* Creates a stamina tax. More Rests = more cards lost. Aggressive bottom play accelerates deck depletion → earlier Rest → more rest-dissolve. This is the natural anti-frontloading mechanic: the system punishes aggression through physics, not reward penalties. The encounter doesn't end early (survival model), so frontloading 3 bottoms leaves the player with a thin deck for the remaining Tides.
- *Threshold timing (phase-flexible):* When a threshold triggers, the player chooses to use it now or bank it for the other phase. This adds a micro-decision (use Root Tier 1 Presence now in Vigil to set up, or save it for Dusk to react to the Tide?). Future design space: some thresholds may be phase-restricted for balance.
- *Rest-turn thresholds:* Carryover pool (after decay) is checked at Vigil start even on Rest turns. A player at Root×5 who Rests still has Root×4 after decay → Root Tier 1 fires → free Presence. This rewards building a strong element engine before Resting. The "dead" turn isn't fully dead.
**Run-level bottom budget:**
- Standard encounter (6–7 Tides): 2–3 bottoms typical. 1–2 Rests, 1–2 rest-dissolves. Encounter-only loss — all return between encounters.
- Boss encounter (10–13 Tides): Bottoms + rest-dissolves are permanent. Player must balance power output against preserving run-level deck integrity.
- Deck growth across a run: +1 card per encounter reward (draft). Expect deck size 13–16 by Realm 3, offsetting Boss losses.
**Trade-off:** Refill model requires tracking deck size separately from hand size. UI needs a visible deck counter so the player can anticipate Rest timing. Rest-dissolve being random adds variance the player can't fully control — a key card might be lost. This is intentional (the spirit is fraying), but may need a "choose which card to dissolve" variant for higher-skill modes.
**Impact on existing decisions:** D18 updated — Root starting deck is now 10 (not 12). Root's 28-card pool: 10 starting + 18 draft. Root cards JSON needs 2 cards moved from starting to draft pool. D12 (bottom-as-dissolve) unchanged in mechanic, but the refill model changes pacing around when bottoms feel affordable.

### D23: Asymmetric damage model + Root rest-dissolve goes dormant
**Decision (damage):** Combat damage is asymmetric. Invader damage to Natives auto-maximizes kills (system assigns, lowest HP first, exactly enough to kill before moving on). Native counter-attack damage is player-assigned (player distributes pooled damage freely across invaders). An auto-assign toggle (lowest HP first) is available for faster play.
**Rationale (damage):** Invaders are a machine — fast, efficient, no decisions needed. Natives are the player's allies — directing them is a tactical micro-decision. This keeps the enemy phase fast while adding depth to the player's counterattack. The auto-assign toggle ensures this is optional complexity, not mandatory busywork.
**Decision (Root rest-dissolve):** When Root rests, the rest-dissolved card goes **dormant** (not removed). It stays in the deck as a dead draw, recoverable via Awaken effects. This is consistent with Root's identity: even Root's losses are recoverable resources, at the cost of deck pollution.
**Rationale (Root rest-dissolve):** Root's identity is resilience. Every other warden loses a card on Rest; Root's card goes inert but stays. This makes Root's Rest less punishing in terms of deck size but more punishing in hand quality (more dead draws). Awaken effects (root_008, root_021) become even more important — they're not just recovering bottoms, they're also cleaning up rest-dissolve pollution. Subject to playtesting — if Root feels too resilient, switch to true removal.
**Trade-off:** Other wardens may want their own rest-dissolve variants in the future (e.g., Ember's rest-dissolve generates a Fear pulse before removing). The system should support warden-specific rest-dissolve behavior via an overridable method.

### D24: Playtest round 1-2 fixes (multiple sub-decisions)

**a) RAVAGE DAMAGE MODEL:** Ravage deals corruption to territory AND the same amount as a damage pool to Natives, auto-maximized (lowest HP first). The corruption value IS the native damage pool. Outrider pre-hit (2 damage to 1 native) happens before the Ravage pool. This replaces "1 damage to each Native."

**b) TIDE RAMP-UP:** Tide 1 runs Advance + Arrive only (no Activate, no counter-attack). Tide 2+ runs the full sequence. Gives the player one turn to see threats before invaders attack.

**c) INITIAL WAVE:** Wave 0 arrives before the player's first Vigil. The board has invaders on A-row when the game starts.

**d) STARTING PRESENCE:** Root starts with 1 Presence on I1.

**e) ROOT DORMANT TO DISCARD:** When Root plays a bottom, the dormant card goes to the discard pile (not the draw pile). The player doesn't see it again until Rest shuffles discards back in. Rest-dissolve dormant stays in the draw pile (already shuffled in during Rest).

**f) SEEDED RANDOMNESS:** All randomness goes through a shared `GameRandom` instance seeded at encounter start. The seed is logged and exportable for replay.

**g) ACTION LOG:** Every player action is recorded with turn/phase/type/card/target. Exportable as seed + action sequence string. Foundation for future undo (truncate log, replay from seed).

**h) THRESHOLD RESOLUTION IS PLAYER-CONFIRMED:** Element thresholds are not auto-resolved. They appear as clickable buttons in the UI. Targeted thresholds enter targeting mode. Untargeted thresholds show an OK button to confirm. Unresolved thresholds are lost at end of turn.

**i) FEAR ACTION RESOLUTION:** Fear Actions revealed at Tide start are shown to the player one at a time. Targeted actions enter targeting mode. The player confirms each before proceeding.

**j) DEBUG OVERLAY:** Toggleable full-screen event log (D key). Logs all game events (card plays, element changes, thresholds, fear, tide steps, combat, counter-attacks, targeting) with color coding by event type. Max 200 entries, auto-scroll. Essential for playtesting — lets the designer see exactly what the system is doing each step.

**k) ACTION LOG EXPORT:** Press P to print the full seed + action sequence to the console. Reproducible game states for bug reporting.

### D25: Native counter-attack is conditional and player-assigned

**a) PROVOCATION:** Natives only counter-attack when the invader action this Tide was Ravage or Corrupt (damage actions). On Rest, Settle, March, Regroup, and Fortify, Natives stay passive. They don't fight unless provoked.

**b) PLAYER-ASSIGNED:** When counter-attack triggers, the player sees the damage pool and assigns damage to invaders manually. The player can choose to skip (assign 0). This matches the D23 asymmetric damage model but adds explicit player agency to the native side.

**c) FUTURE — AROUSE:** An `Arouse` effect type (warden abilities, element thresholds) can force Natives to counter-attack on non-damage turns. This creates design space for a Teacher Warden or native-focused builds.

**d) RATIONALE:** Matches Spirit Island's Dahan behavior. Makes the action card preview more meaningful — seeing Ravage incoming means Natives will fight back, seeing March means they won't. Prevents the "invaders just die to Natives" balance problem observed in early playtesting where Natives auto-killed on every Tide.

### D26: Tide phase has explicit player confirmation between sub-phases

The Tide is not a single automated sequence. It pauses for player input between sub-phases:
1. Fear Actions reveal one at a time — player resolves each (with targeting if needed)
2. Player presses Space → Activate runs (invaders act)
3. Counter-attack prompts per territory if the action was Ravage or Corrupt — player assigns damage
4. Player presses Space → Advance + Arrive run
5. Preview next action card

This prevents the Tide from being an unreadable blur and gives the player agency during the invader phase. Each pause is a deliberate decision point, not just an animation beat.

### D27: Element threshold resolution is always player-visible and player-confirmed

Thresholds never auto-resolve silently. When triggered:
- The threshold button lights up in the element tracker UI
- Player clicks to resolve (with territory targeting for spatial effects)
- Unresolved thresholds are lost at end of turn
- All three tiers (T1/T2/T3) are always visible with descriptions so the player knows what they're working toward at all times

### D28: Presence value system — amplification, vulnerability, and sacrifice
**Decision:** Presence gains three new roles beyond range-anchoring:

a) **AMPLIFICATION (V1):** Every card effect targeting a territory with Presence gets +1 value per Presence token there. ReduceCorruption ×2 with 1 Presence = 3. DamageInvaders ×4 with 2 Presence = 6. This is universal across all wardens. It makes placement decisions meaningful ("where do I stack?" vs "where do I spread?") and directly addresses Root's damage gap — Root's 2 damage bottoms become much stronger in high-presence territories.

b) **VULNERABILITY (V1):**
- Corruption Level 2 (Defiled, 8+ pts): blocks new Presence placement in that territory. Must cleanse below 8 to expand there again.
- Corruption Level 3 (Desecrated, 15+ pts): destroys ALL Presence in that territory. Catastrophic loss — engine bonus, range anchor, and warden passives all gone.

c) **SACRIFICE (V1):** Free action during Vigil or Dusk (no card needed). Sacrifice 1 Presence → cleanse 3 Corruption in that territory. Emergency brake when you don't have a cleanse card. Creates push-your-luck: invest presence for engine value, or hold in reserve as emergency currency.

d) **WARDEN PASSIVES:** Each warden has a unique presence passive in addition to the universal +1 amplification. Root: Network Fear (already designed). Future wardens: Ember (passive fire damage), Veil (movement reduction), Teacher (native damage boost).

**Rationale:** On a 6-territory pyramid, 3 presence covers everything at range 1. Without additional value, placement is a solved problem by turn 3. The amplification bonus creates an ongoing engine (stack for power vs spread for coverage), vulnerability creates stakes (losing presence to Desecration is devastating), and sacrifice creates emergency options (burn your engine to survive).

**Balance impact (requires playtesting — see open questions 11–15):**
- Invader HP may need to increase (Marchers from 3→4?) since damage cards are now stronger.
- Base corruption rate may need to increase if amplified cleanse is too strong.
- Escalation cards (Corrupt) should threaten presence directly — "remove 1 Presence from this territory" as an additional effect.

**Trade-off:** Adds complexity to every card play (player must consider presence count at target). UI needs to show the amplified value, not just the base value. Sacrifice needs its own UI interaction. All worth it for the decision density gained.

### D29: Root combat toolkit — Network Slow, Presence Provocation, Grasping Roots, Rest Growth
**Decision:** Root gets four new mechanics to address the inability to interact with invaders:

a) **NETWORK SLOW:** Invaders in territories adjacent to 2+ Root Presence territories have −1 Advance movement (minimum 0). A dense Root network acts as a web that physically slows invaders. An invader surrounded by presence can't advance at all. This rewards the "spread wide" strategy.

b) **PRESENCE PROVOCATION:** Natives in territories with Root Presence counter-attack on ANY invader action, not just Ravage/Corrupt. This is Root's specific exception to D25. Without Root presence, natives only fight when attacked (D25 rule). With it, they fight every Tide. Presence placement becomes about enabling your combat layer.

c) **GRASPING ROOTS (new starting card):** Replaces root_011 (Reclaim the Soil) in the starting deck. Root, Root elements. Top: DamageInvaders ×2, range 1. Bottom: DamageInvaders ×3 + SlowInvaders, range 2. This gives Root one repeatable damage top. 2 base damage + presence amplification = 3+ damage per play, enough to wound Marchers and kill Outriders. Reclaim the Soil moves to the draft pool.

d) **REST GROWTH:** When Root rests, place 1 free Presence on any territory with existing Presence. The roots grow while the spirit sleeps. Compensates for presence consumed by sacrifice, lost to Desecration, and the fact that Grasping Roots replaced a Presence-placement card.

**Rationale:** Root's starting deck had zero damage tops and only 2 damage bottoms. On a 6-territory pyramid, invaders reach the Heart by Tide 3-4 with nothing to slow them. Root's encounter-closing strategy is now: build presence network (slows invaders + activates natives), play Grasping Roots to wound invaders passing through, let natives finish them on counter-attack, generate fear for fear actions that deal damage at Tide start, and assimilate at Resolution. Root doesn't burst — it creates a hostile environment where invaders erode over multiple Tides.

**Balance notes:**
- Grasping Roots at ×2 damage + presence amplification means 3-4 damage per play in a developed territory. Marchers (3 HP) die in one amplified play. This might be too efficient — may need to reduce to ×1 base damage if playtesting shows invaders dying too easily.
- Network Slow at −1 is significant on a 3-row pyramid. An invader moving from A→M→I normally takes 2 Tides. With Network Slow active in M-row, it takes 3-4 Tides. This may stall the game too much — playtest.
- Presence Provocation + natives (3 damage each, 2 per territory) means 6 damage per territory per Tide from natives alone. Against 2 Marchers (6 HP total), natives kill them all. This is intentional — Root's strength IS the natives. But it means the player's agency is "place presence, natives do the killing." If that feels too passive, consider reducing native damage to 2.

**Impact on cards-root.json:** root_011 (Reclaim the Soil) changes from starting:true to starting:false. New card root_025 (Grasping Roots) added as starting:true.

### D29a: Network Slow territory counting — RESOLVED
**Implemented:** Option C — outnumber check. `RootAbility.GetMovementPenalty()` applies −1 movement when the count of adjacent territories with Root Presence **strictly outnumbers** the alive invader count in the target territory. A wave of 3 marchers pushes through 2 presence neighbors (2 ≤ 3, no slow); a lone scout with 2 presence neighbors is trapped (2 > 1, slowed). This gives dense Root networks real stopping power against scouting units while letting coordinated waves break through.
**Gating:** Also gated by PassiveGating — inactive until `network_slow` passive is unlocked (Shadow T1 trigger). See D32.

### D30: Passive progression — RESOLVED (see D32)
**Implemented as encounter-level gating**, not cross-encounter progression. Root starts each encounter with 3 always-active passives (network_fear, dormancy, assimilation). Network Slow, Presence Provocation, and Rest Growth unlock mid-encounter when specific element thresholds first trigger. Passives reset between encounters — no run-level carryover in V1. Full per-warden configuration in D32.

### D31: Balance tuning pass — first sim-driven adjustments
**Changes made based on 500-encounter simulation (100% Clean, zero tension):**
- **Network Fear halved:** Changed from directed edges (2 Fear per pair) to undirected (1 Fear per pair) — the primary balance lever. See D3.
- **Presence cap:** 3 tokens per territory, enforced by `PresenceSystem.PlacePresence()` via `MaxPresencePerTerritory = 3`. `Territory.PresenceCount` is an unbounded `int`; the cap lives in the system layer.
- **Invader HP — confirmed unchanged:** Marcher=3, Ironclad=5, Outrider=2. HP is not the primary difficulty lever at this encounter scale.
- **Ravage corruption — confirmed unchanged:** Marcher=2, Ironclad=3, Outrider=1, Pioneer=2. Base corruption rates held; Network Fear adjustment resolved the tension deficit.
**Next pass:** Manual playtest data + second sim run will inform HP/corruption tuning if needed.

### D32: Passive gating — wardens unlock passives during encounter
**Decision:** Wardens start each encounter with a subset of passives active. Others unlock when specific element thresholds are first triggered during the encounter. Unlocks persist for the encounter but reset between encounters (no run-level carryover in V1).

`PassiveGating.cs` manages tracking for both wardens. Each warden defines its base passives and unlock conditions in `InitializeRoot()` / `InitializeEmber()`.

**Root config:**
- Base passives (always active): `network_fear`, `dormancy`, `assimilation`
- Unlock conditions: `rest_growth` (Root T1), `presence_provocation` (Root T2), `network_slow` (Shadow T1)

**Ember config:**
- Base passives (always active): `ash_trail`, `flame_out`, `scorched_earth`
- Unlock conditions: `ember_fury` (Ash T1), `heat_wave` (Ash T2), `controlled_burn` (Shadow T1), `phoenix_spark` (Gale T1)

`PassiveGating.ForceUnlock()` / `ForceLock()` allow sim profiles and tests to override gating state. `PassiveGating.Reset()` restores warden defaults for a new encounter.

### D33: Ember warden — burst damage / glass cannon
**Identity:** A dying fire spirit that trades board health for raw damage. Where Ember is present, corruption spreads — but invaders burn.

**Elements:** Ash (primary), Shadow (secondary), Gale (tertiary).
**Starting deck:** 8 cards (smaller than Root's 10 — faster cycling, more Rests, more rest-dissolves).
**Starting presence:** I1 (same as Root).

**Key mechanics (implemented in EmberAbility.cs and ember.json):**
- **Ash Trail** (always active): At Tide start, each presence territory gains 1 Corruption and all invaders there take 1 damage (currently, subject to balance tuning).
- **Flame Out** (always active): Bottoms are always permanently removed — no Dormancy. Every bottom is a one-shot.
- **Scorched Earth** (always active, Resolution): Deals damage to all invaders equal to total corruption across presence territories (lowest HP first). Then smart-cleanse: L0/L1 → fully cleanse; L2 → halve points (round down); L3 → no change (permanent).
- **Ember Fury** (unlocks at Ash T1): All Ember card damage effects get +1 per territory at Corruption Level 1+ (Tainted or worse).
- **Heat Wave** (unlocks at Ash T2): On Rest, deal 2 damage to all invaders in all presence territories (currently, subject to tuning).
- **Controlled Burn** (unlocks at Shadow T1): At Tide start, if 3+ territories are at Corruption Level 1, generate 2 Fear.
- **Phoenix Spark** (unlocks at Gale T1): When a card is permanently removed, generate 3 Fear.

**Presence tolerance:** `PresenceBlockLevel() → 3` — Ember can place Presence in Defiled (L2) territories. Only Desecrated (L3) blocks placement. Ember lives in corrupted land.

**Resolution:** Scorched Earth (see above) — damage scales with how much corruption Ember has let accumulate.

**Balance:** Damage values and corruption amounts are actively being tuned. Read `data/wardens/ember.json` and `EmberAbility.cs` for current values.

### D34: Simulation workbench for balance testing
**Decision:** A deterministic simulation infrastructure for running automated encounters at scale.

**Core library additions (`src/HollowWardens.Core/Run/`):**
- `IPlayerStrategy.cs` — interface for automated play
- `BotStrategy.cs` — Root bot: presence expansion → damage → cleanse → fear → any card
- `EmberBotStrategy.cs` — Ember bot: presence expansion → damage → cleanse at 7+ corruption → fear → any card
- `SimStats.cs` / `SimStatsCollector.cs` — per-encounter + per-tide statistics (outcome, tide counts, fear generated, corruption peaks)
- `EncounterRunner.cs` — full encounter lifecycle (start → tides → resolution → result)
- `ReplayRunner.cs` — deterministic replay from seed + action string

**Sim console app (`src/HollowWardens.Sim/`):**
- `Program.cs` — CLI entrypoint: `--seeds 1-500`, `--warden root|ember`, `--verbose`, `--profile <path>`
- `SimProfile.cs` — JSON-driven configuration with warden/encounter/balance overrides
- `SimProfileApplier.cs` — applies SimProfile overrides to BalanceConfig before encounter start
- `VerboseLogger.cs` — detailed turn-by-turn logs with bot decision reasoning (first 5 encounters + all breaches)

**BalanceConfig centralization:** All tunable constants live in `BalanceConfig.cs` (stored on `EncounterState`). Replaces hardcoded values across 13+ files. SimProfile overrides populate BalanceConfig before encounter start.

**CLI examples:**
```
dotnet run --project src/HollowWardens.Sim/ -- --seeds 1-500 --warden root
dotnet run --project src/HollowWardens.Sim/ -- --profile sim-profiles/X.json
dotnet run --project src/HollowWardens.Sim/ -- --seeds 1-5 --verbose
```

---

## Phase 5: Root Warden (unchanged decisions)

### D1: Dormancy overrides Boss dissolution — UPDATED CONTEXT
**Decision preserved.** But now "dissolve" means "play the bottom." Root's OnBottom fires instead of the default bottom behavior: first bottom play on a card = Dormant state. Boss encounter: playing the bottom of an already-Dormant card = permanent removal.
**Updated flow:**
1. Player plays bottom of a non-dormant Root card
2. `WardenRoot.OnBottom()` fires: card goes to Deck with IsDormant=true (not to DissolvedThisEncounter)
3. On Boss: if IsDormant=true and bottom played → card goes to PermanentlyRemoved
4. Signals emit based on final state after OnBottom fires (D2 timing preserved)

### D2: BottomPlayed signal emission deferred until after OnBottom
**Decision:** `BottomPlayed` and `CardPermanentlyRemoved` signals emit based on which list the card is in AFTER OnBottom fires.
**Rationale:** Unchanged from D2. Warden subclasses override the outcome before UI reacts.
**Note:** Signal renamed from `CardDissolved` to `BottomPlayed` to reflect the new model.

### D3: Network Fear counts undirected edges — UPDATED
**Decision:** Each adjacent Presence pair contributes **1 Fear** (undirected, each pair counted once). Previously counted directed edges (2 Fear per pair), halved in the balance tuning pass after simulation showed runaway Fear generation.
**Implementation:** `PresenceSystem.CalculateNetworkFear()` skips pairs where `id ≥ neighborId` (lexicographic ordering), visiting each adjacency once. Source comment: "Bugfix: count undirected edges only (each pair once, not twice)."
**Cap:** `RootAbility.CalculatePassiveFear()` caps Network Fear output at `BalanceConfig.NetworkFearCap` (currently 4 per Tide, subject to balance tuning). The cap is a Root-specific balance knob, not a system-wide rule.

### D4: Assimilation removes invaders up to PresenceCount — UPDATED
**Decision updated (D30 nerf applied).** At Resolution, for each territory with Presence, `RootAbility.OnResolution()` removes up to `PresenceCount` invaders (weakest HP first) from each adjacent territory. Each removed invader reduces Corruption by 1 in that territory. Stacking presence increases how many invaders each territory can absorb from each neighbor. Previously removed ALL invaders unconditionally — nerfed so assimilation scales with presence investment rather than wiping boards outright.

### D5: AwakeDormant Value=0 means "awaken all" — unchanged
No change. Still valid.

---

## Phase 1–4: Core Architecture (unchanged decisions)

### D6: CardEngine is a plain C# class (not a Node) — unchanged
No change. Still pure logic, injectable dependencies.

### D7: EventBus signals use Godot signals — unchanged
No change.

### D8: TerritoryGraph hardcoded adjacency — SUPERSEDED by D14
The 3×3 adjacency dict is replaced by the pyramid (3-2-1) dict. The principle (hardcoded static adjacency, never changes at runtime) is preserved. The data itself changes.

### D9: DissolveEffect=null defaults to PlacePresence — OBSOLETE
Removed. There is no separate Dissolve action. Every card's bottom IS the dissolve. Weak bottoms are explicitly designed (e.g., "Place 1 Presence, range 1" as a default fallback bottom written explicitly in the card data).

### D10: Custom navigation actions — unchanged
No change.

### D11: CSV localization — unchanged
No change. New keys added for elements, fear levels, natives.
