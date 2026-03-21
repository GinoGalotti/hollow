# Hollow Wardens — BDD Test Coverage Report
_Generated 2026-03-21 · 236 tests passing_

## How to read
Each line: `MethodName → what rule/behaviour it verifies — PASS`
Group headers map to test file / suite.

---

## [ElementSystemTests] — Element Pool & Thresholds
```
AddElementsIncreasesPool             → pool.Count increments when Root elements added — PASS
DecayReducesByOne                    → decay reduces pool by 1 per call — PASS
DecayDoesNotGoBelowZero              → decay clamps at 0, never negative — PASS
Tier1FiresAt4                        → threshold T1 fires when count reaches 4 — PASS
Tier1DoesNotFireAt3                  → threshold T1 does not fire at 3 — PASS
Tier2FiresAt7                        → threshold T2 fires at 7 — PASS
Tier3FiresAt11                       → threshold T3 fires at 11 — PASS
ThresholdFiresOncePerTurnPerTier     → each tier fires at most once per turn — PASS
Tier1InVigilAndTier2InDuskSameTurn   → T1+T2 can both fire in same turn — PASS
BottomDoubleMultiplier               → bottom effects gain ×2 element multiplier — PASS
RestTurnCarryoverCheckFires          → rest turn re-checks thresholds for carry-over — PASS
EngineBuilding5TurnsMatchesMathTable → 5-turn accumulation matches design schedule — PASS
BankedEffectLostOnDecay              → banked effects clear on decay — PASS
```

## [DreadSystemTests] — Dread Level Progression
```
StartsAtLevel1          → initial dread is level 1 — PASS
AdvancesToLevel2At15    → dread advances at 15 fear — PASS
AdvancesToLevel3At30    → dread advances at 30 fear — PASS
AdvancesToLevel4At45    → dread advances at 45 fear — PASS
DoesNotExceedLevel4     → dread caps at level 4 — PASS
FiresDreadAdvancedEvent → DreadAdvanced event fires on each advance — PASS
```

## [BoardStateTests] — Map Topology
```
CreatePyramidHas6Territories         → pyramid board has 6 territories — PASS
ArrivalRowHas3Territories            → arrival row = 3 territories — PASS
MiddleRowHas2Territories             → middle row = 2 territories — PASS
InnerRowHas1Territory                → inner row = 1 (Sacred Heart) — PASS
DistanceA1ToI1Is2                    → pathfinding distance A1→I1 = 2 — PASS
RangeQueryReturnsCorrectTerritories  → range-1 from M1 returns correct set — PASS
```

## [FearActionSystemTests] — Fear Queue
```
QueuesActionEvery5Fear                   → 1 action queued per 5 fear — PASS
DrawsFromCurrentDreadPool                → actions drawn from current dread level pool — PASS
RetroactiveUpgradeOnDreadAdvance         → queued actions upgrade when dread advances — PASS
RevealDequeuesToEmpty                    → RevealAndDequeue clears the queue — PASS
RetroactiveUpgradeReplacesActualObjects  → objects themselves replaced on upgrade — PASS
QueuedCountTracksCorrectly               → count increments at 5-fear boundaries — PASS
```

## [CorruptionSystemTests] — Corruption Levels & Purify
```
ThreePointsReachesLevel1                  → 3+ pts = Level 1 (Tainted) — PASS
EightPointsReachesLevel2                  → 8+ pts = Level 2 (Defiled) — PASS
FifteenPointsReachesLevel3                → 15+ pts = Level 3 (Desecrated) — PASS
ReduceCorruptionClampsAtZero              → reduction can't go negative — PASS
PurifyDropsOneLevel                       → purify removes 1 level — PASS
PurifyDesecrated_DropsToStartOfDefiled    → purify Level 3 → 8 pts — PASS
PurifyDefiled_DropsToStartOfTainted       → purify Level 2 → 3 pts — PASS
PurifyTainted_DropsToClean                → purify Level 1 → 0 pts — PASS
PersistenceLevel1ResetsToZero             → persistence at Level 1 clears all — PASS
PersistenceLevel2BecomesThreePoints       → persistence at Level 2 → 3 pts — PASS
PersistenceLevel3Permanent                → persistence at Level 3 = no change — PASS
```

