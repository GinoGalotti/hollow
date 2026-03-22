# Hollow Wardens — BDD Test Coverage Report
_Updated 2026-03-22 · 487 tests passing_

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

## [WeaveSystemTests] — Weave Cap & Damage
```
RestoreWeave_ClampsAtMaxWeave        → restore beyond max clamps to max (e.g. 18+5 = 20, not 23) — PASS
RestoreWeave_AtMax_NoChange          → restore at max leaves weave unchanged — PASS
RestoreWeave_BelowMax_AddsNormally   → restore below max adds correctly — PASS
DealDamage_ReducesWeave              → deal damage reduces weave — PASS
DealDamage_ClampsAtZero              → damage clamps weave to 0, never negative — PASS
```

## [FearActionSystemTests] — Fear Queue
```
QueuesActionEvery5Fear                         → 1 action queued per 5 fear — PASS
DrawsFromCurrentDreadPool                      → actions drawn from current dread level pool — PASS
RetroactiveUpgradeOnDreadAdvance               → queued actions upgrade when dread advances — PASS
RevealDequeuesToEmpty                          → RevealAndDequeue clears the queue — PASS
RetroactiveUpgradeReplacesActualObjects        → objects themselves replaced on upgrade — PASS
QueuedCountTracksCorrectly                     → count increments at 5-fear boundaries — PASS
FearGenerated_DuringResolution_DoesNotQueue    → fear during BeginResolution guard is discarded (loop prevention) — PASS
FearGenerated_AfterResolutionEnds_QueuesNormally → fear after EndResolution queues normally — PASS
BeginResolution_EndResolution_CanBeCalledMultipleTimes → multiple begin/end cycles work without error — PASS
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
Marcher_BaseHp_IsCorrect                       → Marcher BaseHp = 4 (D30: was 3) — PASS
Outrider_BaseHp_IsCorrect                      → Outrider BaseHp = 3 (D30: was 2) — PASS
Ironclad_BaseHp_IsCorrect                      → Ironclad BaseHp = 5 — PASS
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
Marcher_HasNoSpecialModifiers                  → Marcher is baseline unit (BaseHp 4, no specials) — PASS
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
Ravage_BasePlusPerInvader_CorrectTotal     → 2 Marchers → 2 base + 2+2 = 6 total corruption — PASS
Ravage_SingleMarcher_IncludesBase          → 1 Marcher → 2 base + 2 = 4 total — PASS
Ravage_MixedUnits_CorrectTotal             → 1 Ironclad+1 Outrider → 2 base+3+1 = 6 — PASS
RavageDealsCorruption                      → Ravage adds corruption by unit type — PASS
RavageDamagesNatives                       → Ravage damages natives — PASS
IroncladDealsExtraCorruption               → Ironclad adds +1 corruption — PASS
OutriderDamagesNativeBeforeRavage          → Outrider pre-hits before main ravage — PASS
PioneerBuildsInfrastructureAfterActivate   → Pioneer places infra — PASS
MarchGivesShieldAndHeals                   → March = 2 shield + 1 heal — PASS
SettleGivesShield1ToAll                    → Settle = 1 shield to all — PASS
AutoMaximizeKillsLowestFirst               → auto-damage kills lowest-HP first — PASS
ExactDamageToKillBeforeMovingOn            → damage allocated to kill before next — PASS
ExcessDamageNotWasted                      → overkill carries to next target — PASS
PoolDamageFromAllSurvivors                 → counter-pool sums all survivor damage — PASS
PlayerAssignmentRespected                  → player-assigned counter damage applied — PASS
AutoAssignTargetsLowestFirst               → auto counter-assign targets lowest-HP — PASS
NormalMovementOneStep                      → normal move = 1 step — PASS
MarchGivesExtraStep                        → March = 2 steps — PASS
SettleHoldsPosition                        → Settle = no movement — PASS
IroncladAlternatesMovement                 → Ironclad alternates movement — PASS
OutriderAlwaysExtraStep                    → Outrider always moves extra step — PASS
HeartMarchDealWeaveFromI1                  → invaders in I1 deal heart damage — PASS
NewArrivalsAtI1DontMarchSameTide           → new arrivals at I1 skip damage that tide — PASS
HeartDamageEqualsRemainingHp               → heart damage = current HP — PASS
```

