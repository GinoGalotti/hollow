# Hollow Wardens — UI Redesign Prompt Sequence

Step-by-step prompts to send to Claude. Each prompt block is copy-pasteable. 

**Execution model:** Phase A is sequential (each step depends on the previous). Phases B+C can run in parallel after A completes. Phase D tasks can all run in parallel. Phase E tasks can run in parallel. Swarm prompts are provided where parallelism is safe.

Run smoke tests between phases to validate.

---

## Phase A — Foundation (Sequential — must be in order)

### A1. Theme File

```
Read UI-REDESIGN-PLAN.md (Phase A, section A1) and UI-REDESIGN-AUDIT.md.

Implement A1: Populate hollow_wardens/godot/assets/hollow_wardens_theme.tres as a real Godot Theme resource.

The file currently exists but is empty (74 bytes). Create StyleBoxFlat definitions for PanelContainer (normal), Button (normal/hover/pressed/disabled), and Label. Set default_font to IMFellEnglish-Regular.ttf, default_font_size 13. Use the color palette from the plan: near-black warm backgrounds (#1E1B18), muted warm borders (#5C524A), warm off-white text (#D9CDB8). Rounded corners 6px on panels, 4px on buttons, 8px content margins.

Art direction: dark gothic/nature aesthetic — think Inscryption's moodiness but with forest greens instead of pure black. Consistent dark palette for board and UI, but cards and wardens can have unique accent colors.

Reference: Cobalt Core's clean readability, Slay the Spire's card frames, Balatro's visual feedback.

After writing the theme, verify it's valid by building: dotnet build hollow_wardens/HollowWardens.csproj

Do NOT modify any view controllers yet — just the theme .tres file.
```

### A2. FontCache + Cleanup

```
Read UI-REDESIGN-PLAN.md (Phase A, section A2) and UI-REDESIGN-AUDIT.md.

Create hollow_wardens/godot/autoloads/FontCache.cs — a static class that loads Cinzel-Bold, Cinzel-Regular, IMFellEnglish-Regular, and IMFellEnglish-Italic once. Register it as an autoload in project.godot.

Then go through ALL view controllers in hollow_wardens/godot/views/ and replace every GD.Load<Font>() call with FontCache references. There are ~15 controllers that each independently load the same fonts. Keep the exact same font assignments — just centralize the loading.

Build after: dotnet build hollow_wardens/HollowWardens.csproj
```

### A3. Layout Restructure + PhaseIndicator Fix + Background

```
Read UI-REDESIGN-PLAN.md (Phase A, sections A3-A5) and UI-REDESIGN-AUDIT.md.

Restructure hollow_wardens/godot/scenes/Game.tscn to use proper anchoring instead of hardcoded pixel offsets. The current layout has every panel positioned with fixed coordinates like Vector2(940, 10) which breaks on any non-1920x1080 resolution.

Target layout:
- TopStrip: anchor top, full width, 50px tall — PhaseIndicator + WeaveBar
- LeftPanel: anchor left, 180px wide — StatusInfo, FearQueue, PassivePanel
- CenterArea: fills remaining space — board territories
- RightPanel: anchor right, 300px wide — ElementTracker, DreadBar, TidePreview
- BottomDock: anchor bottom, full width, 180px tall — HandDisplay

Also fix PhaseIndicatorController.cs — it's currently broken (VBoxContainer assigned to CanvasLayer slot). Rewrite it as a HBoxContainer child of TopStrip showing VIGIL | TIDE | DUSK with the current phase highlighted and others dimmed. The tide count (e.g. "Tide 3/6") should show alongside the active phase. Keep all existing signal subscriptions working (EncounterReady, PhaseChanged, TurnStarted, RestStarted, ResolutionTurnStarted, DeckCountsChanged, EncounterEnded). Deck counts (Draw/Disc/Dissolved/Dormant) should display as a small text row near the hand area, not in the phase strip.

Add a subtle dark gradient background (ColorRect with dark forest green-black at top transitioning to dark brown-black at bottom).

Build and verify: dotnet build hollow_wardens/HollowWardens.csproj

IMPORTANT: Do NOT change any game logic or signal wiring. Only change node types, positions, and anchoring. Every signal subscription must still work after the change.
```

### Validate Phase A

Ask Gino to run the smoke tests:
```
! "D:\Downloads\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe" --path "D:\Workspace\hollow\hollow_wardens" -- --run-ui-tests
```
Then review screenshots in `ui-test-screenshots/`.