## [CadenceManagerTests] — Action Deck Cadence
```
PepePattern_MaxStreak1             → streak 1 → P-E-P-E alternation — PASS
PpePattern_MaxStreak2              → streak 2 → P-P-E pattern — PASS
PppePattern_MaxStreak3             → streak 3 → P-P-P-E pattern — PASS
ManualOverride_Respected           → manual pattern followed exactly — PASS
StreakResets_AfterEasy             → painful streak counter resets after Easy — PASS
```

## [PathfindingTests] — Invader Movement
```
FromArrival_MovesToMiddle                      → A-row invader advances to M-row — PASS
FromMiddle_MovesToInner                        → M-row invader advances to I1 — PASS
AtSacredHeart_ReturnsNull                      → I1 invader cannot move further — PASS
PrefersTerritory_WithPresence                  → pathfinding prefers presence territories — PASS
PrefersTerritory_WithNatives                   → pathfinding prefers native territories — PASS
Ironclad_SkipsMove_WhenNotAlternateTurn        → Ironclad skips non-alternate turns — PASS
Ironclad_Moves_WhenAlternateTurn               → Ironclad moves on alternate turns — PASS
ToggleIroncladMove_FlipsFlag                   → ToggleIroncladMove inverts flag — PASS
ToggleIroncladMove_DoesNothing_ForNonIronclad  → only Ironcads toggle — PASS
```

## [UnitModifierTests] — Unit Type Special Rules
```
Ironclad_RavageCorruption_IsPlusTwoBase        → Ironclad Ravage = 3 corruption — PASS
Ironclad_Rest_IsFullHeal                       → Ironclad fully heals on Rest — PASS
Ironclad_Corrupt_KillsTwoNatives               → Ironclad kills 2 natives — PASS
Ironclad_MovesEveryOtherAdvance                → Ironclad alternates movement — PASS
Outrider_HasPlusOneExtraMovement               → Outrider gets +1 movement — PASS
Outrider_RestAdvancesInstead                   → Outrider advances instead of resting — PASS
Outrider_RegroupAdvancesInstead                → Outrider advances instead of regrouping — PASS
Outrider_RavageCorruption_IsOne                → Outrider Ravage = 1 corruption — PASS
Outrider_DamagesFirstNativeOnly                → Outrider pre-hits first native only — PASS
Pioneer_PlacesInfrastructureAfterActivate      → Pioneer places infra after Ravage — PASS
Pioneer_FortifyGrantsFutureCorruption          → Pioneer fortification gives future corruption — PASS
Marcher_HasNoSpecialModifiers                  → Marcher is baseline unit — PASS
```

## [EffectTests] — Individual Effect Types
```
PlacePresenceAddsToTerritory     → PlacePresence increases territory presence count — PASS
ReduceCorruptionRemovesPoints    → ReduceCorruption subtracts points — PASS
DamageInvadersHitsAllInTerritory → DamageInvaders hits all alive invaders — PASS
GenerateFearFiresEvent           → GenerateFear fires FearGenerated event — PASS
```

## [DeckManagerTests] — Deck Cycle Mechanics
```
RefillDrawsToHandLimit               → refill draws to hand limit — PASS
RefillStopsWhenDeckEmpty             → refill stops at empty deck — PASS
PlayTopMovesToDiscard                → top play → discard pile — PASS
PlayBottomDissolves                  → bottom play → dissolved (removed) — PASS
RestShufflesDiscardIntoDeck          → rest shuffles discard → draw pile — PASS
RestDissolveRemovesOneCard           → rest dissolves 1 card from shuffled discard — PASS
FourPlayTurnsThenRestWith10Cards     → 4-turn cycle matches design math — PASS
AggressiveBottomsForceEarlierRest    → bottoms played = smaller deck, earlier rest — PASS
SecondCycleIsThinnerAfterBottoms     → post-bottom cycle 2 deck is thinner — PASS
```