## [RavageDamageModelTests] — Corruption Values by Unit Type
```
Marcher_CorruptionIs2                           → Marcher Ravage = 2 base+2 = 4 total — PASS
Ironclad_CorruptionIs3                          → Ironclad Ravage = 2 base+3 = 5 total — PASS
Outrider_CorruptionIs1                          → Outrider Ravage = 2 base+1 = 3 total — PASS
Pioneer_CorruptionIs2                           → Pioneer Ravage = 2 base+2 = 4 total — PASS
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

## [TideArrivalTests] — Tide Arrival Wave Offset
```
Tide1_SpawnsWave2_NotWave1   → Tide 1 Arrive spawns Wave 2, not Wave 1 again — PASS
Tide2_SpawnsWave3            → Tide 2 Arrive spawns Wave 3 — PASS
NoWaveDefined_GracefulSkip   → no wave defined for tideNumber+1 = graceful no-op — PASS
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
BottomPlay_MakesCardDormant_NotDissolved                        → Root bottom = dormant not removed — PASS
DormantCard_IsInDeck_ButNotPlayable                             → dormant in deck, unplayable — PASS
NetworkFear_GeneratesFear_BasedOnPresenceAdjacency              → 1 adjacent pair = 1 fear (undirected) — PASS
NetworkFear_TwoAdjacentPresencePairs_Returns2NotFour            → A1-M1-I1 chain = 2 undirected edges, not 4 directed — PASS
Assimilation_RemovesUpToPresenceCount_WeakestFirst              → PresenceCount=2 removes 2 weakest, strongest survives — PASS
Assimilation_MoreInvadersThanPresence_SomeRemain                → PresenceCount=1 removes 1 of 3 invaders — PASS
Assimilation_MaxPresence_RemovesThree                           → PresenceCount=3 removes 3 of 5 invaders — PASS
Assimilation_ReducesCorruptionByRemoveCount                     → 2 invaders removed → corruption −2 — PASS
Assimilation_NoInvaders_NoCorruptionChange                      → no invaders = no corruption change — PASS
OnResolution_Assimilation_RemovesAdjacentInvaders               → assimilation removes invaders from adjacent territories — PASS
OnResolution_Assimilation_ReducesNeighborCorruption             → assimilation reduces corruption per removed invader — PASS
```

## [FullStandardEncounterTest] — End-to-End Encounter
```
Encounter_CompletesWithValidResult              → full encounter finishes with valid result — PASS
ElementTier1_Fires_ByTurn3_WithRootAffinity    → T1 fires by turn 3 — PASS
RestOccurs_WhenDeckDepletes                    → rest triggers on empty deck — PASS
FinalDeckSize_SmallerThanStarting_AfterRestDissolve → rest-dissolve creates dormant cards (DormantCount > 0) — PASS
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
OnThresholdTriggered_Tier1_GoesToPending                                → T1 trigger → pending queue — PASS
OnThresholdTriggered_Tier2_GoesToPending                                → T2 trigger → pending queue — PASS
PlayerResolves_Tier1_ExecutesEffect                                     → resolving T1 executes + clears pending — PASS
PlayerResolves_Tier2_ReducesCorruptionInHighestPresenceTerritory        → Root T2 reduces corruption ×3 in territory with most corruption + presence — PASS
UnresolvedThreshold_ClearsAtEndOfDusk                                   → unresolved → ThresholdExpired + cleared — PASS
MultiplePending_ResolveInAnyOrder                                       → multiple pending resolve in any order — PASS
```

## [ThresholdResolverTests] — Per-Element T1 Effects
```
RootTier1_PlacesPresence_AtRangeOneFromExistingPresence → Root T1 places presence adjacent to existing — PASS
MistTier1_RestoresOneWeave                              → Mist T1 restores 1 weave — PASS
ShadowTier1_GeneratesTwoFear                            → Shadow T1 generates 2 fear — PASS
VoidTier1_DamagesLowestHpInvader                        → Void T1 damages lowest-HP invader — PASS
```

## [ThresholdTargetingTests] — Threshold Player-Target Requirements
```
NeedsTarget_RootT1_ReturnsTrue                  → Root T1 requires territory selection — PASS
NeedsTarget_RootT2_ReturnsFalse                 → Root T2 auto-resolves (no selection) — PASS
NeedsTarget_RootT3_ReturnsFalse                 → Root T3 auto-resolves — PASS
NeedsTarget_NonRootElements_ReturnFalse         → Mist/Shadow/Ash/Gale/Void T1 all auto-resolve — PASS
GetTargetEffect_RootT1_ReturnsPlacePresenceRange1 → Root T1 target effect = PlacePresence range 1 — PASS
GetTargetEffect_RootT2_ReturnsNull              → Root T2 returns null (no targeting needed) — PASS
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
AshTier3_NoCorruptionAdded_CorruptionRiderRemoved            → Ash T3 does NOT add corruption (D31 fix — was adds 1 per territory) — PASS
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

