# Core Loop Redesign — Sequential Implementation Plan

## Claude Code Prompt

> Read `CORE_LOOP_PLAN.md` and `CORE_LOOP_REDESIGN.md` in the project root.
> The REDESIGN doc has full design rationale, card tables, and simulated turns.
> This PLAN doc has implementation tasks. Work through each TASK in order.
> If stuck, STOP and report. After each task print status. When done, print
> the full summary and delete `CORE_LOOP_PLAN.md`.

## Ground Rules

1. **Read before you write.** Read each referenced file before modifying.
2. **Build and test after every task.** Run `dotnet test` after each task.
3. **LOCALIZATION:** All player-facing strings use `Loc.Get()` + `strings.csv`.
4. **DATA-DRIVEN:** Card data, terrain data, decay rates — all JSON/BalanceConfig.
5. **Preserve what works.** Territory, corruption, invaders, fear/dread, presence,
   natives, progression, events, map — all unchanged. Only the TURN STRUCTURE
   and CARD LIFECYCLE change.
6. **If stuck:** Print `TASK N: STUCK — [reason]` and STOP.

---

## TASK 1: Card Model + Dual Discard Piles

**MODIFY `Card.cs`** — add `CardTiming` enum (Fast/Slow), `TopTiming` field.

**MODIFY `DeckManager.cs`** — replace single discard with:
- `TopDiscard` list (safe — always recovered on rest)
- `BottomDiscard` list (at-risk — 2 random dissolved on rest)
- `Dissolved` list (gone for encounter)
- Methods: `PlayAsTop(card)`, `PlayAsBottom(card)`, `SoakDamage(card)`,
  `Rest(rng)` → returns `RestResult` with which 2 were dissolved,
  `RerollDissolve(cardToSave, rng)` → save that card, random replacement dies

**MODIFY `IDeckManager.cs`** — update interface.

**MODIFY `WardenLoader.cs`** — parse `top_timing` from JSON.

**MODIFY `data/wardens/root.json` + `ember.json`** — add `"top_timing": "slow"`
to every card (actual values assigned in Task 5).

**Tests (8+):** PlayAsTop→TopDiscard, PlayAsBottom→BottomDiscard,
Rest recovers tops, Rest dissolves 2 random bottoms, Rest with 0-1 bottoms,
SoakDamage→TopDiscard, RerollDissolve mechanics, CardTiming parsed from JSON.

**Print:** `TASK 1: DONE (X tests)`

---

## TASK 2: Turn Structure — Pairing System

**NEW `CardPair.cs`** — `record CardPair(Card TopCard, Card BottomCard)` with
`TopIsFast`/`TopIsSlow` properties.

**MODIFY `TurnManager.cs`** — replace Vigil/Tide/Dusk phases with:
`Plan → Fast → Tide → Slow → Dusk → Elements → Cleanup`

Flow: Player submits pair → if Fast top, resolve before Tide → Tide runs
(Ravage/March/Arrive unchanged) → if Slow top, resolve after Tide → Dusk
resolves bottom → Elements: add top elements ×1, bottom ×2, check thresholds
(player targets each) → Cleanup: top→TopDiscard, bottom→BottomDiscard,
apply decay → next turn or rest.

**Rest turn:** No pair played. Tide still runs (invaders act). After tide:
execute Rest on DeckManager, show dissolved cards, offer rerolls.

**MODIFY `EncounterState.cs`** — add: `CurrentPair`, `RestCycleCount`,
`IsRestTurn`, `RootOfferingUsedThisCycle`.

Read `TideRunner.cs` — Ravage/March/Arrive logic is UNCHANGED. It still
runs in the Tide phase. Only its position relative to player effects changes.

**Tests (10+):** FastTop resolves before Tide, SlowTop resolves after Tide,
Tide runs during rest, elements contributed (top ×1, bottom ×2), cards go
to correct discard piles, hand empty forces rest, pair validation (must be
2 different cards), rest turn invaders still act.

**Print:** `TASK 2: DONE (X tests)`

---

## TASK 3: Element Decay Scaling + Root Passive

**MODIFY `ElementSystem.cs`** — decay scales with current tier:
Below T1: 1/turn. At T1: 2/turn. At T2: 3/turn. At T3: 4/turn.

**MODIFY `BalanceConfig.cs`** — add `ElementDecayBelowT1`, `ElementDecayAtT1`,
`ElementDecayAtT2`, `ElementDecayAtT3`, `RootRestExtraDecay`. All configurable
via SimProfile balance_overrides.