## [ActionDeckTests] — Invader Action Deck
```
Draw_ReturnsCardFromCorrectPool             → draw returns from Painful/Easy pool — PASS
Draw_ReshufflesWhenPainfulPoolExhausted     → painful pool reshuffles when empty — PASS
Draw_ReshufflesWhenEasyPoolExhausted        → easy pool reshuffles when empty — PASS
AddEscalationCard_IncreasesPainfulPool      → escalation cards added to pool — PASS
AddEscalationCard_RoutesToCorrectPool       → escalation routes to correct pool — PASS
```

## [SpawnManagerTests] — Invader Spawning
```
WeightedSelection_FavorsHigherWeightOption    → weighted spawn favors high-weight — PASS
PreviewWave_ReturnsWave_AndFiresLocationEvent → PreviewWave fires event — PASS
PreviewWave_ReturnsNull_WhenNoWaveScheduled   → no wave = null — PASS
RevealComposition_FiresCompositionEvent       → composition event fires — PASS
RevealComposition_ReturnsNull_WhenNoOptions   → no options = null — PASS
```

## [Foundation] — Seeded Randomness & Action Log

### [GameRandomTests]
```
SameSeedProducesSameSequence               → same seed = deterministic sequence — PASS
DifferentSeedProducesDifferentSequence     → different seeds diverge — PASS
ShuffleSameSeedProducesSameOrder           → same seed = same shuffle — PASS
ShuffleDifferentSeedProducesDifferentOrder → different seeds shuffle differently — PASS
```

### [ActionLogTests]
```
RecordIncrementsTimestamp        → record increments timestamp counter — PASS
ExportContainsSeedAndActions     → export has seed + actions — PASS
ParseSeedExtractsCorrectly       → ParseSeed extracts from export — PASS
TruncateRemovesActionsAfterIndex → truncate drops tail actions — PASS
EmptyLogExportsJustSeed          → empty log exports seed only — PASS
```

### [SeededDeterminismTests]
```
DeckShuffleDeterministic             → deck shuffle is seed-deterministic — PASS
SpawnWaveSelectionDeterministic      → spawn wave is seed-deterministic — PASS
ActionDeckDrawDeterministic          → action deck draw is seed-deterministic — PASS
```

## [CombatSystemTests] — Combat Resolution
```
RavageDealsCorruption                 → Ravage adds corruption by unit type — PASS
RavageDamagesNatives                  → Ravage damages natives — PASS
IroncladDealsExtraCorruption          → Ironclad adds +1 corruption — PASS
OutriderDamagesNativeBeforeRavage     → Outrider pre-hits before main ravage — PASS
PioneerBuildsInfrastructureAfterActivate → Pioneer places infra — PASS
MarchGivesShieldAndHeals              → March = 2 shield + 1 heal — PASS
SettleGivesShield1ToAll               → Settle = 1 shield to all — PASS
AutoMaximizeKillsLowestFirst          → auto-damage kills lowest-HP first — PASS
ExactDamageToKillBeforeMovingOn       → damage allocated to kill before next — PASS
ExcessDamageNotWasted                 → overkill carries to next target — PASS
PoolDamageFromAllSurvivors            → counter-pool sums all survivor damage — PASS
PlayerAssignmentRespected             → player-assigned counter damage applied — PASS
AutoAssignTargetsLowestFirst          → auto counter-assign targets lowest-HP — PASS
NormalMovementOneStep                 → normal move = 1 step — PASS
MarchGivesExtraStep                   → March = 2 steps — PASS
SettleHoldsPosition                   → Settle = no movement — PASS
IroncladAlternatesMovement            → Ironclad alternates movement — PASS
OutriderAlwaysExtraStep               → Outrider always moves extra step — PASS
HeartMarchDealWeaveFromI1             → invaders in I1 deal heart damage — PASS
NewArrivalsAtI1DontMarchSameTide      → new arrivals at I1 skip damage that tide — PASS
HeartDamageEqualsRemainingHp          → heart damage = current HP — PASS
```

