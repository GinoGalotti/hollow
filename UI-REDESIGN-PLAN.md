# Hollow Wardens — UI Redesign Plan

## Executive Summary

The current UI is a **functionally complete developer prototype** — every game system is wired and working, but the visual presentation reads like a debug view. The gap between "prototype" and "polished" comes down to: no theme, no card frames, no animations, text-only everything, hardcoded pixel positions, and zero atmosphere.

The good news: the architecture is solid (clean Core/Bridge/UI separation with 50+ signals), and we have **1,653 Kenney assets** (RPG UI pack, board game icons, playing cards), professional fonts (Cinzel + IM Fell English), and a 1920x1080 canvas to work with. Nothing needs to be rewritten from scratch — every improvement layers onto existing controllers.

This plan is organized into **5 phases** (A through E), ordered by visual impact per effort. Each phase is independently shippable — you see improvement after every phase.

---

## Open Design Questions (Need Your Input)

These decisions affect the art direction of Phases B–E. Phase A (Foundation) can start immediately regardless. Gino's answer has been added after.

| # | Question | Options | Impact |
|---|----------|---------|--------|
| 1 | **Art direction** | Painterly/watercolor (Spirit Island), dark gothic (Inscryption), clean vector (Balatro), warm parchment (tabletop feel) | Sets color palette, texture choices, card frame style |
> I would like to try maybe some of them. The dark Gothic sounds very good, but painterly/watercolor is also a great choice. The board should have a consistent colour palette, and the UI matcihng it; but cards and warden can have all unique colours. 
| 2 | **Card illustrations** | Individual card art (AI-generated?), abstract element patterns, icons-only, text-only with better frames | Determines card layout proportions and visual weight |
> Ideally individual card art but I have no artistic inclination. Maybe I'll try some Paint designs. But if we can try also text-only with better frames it might be good in-between.
| 3 | **Scope** | Full visual overhaul vs targeted polish of existing layout | Determines whether we restructure layout or just skin it |
> we can do a full overhaul if needed. UI hasn't had much thought behind. 
| 4 | **Platform** | Desktop only vs eventual mobile | Affects minimum touch target sizes and card scaling |
> Desktop for now. It could be a mobile game, and I think there is nothing impeding it; but we can optimise for that aafter.
| 5 | **Reference games** | Which card game UIs do you admire? | Fastest path to aligned art direction |
> Slay the spire is a master piece, but also Lonestar as a more simple UI (or even the UI  of Cobalt Core). Balatro is also a good example. Or even Pyrene as having soeting on the board.
| 6 | **Animation priority** | Animations first vs layout/readability first | Determines Phase E timing |
> I don't know what would it be. I see small animations when actions happen on territories, when cards moove, and maybe some invaders. But we can probably focus on readability first. 

**Default assumption if no answer:** Warm parchment/nature aesthetic (fits "ancient spirit defending the forest"), icons-only cards with better frames, full overhaul, desktop-only, layout-first.

---

## Current State (What We're Fixing)

### What Works
- All 20 view controllers render correct game state
- GameBridge event hub (50+ signals) cleanly separates Core from UI
- Fonts loaded (Cinzel Bold for headers, IM Fell English for body)
- Kenney board-game icons used for elements and passives
- Fear queue has a basic card-flip tween animation
- Territory custom `_Draw()` rendering is efficient

### What Doesn't Work
| Issue | Where | Severity |
|-------|-------|----------|
| **No theme** — hollow_wardens_theme.tres is 74 bytes (empty) | Every controller | Critical |
| **All positions hardcoded** — breaks on any resolution != 1920x1080 | All 20 controllers | Critical |
| **Cards are text-only** — `DamageInvaders ×4 r1` with no frames/art | CardViewController.cs | High |
| **Board is flat rectangles** — no terrain identity, no atmosphere | TerritoryViewController.cs | High |
| **Invaders are letter-squares** — M/N in 22px colored boxes | TerritoryViewController.cs | High |
| **Phase indicator broken** — VBoxContainer/CanvasLayer mismatch | PhaseIndicatorController.cs | High |
| **ASCII element bars** — `[····:··:···::]` | ElementTrackerController.cs | High |
| **ASCII dread bar** — `[######:------:------]` | DreadBarController.cs | Medium |
| **Tide Preview overlaps cards** — layout collision at mid-right | TidePreviewController.cs / HandDisplay | Medium |
| **No animations** — phase changes are instant text swaps | All controllers | Medium |
| **No visual hierarchy** — Weave (your health!) same weight as everything | All panels | Medium |
| **Passive panel cramped** — 9pt text in bottom-left corner | PassivePanelController.cs | Low |
| **Fonts loaded per controller** — redundant I/O, no caching | All controllers | Low |