**MODIFY `RootAbility.cs`** — add Elemental Offering passive:
Once per rest cycle, discard a card from hand (→ top-discard, safe). Add
card's elements ×1 to pool. No effect resolves. `RootOfferingUsedThisCycle`
flag. Resets on rest.

Variant for later: dissolve instead of top-discard for ×2 elements.

**MODIFY rest logic** — Root loses extra elements (RootRestExtraDecay) on
rest. Ember does not. Offering flag resets.

**Tests (10+):** Decay at each tier level, crossing tier boundary,
RootOffering adds elements + card to top-discard, offering once per cycle,
offering resets on rest, Root extra decay on rest, Ember no extra decay.

**Print:** `TASK 3: DONE (X tests)`

---

## TASK 4: Terrain System

**NEW `TerrainType.cs`** — enum: Plains, Forest, Mountain, Wetland, Sacred,
Scorched, Blighted, Ruins, Fertile.

**MODIFY `Territory.cs`** — add `Terrain` + `TerrainTimer` fields.

**NEW `TerrainEffects.cs`** — static methods returning modifiers per terrain:
- `GetDamageModifier` (Forest +1 for both player AND invaders)
- `GetFearModifier` (Mountain +2)
- `GetCorruptionThresholdModifier` (Wetland +2)
- `GetInvaderEntryDamage` (Scorched 2)
- `GetCorruptionMaxLevel` (Sacred L1 max)
- `GetInvaderRavageCorruptionModifier` (Forest +1 — the trade-off)
- `GetInvaderRestHeal` (Wetland 1)
- `GetInvaderCounterAttackModifier` (Mountain +1)
- `CanSpawnNatives` (Scorched false)

**NEW `TerrainTransitions.cs`** — data-driven transitions:
Forest→Scorched at L2, Mountain→Ruins at L3, Sacred→Blighted on Settle,
Blighted→Plains when cleansed, Scorched→Plains after 3 clean tides,
Fertile→Plains when 3+ invaders.

**NEW `data/terrain_presets.json`** — terrain configurations per board.

**Wire into existing systems:** effect resolution adds terrain modifier,
Ravage adds invader terrain bonus, invader movement checks entry damage,
corruption checks Sacred max level, Blighted auto-corrupts per tide.
Call `TerrainTransitions.CheckTransitions` at tide end.

**MODIFY `EncounterConfig`** — add `TerrainPreset` + `TerrainOverrides`.

**Tests (15+):** Each terrain bonus/penalty, each transition trigger,
terrain preset loads correctly, trade-offs work both directions.

**Print:** `TASK 4: DONE (X tests)`

---

## TASK 5: Card Data Redesign

Read `CORE_LOOP_REDESIGN.md` and `WARDEN_DEEP_DIVE.md` for the full card
tables with redesigned values.

**REPLACE card definitions in `data/wardens/root.json`** — 10 cards with
Fast/Slow tags, redesigned tops and bottoms. Key cards:
- Deep Roots (SLOW: PP×2 / PP×3+SN×1) — core expansion
- Grasping Thorns (FAST: DI×2 / DI×4+Pull×2) — damage+pull combo
- Reclaim the Wild (SLOW: DI×3+RC×2 / DI×5+RC×4) — impossible choice
- Roots of Knowing (SLOW: DI×2+presence scaling / GF=total presence) — engine

**REPLACE card definitions in `data/wardens/ember.json`** — 8 cards:
- Wildfire (SLOW: Pull×3 / CorruptionDetonate) — THE Ember card
- Burning Ground (SLOW: DI×1 all / DI×2 all + AddCorruption×2) — dilemma
- Dying Light (FAST: RW×2+GF×2 / RW×4+GF×4 + conditional DI×3 at low weave)

**NEW effect types if needed:** PullInvaders, CorruptionDetonate, AddCorruption,
Ignite (territory status), ConditionalDamage (weave threshold).

Create any missing effect classes in `src/HollowWardens.Core/Effects/`.

Add localization keys for all card names and descriptions.

**Tests (9+):** Cards parse correctly, Fast/Slow assigned per design, bottoms
stronger than tops, new effect types resolve correctly, PullInvaders gathers
from adjacent, CorruptionDetonate deals damage = corruption then cleanses.

**Print:** `TASK 5: DONE (X tests)`

---

## TASK 6: Damage Soak + Push/Pull Rework

**Damage Soak:** Before heart damage applies, player may discard a card from
hand to block up to 3 damage. Card → top-discard (safe).