## [RavageDamageModelTests] — Corruption Values by Unit Type
```
Marcher_CorruptionIs2                           → Marcher Ravage = 2 corruption — PASS
Ironclad_CorruptionIs3                          → Ironclad Ravage = 3 corruption — PASS
Outrider_CorruptionIs1                          → Outrider Ravage = 1 corruption — PASS
Pioneer_CorruptionIs2                           → Pioneer Ravage = 2 corruption — PASS
CorruptionEqualsNativeDamagePool                → corruption value = native counter damage — PASS
Outrider_PreHit2_DamagesNativeBeforeMainRavage  → Outrider pre-hit 2 dmg — PASS
```

## [RootDormancyTests] — Root Warden Dormancy Rules
```
BottomPlayMakesCardDormant           → Root bottom play → card dormant — PASS
DormantCardGoesToDiscard             → dormant card routes to discard — PASS
DormantCardIsNotPlayable             → dormant cards unplayable — PASS
RestDissolveGosDormantForRoot        → Root rest-dissolve → dormant not removed — PASS
AwakeDormantReactivatesCard          → Awaken reactivates dormant — PASS
AwakeAllReactivatesAllDormant        → AwakenAll reactivates all dormant — PASS
BossDoubleDissolveRemovesPermanently → playing dormant card removes it permanently — PASS
DormantCountTracksCorrectly          → dormant count accurate across piles — PASS
```

## [DormantToDiscardTests] — Dormant Card Flow
```
PlayBottom_Dormant_CardInDiscard           → bottom play → dormant in discard — PASS
PlayBottom_Dormant_DrawPileUnchanged       → bottom play doesn't touch draw pile — PASS
PlayBottom_Dormant_CardIsDormant           → bottom play marks card dormant — PASS
PlayBottom_Dormant_RestCyclesCardToDrawPile → rest cycles dormant to draw — PASS
```

## [CardPlayLimitTests] — Play Budget Enforcement
```
PlayTop_ThirdPlay_IsRejected_InVigil     → 3rd top play rejected (limit 2) — PASS
PlayBottom_SecondPlay_IsRejected_InDusk  → 2nd bottom play rejected (limit 1) — PASS
PlayTop_CounterResetsOnNewTurn           → top play counter resets each turn — PASS
```

## [TargetingTests] — Target Resolution
```
NeedsTarget_PlacePresence_WithRange1_ReturnsTrue   → PlacePresence r>0 = needs target — PASS
NeedsTarget_GenerateFear_WithRange1_ReturnsFalse   → GenerateFear = no target — PASS
NeedsTarget_RestoreWeave_WithRange1_ReturnsFalse   → RestoreWeave = no target — PASS
NeedsTarget_AwakeDormant_WithRange1_ReturnsFalse   → AwakeDormant = no target — PASS
NeedsTarget_PlacePresence_WithRange0_ReturnsFalse  → PlacePresence r=0 = no target — PASS
GetValidTargets_PresenceOnI1_Range1_ReturnsMAndI   → valid targets from I1 r=1 — PASS
GetValidTargets_NoPresence_ReturnsEmpty             → no presence = no valid targets — PASS
```

## [TideRampTests] — Tide 1 Ramp-Up Rules
```
Tide1_FearActions_AreNotRevealed                   → fear actions not revealed on Tide 1 — PASS
Tide1_Activate_IsSkipped_NoCorruptionFromInvaders  → activate skipped on Tide 1 — PASS
Tide1_Escalate_IsSkipped_NoEscalationCardAdded     → escalate skipped on Tide 1 — PASS
Tide2_RunsAllSteps_FearActionsRevealed             → full sequence runs on Tide 2 — PASS
```