---

## Phase A — Foundation (Theme + Layout)

**Goal:** Every panel looks intentional. Nothing is pixel-positioned. The game has a cohesive visual identity even before individual components are upgraded.

**Estimated effort:** 1–2 sessions

### A1. Populate hollow_wardens_theme.tres

Create a real Godot Theme resource with:

```
StyleBoxFlat for PanelContainer:
  - bg_color: Color(0.12, 0.11, 0.10, 0.92)  # near-black with slight warmth
  - corner_radius: 6px all
  - border_width: 1px all
  - border_color: Color(0.35, 0.30, 0.25, 0.6)  # muted warm border
  - content_margin: 8px all

StyleBoxFlat for Button (normal):
  - bg_color: Color(0.18, 0.16, 0.14, 0.9)
  - corner_radius: 4px
  - border: 1px Color(0.4, 0.35, 0.3)

StyleBoxFlat for Button (hover):
  - bg_color: Color(0.25, 0.22, 0.18, 0.95)
  - border: 1px Color(0.7, 0.6, 0.4)  # gold highlight

StyleBoxFlat for Button (pressed):
  - bg_color: Color(0.15, 0.13, 0.11)

Font overrides:
  - default_font: IMFellEnglish-Regular.ttf
  - default_font_size: 13
  - Label heading font: Cinzel-Bold.ttf at 16
  - Button font: Cinzel-Regular.ttf at 12

Color constants:
  - font_color: Color(0.85, 0.8, 0.72)          # warm off-white
  - font_hover_color: Color(1.0, 0.95, 0.8)     # bright warm
  - font_disabled_color: Color(0.45, 0.42, 0.38) # muted
```

**Why this first:** One file change, every PanelContainer/Button/Label in the entire game instantly looks better. The biggest bang-for-buck in the entire plan.

### A2. Create FontCache autoload

Replace per-controller font loading with a single autoload:

```csharp
// FontCache.cs — registered as autoload
public static class FontCache
{
    public static Font CinzelBold { get; private set; }
    public static Font CinzelRegular { get; private set; }
    public static Font IMFell { get; private set; }
    public static Font IMFellItalic { get; private set; }
    
    public static void Load() { /* GD.Load once */ }
}
```

Remove all per-controller `GD.Load<Font>()` calls. ~60 lines of duplicate code deleted across 15 controllers.

### A3. Fix screen layout with anchors

Replace hardcoded pixel offsets with a proper layout structure in Game.tscn:

```
Game (Node)
├── Background (ColorRect, full_rect, dark gradient)
├── UI (CanvasLayer)
│   ├── TopStrip (HBoxContainer, anchor top, h=50px)
│   │   ├── PhaseIndicator
│   │   └── WeaveBar
│   ├── LeftPanel (VBoxContainer, anchor left, w=180px)
│   │   ├── StatusInfo (draw/disc/dormant counts)
│   │   ├── FearQueue
│   │   └── PassivePanel
│   ├── CenterArea (MarginContainer, fills remaining)
│   │   └── BoardContainer (centered, territories)
│   ├── RightPanel (VBoxContainer, anchor right, w=300px)
│   │   ├── ElementTracker
│   │   ├── DreadBar
│   │   └── TidePreview
│   └── BottomDock (HBoxContainer, anchor bottom, h=180px)
│       └── HandDisplay
├── Overlays (CanvasLayer, layer 10)
│   ├── FearConfirm
│   ├── CounterAttack
│   ├── RestScreen
│   ├── RewardScreen
│   ├── EventScreen
│   └── UpgradeScreen
└── Debug (CanvasLayer, layer 100)
    ├── DevConsole
    └── DebugLog
```

