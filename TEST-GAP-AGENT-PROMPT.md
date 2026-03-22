# Hollow Wardens — Test Gap Remediation (Agentic)
> Drop this file in the repo root and run:
> `claude --print < TEST-GAP-AGENT-PROMPT.md`
> or paste the contents as a single Claude Code prompt.
> Claude will work through all tasks sequentially without further input.

---

## Your Role

You are working autonomously on the `HollowWardens.Core` xUnit test project.
Work through every task in the TASK LIST below, in order, without stopping for confirmation.
After completing ALL tasks, print a final summary report.

---

## Ground Rules

1. **Read before you write.** Before adding any test, read the relevant existing test file
   and the relevant production class to understand current structure, naming conventions,
   and what helpers already exist. Never guess at class or method names.

2. **One task at a time.** Complete a task fully (write, build, run, confirm green) before
   moving to the next. Do not batch tasks together.

3. **Build and run after every task.** After writing each new test class or modifying an
   existing one, run `dotnet test --filter "ClassName=<TestClassName>"` to confirm all new
   tests pass before proceeding. If a test fails, fix it before moving on.

4. **Do not change production code** unless a task explicitly says to. If a test reveals a
   genuine bug, note it in your final summary report but leave the production code unchanged.

5. **Never delete existing passing tests.** You may modify a test if a task explicitly
   instructs it (Task 10A). All other existing tests must remain green throughout.

6. **If a task says "skip if not applicable",** write a one-line comment in the test file
   explaining why it was skipped, then move on.

7. **Naming conventions:** Match the existing test file's naming style exactly.
   Use the method names suggested in each task unless they conflict with an existing name.

8. **Stop condition:** After Task 17, print the FINAL SUMMARY REPORT and stop.

---

## TASK LIST

---

### TASK 1 — Presence Amplification Tests

**Context:**
Every card effect targeting a territory with Presence gets +1 value per Presence token there.
e.g. `ReduceCorruption ×2` in a territory with 2 Presence tokens → cleanses 4 points, not 2.
This applies to: `ReduceCorruption`, `DamageInvaders`, `GenerateFear`, `ShieldNatives`.
`GenerateFear` only benefits from presence if the territory has invaders.

**File:** Create `PresenceAmplificationTests.cs` in the test project.

**Write these tests:**
- `ReduceCorruption_WithOnePresence_CleansesPlusOne` — ReduceCorruption ×2, 1 presence → 3 pts removed
- `ReduceCorruption_WithTwoPresence_CleansesPlusTwoTotal` — ×2 with 2 presence → 4 pts removed
- `DamageInvaders_WithPresence_AddsPresenceCountToDamage` — DamageInvaders ×4, 1 presence → 5 damage
- `GenerateFear_WithPresence_InInvaderTerritory_GivesBonus` — GenerateFear ×3, 1 presence, territory has invaders → 4 Fear
- `GenerateFear_WithPresence_NoInvaders_NoBonus` — GenerateFear ×3, 1 presence, NO invaders → 3 Fear (no bonus)
- `ShieldNatives_WithPresence_IncreasesShieldValue` — ShieldNatives ×2, 1 presence → shield 3
- `ShieldNatives_Baseline_NoPresence` — ShieldNatives ×2, 0 presence → shield 2 (add this if ShieldNatives has no existing baseline test)
- `AmplificationDoesNotApply_WhenNoPresenceInTerritory` — ReduceCorruption ×2, 0 presence → removes exactly 2 pts

**After writing:** `dotnet test --filter "ClassName=PresenceAmplificationTests"`

---

### TASK 2 — Presence Vulnerability Tests

**Context:**
- Corruption Level 2 (Defiled, 8+ pts): Cannot place NEW Presence. Existing presence survives.
- Corruption Level 3 (Desecrated, 15+ pts): ALL Presence in the territory is destroyed.

**File:** Create `PresenceVulnerabilityTests.cs` in the test project.

**Write these tests:**
- `CanPlacePresence_InCleanTerritory` — 0 corruption → PlacePresence succeeds
- `CanPlacePresence_InTaintedTerritory` — 3–7 pts → PlacePresence succeeds
- `CannotPlacePresence_InDefiledTerritory` — 8+ pts → PlacePresence is rejected/no-op
- `CannotPlacePresence_InDesecratedTerritory` — 15+ pts → PlacePresence is rejected/no-op
- `ExistingPresenceSurvives_WhenTerritoryBecomesDefiled` — territory has 1 presence, corruption reaches 8 pts → presence count still 1
- `PresenceDestroyed_WhenTerritoryBecomesDesecrated` — territory has 2 presence, corruption reaches 15 pts → presence count = 0
- `PresenceDestroyedEvent_Fires_OnDesecration` — reaching Level 3 with presence tokens → relevant destroyed event/signal fires