## [D28_PresenceValueTests] — Presence Amplification, Vulnerability & Sacrifice

### Amplification
```
Amplification_NoPresence_ReturnsBaseValue                     → no presence = base value unchanged — PASS
Amplification_OnePresence_AddOne                              → 1 presence adds +1 to effect value — PASS
Amplification_ThreePresence_AddsThree                         → 3 presence adds +3 — PASS
Amplification_NullTerritory_ReturnsBaseValue                  → unknown territory = no bonus — PASS
DamageInvaders_AmplifiedByPresence                            → DamageInvaders resolved at base+presence — PASS
DamageInvaders_NoPresence_BaseValueOnly                       → DamageInvaders at base value when no presence — PASS
ReduceCorruption_AmplifiedByPresence                          → ReduceCorruption resolved at base+presence — PASS
ReduceCorruption_HighPresence_ClampsToZero                    → amplified cleanse clamps to 0 — PASS
Amplification_Spec_Example_ReduceCorruption2_With1Presence_Equals3 → spec example: RC×2 + 1p = 3 — PASS
Amplification_Spec_Example_DamageInvaders4_With2Presence_Equals6   → spec example: DI×4 + 2p = 6 — PASS
```

### Vulnerability
```
Vulnerability_Level1_AllowsPresencePlacement          → Level 1 (5 pts) allows placement — PASS
Vulnerability_Level2_BlocksPresencePlacement          → Level 2 (8 pts) blocks new placement — PASS
Vulnerability_Level3_BlocksPresencePlacement          → Level 3 (15 pts) blocks new placement — PASS
Vulnerability_Level3_DestroysAllPresence_OnCrossing   → crossing to Level 3 destroys all presence — PASS
Vulnerability_Level3_DoesNotFire_WhenAlreadyDesecrated → no second destroy event at Level 3 — PASS
Vulnerability_Level1ToLevel3_DestroysPresence         → direct L1→L3 jump also destroys presence — PASS
```

### Sacrifice
```
Sacrifice_RemovesOnePresence_CleanseThreeCorruption  → sacrifice removes 1 presence, cleanses 3 pts — PASS
Sacrifice_FailsOnEmptyTerritory                      → sacrifice returns false when no presence — PASS
Sacrifice_AllowedDuringVigil                         → TurnManager.CanSacrifice() = true in Vigil — PASS
Sacrifice_AllowedDuringDusk                          → TurnManager.CanSacrifice() = true in Dusk — PASS
Sacrifice_BlockedDuringTide                          → sacrifice blocked during Tide — PASS
Sacrifice_BlockedDuringRest                          → sacrifice blocked during Rest — PASS
Sacrifice_DoesNotConsumePlaySlot                     → sacrifice is free action, no play counter increment — PASS
Sacrifice_CorruptionClampsToZero                     → cleanse clamps to 0 — PASS
Sacrifice_FiresEvent                                 → PresenceSacrificed event fires — PASS
```

### Presence Cap
```
PlacePresence_CapsAtMaxPerTerritory   → placing 5 presence on empty territory clamps to MaxPresencePerTerritory (3) — PASS
PlacePresence_AtMax_AddsNothing       → placing presence at max = no change — PASS
PlacePresence_BelowMax_AddsNormally   → placing 1 presence below max adds correctly — PASS
PlacePresence_BulkAdd_ClampsToMax     → bulk place (territory at 2, add 3) clamps to max (3) — PASS
```

### Shield/Boost Natives Amplification
```
ShieldNatives_AmplifiedByPresence   → ShieldNatives value+presence = amplified shield — PASS
ShieldNatives_NoPresence_BaseValueOnly → ShieldNatives base value only when no presence — PASS
BoostNatives_AmplifiedByPresence    → BoostNatives value+presence = amplified boost — PASS
BoostNatives_NoPresence_BaseValueOnly → BoostNatives base value only when no presence — PASS
```