**Key change:** Everything uses anchors and containers instead of `Position = new Vector2(940, 10)`. The game works at any resolution (or at minimum degrades gracefully).

### A4. Fix PhaseIndicator bug

Replace the current VBoxContainer approach. The PhaseIndicator should be a **HBoxContainer child** of the TopStrip, not a standalone VBoxContainer fighting with the CanvasLayer.

New design: horizontal phase strip showing `VIGIL | TIDE | DUSK` with the current phase highlighted (bright, enlarged) and others dimmed. Tide count displayed alongside.

### A5. Add background

Replace the current pitch-black background with a subtle dark gradient or muted texture. Even a simple `ColorRect` with a vertical gradient (dark green-black at top → dark brown-black at bottom) dramatically sets the "ancient forest" mood.

---

## Phase B — Card Visual Upgrade

**Goal:** Cards look like cards, not text dumps. The hand is scannable and interactive.

**Estimated effort:** 2–3 sessions

### B1. Card frame design

Create a card frame using `StyleBoxFlat` or a 9-patch PNG:

```
Card anatomy (revised from current 150x155):
┌─────────────────────┐  ← Card frame (160x200px)
│ ◆◆ Root, Mist   ⚡  │  ← Element icons + timing badge
│─────────────────────│
│    LIVING WALL      │  ← Name (Cinzel Bold 13pt, rarity color)
│─────────────────────│
│ TOP                 │  ← Section label (small caps, 9pt)
│ Place Presence ×3   │  ← Effect (IM Fell 11pt, colored values)
│ range 1             │  
│─────────────────────│
│ BOT                 │  ← Darker background tint
│ Damage Invaders ×1  │  ← IM Fell Italic 11pt
│ range 1             │
│─────────────────────│
│   [Play Top]        │  ← Buttons only during playable phase
└─────────────────────┘
```

**Rarity frame tints:**
- Dormant: desaturated, vine overlay pattern
- Awakened: subtle blue border glow
- Ancient: gold border with shimmer

**Timing badges** (replace `[FAST]`/`[SLOW]` text):
- FAST: blue lightning bolt icon (from Kenney) + blue tinted badge
- SLOW: amber hourglass icon + amber tinted badge

### B2. Effect text formatting

Replace `DamageInvaders ×4 r1` with human-readable colored text:

```
Current:  [SLOW] DamageInvaders ×4 r1
Proposed: ⚔ Deal 4 damage (range 1)
          ↑ red    ↑ white   ↑ gray
```

Color coding for effect values:
- **Red:** damage values
- **Green:** healing, corruption reduction, weave restore
- **Cyan:** presence placement
- **Purple:** fear generation
- **Gold:** awaken dormant

Use localization keys (already in strings.csv) for effect descriptions instead of raw type names.

### B3. Card hover interaction

```csharp
// On MouseEntered:
CreateTween().TweenProperty(this, "scale", new Vector2(1.15f, 1.15f), 0.1f);
CreateTween().TweenProperty(this, "position:y", Position.Y - 30f, 0.1f);
ZIndex = 10; // Render above siblings

// On MouseExited:
CreateTween().TweenProperty(this, "scale", Vector2.One, 0.08f);
CreateTween().TweenProperty(this, "position:y", originalY, 0.08f);
ZIndex = 0;
```

This single interaction is what makes Slay the Spire's hand feel responsive. Highest juice-to-effort ratio.

### B4. Hand scaling for large hands

```
Cards 1-5:  full size (160px), 8px gaps
Cards 6-8:  140px, overlap by 15px  
Cards 9-10: 120px, overlap by 25px, slight fan arc (2° per card from center)
```

### B5. Card play animations

- **Play top:** Card slides upward toward target territory (0.3s), white flash at destination, card fades to discard area
- **Play bottom:** Card cracks apart at the horizontal divider — top half fades, bottom half glows then dissolves into particles (communicates the sacrifice)
- **Draw:** Cards slide in from left edge with 0.15s ease-out and slight rotation snap
- **Dormant:** Card slowly desaturates with a subtle vine overlay creeping across it

---

## Phase C — Board Visual Upgrade

**Goal:** Territories feel like places, not spreadsheet cells. Units are recognizable at a glance.

