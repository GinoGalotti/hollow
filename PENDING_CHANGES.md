# Hollow Wardens — Pending Changes Tracker

Last updated: Localization live (432 tests). Sequential plan prepared.

---

## ACTIVE

### SEQUENTIAL_PLAN.md ← drop in project root, fire with prompt inside
Contains 5 tasks in order:
1. UI Launch Fix (null guards + WardenSelect type fix + EncounterReady signal)
2. Targeting Fix (card effects enter targeting mode with 2+ valid territories)
3. Encounter Variety (5 encounters + CLI + Godot selector + tests)
4. Board Carryover (extract/apply/SimProfile support)
5. SIM_REFERENCE.md update

### Validate Ember Card Nerf ← manual sim run, do anytime
```
dotnet run --project src/HollowWardens.Sim/ -- --seeds 1-500 --warden ember --verbose --output sim-results/ember-card-nerf/
```
Paste summary.txt to balance buddy.

### DOC_SYNC_V2_SPEC.md ← fire after sequential plan completes
Comprehensive doc update for all three design docs.

---

## NOT YET SPECCED (future)

- Encounter design levers (20 levers — Phase 3-4 of encounter variety)
- Board layouts (wide/narrow/twin_peaks — Phase 5 of encounter variety)
- Bulk localization migration (~95 remaining strings)
- Upgrade system design
- Run structure design
- Bot skill levels
- Third warden (Veil)

---

## COMPLETED

| Item | Tests |
|------|-------|
| All D28-D29 + Migration + UI + Bugfixes | 312 |
| Test Coverage + Root Tightening | 369 |
| Ember Warden + Balance Patch | 400+ |
| SimProfile + Seeds + Verbose + Combos | 415 |
| Threshold Config (per-element) | 415 |
| Localization Infrastructure | 432 |