### Combined scenarios
```
Sacrifice_ThenDamage_AmplifiedByRemainingPresence  → post-sacrifice damage reflects reduced presence count — PASS
Desecration_ThenPlacement_Blocked                  → placement still blocked after desecration destroys presence — PASS
```

## [D29_RootCombatTests] — Network Slow, Provocation, SlowInvaders & Rest Growth

### Network Slow
```
NetworkSlow_TwoPresenceNeighbors_ReturnsPenalty1          → 2 presence > 1 invader → −1 movement — PASS
NetworkSlow_OnePresenceNeighbor_ReturnsZero               → 1 presence neighbor → no penalty — PASS
NetworkSlow_ThreePresenceNeighbors_StillReturnsPenalty1   → penalty caps at 1 regardless of count — PASS
NetworkSlow_OneInvader_TwoPresenceNeighbors_Slowed        → 2 presence > 1 invader → slowed (outnumber check) — PASS
NetworkSlow_TwoInvaders_TwoPresenceNeighbors_NotSlowed    → 2 presence == 2 invaders → not slowed — PASS
NetworkSlow_ThreeInvaders_TwoPresenceNeighbors_NotSlowed  → 2 presence < 3 invaders → not slowed — PASS
NetworkSlow_OneInvader_ThreePresenceNeighbors_Slowed      → 3 presence > 1 invader → slowed — PASS
NetworkSlow_ZeroInvaders_ReturnsZero                      → 0 invaders = no penalty (nothing to slow) — PASS
NetworkSlow_WaveBreaksThrough                              → 4 invaders vs 3 presence → not outnumbered → penalty 0 — PASS
NetworkSlow_DefaultInterface_ReturnsZero                  → default IWardenAbility returns 0 — PASS
NetworkSlow_InvaderStays_WhenPenaltyEqualsMoves           → 1 step − 1 penalty = 0 → invader stays — PASS
NetworkSlow_Outrider_ReducedByPenalty                     → Outrider 2 steps − 1 penalty = 1 step — PASS
NetworkSlow_NoPresence_NoImpactOnMovement                 → no presence = no movement reduction — PASS
```

### Presence Provocation
```
ProvokesNatives_WithPresence_ReturnsTrue         → territory with presence provokes natives — PASS
ProvokesNatives_NoPresence_ReturnsFalse          → territory without presence does not provoke — PASS
ProvokesNatives_DefaultInterface_ReturnsFalse    → default interface method returns false — PASS
```

### Slow Invaders effect
```
SlowInvaders_MarksAliveInvaders                              → SlowInvadersEffect marks alive invaders — PASS
SlowInvaders_DoesNotMarkDeadInvaders                         → dead invaders not marked — PASS
SlowInvaders_EffectResolverWorks                             → EffectResolver maps SlowInvaders → SlowInvadersEffect — PASS
SlowInvaders_HalvesMovement_Base2Becomes1                    → slowed invader: 2 steps → 1 — PASS
SlowInvaders_HalvesMovement_Base1BecomesZero                 → slowed invader: 1 step → 0 — PASS
SlowInvaders_StacksWithNetworkSlow_Base2HalvedTo1ThenMinus1Equals0 → slow + network penalty stacks to 0 — PASS
```

### Rest Growth
```
RestGrowth_PlacesPresence_WhenTerritoryHasPresence  → OnRest adds 1 presence to target territory — PASS
RestGrowth_NoChange_WhenTerritoryHasNoPresence      → no growth if target has 0 presence — PASS
RestGrowth_Blocked_ByDefiledCorruption              → growth blocked by Level 2 corruption — PASS
RestGrowth_NullTarget_NoChange                      → null target = no-op — PASS
RestGrowth_Stacks_TwoPresenceBecomesThree           → 2 presence → 3 after rest — PASS
RestGrowth_TurnActions_CallsWardenOnRest             → TurnActions.Rest() invokes OnRest — PASS
RestGrowth_TurnManager_PassesTargetThrough          → TurnManager.Rest(target) propagates target — PASS
```