**After writing:** `dotnet test --filter "ClassName=PresenceVulnerabilityTests"`

---

### TASK 3 — Presence Sacrifice Tests

**Context:**
Presence Sacrifice is a free action (no card needed) available in Vigil or Dusk:
"Sacrifice 1 Presence → Cleanse 3 Corruption in that territory."
It is NOT a card play — it does not consume the player's card play allowance.

**File:** Create `PresenceSacrificeTests.cs` in the test project.

**Write these tests:**
- `PresenceSacrifice_ReducesPresenceCountByOne` — territory has 2 presence → sacrifice → 1 presence remains
- `PresenceSacrifice_CleanseThreeCorruption` — 8 pts corruption, 1 presence → sacrifice → 5 pts corruption
- `PresenceSacrifice_ClampsAtZero` — 2 pts corruption, 1 presence → sacrifice → 0 pts (not negative)
- `PresenceSacrifice_NotAvailable_WhenNoPresence` — 0 presence → sacrifice attempt → no-op or exception handled gracefully
- `PresenceSacrifice_IsAvailableInVigil` — can be called during Vigil phase
- `PresenceSacrifice_IsAvailableInDusk` — can be called during Dusk phase
- `PresenceSacrifice_DoesNotCountAsCardPlay` — sacrificing does not decrement the phase's card play allowance

**After writing:** `dotnet test --filter "ClassName=PresenceSacrificeTests"`

---

### TASK 4 — Root Network Slow Tests

**Context:**
Root passive: "Invaders in territories adjacent to 2 or more Root Presence territories
have −1 Advance movement (minimum 0)."
This is adjacency-based (neighbouring territories have presence), not co-location-based.
Distinct from Network Fear which IS already tested.

**File:** Create `NetworkSlowTests.cs` in the test project.

**Write these tests:**
- `NetworkSlow_ReducesAdvance_WhenAdjacentToTwoPlusPresenceTerritories` — invader adjacent to 2 presence territories → Advance = base - 1
- `NetworkSlow_DoesNotApply_WithOnlyOneAdjacentPresenceTerritory` — 1 adjacent presence territory → normal advance
- `NetworkSlow_DoesNotApply_WhenPresenceIsInSameTerritory` — presence is in invader's OWN territory, not adjacent → no slow
- `NetworkSlow_ClampsAdvanceAtZero` — base Advance 1, Network Slow applies → Advance = 0, not -1
- `NetworkSlow_FullySurrounded_CannotAdvance` — invader in M1 with Root presence in all adjacent territories → Advance = 0

**After writing:** `dotnet test --filter "ClassName=NetworkSlowTests"`

---

### TASK 5 — Root Provocation Exception Tests

**Context:**
Standard rule: Natives counter-attack only on Ravage or Corrupt.
Root exception (§8.1): Natives in territories WITH Root Presence counter-attack on ANY
invader action. This is presence-gated — the presence is what enables always-on provocation.

**File:** Create `RootProvocationTests.cs` in the test project.

**Write these tests:**
- `RootPresence_ProvokesOnMarch` — territory has Root Presence + Natives, action = March → counter-attack triggers
- `RootPresence_ProvokesOnSettle` — same setup, action = Settle → counter-attack triggers
- `RootPresence_ProvokesOnRest` — same setup, action = Rest → counter-attack triggers
- `RootPresence_ProvokesOnRegroup` — same setup, action = Regroup → counter-attack triggers
- `RootPresence_StillProvokesOnRavage` — Root presence + Ravage → counter-attack triggers (baseline sanity)
- `NoRootPresence_DoesNotProvoke_OnMarch` — NO Root presence, action = March → counter-attack does NOT trigger
- `RootProvocation_OnlyApplies_ToPresenceTerritories` — presence in M1 only; March fires → M1 natives counter-attack, A1 natives do not

**After writing:** `dotnet test --filter "ClassName=RootProvocationTests"`

---

### TASK 6 — Root Rest Bonus Tests