---

## Phase B + C — Cards & Board (PARALLEL)

After Phase A validates, send this single swarm prompt that does B and C simultaneously:

### Swarm: Cards + Board in parallel

```
I need you to implement two parallel workstreams from our UI redesign plan. Read UI-REDESIGN-PLAN.md and UI-REDESIGN-AUDIT.md for full context.

Use two agents working in parallel:

AGENT 1 — CARD VISUAL UPGRADE (CardViewController.cs + HandDisplayController.cs + strings.csv):

Redesign CardViewController.cs:
1. CARD SIZE: Increase to 160x210px.
2. CARD FRAME: StyleBoxFlat with rarity-specific borders (Dormant: gray desaturated, Awakened: blue tint, Ancient: gold tint). Use FontCache for fonts.
3. CARD LAYOUT top to bottom: element icons + timing badge | card name (Cinzel Bold 13pt, rarity color) | "TOP" divider | top effect (IM Fell 11pt) | "BOT" divider on darker bg | bottom effect (IM Fell Italic 11pt) | play buttons (only when playable)
4. TIMING BADGES: Replace "[FAST]"/"[SLOW]" text with colored badges — blue bg for FAST, amber bg for SLOW. Use Kenney icons if suitable 16x16 icons exist.
5. EFFECT TEXT: Human-readable colored text:
   - "DamageInvaders ×4 r1" → "Deal 4 damage (range 1)" with 4 in red
   - "ReduceCorruption ×3" → "Cleanse 3 corruption" with 3 in green
   - "PlacePresence ×1 r1" → "Place 1 presence (range 1)" with 1 in cyan
   Add localization keys to data/localization/strings.csv.
6. CARD HOVER: MouseEntered → tween Scale (1.15, 1.15) + Position.Y -30 + ZIndex=10 over 0.1s. MouseExited → reverse over 0.08s.
7. HAND SCALING in HandDisplayController.cs:
   - 1-5 cards: 160px, 8px gaps
   - 6-8 cards: 140px, overlap 15px
   - 9-10 cards: 120px, overlap 25px
8. CARD PLAY ANIMATIONS:
   - Play top: tween upward + scale to 0.5x + fade over 0.4s
   - Play bottom: flash bottom brighter 0.1s, then shrink + fade 0.3s
   - Draw: cards slide from left with 0.15s ease-out, staggered 0.05s per card

Files to modify: hollow_wardens/godot/views/CardViewController.cs, hollow_wardens/godot/views/HandDisplayController.cs, data/localization/strings.csv
Do NOT touch: TerritoryViewController.cs, ElementTrackerController.cs, DreadBarController.cs, GameBridge.cs

AGENT 2 — BOARD VISUAL UPGRADE (TerritoryViewController.cs):

Redesign TerritoryViewController.cs:
1. TERRITORY SIZE: Increase to 150x110px rounded rectangles. 2px border reflecting corruption level.
2. CORRUPTION BACKGROUNDS: Gradient fills per level:
   - L0: Color(0.12, 0.22, 0.12) deep forest green
   - L1: Color(0.25, 0.22, 0.10) sickly yellow-green
   - L2: Color(0.30, 0.15, 0.08) burnt orange
   - L3: Color(0.35, 0.08, 0.08) deep crimson
   Lerp between levels based on corruption point progress.
3. TERRITORY HEADER: ID (left, Cinzel Bold 12pt) + terrain icon (right, 16x16 Kenney: tree=Forest, mountain=Mountain, water_drop=Wetland, star=Sacred, fire=Scorched, skull=Blighted) + presence dots (8px circles, green for Root, orange for Ember; show up to 3 dots, "×N" for 4+)
4. INVADER TOKENS: Replace letter-squares with Kenney icons:
   - Marcher: sword icon, 20x20, red-brown bg + HP bar (2px, green→yellow→red)
   - Ironclad: shield icon, purple bg + HP bar
   - Outrider: boot/arrow icon, orange bg + HP bar
   - Pioneer: hammer icon, tan bg + HP bar
   Fall back to current letters if icon not found.
5. NATIVE TOKENS: Leaf icon, 18x18, forest-green bg + HP bar
6. CORRUPTION BAR: 80x4px segmented bar at territory bottom showing corruption progress
7. ADJACENCY LINES: 1px gray lines between adjacent territory centers, drawn behind tiles
8. TARGETING: Valid targets pulse yellow border (0.8s cycle), invalid darken to 40%
9. FLOATING NUMBERS: Damage red, healing green, fear purple. Pop animation (1.3x→1.0x in 0.1s, then rise+fade 0.5s). Cinzel Bold 16pt.

Files to modify: hollow_wardens/godot/views/TerritoryViewController.cs (optionally create BoardOverlayController.cs for adjacency lines)
Do NOT touch: CardViewController.cs, HandDisplayController.cs, GameBridge.cs

Both agents: Use FontCache for all font loading. Build after: dotnet build hollow_wardens/HollowWardens.csproj
```

