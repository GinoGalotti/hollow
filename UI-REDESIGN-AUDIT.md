# Hollow Wardens — UI Audit Summary

## Controller Inventory

| Controller | Node Type | Lines | Role | Key Issues |
|-----------|-----------|-------|------|------------|
| GameBridge.cs | Node (Autoload) | 2,061 | Master event hub, 50+ signals | None — well architected |
| CardViewController.cs | PanelContainer | 248 | Single card render + play buttons | No frame, text-only effects, TODO for card_empty.png |
| TerritoryViewController.cs | Node2D | 369 | Territory + units via custom _Draw() | Flat rectangles, letter-square tokens, hardcoded colors |
| ElementTrackerController.cs | VBoxContainer | 171 | 6 element bars + T1/T2/T3 buttons | ASCII bars, hardcoded threshold positions |
| PhaseIndicatorController.cs | VBoxContainer | 114 | Phase label + deck counts | **BROKEN** — VBoxContainer/CanvasLayer binding failure |
| HandDisplayController.cs | HBoxContainer | 64 | Card hand row | Hardcoded width constraint, no scaling for 6+ cards |
| DreadBarController.cs | VBoxContainer | 86 | Dread + fear + weave display | ASCII bar, hardcoded thresholds |
| PassivePanelController.cs | VBoxContainer | 156 | Passive ability list | 9pt text, cramped, hardcoded gating logic |
| TidePreviewController.cs | VBoxContainer | 86 | Current + next action card | Text-only, overlaps hand area, TODO for mini card frame |
| FearActionQueueController.cs | VBoxContainer | 114 | Queued fear actions + reveal | Good flip tween, but fragile IsInstanceValid workaround |
| FearConfirmController.cs | Control | 87 | Fear action confirm overlay | Static positioning, no animation |
| CounterAttackController.cs | PanelContainer | 212 | Damage assignment UI | Hardcoded position, no visual feedback |
| RewardScreenController.cs | CanvasLayer (L10) | 198 | Post-encounter rewards | Text input for card removal (should be dropdown) |
| EventScreenController.cs | CanvasLayer (L10) | 141 | Event choice screen | No locked/unlocked visual distinction |
| UpgradeScreenController.cs | CanvasLayer (L10) | 228 | Card/passive upgrade | No "already applied" indicator |
| RestScreenController.cs | CanvasLayer (L8) | 148 | Rest dissolve + reroll | Fragile DisableAllRerollButtons hack |
| WardenSelectController.cs | CanvasLayer (L2) | 613 | 5-screen selection flow | Visibility flags instead of state machine |
| DevConsole.cs | CanvasLayer (L100) | 161 | Debug command input | 16 commands stubbed |
| DebugLogController.cs | Control | 234 | Event log + state export | Hardcoded colors, no filtering |
| UILayerController.cs | CanvasLayer | 11 | Godot 4.6 bug workaround | Intentionally empty |
| DummyUIChildController.cs | VBoxContainer | 11 | Sacrificial first child | Intentionally empty |

**Total UI code:** ~3,600 lines across 20 controllers + 2,061 lines in GameBridge

---

## Architecture Assessment