## [WardenLoaderTests] — Unified Warden JSON Loading
```
FullLoad_ReturnsCorrectMetadata                              → wardenId, name, hand limit parsed — PASS
StartingPresence_IsI1Count1                                  → starting presence I1×1 — PASS
Passives_Count_IsSix                                         → 6 passives loaded — PASS
Passive_NetworkFear_HasCorrectTriggerAndMechanic             → network_fear passive fields correct — PASS
Cards_TotalCount_Is25                                        → 25 total cards in pool — PASS
Cards_StartingCount_Is10                                     → 10 starting cards — PASS
CardSwap_Root025_IsStarting_Root011_IsNot                    → root_025 starting, root_011 draft — PASS
EffectParsing_Root025_TopIsDamageRange1_BottomSecondaryIsSlowInvaders → root_025 effects parsed correctly — PASS
ElementAffinity_IsRoot_Mist_Shadow                           → element affinity fields parsed — PASS
ConvenienceMethods_ReturnSameCountAsFullLoad                 → LoadCards/LoadPassives match full load — PASS
Load_NonExistentFile_Throws                                  → FileNotFoundException on bad path — PASS
```

## [BotStrategyTests] — Simulation Bot Decision Logic
```
ChooseTopPlay_ReturnsCard_WhenPlayableCardsExist                           → bot picks a card when options exist — PASS
ChooseTopPlay_ReturnsNull_WhenAllCardsDormant                              → null when entire hand is dormant — PASS
ChooseTopPlay_PrioritizesPresence_WhenFewerThan3PresenceTerritories        → presence expansion prioritised below 3 territories — PASS
ChooseTopPlay_PrioritizesDamage_WhenInvadersPresent_And3OrMorePresenceTerritories → damage prioritised once network is established — PASS
ChooseTarget_DamageInvaders_PicksMostInvadedTerritory                      → DamageInvaders targets most-invaded territory — PASS
ChooseTarget_ReduceCorruption_PicksMostCorruptedTerritory                  → ReduceCorruption targets highest corruption — PASS
ChooseTarget_PlacePresence_PrefersAdjacentNonDefiledTerritory              → PlacePresence expands to adjacent non-Defiled — PASS
ChooseRestGrowthTarget_ReturnsHighestPresenceNonDefiledTerritory           → rest growth targets densest clean territory — PASS
ChooseRestGrowthTarget_ReturnsNull_WhenAllPresenceTerritoriesDefiled       → null when all presence territories are Defiled — PASS
```

## [ReplayTests] — Deterministic Replay
```
SameSeed_ProducesSameExportString          → same seed → identical export string — PASS
DifferentSeeds_ProduceDifferentExportStrings → different seeds produce different exports — PASS
ExportString_ContainsSeedPrefix            → export starts with "SEED:N|" — PASS
ImportFull_RoundTrips_Seed                 → imported seed matches original — PASS
Replay_ProducesSameFinalWeave              → replay produces same final weave as original run — PASS
Replay_ProducesSameActionCount             → replay produces same number of recorded actions — PASS
```

---

## [BoardLayoutTests] — 4 Board Layouts
```
StandardLayout_Has6Territories        → standard (3-2-1) has 6 territories — PASS
WideLayout_Has10Territories           → wide (4-3-2-1) has 10 territories — PASS
NarrowLayout_Has4Territories          → narrow (2-1-1) has 4 territories — PASS
TwinPeaksLayout_Has8Territories       → twin_peaks (3-2-2-1) has 8 territories — PASS
WideLayout_ArrivalRowHas4             → wide board has 4 arrival territories — PASS
NarrowLayout_ArrivalRowHas2           → narrow board has 2 arrival territories — PASS
TwinPeaks_MiddleRowNotAdjacent        → M1 and M2 are NOT adjacent in twin_peaks — PASS
WideLayout_DistanceA1ToI1Is3          → wide board is 3 steps deep — PASS
NarrowLayout_DistanceA1ToI1Is2        → narrow board is 2 steps deep — PASS
AllLayouts_HaveExactlyOneHeart        → all 4 layouts have HeartId="I1" — PASS
Create_UnknownLayout_DefaultsToStandard → unknown layout ID falls back to standard — PASS
```

