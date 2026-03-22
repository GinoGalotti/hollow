# Hollow Wardens — Execution Plan

## Dependency Graph

```
               D28 ✅ DONE (263 tests)
                    │
       ┌────────────┴────────────┐
       ▼                         ▼
  Stream A: D29            Stream B: Warden
  Root Combat Toolkit      Data Migration
  01_D29_SPEC.md           02_WARDEN_MIGRATION_SPEC.md
  (Core layer only)        (Data + GameBridge)
       │                         │
       │    ZERO FILE OVERLAP    │
       │    RUN IN PARALLEL      │
       ▼                         ▼
  ┌────┴─────────────────────────┴────┐
  │     Merge both → full test suite  │
  └──────────────┬────────────────────┘
                 ▼
           Stream C: Passive Panel
           03_PASSIVE_PANEL_SPEC.md
           (needs WardenData from B)
                 │
                 ▼
           Stream D: Visual UI Pass
           04_VISUAL_UI_PASS_SPEC.md
           (needs Passive Panel from C)
```

## Specs Ready

| # | Spec | File | Prereqs | Parallel? |
|---|------|------|---------|-----------|
| 1 | D29 Root Combat | 01_D29_SPEC.md | D28 ✅ | With #2 |
| 2 | Warden Migration | 02_WARDEN_MIGRATION_SPEC.md | None | With #1 |
| 3 | Passive Panel UI | 03_PASSIVE_PANEL_SPEC.md | #2 merged | Sequential |
| 4 | Visual UI Pass | 04_VISUAL_UI_PASS_SPEC.md | #1-3 merged | Sequential |

## How to run each spec

For each spec, open Claude Code and paste:

1. The **Claude Code Prompt** from the top of the spec document
2. Then say: **"Here is the full spec:"** and paste the entire document

Claude Code will read the referenced files, implement changes, and run tests.

## Step-by-step

**Step 1: Launch two parallel sessions**
- Session A: paste `01_D29_SPEC.md`
- Session B: paste `02_WARDEN_MIGRATION_SPEC.md`

**Step 2: After both complete, merge and verify**
```bash
dotnet test src/HollowWardens.Tests/
```
Should be ~285+ tests (263 existing + ~22 new from D29 + ~10 from migration).

**Step 3: Launch Session C**
- Paste `03_PASSIVE_PANEL_SPEC.md`
- Run game in Godot to verify passive panel renders

**Step 4: Launch Session D**
- Paste `04_VISUAL_UI_PASS_SPEC.md`
- This is the riskiest — visual changes can break layouts
- Run game after each major change

## Key gotcha from D28

Claude Code found two issues with the D28 spec:
1. `ActionLog.Log()` doesn't exist → removed (no test covers it)
2. `Invader.Type` → should be `Invader.UnitType`

Both are already correct in the D29 spec. But generally: Claude Code adapts
well to small mismatches between spec and actual code — it reads the files
and adjusts. The specs are architecture guides, not literal copy-paste.