### Validate Phase B+C

Run smoke tests, review:
- Cards: readability, frames, colored text, hover, hand scaling
- Board: territory shapes, corruption colors, invader icons, HP bars, adjacency lines

---

## Phase D — HUD Panels (PARALLEL — 3 agents)

All three touch different controllers with no file overlap. Send as one swarm prompt:

### Swarm: Element Bars + Weave/Dread + Tide Preview

```
I need three parallel HUD upgrades from UI-REDESIGN-PLAN.md Phase D. Read UI-REDESIGN-PLAN.md and UI-REDESIGN-AUDIT.md for context.

AGENT 1 — ELEMENT TRACKER (ElementTrackerController.cs only):

Redesign ElementTrackerController.cs to replace ASCII bars with graphical fill bars.

Current: "Root  0  [····:··:···::]  T1  T2  T3"

New per-row (280px wide):
1. Element icon (20x20, keep existing Kenney icons)
2. Name label (48px, current font via FontCache)
3. Fill bar (120px wide, 12px tall) using _Draw() on a custom Control or TextureProgressBar:
   - Background: dark gray Color(0.15, 0.15, 0.15)
   - Fill width = (value / 13.0) * 120px in element color
   - Vertical notch lines at 4/13, 7/13, 11/13 positions — white 30% opacity (unfired), gold (fired)
4. T1/T2/T3 buttons: keep existing pulse-gold-when-pending behavior

Element fill colors: Root=Color(0.2,0.7,0.2), Mist=Color(0.4,0.6,0.8), Shadow=Color(0.5,0.3,0.6), Ash=Color(0.8,0.3,0.1), Gale=Color(0.6,0.8,0.3), Void=Color(0.3,0.3,0.4)

Keep all signals: ElementChanged, ThresholdPending, ThresholdExpired, ThresholdResolved.
Do NOT touch other controllers.

AGENT 2 — WEAVE + DREAD (DreadBarController.cs only):

Redesign DreadBarController.cs to replace ASCII bars with visual elements.

1. WEAVE BAR: 20 segments, each ~11px wide = 220px, 14px tall, using _Draw():
   - Filled: blue Color(0.2,0.6,0.9) when >50%, yellow 25-50%, red <25%
   - Empty: dark gray Color(0.15,0.15,0.15)
   - 1px gap between segments
   - "X/20" label right of bar
   - Pulse animation when ≤5 weave (oscillate alpha 0.6–1.0)

2. DREAD: 4 skull icons (Kenney, 16x16) in a row. Lit = orange Color(0.9,0.5,0.1), unlit = gray Color(0.3,0.3,0.3). "Dread" label left.

3. FEAR PROGRESS: Thin bar 160x6px, purple fill Color(0.5,0.2,0.7), progress toward next 15-threshold. "Fear: X (next: Y)" label.

Keep all signals: EncounterReady, DreadAdvanced, FearGenerated, WeaveChanged.
Do NOT touch other controllers.

AGENT 3 — TIDE PREVIEW (TidePreviewController.cs only):

Redesign TidePreviewController.cs with mini card frames.

Current: text "Tide 1/6", "▶ Ravage", "Next: Ravage"

New:
1. Header "TIDE PREVIEW" (keep Cinzel Bold)
2. "Tide N/Total" (Cinzel Bold 14pt)
3. Current action: 100x60px PanelContainer with StyleBoxFlat — dark red bg + bright red 2px border for Painful, dark green + bright green for Easy. "▶ ActionName" (Cinzel Bold 12pt). 1-line description below (IM Fell 10pt, gray).
4. "Next:" label (small gray)
5. Next action: same frame at 60% opacity

Keep all signals: EncounterReady, TurnStarted, ActionCardRevealed, NextActionPreviewed, PhaseChanged, ResolutionTurnStarted.
Do NOT touch other controllers.

All agents: Use FontCache. Build after: dotnet build hollow_wardens/HollowWardens.csproj
```