## [EncounterVarietyTests] — 5 Encounter Configs + B2 Verification
```
CreatePaleMarchScouts_HasCorrectTideCount       → scouts = 6 tides — PASS
CreatePaleMarchScouts_WavesAreOutriderHeavy     → scouts wave 1 has Outriders — PASS
CreatePaleMarchSiege_Has8Tides                  → siege = 8 tides — PASS
CreatePaleMarchSiege_HasIronclads               → siege waves contain Ironclads — PASS
CreatePaleMarchElite_HasStartingCorruption       → elite has A1 corruption = 3 — PASS
CreatePaleMarchElite_IsEliteTier                → elite tier = EncounterTier.Elite — PASS
CreatePaleMarchStandard_HasCorrectTideCount     → standard = 6 tides — PASS
CreatePaleMarchStandard_UsesPyramidBoard        → standard uses default/standard board layout — PASS
CreatePaleMarchFrontier_Has7Tides               → frontier = 7 tides — PASS
CreatePaleMarchFrontier_UsesWideBoard           → frontier BoardLayout = "wide" — PASS
CreatePaleMarchFrontier_HasFourArrivalPoints    → frontier waves use A4 (wide board) — PASS
EncounterLoader_Create_ReturnsCorrectType       → Create() dispatcher returns correct encounter for all 5 IDs — PASS
EncounterLoader_Create_UnknownId_Throws         → unknown ID throws ArgumentException — PASS
B2Applied_Standard_EveryWaveOptionHasA1Marcher  → every option in standard has A1 Marcher (B2) — PASS
B2Applied_Scouts_EveryWaveOptionHasA1Marcher    → every option in scouts has A1 Marcher (B2) — PASS
B2Applied_Siege_EveryWaveOptionHasA1Marcher     → every option in siege has A1 Marcher (B2) — PASS
B2Applied_Elite_EveryWaveOptionHasA1Marcher     → every option in elite has A1 Marcher (B2) — PASS
```
_Note: B2 tests (last 4) would fail if `AddB2Marchers()` is removed, as several options in each encounter originally have no A1 entry._

## [EncounterLeverTests] — 22 Encounter Levers
```
SurgeTide_SpawnsDoubleWave                    → surge_tides causes double spawn on that tide — PASS
InvaderAdvanceBonus_IncreasesMovement         → invader_advance_bonus adds extra movement steps — PASS
InvaderCorruptionScaling_BonusHp_MatchesL1Count → invaders gain +HP per L1+ territory on arrival — PASS
InvaderHpBonus_AppliedOnCreation              → invader_hp_bonus increases base HP at spawn — PASS
InvaderArrivalShield_AppliedOnSpawn           → invaders spawn with shield points — PASS
PresencePlacementCorruptionCost_AddsCorruption → placing presence adds corruption to territory — PASS
CorruptionSpread_L1Territory_SpreadsToAdjacentL0 → L1+ territories spread corruption to clean neighbours — PASS
CorruptionSpread_L0Territory_DoesNotSpread    → clean territories don't spread — PASS
BlightPulse_AddsCorruptionEveryNTides         → blight_pulse_interval triggers every N tides — PASS
NativeErosion_ReducesHpEachTide               → native_erosion_per_tide reduces native HP per tide — PASS
NativeErosion_KillsNativeAtZero               → natives at 0 HP are removed — PASS
NativeOverride_CustomHpAndDamage              → native_hp_override and native_damage_override applied at spawn — PASS
NativeSpawns_PopulatesTerritoriesWithCorrectCount → NativeSpawns field places correct native counts — PASS
NativeSpawns_NativesHaveCorrectStats          → spawned natives use config hp/damage — PASS
ElementDecayOverride_UsedInsteadOfGlobal      → element_decay_override replaces global decay rate — PASS
ThresholdDamageBonus_AppliedToAllTiers        → threshold_damage_bonus adds to T1/T2/T3 damage — PASS
FearMultiplier_HalvesFear                     → fear_multiplier = 0.5 halves generated fear — PASS
HeartDamageMultiplier_IncreasesWeaveLoss      → heart_damage_multiplier increases weave loss on heart hit — PASS
PlayLimitOverrides_RestrictCardPlays          → play_limit_overrides cap top/bottom play counts — PASS
ApplyStartingCorruption_SetsPoints            → StartingCorruption field pre-corrupts territories — PASS
```