**Context:**
Root's "Roots Grow" rest bonus: when Root rests, in addition to normal shuffle + rest-dissolve,
place 1 Presence for free on any territory that ALREADY HAS existing Presence.
Other wardens do NOT get this bonus.
Free placement is still subject to Defiled blocking.

**File:** Create `RootRestBonusTests.cs` in the test project.

**Write these tests:**
- `RootRest_PlacesFreePresence_OnTerritoryWithExistingPresence` — territory has 1 presence, Root rests → territory has 2 presence
- `RootRest_FreePresence_CannotPlaceOnEmptyTerritory` — all territories have 0 presence → Root rests → no presence placed
- `RootRest_FreePresence_BlockedByDefiled` — only valid target is Defiled (Level 2) → free placement is blocked
- `RootRest_NormalRestEffects_StillApply` — shuffle + rest-dissolve still occur alongside free presence
- `NonRootWarden_Rest_DoesNotPlaceFreePresence` — non-Root warden rests → no presence placement

**After writing:** `dotnet test --filter "ClassName=RootRestBonusTests"`

---

### TASK 7 — Missing Ash T1 and Gale T1 Threshold Tests

**Context:**
ThresholdResolverTests.cs has T1 tests for Root, Mist, Shadow, Void — but NOT Ash or Gale.
- Ash T1 (4 Ash): Deal 1 damage to all invaders in ONE player-selected territory. No corruption added.
- Gale T1 (4 Gale): Push 1 player-chosen invader one territory toward spawn.
Both are player-targeted effects (they go to the pending queue, not auto-resolve).

**File:** Extend `ThresholdResolverTests.cs` or create `AshGaleTier1Tests.cs`.

**Write these tests:**
- `AshTier1_DealsOneDamage_ToAllInvadersInTargetedTerritory` — 2 invaders in target territory → both take 1 damage
- `AshTier1_DoesNotDamageInvaders_InOtherTerritories` — only the targeted territory is affected
- `AshTier1_DoesNotAddCorruption` — unlike T2, T1 adds no corruption to the territory
- `AshTier1_RequiresPlayerTargeting` — effect goes to pending queue, not auto-resolved
- `GaleTier1_PushesOneInvader_OneStepTowardSpawn` — invader in M1 → pushed to A-row
- `GaleTier1_PlayerChoosesWhichInvader` — multiple board invaders → effect requires player to choose (enters pending/targeting mode)
- `GaleTier1_DoesNothing_WhenNoBoardInvaders` — empty board → no crash, no side effects

**After writing:** `dotnet test --filter "ClassName=AshGaleTier1Tests"` (or ThresholdResolverTests if extended)

---

### TASK 8 — Weave Drain Event Tests

**Context:**
Weave drains (§4.1):
- Desecrated territory (Level 3): −1 Weave per turn passively. Multiple stack.
- Invader Heart march: invader in I1 that was there BEFORE this Tide's Advance deals Weave
  damage = remaining HP (minimum 1). One-turn grace: invaders that JUST arrived in I1 this
  Advance don't march yet.
- Sacred Site falls: −3 Weave.
Starting Weave = 20.

**File:** Create `WeaveDrainTests.cs` in the test project.

**Write these tests:**
- `StartingWeaveIs20` — new game state → Weave = 20
- `DesecratedTerritory_DrainsOneWeavePerTurn` — 1 Desecrated territory → end of turn → Weave -1
- `MultipleDesecratedTerritories_DrainStacks` — 2 Desecrated → end of turn → Weave -2
- `HeartMarch_DealsWeaveDamage_EqualToRemainingHp` — invader with 3 HP, in I1 since last Tide → marches → Weave -3
- `HeartMarch_MinimumOneDamage` — invader with 1 HP → Weave -1
- `HeartMarch_GracePeriod_NewArrivalDoesNotMarchThisTide` — invader just moved into I1 this Advance → does NOT march this Tide
- `HeartMarch_GracePeriod_Expires_NextTide` — same invader is now "old" in I1 at next Tide start → DOES march
- `WeaveCannotGoBelowZero` — Weave at 1, drain of 3 → Weave = 0 (clamps)

**After writing:** `dotnet test --filter "ClassName=WeaveDrainTests"`

---

### TASK 9 — Tide 1 Ramp-Up Tests

**Context:**
Tide 1 special rule (§4.3): runs Advance and Arrive ONLY.
Skips: Fear Actions, Activate, Native counter-attack, Escalate, Preview.
Tide 2 onwards runs the full sequence.

**File:** Create `Tide1RampUpTests.cs` in the test project.