**Estimated effort:** 2–3 sessions

### C1. Territory tile redesign

Replace flat colored rectangles with shaped panels:

```
Territory tile (140x100px, rounded rectangle):
┌──────────────────────┐
│ M1        🌲  ◆◆◆   │  ← ID + terrain icon + presence dots
│                      │
│  ⚔⚔  🛡   🍃🍃    │  ← Invader icons + native icons
│                      │
│ ████████░░░░ L1      │  ← Corruption bar + level
└──────────────────────┘
```

**Corruption-level backgrounds:**
- L0 (Clean): deep forest green, subtle leaf texture
- L1 (Tainted): sickly yellow-green, faint corruption veins
- L2 (Defiled): burnt orange, visible corruption cracks
- L3 (Desecrated): deep crimson, animated pulsing dark veins

**Terrain type icons** (from Kenney board-game icons):
- Forest: tree icon (green)
- Mountain: peak icon (gray)
- Wetland: water drop (blue)
- Sacred: star (gold)
- Scorched: fire (orange)
- Blighted: skull (purple)

### C2. Unit token redesign

Replace 22px letter-squares with recognizable tokens:

**Invaders (warm red family):**
- Marcher: sword icon, red-brown bg, HP bar underneath
- Ironclad: shield icon, purple bg, HP bar
- Outrider: boot icon, orange bg, HP bar
- Pioneer: hammer icon, tan bg, HP bar

**Natives (cool green family):**
- Leaf/village icon, forest-green bg, HP bar

**HP bars:** 2px tall, color transitions from green → yellow → red as HP drops

**Presence tokens:**
- Small glowing circles in warden color (green for Root, orange for Ember)
- 1-3: show individual circles
- 4+: single circle with count number

### C3. Adjacency lines

Draw thin connection lines between adjacent territories (faded gray, 1px). During targeting mode:
- Valid targets: pulsing yellow border, slight brightness increase
- Invalid targets: darken to 40% opacity
- Lines to valid targets: brightened

### C4. Floating combat numbers

Enhance existing floating text feedback:
- **Damage:** Red numbers, rise + fade, brief scale-up pop (0.1s up to 1.3x, then 0.2s back to 1.0x, then 0.3s fade)
- **Healing/cleanse:** Green numbers
- **Fear generated:** Purple wisps floating toward fear counter
- **Presence placed:** Cyan flash at territory

---

## Phase D — HUD Panel Upgrades

**Goal:** Information panels communicate at a glance instead of requiring text parsing.

**Estimated effort:** 2 sessions

### D1. Element tracker — graphical bars

Replace ASCII `[····:··:···::]` with visual fill bars:

```
Each element row (280px wide):
[Icon 20px] [Name 48px] [═══════|════|════════] [T1] [T2] [T3]
                         ↑filled   ↑4   ↑7  ↑11
```

- Fill bar: 120px wide, 13 segments
- Filled segments: element's color (bright)
- Empty segments: dark gray
- Threshold notches: visible vertical lines at positions 4, 7, 11
- When threshold reached: bar segment glows, T button pulses gold (keep existing behavior)
- Pending threshold: tooltip on T button shows effect description

### D2. Weave bar — segmented health bar

Replace `Weave: 20/20` text with a visual health bar:

```
[████████████████████] 20/20
 ↑ blue segments when full
[████████████░░░░░░░░] 12/20
 ↑ blue filled, dark gray empty
[████░░░░░░░░░░░░░░░░]  4/20
 ↑ red + pulse when ≤5
```

- 20 segments, each 12px wide = 240px total
- Smooth color transition: blue → yellow (at 50%) → red (at 25%)
- Damage: brief flash white → settle to new color
- Below 5: pulse animation (opacity oscillation)
- At 0 (breach): dramatic red border around entire screen

### D3. Dread display — skull icons

Replace `Dread Level: 1` text with visual skulls:

```
💀  💀  💀  💀     Fear: 7/15 [████████░░░░░░░]
↑lit ↑dim ↑dim ↑dim
```

- 4 skull icons from Kenney
- Lit skulls: orange glow
- Unlit: dark gray
- Fear progress: thin bar showing progress to next 15-threshold
- On dread advance: brief screen darken + skull ignite animation