## [BoardCarryoverTests] — Run Arc Carryover
```
ExtractCarryover_CleanBoard_EmptyCorruption           → clean board extracts empty corruption map — PASS
ExtractCarryover_DefiledTerritory_PersistsAsL1        → L2 (Defiled) persists as 3 pts (L1 threshold) — PASS
ExtractCarryover_DesecratedTerritory_FullPersistence  → L3 (Desecrated) persists at full points — PASS
ExtractCarryover_WeavePreserved                       → final weave captured in carryover — PASS
ExtractCarryover_DreadPreserved                       → dread level captured in carryover — PASS
ApplyCarryover_SetsCorruption                         → ApplyCarryover pre-corrupts territories as specified — PASS
ApplyCarryover_SetsWeave                              → ApplyCarryover sets starting weave — PASS
ApplyCarryover_RemovesCards                           → PermanentlyRemovedCards removed from starting deck — PASS
```

## [LocalizationTests] — Loc.cs Key-Value System
```
Get_ExistingKey_ReturnsValue             → Loc.Get returns value for loaded key — PASS
Get_MissingKey_ReturnsKeyItself          → missing key falls back to key string (fail visible) — PASS
Get_WithFormatArgs_FormatsCorrectly      → Loc.Get(key, args) formats {0} {1} placeholders — PASS
Get_WithBadFormatArgs_ReturnsTemplate    → bad format args returns template string (no throw) — PASS
Has_ReturnsTrueForLoaded                 → Loc.Has returns true for loaded key — PASS
Has_ReturnsFalseForMissing               → Loc.Has returns false for missing key — PASS
Clear_RemovesAllStrings                  → Loc.Clear resets state — PASS
LoadFromCsv_ParsesCorrectly              → CSV file loaded and parsed — PASS
LoadFromCsv_HandlesQuotedCommas          → quoted CSV fields with commas parsed correctly — PASS
LoadFromCsv_MissingFile_NoException      → missing CSV file does not throw — PASS
```

## [SimProfileTests] — Sim Profile Loading & Application
```
LoadProfile_ParsesAllFields              → all SimProfile JSON fields deserialized — PASS
CliFlags_OverrideProfileValues           → CLI --warden/--seeds flags override profile — PASS
ApplyBalanceOverrides_SetsConfigFields   → balance_overrides fields applied to BalanceConfig — PASS
ApplyEncounterOverrides_ChangesTideCount → encounter_overrides.tide_count applied — PASS
ApplyWardenOverrides_AddsCards           → warden_overrides.add_cards applied to deck — PASS
ApplyWardenOverrides_RemovesCards        → warden_overrides.remove_cards removes from deck — PASS
ApplyWardenOverrides_UpgradesCardValue   → warden_overrides.upgrade_cards changes card value — PASS
ApplyPassiveOverrides_ForcesLock         → passive_overrides force_lock disables passive — PASS
ApplyPassiveOverrides_ForcesUnlock       → passive_overrides force_unlock enables passive — PASS
ApplyCarryover_RemovesCards              → board_carryover.removed_cards removes from deck — PASS
SimProfile_ElementOverrides_Applied      → element_overrides applied to BalanceConfig — PASS
```

## [BalanceConfigTests] — Data-Driven Threshold Overrides
```
DefaultConfig_MatchesCurrentHardcodedValues        → default BalanceConfig values are as designed — PASS
GetThreshold_DefaultsToGlobal                      → element without override uses global threshold — PASS
GetThreshold_UsesElementOverride                   → element with override uses per-element threshold — PASS
GetThreshold_OtherElementsUnaffected               → override for one element doesn't affect others — PASS
GetThresholdDamage_DefaultsToGlobal                → threshold damage uses global when no override — PASS
GetThresholdDamage_UsesElementOverride             → threshold damage uses per-element override — PASS
Config_Clone_IsIndependent                         → Clone() creates independent copy (no shared dict) — PASS
PerElementThreshold_IntegratesWithElementSystem    → overrides wired end-to-end through ElementSystem — PASS
```