**Write these tests:**
- `Tide1_SkipsFearActions` — Fear actions queued before Tide 1 → Tide 1 runs → actions remain queued (not resolved)
- `Tide1_SkipsActivate` — invaders on board → Tide 1 runs → no corruption added, no native damage
- `Tide1_SkipsNativeCounterAttack` — no counter-attack prompt fires during Tide 1
- `Tide1_SkipsEscalate` — Tide 1 runs → Escalation step does not fire (Corrupt not added to pool)
- `Tide1_SkipsPreview` — Tide 1 runs → no next-Tide action card is drawn/previewed
- `Tide1_StillRunsAdvance` — invader in A1 at Tide 1 start → Tide 1 ends → invader is in M1
- `Tide1_StillRunsArrive` — encounter has Wave 1 arrivals → Tide 1 ends → new invaders at arrival points
- `Tide2_RunsFearActionsBeforeActivate` — after Tide 1, Tide 2 runs full sequence; fear actions resolve before Activate
  (Use a manually constructed scenario — do NOT rely on seed draws to avoid fragility)

**After writing:** `dotnet test --filter "ClassName=Tide1RampUpTests"`

---

### TASK 10 — Fix Stale T2 Test + Root Boss Dormancy Tests

**PART A — Fix stale test (modify existing file):**

In `ThresholdPendingTests.cs`, the test `PlayerResolves_Tier2_ClearsPendingWithoutEffect`
is marked STALE. T2 is now fully implemented with real effects.

1. Read the T2 effect for the relevant element (check ThresholdResolver or equivalent).
2. Update the test to assert the actual T2 effect fires correctly.
3. Rename it to `PlayerResolves_Tier2_ExecutesRealEffect` (or a name matching the element).
4. Run `dotnet test --filter "ClassName=ThresholdPendingTests"` — all must still pass.

**PART B — Root Boss dormancy double-dissolve:**

Per §8.1: "On Boss encounters, double-dissolving a Dormant card removes it permanently."
On Standard/Elite, playing the bottom of an already-dormant card keeps it dormant (not removed).

**File:** Create `RootBossDormancyTests.cs` in the test project.

**Write these tests:**
- `BossEncounter_BottomPlay_OfDormantCard_RemovesPermanently` — card is dormant, Root plays its bottom in Boss → card is permanently removed (not in draw, discard, or dissolved list)
- `StandardEncounter_BottomPlay_OfDormantCard_StaysDormant` — same action in Standard → card remains dormant, NOT permanently removed

**PART C — Two dormancy paths:**
- `RootBottomPlayed_DormantCard_GoesToDiscardPile` — Root plays a bottom → card becomes dormant → card is in discard pile, not draw pile
- `RootRestDissolve_DormantCard_RemainsInDrawPile` — Rest fires, rest-dissolve targets a card → that card becomes dormant AND is in the draw pile (already shuffled in)
- `DormantCard_InDrawPile_IsDeadDraw_CannotBePlayed` — dormant card drawn during refill → cannot be played (top or bottom), sits inert in hand

**After writing:** `dotnet test --filter "ClassName=RootBossDormancyTests"` and re-run `ThresholdPendingTests`

---

### TASK 11 — Resolution Turn Limit Tests

**Context:**
After the final Tide step, invaders stop spawning/advancing. Resolution turns by tier:
- Standard: 2 turns. Elite: 3 turns. Boss: 1 turn.
If invaders remain after all Resolution turns: result = Breach.
Breach carry-overs (§3.4): one territory starts next encounter pre-Tainted, one invader carries over,
next encounter's Escalate clock starts one step ahead.

**File:** Create `ResolutionTests.cs` in the test project.

**Write these tests:**
- `StandardEncounter_ResolutionAllowsExactly2Turns`
- `EliteEncounter_ResolutionAllowsExactly3Turns`
- `BossEncounter_ResolutionAllowsExactly1Turn`
- `Resolution_EndsEarly_WhenNoInvadersRemain` — board cleared in turn 1 of Resolution → encounter ends immediately
- `EncounterResult_IsClean_WhenClearedBeforeResolution` — no invaders remain at Resolution start → Clean
- `EncounterResult_IsWeathered_WhenClearedDuringResolution` — cleared in Resolution turn 1 (Standard) → Weathered
- `EncounterResult_IsBreach_WhenInvadersRemainAfterResolution` — invaders remain after both turns → Breach
- `Breach_AppliesAllThreeCarryOverEffects` — Breach result → verify pre-Tainted territory, carried-over invader, and Escalate clock offset are all set