## [TideSequenceOrderTest] — Tide Step Ordering
```
Tide1_RampUp_OnlyRunsAdvanceArrivePreview     → Tide 1 = Advance, Arrive, Preview — PASS
Tide2_RunsFullSequence                        → Tide 2 non-provocative = no CounterAttack — PASS
Tide2_RavageCard_IncludesCounterAttackStep    → Tide 2 + Ravage = CounterAttack included — PASS
ActionCardRevealedFiresBeforeFirstStep        → ActionCardRevealed fires before first step — PASS
```

## [HeartMarchGracePeriodTest] — Sacred Heart Grace Period
```
InvaderEntersI1_NoHeartDamage_SameTide   → invader entering I1 skips damage that tide — PASS
InvaderInI1_BeforeAdvance_DoesMarchOnHeart → invader already in I1 deals damage — PASS
```

## [InitialWaveTests] — Wave 1 Setup
```
SpawnInitialWave_ARowHasInvaders               → initial wave populates A-row — PASS
SpawnInitialWave_TideRunnerDoesNotRespawnWave1 → Tide 1 doesn't re-spawn initial wave — PASS
```

## [CadencePatternTest] — Cadence Integration
```
PEPEPattern_ProducesCorrectPoolPerTide          → P-E-P-E pattern pools correct — PASS
RuleBasedCadence_MaxStreak1_AlternatesAfterPainful → streak-1 alternates — PASS
```

## [DreadThresholdPushTest] — Fear Queue Upgrade
```
QueuedActions_UpgradeWhenDreadAdvances          → queued actions upgrade on dread advance — PASS
QueuedActions_DontUpgrade_IfDreadStaysAtLevel1  → no upgrade if dread stays 1 — PASS
```

## [BottomBudgetTest] — Deck Economy
```
RestForcedOnTurn4_After3BottomPlays → 1 top+1 bottom/turn → rest on turn 4 — PASS
DeckThinner_AfterRest               → deck thinner after rest due to dissolution — PASS
```

## [FrontloadingPenaltyTest] — Deck Economy (Aggressive Play)
```
AggressiveBottomPlay_LeavesSmallResolutionHand → aggressive bottoms thin resolution hand — PASS
ConservativePlay_HasFullerResolutionHand       → idle play preserves full hand — PASS
```

## [BottomPlayTests] — Bottom Effect Resolution
```
BottomPlay_UsesBottomEffect_NotTopEffect       → bottom play uses bottom effect — PASS
BottomPlay_AddsElements_WithMultiplierTwo      → bottom play elements ×2 — PASS
BottomPlay_ResolvesBottomSecondary_WhenPresent → bottom secondary effect resolves — PASS
```

## [FearWiringTests] — Fear System Integration
```
FearGenerated_AccumulatesInDread               → fear accumulates in dread system — PASS
DreadAdvances_AtThreshold15                   → dread advances at 15 — PASS
FearActions_QueuedAt5FearIncrements           → fear actions queue at 5-fear marks — PASS
DreadAdvanced_UpgradesQueuedFearActions        → dread advance upgrades queue — PASS
FearGenerated_EventFiresAfterDreadUpdated      → event fires after dread update — PASS
FullChain_GenerateFearEffect_WiresThrough_BothSystems → full fear chain wires — PASS
```

## [RootFullEncounterTest] — Root Warden Full Encounter
```
BottomPlay_MakesCardDormant_NotDissolved           → Root bottom = dormant not removed — PASS
DormantCard_IsInDeck_ButNotPlayable                → dormant in deck, unplayable — PASS
NetworkFear_GeneratesFear_BasedOnPresenceAdjacency → network fear counts adjacency — PASS
OnResolution_Assimilation_RemovesAdjacentInvaders  → assimilation removes invaders — PASS
OnResolution_Assimilation_ReducesNeighborCorruption → assimilation reduces corruption — PASS
```