### D4. Tide preview — mini card frames

Replace text-only preview with small card-style frames:

```
── TIDE PREVIEW ──
   Tide 3/6

  ┌─────────┐
  │ ▶ Ravage│  ← red border (Painful)
  │ Dmg + 2C│
  └─────────┘
  Next:
  ┌─────────┐
  │  Settle │  ← green border (Easy)
  │ Shield 1│
  └─────────┘
```

60x84px mini card frames with colored borders (red for Painful pool, green for Easy pool).

### D5. Phase indicator — horizontal strip

Replace broken VBoxContainer with a top-of-screen phase strip:

```
┌───────────────────────────────────────────────────┐
│    VIGIL        │      TIDE 3/6     │    DUSK     │
│   (dimmed)      │   ◀ ACTIVE ▶      │  (dimmed)   │
└───────────────────────────────────────────────────┘
```

- Current phase: bright color, larger text, underline
- Other phases: 50% opacity, smaller
- Phase transition: highlight slides with 0.3s tween
- Contextual hint below: "Play up to 2 tops" / "Invaders acting..." / "Play 1 bottom"

---

## Phase E — Animation & Juice

**Goal:** The game feels alive. Every state change has feedback. Playing cards is satisfying.

**Estimated effort:** 2–3 sessions

### E1. Phase transitions

- **Vigil start:** Brief full-screen blue tint flash (0.3s fade-in → 0.5s hold → 0.3s fade-out)
- **Tide:** Screen edges darken to red. Red line sweeps across board. Tide action text appears center-screen briefly (0.8s)
- **Dusk:** Amber/sunset tint at screen edges

### E2. Screen shake

When Weave takes damage:
```csharp
var tween = CreateTween();
for (int i = 0; i < 3; i++)
{
    var offset = new Vector2(GD.Randf() * 4 - 2, GD.Randf() * 4 - 2);
    tween.TweenProperty(rootNode, "position", offset, 0.05f);
}
tween.TweenProperty(rootNode, "position", Vector2.Zero, 0.1f);
```
3 oscillations, 2-4px amplitude, 0.25s total. **The single most impactful juice effect.**

### E3. Number animations

Anywhere a number changes, don't snap — animate:
```csharp
// Instead of: label.Text = newValue.ToString();
// Do: AnimateNumber(label, oldValue, newValue, 0.3f);
void AnimateNumber(Label label, int from, int to, float duration)
{
    var tween = CreateTween();
    tween.TweenMethod(Callable.From<int>(v => label.Text = v.ToString()), 
                       from, to, duration);
}
```

### E4. Fear generation particles

Purple wisps float upward from the source territory → drift toward Fear counter → counter number ticks up on arrival. Simple `CPUParticles2D` with purple color, upward velocity, fade-out.

### E5. Element threshold flash

When element count crosses a threshold:
- Entire element row flashes golden (0.2s)
- A brief radial glow pulse behind the T button
- Subtle screen-edge glow in the element's color

### E6. Card state transitions

- **Draw from pile:** Slide from left with bounce easing
- **Discard:** Slide right with decelerate easing
- **Rest shuffle:** All discard cards briefly appear, swirl, then vanish into draw pile area

---

## Implementation Priority Matrix

| Phase | Visual Impact | Effort | Dependencies | Ship Alone? |
|-------|--------------|--------|--------------|-------------|
| **A: Foundation** | ★★★★★ | Low | None | Yes |
| **B: Cards** | ★★★★★ | Medium | Phase A | Yes |
| **C: Board** | ★★★★☆ | Medium | Phase A | Yes |
| **D: HUD** | ★★★☆☆ | Medium | Phase A | Yes |
| **E: Animations** | ★★★★☆ | Medium | Phases A-D | Yes (incremental) |

**Recommended order:** A → B → C → D → E (but B and C can run in parallel)

---

## Asset Requirements