**Push rework:** PushInvaders moves invaders AWAY from territory. Player
chooses destination per invader (adjacent only). Can split across neighbors.

**Pull (new):** PullInvaders gathers up to N invaders FROM adjacent
territories INTO target. Combined with Scorched terrain = entry damage combo.

**Tests (8+):** Soak blocks up to 3, soak card goes to top-discard, push
player chooses per invader, push can split, pull gathers from adjacent,
pull respects max count, pull into Scorched = entry damage.

**Print:** `TASK 6: DONE (X tests)`

---

## TASK 7: Bot Strategy for Pairing

**MODIFY `BotStrategy.cs`** — bot picks pairs instead of individual cards.

Score all possible pairs (N×(N-1) orientations). Score considers:
- Effect value of both top and bottom
- Fast top bonus before Ravage tides
- Element synergy (matching elements build thresholds)
- Bottom risk penalty (don't risk high-value cards as bottom)

Bot rest decision: rest when hand ≤ 2 or empty.
Bot reroll decision: reroll if dissolved card value > 8 and weave > 6.
Bot offering decision (Root): use turn 1 if it reaches a threshold.

**Tests (8+):** Bot picks highest-scoring pair, prefers Fast before Ravage,
considers element synergy, penalizes risking good bottoms, rests when
appropriate, reroll logic, offering logic.

**Print:** `TASK 7: DONE (X tests)`

---

## TASK 8: Sim Validation

Run baseline sims with new pairing system:

```bash
dotnet run --project src/HollowWardens.Sim/ -- --seeds 1-100 --warden root --encounter pale_march_standard
dotnet run --project src/HollowWardens.Sim/ -- --seeds 1-100 --warden ember --encounter pale_march_standard
dotnet run --project src/HollowWardens.Sim/ -- --seeds 1-100 --warden root --encounter pale_march_scouts
dotnet run --project src/HollowWardens.Sim/ -- --seeds 1-100 --warden ember --encounter pale_march_scouts
dotnet run --project src/HollowWardens.Sim/ -- --mode chain --seeds 1-50 --warden root
dotnet run --project src/HollowWardens.Sim/ -- --mode chain --seeds 1-50 --warden ember
```

Paste all 6 summaries.

**Print:** `TASK 8: DONE`

---

## TASK 9: Godot UI — Pairing Interface

**MODIFY `GameBridge.cs`** — replace PlayTop/PlayBottom with `SubmitPair`,
`UseElementalOffering`, `SoakWithCard`, `RerollDissolve`.

**MODIFY `CardViewController.cs`** — pairing selection: click card 1
(selected), click card 2, assign Top/Bottom, confirm. Show Fast/Slow
indicator on each card.

**NEW: Rest Screen** — shows dissolved cards after rest, offers reroll
buttons (2 weave each), continue button.

**MODIFY phase indicator** — show new phases: Plan/Fast/Tide/Slow/Dusk.

**MODIFY territory view** — show terrain type and icon.

All strings via `Loc.Get()`.

**Print:** `TASK 9: DONE`

---

## TASK 10: Documentation

Update `SIM_REFERENCE.md` with: new turn phases, pairing mechanics, rest
mechanic, terrain system + presets, element decay scaling, Root passive,
new balance keys, new effect types.

**Print:** `TASK 10: DONE`

---

## FINAL SUMMARY

```
=== CORE LOOP REDESIGN COMPLETE ===

Tasks completed: X/10
Tests: [starting] → [final]

Per-task results:
  TASK 1  (Card Model + Dual Discard): [DONE/STUCK]
  TASK 2  (Turn Structure — Pairing): [DONE/STUCK]
  TASK 3  (Element Decay + Root Passive): [DONE/STUCK]
  TASK 4  (Terrain System): [DONE/STUCK]
  TASK 5  (Card Data Redesign): [DONE/STUCK]
  TASK 6  (Damage Soak + Push/Pull): [DONE/STUCK]
  TASK 7  (Bot Strategy): [DONE/STUCK]
  TASK 8  (Sim Validation): [DONE/STUCK]
  TASK 9  (Godot UI): [DONE/STUCK]
  TASK 10 (Documentation): [DONE/STUCK]

New files: [list]
Modified files: [list]
New effect types: [list]
Localization keys added: [count]

Sim results:
  Root standard: [summary]
  Ember standard: [summary]
  Chain root: [summary]
  Chain ember: [summary]

Deviations: [list or "None"]
Issues: [list or "None"]
==========================================
```