## [FullStandardEncounterTest] — End-to-End Encounter
```
Encounter_CompletesWithValidResult              → full encounter finishes with valid result — PASS
ElementTier1_Fires_ByTurn3_WithRootAffinity    → T1 fires by turn 3 — PASS
RestOccurs_WhenDeckDepletes                    → rest triggers on empty deck — PASS
FinalDeckSize_SmallerThanStarting_AfterRestDissolve → deck shrinks after rest — PASS
```

## [NativeProvocationTests] — Counter-Attack Provocation Rule
```
IsProvoked_Ravage_ReturnsTrue            → Ravage provokes — PASS
IsProvoked_PmRavage_ReturnsTrue          → pm_ravage provokes (Contains match) — PASS
IsProvoked_PmCorrupt_ReturnsTrue         → pm_corrupt provokes — PASS
IsProvoked_PmMarch_ReturnsFalse          → March does not provoke — PASS
IsProvoked_PmRest_ReturnsFalse           → Rest does not provoke — PASS
IsProvoked_PmSettle_ReturnsFalse         → Settle does not provoke — PASS
ExecuteActivate_PmRavage_ExecutesRavageEffect → pm_ravage executes correctly — PASS
ExecuteActivate_PmMarch_ExecutesMarchEffect   → pm_march executes correctly — PASS
```

## [ThresholdPendingTests] — Player-Driven Threshold Queue
```
OnThresholdTriggered_Tier1_GoesToPending         → T1 trigger → pending queue — PASS
OnThresholdTriggered_Tier2_GoesToPending         → T2 trigger → pending queue — PASS
PlayerResolves_Tier1_ExecutesEffect              → resolving T1 executes + clears pending — PASS
PlayerResolves_Tier2_ClearsPendingWithoutEffect  → resolving T2 clears only (stub) ⚠️ STALE — T2 now executes real effects — PASS
UnresolvedThreshold_ClearsAtEndOfDusk            → unresolved → ThresholdExpired + cleared — PASS
MultiplePending_ResolveInAnyOrder                → multiple pending resolve in any order — PASS
```

## [ThresholdResolverTests] — Per-Element T1 Effects
```
RootTier1_PlacesPresence_AtRangeOneFromExistingPresence → Root T1 places presence adjacent to existing — PASS
MistTier1_RestoresOneWeave                              → Mist T1 restores 1 weave — PASS
ShadowTier1_GeneratesTwoFear                            → Shadow T1 generates 2 fear — PASS
VoidTier1_DamagesLowestHpInvader                        → Void T1 damages lowest-HP invader — PASS
```

## [ThresholdT2T3Tests] — Per-Element T2 & T3 Effects

### Root
```
RootTier2_ReducesCorruptionByThree_InHighestCorruptTerritory → Root T2 reduces corruption ×3 in highest-corrupt presence territory — PASS
RootTier2_PicksTerritoryWithPresenceAndHighestCorruption     → Root T2 only targets territories with presence (ignores non-presence even if higher corruption) — PASS
RootTier3_PlacesTwoPresenceTokens                            → Root T3 places 2 presence tokens — PASS
RootTier3_ReducesCorruptionByTwoInEachPresenceTerritory      → Root T3 reduces corruption ×2 in every territory with presence — PASS
```

### Mist
```
MistTier2_ReturnsOneCardFromDiscardToHand    → Mist T2 moves 1 card from discard back to hand — PASS
MistTier2_DoesNothing_WhenDiscardIsEmpty     → Mist T2 is a no-op when discard is empty — PASS
MistTier3_RestoresThreeWeave                 → Mist T3 restores 3 weave — PASS
MistTier3_ReturnsAllDiscardedCardsToHand     → Mist T3 returns entire discard pile to hand — PASS
```

