# Hollow Wardens — Execution Plan + Prompts

## Current Status

| Stream | Status | Tests |
|--------|--------|-------|
| D28 Amplification/Vulnerability/Sacrifice | ✅ Merged | 263 |
| D29 Root Combat Toolkit | ✅ Merged | 286 |
| Warden Data Migration | ✅ Merged | 286 |
| Passive Panel UI | ✅ Merged | — |
| Visual UI Pass | ✅ Merged | — |

## What's Next (in order)

### 1. PLAYTEST FIX PASS — do this NOW

Fixes crashes (broken asset paths), stuck UI (fear actions), and adds key UX
(amplification feedback, threshold tooltips, log export, etc).

**File:** `PLAYTEST_FIX_SPEC.md` (drop in project root)

**Prompt:**
```
Read PLAYTEST_FIX_SPEC.md in the project root and apply all changes. Read each
referenced file before modifying it. Fix bugs first (Parts A-C), then UX
improvements (Parts D-H). After all changes, run dotnet test
src/HollowWardens.Tests/ — all existing tests must pass. Verify the project
compiles for Godot. When done and all tests pass, delete PLAYTEST_FIX_SPEC.md.
```

### 2. SIMULATION HARNESS — after playtest fixes

Bot strategy + stats collector that runs N encounters and dumps balance data.
Tests D28/D29 balance questions. (Spec not yet written — will write after fix
pass results come in.)

### 3. REPLAY WIRING — after simulation harness

Wire up the existing ImportAndReplay stub so exported seed strings can be
replayed step-by-step for analysis.

### 4. PASSIVE PROGRESSION (D30) — design session

Start Root with 2-3 passives, unlock the rest through encounters/events.
Needs design discussion before spec.

---

## Reference: Previously completed prompts

These are already done but included for reference in case you need to re-run.

### D29 Root Combat Toolkit (DONE)

**File was:** `D29_SPEC.md`

**Prompt was:**
```
Read D29_SPEC.md in the project root and apply all changes sequentially. Read
each referenced source file before modifying it. For MODIFY steps, make only
the described changes. For NEW FILE steps, create the file at the specified
path. Mark all new/changed code with // D29: comments. Implementation order:
Part A → B → C → D. After all changes, run dotnet test
src/HollowWardens.Tests/ — all existing + new tests must pass. If any existing
test breaks, fix the issue while preserving the original test's intent. When
done and all tests pass, delete D29_SPEC.md.
```

### Warden Data Migration (DONE)

**File was:** `WARDEN_MIGRATION_SPEC.md`

**Prompt was:**
```
Read WARDEN_MIGRATION_SPEC.md in the project root and apply all changes
sequentially. Read each referenced source file before modifying it. For MODIFY
steps, make only the described changes. For NEW FILE steps, create the file at
the specified path. Implementation order: Parts A through H. After all changes,
run dotnet test src/HollowWardens.Tests/ — all existing + new tests must pass.
Verify there are no compilation errors in the Godot bridge files. When done and
all tests pass, delete WARDEN_MIGRATION_SPEC.md.
```

### Passive Panel UI (DONE)

**File was:** `PASSIVE_PANEL_SPEC.md`

**Prompt was:**
```
Read PASSIVE_PANEL_SPEC.md in the project root and apply all changes. This is a
Godot UI task. Read each referenced file before modifying it. Verify the project
compiles after changes. When done, delete PASSIVE_PANEL_SPEC.md.
```

### Visual UI Pass (DONE)

**File was:** `VISUAL_UI_PASS_SPEC.md`

**Prompt was:**
```
Read VISUAL_UI_PASS_SPEC.md in the project root and apply all changes. This is
visual polish — no Core logic changes. Focus on territories and cards first. If
any change is too complex or breaks layout, skip it with a // TODO: visual
upgrade comment. Verify the project compiles. When done, delete
VISUAL_UI_PASS_SPEC.md.
```