### Need to Create
| Asset | Format | Used By | Notes |
|-------|--------|---------|-------|
| card_frame.png | 9-patch PNG | CardViewController | Card border, 3 rarity variants |
| card_back.png | PNG | FearActionQueue | Face-down card in fear queue |
| territory_bg_L0-L3.png | PNG or shader | TerritoryViewController | 4 corruption-level backgrounds |
| phase_strip_bg.png | 9-patch PNG | PhaseIndicator | Horizontal strip background |
| bg_gradient.png | PNG | Background | Dark forest gradient |
| timing_fast.png | 16x16 PNG | CardViewController | Lightning bolt badge |
| timing_slow.png | 16x16 PNG | CardViewController | Hourglass badge |

### Already Have (Kenney Packs)
| Asset Pack | Count | Used For |
|------------|-------|----------|
| kenney_ui-pack-rpg-expansion | 90 | Panels, buttons, progress bars, cursors |
| kenney_board-game-icons | 513 | Element icons, terrain icons, unit icons, passive icons |
| kenney_game-icons | 425 | Directional arrows, game control icons |
| kenney_playing-cards-pack | 285 | Card frame references, suit/value graphics |

### Kenney Icon Mapping (Proposed)

**Elements:**
- Root → tree / resource_wood (already used)
- Mist → flask_half (already used)
- Shadow → skull (already used)  
- Ash → fire (already used)
- Gale → arrow_right (already used)
- Void → hexagon_outline (already used)

**Invader Unit Types:**
- Marcher → sword_long or axe
- Ironclad → shield_bronze
- Outrider → boot or horseshoe
- Pioneer → hammer or pickaxe

**Terrain Types:**
- Forest → tree_pine
- Mountain → mountain or gem
- Wetland → potion_blue or water_drop
- Sacred → star_gold or sun
- Scorched → fire_small
- Blighted → skull_crossbones

**UI Controls:**
- Play Top button → arrow_up (from game-icons)
- Play Bottom button → arrow_down
- Confirm → check_circle (green)
- Cancel → cross_circle (red)

---

## Files Affected Per Phase

### Phase A (Foundation)
```
MODIFY: hollow_wardens/godot/assets/hollow_wardens_theme.tres
CREATE: hollow_wardens/godot/autoloads/FontCache.cs
MODIFY: hollow_wardens/godot/scenes/Game.tscn (layout restructure)
MODIFY: hollow_wardens/godot/views/PhaseIndicatorController.cs (rewrite)
MODIFY: hollow_wardens/project.godot (register FontCache autoload)
MODIFY: All 15 view controllers (remove per-controller font loading)
```

### Phase B (Cards)
```
MODIFY: hollow_wardens/godot/views/CardViewController.cs (major rewrite)
MODIFY: hollow_wardens/godot/views/HandDisplayController.cs (scaling logic)
MODIFY: data/localization/strings.csv (effect description keys)
CREATE: hollow_wardens/godot/assets/art/card_frame_dormant.png (or StyleBox)
CREATE: hollow_wardens/godot/assets/art/card_frame_awakened.png
CREATE: hollow_wardens/godot/assets/art/card_frame_ancient.png
CREATE: hollow_wardens/godot/assets/art/timing_fast.png
CREATE: hollow_wardens/godot/assets/art/timing_slow.png
```

### Phase C (Board)
```
MODIFY: hollow_wardens/godot/views/TerritoryViewController.cs (major rewrite)
CREATE: hollow_wardens/godot/assets/art/territory_L0.png (or gradient shader)
CREATE: hollow_wardens/godot/assets/art/territory_L1.png
CREATE: hollow_wardens/godot/assets/art/territory_L2.png
CREATE: hollow_wardens/godot/assets/art/territory_L3.png
```

### Phase D (HUD)
```
MODIFY: hollow_wardens/godot/views/ElementTrackerController.cs (bar rewrite)
MODIFY: hollow_wardens/godot/views/DreadBarController.cs (visual bars)
MODIFY: hollow_wardens/godot/views/TidePreviewController.cs (mini cards)
MODIFY: hollow_wardens/godot/views/PassivePanelController.cs (polish)
```

### Phase E (Animations)
```
CREATE: hollow_wardens/godot/views/JuiceManager.cs (screen shake, flashes)
MODIFY: hollow_wardens/godot/bridge/GameBridge.cs (emit animation signals)
MODIFY: hollow_wardens/godot/views/CardViewController.cs (play animations)
MODIFY: hollow_wardens/godot/views/TerritoryViewController.cs (combat FX)
MODIFY: hollow_wardens/godot/views/DreadBarController.cs (number anims)
```