**After writing:** `dotnet test --filter "ClassName=ResolutionTests"`

---

### TASK 12 — Escalation Timing Tests

**Context:**
Escalation schedule (§4.4):
- Escalation 1 at Tide ~3–4: Adds "Corrupt" to Painful pool
- Escalation 2 at Tide ~6–7: Adds "Fortify" to Painful pool
- Escalation 3 (Boss only) at Tide 9+: Removes "Regroup" from Easy pool
ActionDeckTests.cs confirms cards CAN be added, but doesn't test trigger timing or specific cards.

**File:** Create `EscalationTimingTests.cs` in the test project.

**Write these tests:**
- `Escalation1_AddsCorrupt_AtTide3Or4` — run encounter to Escalation 1 trigger → Corrupt is now in Painful pool
- `Escalation2_AddsFortify_AtTide6Or7` — run to Escalation 2 → Fortify in Painful pool
- `BossEscalation3_RemovesRegroup_AtTide9Plus` — Boss encounter, run to Tide 9 → Regroup removed from Easy pool
- `Escalation1_DoesNotDuplicate_OnRepeatTrigger` — Escalation 1 trigger fires twice → Corrupt only added once
- `Escalation3_DoesNotFire_OnStandardEncounter` — Standard runs past Tide 9 → Regroup NOT removed
- `AllEscalations_FinalPoolComposition` — Boss through all escalation points → Painful = Ravage+March+Corrupt+Fortify, Easy = Rest+Settle

**After writing:** `dotnet test --filter "ClassName=EscalationTimingTests"`

---

### TASK 13 — Settle and Regroup Base Behavior Tests

**Context:**
Settle (Easy pool, §4.4):
- All units gain Shield 1 (even without a Pioneer)
- Pioneer places Infrastructure if 2+ units present in territory
- Advance modifier: 0 (hold)
Regroup (Easy pool, §4.4):
- A-row units return to spawn. Non-A-row units hold.
- Advance modifier: 0

**File:** Create `SettleRegroupTests.cs` in the test project.

**Write these tests:**
- `Settle_AllUnitsGainShieldOne_WithNoPioneer` — Marcher + Ironclad, no Pioneer → Settle → both gain Shield 1
- `Settle_PioneerPlacesInfra_WithTwoPlusUnits` — Pioneer + 1 other unit → Settle → Infrastructure placed
- `Settle_PioneerDoesNotPlaceInfra_WithFewerThanTwoUnits` — Pioneer alone → Settle → no Infrastructure
- `Settle_AdvanceModifierIsZero` — Settle fires → invaders do not advance
- `Regroup_ARowUnits_ReturnToSpawn` — invader in A1 → Regroup → invader returns to spawn point
- `Regroup_NonARowUnits_HoldPosition` — invader in M1 → Regroup → invader stays in M1
- `Regroup_AdvanceModifierIsZero_ForNonARowUnits` — non-A-row units don't advance during Regroup

**After writing:** `dotnet test --filter "ClassName=SettleRegroupTests"`

---

### TASK 14 — Wave 0 Initial Spawn Tests

**Context:**
§3.1: "Before the player's first Vigil, a starting wave (Wave 0) arrives at A-row positions.
The board ALREADY HAS INVADERS when the game begins."

StartingStateTests.cs has `BoardStartsWithNoInvaders_BeforeFirstTide`.
First: read this test and determine its intent.
  - If it tests the internal pre-Wave-0 state (before encounter init applies Wave 0), rename it
    to `InternalState_PreWave0_HasNoInvaders` to make the intent explicit.
  - If it asserts the player starts the game with an empty board (contradicting the design),
    flag it in your summary report as a potential bug and do NOT modify it — just flag it.

**File:** Extend `StartingStateTests.cs`.

**Write these tests:**
- `AfterWave0_BoardHasInvaders_AtARowPositions` — after encounter init (Wave 0 applied), A-row has invaders
- `AfterWave0_InvaderCount_MatchesEncounterData` — invader count matches EncounterData's Wave 0 spec
- `FirstVigilBegins_WithInvadersAlreadyOnBoard` — at first Vigil phase start, board is not empty
- `Wave0_DoesNotRunActivateOrAdvance` — Wave 0 arrival is pure spawn; no Activate/Advance/FearActions fire