### Validate Phase D

Run smoke tests, check element bars, weave bar, dread skulls, tide preview cards.

---

## Phase E — Animation & Juice (PARALLEL — 2 agents)

### Swarm: Screen Effects + Number Animations

```
I need two parallel animation upgrades from UI-REDESIGN-PLAN.md Phase E. Read the plan for context.

AGENT 1 — JUICE MANAGER (new file + Game.tscn):

Create hollow_wardens/godot/views/JuiceManager.cs as a Node. Register in Game.tscn.

1. SCREEN SHAKE on WeaveChanged (when value decreases):
   - 3 oscillations: random 2-4px offset, 0.05s each, return to Zero over 0.1s
   - Apply to the root UI container node

2. PHASE COLOR WASHES on PhaseChanged:
   - Vigil: ColorRect overlay → Color(0.2,0.4,0.7,0.15) fade in 0.3s, hold 0.3s, fade out 0.3s
   - Tide: Color(0.7,0.2,0.2,0.15)
   - Dusk: Color(0.7,0.5,0.2,0.1)
   Create the ColorRect dynamically, free it after the animation.

3. DREAD ADVANCE on DreadAdvanced:
   - Overlay to Color(0,0,0,0.3) over 0.3s, hold 0.5s, fade over 0.5s

Do NOT touch any existing view controllers.

AGENT 2 — ANIMATION POLISH (existing controllers):

1. NUMBER ANIMATIONS in DreadBarController.cs: When weave or fear values change, tween from old→new over 0.3s using TweenMethod with Callable.From<int>() that updates the label text each step. Store previous values to detect changes.

2. ELEMENT THRESHOLD FLASH in ElementTrackerController.cs: On ThresholdPending, flash the element row gold (Modulate → Color(1.0,0.9,0.5)) for 0.15s, tween back to white over 0.2s. Add this alongside the existing T-button pulse.

3. CORRUPTION COLOR LERP in TerritoryViewController.cs: Instead of snapping background color on corruption change, store target color and lerp toward it in _Process() at rate 2.0 * delta. Call QueueRedraw() each frame while lerping.

Do NOT touch JuiceManager.cs or Game.tscn.

Both agents: Build after: dotnet build hollow_wardens/HollowWardens.csproj
```

### Validate Phase E

Run the game manually (not just smoke tests) to see animations in action:
```
! "D:\Downloads\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe" --path "D:\Workspace\hollow\hollow_wardens"
```

---

## Execution Summary

| Step | Prompt | Mode | Depends On |
|------|--------|------|------------|
| A1 | Theme file | Solo | Nothing |
| A2 | FontCache | Solo | A1 |
| A3 | Layout + PhaseIndicator | Solo | A2 |
| **Smoke test** | Validate Phase A | Manual | A3 |
| B+C | Cards + Board swarm | **2 parallel agents** | Phase A |
| **Smoke test** | Validate Phase B+C | Manual | B+C |
| D | Element + Weave + Tide swarm | **3 parallel agents** | Phase A (B+C nice-to-have) |
| **Smoke test** | Validate Phase D | Manual | D |
| E | JuiceManager + Anim polish swarm | **2 parallel agents** | Phases B+C+D |
| **Smoke test + manual play** | Validate Phase E | Manual | E |

**Total: 5 prompts + 4 validation steps** (down from 12 sequential prompts)

---

## Post-Phase Checklist

After all phases complete:

- [ ] All 9 existing smoke tests still pass
- [ ] No build warnings introduced (beyond the pre-existing PassiveGating one)
- [ ] Theme applies consistently to all panels
- [ ] Cards are readable at hand sizes 1-10
- [ ] Effect text is human-readable with colored values
- [ ] Territories show corruption level visually
- [ ] Invader/native tokens use icons not letters
- [ ] Element bars are graphical not ASCII
- [ ] Weave bar shows segmented health
- [ ] Phase indicator works (was broken before)
- [ ] Tide preview shows mini card frames
- [ ] Screen shake fires on weave damage
- [ ] Phase transitions show color washes
- [ ] No hardcoded pixel positions remain (all anchored)
