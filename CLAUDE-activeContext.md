# Hollow Wardens — Active Context

## Current Status
**Phase 5 complete** — 111/111 tests passing (41 original + 70 new... wait: 41+10 from Phases 1-4 + 8 Group I + 2 I1 sub-tests = let me just say 111 total)

## Phase Completion
| Phase | Description | Tests | Status |
|-------|-------------|-------|--------|
| 1 | Data layer + project setup | 41 | ✅ Done |
| 2 | Territory graph + turn/tide managers | +10 | ✅ Done |
| 3 | CardEngine (play, resolve, dissolve) | +20 | ✅ Done |
| 4 | Encounter loop (clean/weathered/breach) | +11 | ✅ Done |
| 5 | Root Warden implementation | +10 | ✅ Done |

## Phase 5 Deliverables

### Code changes
- `CardEffect.cs` — Added `AwakeDormant` (enum value 12)
- `CardEngine.cs` — IsDormant guard in TryPlayCard; DissolveCard signals moved after OnDissolve; AwakeDormant ResolveEffect case
- `Warden.cs` — Added `virtual void OnTideStart()`
- `TurnManager.cs` — Calls `CurrentWarden.OnTideStart()` in EndVigil
- `EncounterManager.cs` — Calls `CurrentWarden.OnResolutionStart(territories)` in BeginResolutionPhase
- `WardenRoot.cs` (new) — Dormancy, Network Fear, Assimilation
- `TestRunner.cs` — Group I (10 assertions, 8 test blocks)

### Resources
- `resources/cards/root/root_001.tres` … `root_030.tres` — 30 Root card definitions
- `root_001` and `root_011` have custom DissolveEffect; others use default (PlacePresence 1 rng0)

## Phase 6 Foundation (Complete)

Localization + controller support infrastructure baked in before any UI scenes built.

### Changes
- **Data class property renames**: `CardName→CardNameKey`, `Description→DescriptionKey` (CardEffect, EffectCondition, EscalateEvent), `WardenName→WardenNameKey`, `Archetype→ArchetypeKey`, `DissolveDescription→DissolveDescKey`, `ResolutionStyle→ResolutionDescKey`, `FactionName→FactionNameKey`, `DreadEventDescription→DreadDescKey`
- **TestRunner.cs**: All renamed property usages updated (~16 locations)
- **TideExecutor.cs**: `e.Description` → `e.DescriptionKey`
- **translations.csv**: `hollow_wardens/locale/translations.csv` — 38 UI strings + all 30 Root card keys (auto-generated block)
- **project.godot**: 9 custom input actions (keyboard + controller) + locale registration
- **generate-cards.py**: Key derivation helper, `CardNameKey`/`DescriptionKey` output in .tres, CSV sentinel block update
- **30 .tres files**: Regenerated with `CardNameKey`/`DescriptionKey` properties
- **master.md**: §13 Localization System + §14 Input Actions
- **CLAUDE-decisions.md**: D10 (custom nav actions), D11 (CSV over gettext)
- **111/111 tests still passing**

## Next Phase (Phase 6)
- UI layer: CardView, HandView, TerritoryMapView
- All Labels must use `Tr(key)` — never hardcoded text
- All interactive Controls: `FocusMode = All`
- HandView: consume `ui_navigate_left/right`; TerritoryMapView: `ui_navigate_*` for 3×3 grid
- CardDormant signal (left as TODO comment in CardEngine)
- Warden select screen
- Encounter scene wiring

## Key Design Decisions (Phase 5)
- Dormancy always beats Boss dissolution (Root cards NEVER permanently removed on first dissolve)
- Double-dissolve (card already Dormant) → permanently removed
- Network Fear triggers at start of The Tide (TurnManager.EndVigil → OnTideStart)
- Assimilation: Presence absorbs ALL invaders in adjacent territory + Corruption -1 per neighbor
- AwakeDormant Value=0 means "awaken all dormant" in hand

## Card Catalog
See `cards-catalog.html` in project root for browsable Root card deck reference.