**After writing:** `dotnet test --filter "ClassName=StartingStateTests"`

---

### TASK 15 — TideExecutor Sequence Order Tests

**Context:**
Correct Tide sequence (§4.3): Fear Actions → Activate → Native Counter-Attack → Advance → Arrive → Escalate → Preview
FearWiringTests covers the accumulation chain but not the ordering within TideExecutor.
Build concrete scenarios that can ONLY pass if steps ran in the right order.

**File:** Create `TideSequenceOrderTests.cs` in the test project.

**Write these tests:**
- `TideExecutor_FearActionsFireBeforeActivate`
  Scenario: Fear Action places presence in a territory. Activate (Ravage) checks that territory.
  If Fear Action ran first, presence was there during Activate; if not, it wasn't.
  Assert the presence-amplified Activate result proves Fear Action ran first.

- `TideExecutor_NativeCounterAttack_FiresAfterActivate`
  Scenario: Ravage action deals native damage during Activate. Surviving natives then counter-attack.
  Assert Activate's native damage resolved before any counter-attack damage was applied to invaders.

- `TideExecutor_Arrive_FiresAfterAdvance`
  Scenario: Advancing invader and a new arrival both target A1.
  Assert advancing invader has already moved to M1 before the new arrival appears at A1.

- `TideExecutor_Preview_IsLastStep`
  Assert the next-Tide preview state is only set after all other steps have completed.

**After writing:** `dotnet test --filter "ClassName=TideSequenceOrderTests"`

---

### TASK 16 — Dread Retroactive Upgrade Edge Cases

**Context:**
When Dread Level advances, all queued (unrevealed) Fear Actions are retroactively upgraded.
FearActionSystemTests.cs covers the main case but misses two edge cases.

**File:** Extend `FearActionSystemTests.cs`.

**Write these tests:**
- `RetroactiveUpgrade_DoesNothing_WhenQueueIsEmpty` — Dread advances with 0 actions queued → no crash, queue stays empty
- `RetroactiveUpgrade_AtMaxDread4_IsNoOp` — at Dread 4, if upgrade code is called, action objects are not corrupted and no exception is thrown

**After writing:** `dotnet test --filter "ClassName=FearActionSystemTests"`

---

### TASK 17 — Stub Placeholder Tests for Unimplemented Features

**Context:**
Three threshold effects are currently stubbed (not fully implemented). Add placeholder tests
that document the FULL intended behavior and are marked `[Fact(Skip = "Stub — not yet implemented")]`.
These serve as living documentation and will fail once the stubs are completed, reminding the team
to update them.

**File:** Create `StubPlaceholderTests.cs` in the test project.

**Write skip-marked placeholder tests for:**

Shadow T3 full behavior:
- `ShadowTier3_PresentsChoiceBetweenTwoFearActions` (Skip)
- `ShadowTier3_SelectedAction_IsQueued_OtherIsDiscarded` (Skip)
- `ShadowTier3_UnresolvedChoice_AtEndOfDusk_DefaultBehaviorApplies` (Skip)

Gale T3 full behavior:
- `GaleTier3_PushedInvaders_SkipTheirNextAdvance` (Skip)
- `GaleTier3_SkipFlag_ClearsAfterOneAdvance` (Skip)

Void T3 full behavior:
- `VoidTier3_KilledInvaders_DoNotGenerateCorruption` (Skip)
- `VoidTier3_SurvivedInvaders_StillGenerateCorruption_WhenKilledElsewhere` (Skip)

**After writing:** `dotnet test --filter "ClassName=StubPlaceholderTests"` — all should show as Skipped, none as Failed.

---

### TASK 18 — Full Test Suite Green Check

**This is the final task.**

Run the complete test suite:
```
dotnet test
```

Every previously-passing test must still pass.
All new tests must pass (except the intentional Skips in Task 17).

If any test is red, fix it before proceeding to the summary report.

---

## FINAL SUMMARY REPORT

After completing all tasks, print a report in this format:

```
=== TEST GAP REMEDIATION COMPLETE ===

Tasks completed: X/18
New test classes created: [list]
Existing test files modified: [list]
Tests added (passing): X
Tests skipped (stub placeholders): X
Tests fixed (stale): X

Potential bugs flagged (do not auto-fix):
- [list any production code issues discovered, or "None"]

Tests that could not be implemented (with reason):
- [list any skipped tasks with explanation, or "None"]

Final suite: X passing, X skipped, 0 failed
=====================================
```