### Shadow
```
ShadowTier2_ElevatesNextQueuedFearActionByOneDreadLevel  → Shadow T2 causes next queued action to draw from Dread+1 pool — PASS
ShadowTier2_ElevationConsumedAfterOneAction_NotSecond    → Shadow T2 elevation applies to first queued action only, not subsequent — PASS
ShadowTier3_GeneratesFiveFear                            → Shadow T3 generates 5 fear — PASS
```

### Ash
```
AshTier2_DealsTwoDamageToAllInvadersInMostInvadedTerritory → Ash T2 deals 2 damage to all invaders in auto-selected territory — PASS
AshTier2_AddsOneCorruptionToTargetTerritory                 → Ash T2 adds 1 Corruption to the targeted territory — PASS
AshTier3_DealsThreeDamageToAllBoardInvaders                 → Ash T3 deals 3 damage to every invader on the board — PASS
AshTier3_AddsOneCorruptionToEachAffectedTerritory           → Ash T3 adds 1 Corruption to each territory that had invaders — PASS
AshTier3_IroncladSurvivesThreeDamage                        → Ash T3 leaves Ironclad (HP 5) alive at HP 2 — PASS
```

### Gale
```
GaleTier2_PushesAllInvadersOutOfClosestTerritory → Gale T2 pushes all invaders out of most-threatened territory — PASS
GaleTier2_LeavesOtherTerritoryUntouched          → Gale T2 only targets one territory; others unaffected — PASS
GaleTier3_PushesAllInvadersOnBoard               → Gale T3 displaces every invader from their starting territory — PASS
GaleTier3_ARowInvadersCanPushToAdjacentARow      → Gale T3 laterally pushes A1→A2 (same distance from I1 = valid target) — PASS
```

### Void
```
VoidTier2_AllInvadersOnBoardTakeOneDamage  → Void T2 deals 1 damage to every alive invader — PASS
VoidTier2_KillsOutrider_WithOneDamage      → Void T2 kills invaders with only 1 HP remaining — PASS
VoidTier3_AllInvadersTakeTwoDamage         → Void T3 deals 2 damage to every alive invader — PASS
VoidTier3_KillsOutrider_WithTwoDamage      → Void T3 kills Outrider (MaxHp 2) outright — PASS
```

## [StartingStateTests] — Encounter Initialisation
```
NativeSpawns_PopulatesTerritoriesWithCorrectCount → native spawn counts match config — PASS
NativeSpawns_NativesHaveCorrectStats              → natives = HP 2, Damage 3 — PASS
StartingPresence_I1HasPresenceCount1              → starting presence on I1 — PASS
BoardStartsWithNoInvaders_BeforeFirstTide         → board empty before wave 1 — PASS
```

---

## Staleness / Review Flags

| Test | Risk | Note |
|---|---|---|
| `PlayerResolves_Tier2_ClearsPendingWithoutEffect` | **Needs update** | Was a stub test; T2 now has real effects — this test should be revised to assert the actual effect fires |
| `Tide2_RunsFullSequence` | Low | Assumes default seed draws non-provocative card — fragile if seed changes |
| `AggressiveBottomPlay_LeavesSmallResolutionHand` | Low | Depends on hard-coded hand size constants |
| `HeartMarchGracePeriodTest` | Low | Confirm grace period is still tide-scoped not turn-scoped |
| `BottomBudgetTest.DeckThinner_AfterRest` | Low | Confirm design still uses exactly 1 dissolve per rest |

## Intentional Stubs (design features not yet implemented)

| Threshold | Stub behaviour | Full design |
|---|---|---|
| Shadow T3 | Generates 5 Fear only | Should also preview 2 Fear Actions and let player choose |
| Gale T3 | Pushes all board invaders | Should also flag pushed invaders to skip their next Advance |
| Void T3 | Deals 2 damage to all | Invaders killed by this should not generate Corruption on death (no per-death Corruption mechanic yet) |