### Strengths
- **Clean separation:** Core (pure C#) → Bridge (signals) → UI (Godot nodes)
- **Event-driven:** All 50+ signals flow through GameBridge singleton, no polling
- **Modular:** Each controller is self-contained, replaceable independently
- **No circular deps:** UI never imports from Core directly
- **Testable:** Smoke tests capture screenshots for validation

### Weaknesses
- **No theme:** hollow_wardens_theme.tres is empty (74 bytes)
- **All styling inline:** Colors, fonts, sizes hardcoded in each of 20 controllers
- **Fonts loaded per controller:** ~15 controllers each do `GD.Load<Font>()`
- **All positions hardcoded:** Fixed pixel offsets, breaks on non-1920x1080
- **Procedural UI:** All nodes built in _Ready(), no visual scene design
- **No state machine:** Screen navigation via visibility toggling
- **No animation system:** Only FearActionQueue has a basic tween

---

## Asset Inventory

### Available
| Category | Count | Source | Location |
|----------|-------|--------|----------|
| RPG UI elements | 90 | kenney_ui-pack-rpg-expansion | godot/assets/art/ |
| Board game icons | 513 | kenney_board-game-icons | godot/assets/art/ |
| Game icons | 425 | kenney_game-icons | godot/assets/art/ |
| Playing cards | 285 | kenney_playing-cards-pack | godot/assets/art/ |
| Hexagon tiles | 81 | kenney_hexagon-kit | godot/assets/art/ |
| Cinzel font | 6 weights | Google Fonts | godot/assets/fonts/ |
| IM Fell English | 2 weights | Google Fonts | godot/assets/fonts/ |

**Total image assets available: 1,394 (deduplicated)**

### Missing
- Card frame / border art
- Card back art (for face-down fear cards)
- Territory background textures
- Phase strip background
- Background gradient/texture
- Timing badge icons (fast/slow)
- Custom shaders (none exist)
- Sound effects (none exist)

---

## Known Bugs

| Bug | File | Severity | Fix Complexity |
|-----|------|----------|----------------|
| PhaseIndicator never renders | PhaseIndicatorController.cs | **Critical** | Medium — needs node type change |
| Tide Preview overlaps hand | TidePreviewController.cs + HandDisplay | Medium | Low — layout fix |
| Fear tween ObjectDisposedException risk | FearActionQueueController.cs:55-74 | Low | Low — defer callback |
| Reroll button hack | RestScreenController.cs | Low | Low — data-driven flag |
| Card removal uses text input | RewardScreenController.cs | Low | Medium — build dropdown |

---

## Hardcoded Values Requiring Theme Migration

### Colors (sampled across controllers)
```
Headers:        Color(0.9f, 0.85f, 0.7f)   — warm tan
Vigil phase:    Color(0.5f, 0.8f, 1.0f)    — blue
Tide phase:     Color(1.0f, 0.4f, 0.4f)    — red
Dusk phase:     Color(1.0f, 0.7f, 0.3f)    — orange
Dormant card:   Color(0.5f, 0.5f, 0.5f)    — gray
Targeting:      Color(1.0f, 1.0f, 0.0f)    — yellow
Weave healthy:  Color(0.0f, 0.9f, 0.9f)    — cyan
Weave critical: Color(1.0f, 0.3f, 0.3f)    — red
```

### Positions (sampled)
```
ElementTracker:    Vector2(940, 10)
DreadBar:          Vector2(940, 320)
TidePreview:       Vector2(940, 540)
HandDisplay:       Offset(230, 490), size(705, 158)
FearQueue:         Vector2(10, 270)
PassivePanel:      Vector2(10, 550)
```

### Font Sizes (sampled)
```
Card name:         13pt Cinzel-Bold
Card effect:       11pt IMFellEnglish
Section headers:   16pt Cinzel-Bold (inconsistent — some use 13pt)
Phase label:       varies per controller
Passive desc:      9pt IMFellEnglish
```

---

## Signal Flow (UI-relevant subset)

```
GameBridge emits:                    Consumed by:
─────────────────                    ────────────
EncounterReady          →  ALL controllers (rebuild)
PhaseChanged(phase)     →  PhaseIndicator, HandDisplay, CardView, ElementTracker
HandChanged()           →  HandDisplay (rebuild cards)
ElementChanged(e, val)  →  ElementTracker (update bar)
ThresholdPending(e, t)  →  ElementTracker (pulse T button)
ThresholdResolved(e, t) →  ElementTracker (disable T button)
FearGenerated(amount)   →  DreadBar (update counter)
DreadAdvanced(level)    →  DreadBar (update skulls)
WeaveChanged(val)       →  DreadBar (update bar)
ActionCardRevealed(card)→  TidePreview (show current)
NextActionPreviewed(c)  →  TidePreview (show next)
FearActionQueued()      →  FearActionQueue (add face-down)
FearActionRevealed(desc)→  FearActionQueue (flip card)
FearActionPending(desc) →  FearConfirm (show overlay)
InvaderArrived(t, unit) →  TerritoryView (redraw)
InvaderDefeated(t, id)  →  TerritoryView (redraw + float text)
CorruptionChanged(t, v) →  TerritoryView (redraw)
TargetingModeChanged()  →  TerritoryView, CardView, FearConfirm
CounterAttackStarted(t) →  CounterAttackController (show overlay)
EncounterEnded(result)  →  PhaseIndicator, RewardScreen
ChainAdvanceReady()     →  WardenSelect (show continue screen)
```

This signal architecture is excellent and will support all proposed visual changes without modification. New animations simply subscribe to existing signals.