---

## Color Palette (Proposed)

A consistent color language across all panels:

| Purpose | Color | Hex | Usage |
|---------|-------|-----|-------|
| **Player positive** | Forest green | `#4CAF50` | Presence, natives, weave heal, cleanse |
| **Threat** | Warm red | `#E53935` | Invaders, corruption, damage |
| **Resources** | Soft blue | `#42A5F5` | Elements, draw pile, phase indicator |
| **Fear/Dread** | Deep purple | `#7E57C2` | Fear generation, dread, fear queue |
| **Actionable** | Gold | `#FFB300` | Thresholds, rewards, clickable highlights |
| **Inactive** | Muted gray | `#616161` | Disabled, locked, unavailable |
| **Background** | Near-black warm | `#1E1B18` | Base UI background |
| **Panel bg** | Dark warm | `#2A2520` | Panel interiors |
| **Border** | Muted warm | `#5C524A` | Panel borders |
| **Text primary** | Warm off-white | `#D9CDB8` | Default text |
| **Text secondary** | Muted tan | `#8C8070` | Descriptions, hints |

---

## Comparison: Before → After

### Cards
| Before | After |
|--------|-------|
| 150x155 PanelContainer, default style | 160x200 framed card with rarity border |
| `DamageInvaders ×4 r1` | `⚔ Deal 4 damage (range 1)` with colored values |
| `[FAST]` / `[SLOW]` text | ⚡ / ⏳ icon badges |
| Static row, no hover | Hover lifts + scales, hand fans for 6+ cards |
| Instant appear/disappear | Slide-in draw, slide-up play, shatter dissolve |

### Board
| Before | After |
|--------|-------|
| Green/dark rectangles | Shaped tiles with corruption-gradient backgrounds |
| `3 inv` text | 3 unit icons with type silhouettes + HP bars |
| `0 pres` text | Glowing circles in warden color |
| No terrain visual | Terrain icon badge (tree/mountain/water) |
| No adjacency lines | Faded gray connection lines, bright on targeting |

### HUD
| Before | After |
|--------|-------|
| `[····:··:···::]` ASCII bars | Colored fill bars with threshold notches |
| `Weave: 20/20` text | 20-segment health bar with color transitions |
| `Dread Level: 1` text | 4 skulls (lit/unlit) + fear progress bar |
| Text-only tide preview | Mini card frames with colored borders |
| Broken phase indicator | Horizontal strip with animated phase highlight |

---

## Validation Approach

After each phase, run the existing smoke test pipeline:

```bash
# User runs:
"D:\Downloads\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe" \
  --path "D:\Workspace\hollow\hollow_wardens" -- --run-ui-tests

# Claude reads screenshots from ui-test-screenshots/ and reviews
```

New screenshots should be added for each visual milestone. Suggested additions to UISmokeTest.cs:

```csharp
// Phase A validation
Screenshot("a_theme_applied_board");
Screenshot("a_phase_strip_vigil");

// Phase B validation  
Screenshot("b_card_hover");
Screenshot("b_hand_10_cards");
Screenshot("b_card_dormant_state");

// Phase C validation
Screenshot("c_territory_L0_clean");
Screenshot("c_territory_L2_defiled");
Screenshot("c_invader_tokens");

// Phase D validation
Screenshot("d_element_bars");
Screenshot("d_weave_bar_full");
Screenshot("d_weave_bar_critical");
```

---

## Research Sources

This plan was informed by analysis of:
- **Slay the Spire** — Card layout, hand fan, hover interaction, energy orb, intent icons
- **Spirit Island (digital)** — Territory density management, element tracking, fear deck reveals
- **Monster Train** — Multi-lane board, damage prediction on drag, unit visualization
- **Balatro** — Low-budget juice (screen shake, number counting, card flip + shimmer)
- **Inscryption** — Atmosphere through minimalism, physical-table metaphor
- **Kenney Asset Packs** — Available icons and UI elements already in the project
- **Card Game UI Framework** (ycarowr/UiCard) — Hand scaling math, fan arc angles
- **Godot 4 best practices** — Theme resources, tween-based animation, StyleBoxFlat