## [EmberAbilityTests] — Ember Warden Passives
```
Ember_AshTrail_AddsCorruption_AndDamagesInvaders   → Ash Trail adds 1 corruption + 1 damage per presence territory — PASS
Ember_AshTrail_OnlyAffectsPresenceTerritories      → Ash Trail only fires in territories with presence — PASS
Ember_AshT3_DealsOnlyDamage_NoCorruption           → Ash T3 (B1 nerf path) — see AshTier3 in ThresholdT2T3Tests — PASS
Ember_AshTrail_Ash_T3 tests → see [ThresholdT2T3Tests] Ash section — PASS
Ember_EmberFury_BonusDamage_PerCorruptedTerritory  → EmberFury adds +1 damage per L1+ territory — PASS
Ember_EmberFury_Inactive_WhenLocked                → EmberFury passive locked = no bonus damage — PASS
Ember_ControlledBurn_Generates2Fear_With3PlusL1Territories → Controlled Burn generates 2 fear at 3+ L1 — PASS
Ember_ControlledBurn_NoFear_WithFewerThan3L1       → below 3 L1 territories = no fear — PASS
Ember_ControlledBurn_OnlyCountsL1_NotL2OrL0        → only L1 (Tainted) territories count, not L2/L3 — PASS
Ember_ScorchedEarth_DamageEqualsCorruptionSum       → Resolution damage = sum of corruption in presence territories — PASS
Ember_ScorchedEarth_FullyCleansesL0AndL1           → Resolution cleanses L0 and L1 fully — PASS
Ember_ScorchedEarth_HalvesLevel2                   → Resolution halves L2 corruption (round down) — PASS
Ember_ScorchedEarth_NoChangeLevel3                 → Resolution does not change L3 (permanent) — PASS
Ember_ScorchedEarth_MixedBoard_CorrectCleanse       → mixed board: L0 cleansed, L2 halved, L3 unchanged — PASS
Ember_PhoenixSpark_GeneratesFear_OnPermanentRemoval → PhoenixSpark generates 3 fear when card permanently removed — PASS
Ember_PresencePlacement_AllowedAtLevel2             → Ember can place presence in Defiled (L2) territory — PASS
Ember_PresencePlacement_BlockedAtLevel3             → Ember blocked from Desecrated (L3) territory — PASS
Ember_CleanWin_Possible_WhenAllPresenceAtL1         → Ember can achieve Clean outcome with correct play — PASS
```

## [EmberLoaderTests] — Ember JSON Loading
```
EmberLoad_WardenId_IsEmber          → wardenId = "ember" — PASS
EmberLoad_ElementAffinity_IsAsh_Shadow_Gale → element affinity = Ash/Shadow/Gale — PASS
EmberLoad_StartingCards_Is8         → 8 starting cards (smaller deck = faster cycling) — PASS
EmberLoad_Cards_TotalCount          → total card pool count — PASS
EmberLoad_Passives_Count            → 7 passives (6 unlockable + base set) — PASS
```

## [PassiveGatingTests] — Passive Unlock/Lock System
```
Root_StartsWithThreeActivePassives          → Root has 3 base passives active at encounter start — PASS
Root_NetworkSlowInactive_AtStart            → network_slow locked at start (unlocks on Shadow T1) — PASS
Root_ProvocationInactive_AtStart            → presence_provocation locked at start — PASS
Root_RestGrowthInactive_AtStart             → rest_growth locked at start — PASS
Root_RestGrowth_UnlocksOnRootT1             → rest_growth unlocks when Root T1 fires — PASS
Root_NetworkSlow_UnlocksOnShadowT1          → network_slow unlocks when Shadow T1 fires — PASS
Root_Provocation_UnlocksOnRootT2            → presence_provocation unlocks when Root T2 fires — PASS
Root_DuplicateThreshold_DoesNotReUnlock     → threshold firing twice doesn't re-unlock — PASS
Root_UnlockFiresEvent                       → PassiveUnlocked event fires on unlock — PASS
Root_Reset_ClearsUnlocks                    → Reset() restores warden defaults — PASS
Root_GetMovementPenalty_ReturnsZero_WhenNetworkSlowLocked → locked network_slow returns 0 penalty — PASS
Root_NetworkFearCapped_At4                  → Network Fear capped at 4 per BalanceConfig — PASS
Root_ProvokesNatives_ReturnsFalse_WhenLocked → locked provocation = false — PASS
ApplyPassiveOverrides_ForcesUnlock          → force_unlock overrides gating — PASS
ApplyPassiveOverrides_ForcesLock            → force_lock prevents unlock — PASS
```

---

## Staleness / Review Flags

| Test | Risk | Note |
|---|---|---|
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
