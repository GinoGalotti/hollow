# Hollow Wardens — Pending Changes & Planning Tracker

Last updated: Ember live (384 tests), SimProfile running. 
Sim: Root 94.4% Clean | Ember 100% Weathered (pre-balance-patch).

---

## EXECUTION ORDER

### 1. SIM_PROFILE_SPEC.md ← RUNNING NOW
BalanceConfig centralization + JSON-driven sim profiles + A/B testing.
**Status:** In progress.

### 2. EMBER_BALANCE_SPEC.md ← fire after SimProfile merges
Corruption sweet spot: L3 blocks Ember (not L2), smart cleanse at Resolution,
Controlled Burn passive, bot sweet-spot management.
**Prompt:**
```
Read EMBER_BALANCE_SPEC.md in the project root and apply all changes
sequentially. Read each referenced file before modifying it. After all changes,
run dotnet test src/HollowWardens.Tests/ — fix any broken tests while
preserving their intent. Then run dotnet run --project src/HollowWardens.Sim/
-- --runs 500 --seed 42 --warden ember --output sim-results/ember-patched/
and paste the full output. When done and all tests pass, delete
EMBER_BALANCE_SPEC.md.
```

### 3. Sim Seed Refactor ← fire after SimProfile merges
Replace --runs N --seed S with --seeds range. Each seed = one deterministic encounter.
**Prompt:**
```
In src/HollowWardens.Sim/Program.cs, refactor the seed/runs model:
1. Replace --runs N --seed S with --seeds which accepts a range or list.
   Examples: --seeds 42-541 runs 500 encounters (one per seed).
   --seeds 42,100,200 runs 3 specific encounters. --seed 42 (singular, kept
   for backward compat) runs one encounter.
2. When no seed flag is provided, default to --seeds 1-500.
3. Each seed produces exactly one deterministic encounter. The summary
   aggregates across all seeds. Per-seed detail goes into the CSV output.
4. The summary header should show: === HOLLOW WARDENS SIMULATION — 500
   encounters (seeds 42-541) ===
5. Keep --warden, --output, and --profile flags working as before. In
   SimProfile JSON, replace "runs" and "seed" with "seeds": "1-500".
6. Update the example profile JSONs to use the new seeds format.
Run dotnet test after to verify no breaks. Then run dotnet run --project
src/HollowWardens.Sim/ -- --seeds 1-500 --warden root and paste the output.
```

### 4. Verbose Sim Logging ← fire after seed refactor (conflicts with Program.cs)
Detailed turn-by-turn logs showing bot decisions, board state, and reasoning.
**Prompt:**
```
Add a verbose logging mode to the simulation. Changes:

1. Add --verbose flag to src/HollowWardens.Sim/Program.cs. When set, writes a
   detailed turn-by-turn log for EACH encounter to
   {output}/logs/encounter_{seed}.txt.

2. Create src/HollowWardens.Sim/VerboseLogger.cs that subscribes to GameEvents
   and the bot's decisions. For each turn, log:
   - Phase and tide number
   - Board state snapshot: for each territory, list presence count, corruption
     points/level, alive invaders (type + HP), alive natives
   - Hand contents: each card's ID, name, elements, top effect, bottom effect,
     dormant status
   - Bot decision: which card was chosen, which priority rule matched, what
     alternatives existed
   - Effect resolution: what happened (e.g. DamageInvaders x2 + 2 presence =
     4 damage on M1, killed Marcher)
   - Tide events: action card, invader actions, counter-attacks, advances,
     arrivals
   - Fear: generated, queued, resolved
   - Passive unlocks: which passive, what triggered it

3. In BotStrategy.cs (and EmberBotStrategy.cs), add a
   public string LastDecisionReason property that's set whenever the bot makes
   a choice. Format: "PRIORITY: {rule} — {context}". Examples:
   - "PRIORITY: presence_expansion — only 2 presence territories, need 3+"
   - "PRIORITY: damage — M1 has 3 invaders (most), playing Grasping Roots"
   - "PRIORITY: cleanse — A1 at 7 corruption (approaching Defiled)"
   - "PRIORITY: fear — no urgent threats, generating fear with Shiver"
   - "SKIP: all cards dormant, no playable options"

4. When --verbose is combined with a small seed range (e.g. --seeds 42-46
   --verbose), it produces 5 detailed log files. When used with large ranges,
   only log the first 5 encounters plus any Breach encounters.

Run dotnet test after. Then run dotnet run --project src/HollowWardens.Sim/
-- --seeds 42-44 --warden root --verbose --output sim-results/verbose-root/
and paste the FIRST encounter log file contents.
```

### 5. Combo Testing Scripts ← fire after verbose logging
Card/passive combination sweeps + balance knob sweeps.
**Prompt:**
```
Create a combo testing toolkit in sim-profiles/scripts/:

1. sim-profiles/scripts/combo-cards.sh — iterates through all pairs of draft
   cards for a given warden, runs a sim for each combo, captures summary. Prints
   sorted table: card1, card2, clean%, weathered%, breach%, avg_weave. Flags
   any combo where breach% > 10 or clean% < 70.
   Usage: ./combo-cards.sh root 100 42

2. sim-profiles/scripts/combo-passives.sh — tests all combinations of
   force-unlocking 1-2 passives at start.
   Usage: ./combo-passives.sh root 100 42

3. sim-profiles/scripts/sweep-balance.sh — sweeps a single BalanceConfig value
   across a range. Usage: ./sweep-balance.sh root max_presence_per_territory
   1 5 200 42

4. sim-profiles/scripts/compare.sh — side-by-side diff of two sim-results
   directories, highlights metrics changed by >5%.
   Usage: ./compare.sh sim-results/variant-a/ sim-results/variant-b/

All scripts should use --profile and --output flags. Create .ps1 PowerShell
versions for Windows. When done, run combo-cards.sh for root with 50 runs
to verify it works.
```

### 6. DOC_SYNC_SPEC.md ← fire last
Updates all design docs for everything.
**Prompt:**
```
Read DOC_SYNC_SPEC.md in the project root and apply all documentation updates.
Read each target document AND the referenced source files before modifying docs
— verify accuracy by reading the actual code. Do not change any code files.
When done, delete DOC_SYNC_SPEC.md.
```

---

## EXECUTION MAP

```
SIM_PROFILE (running) ──→ Ember Balance ──→ Seed Refactor ──→ Verbose Logging ──→ Combo Scripts ──→ Doc Sync
                              │                    │
                              │                    └── conflicts on Program.cs, must be sequential
                              └── conflicts on EmberAbility.cs, must be after SimProfile
```

Items 2 + 3 can run in parallel (different files).
Items 3 + 4 CANNOT parallel (both touch Program.cs + BotStrategy.cs).
Item 5 depends on 3 + 4 (needs --seeds and --verbose to exist).

---

## DESIGN QUEUE (conversation sessions, not code specs)

### Upgrade System Design
**When:** After SimProfile + Verbose Logging are live (so we can test designs immediately).
**Questions to answer:**
- What does a card upgrade look like? (+1 value? New secondary? Element change?)
- How many upgrade choices per encounter reward?
- Can passives upgrade? (Network Fear: 1→2 fear/pair)
- Upgrade paths: linear (Bronze→Silver→Gold) or branching (choose between two upgrades)?
- Do upgrades persist across the full run?

### Run Structure Design
**When:** After upgrade system is designed.
**Questions to answer:**
- How many encounters per run? (3 acts × 3 encounters = 9?)
- What happens between encounters? (Draft 1 card, upgrade 1 card, 1 event?)
- How does difficulty escalate? (More tides? Tougher waves? Corruption carryover?)
- Map structure: linear path or branching (Slay the Spire style)?

### Bot Skill Levels
**When:** After verbose logging reveals bot behavior patterns.
**Tiers:**
- Beginner: random card selection, no targeting priority
- Average: follows priority list but doesn't plan ahead
- Expert: current bot (optimized priorities)
- Oracle: looks 1 turn ahead (would require search — probably too complex)

Running balance sims at Average level gives more realistic numbers for typical
players. Expert level shows the ceiling.

### Third Warden (Veil)
**When:** After Root + Ember are balanced.
Mist/Shadow/Void. Stealth/evasion identity.

---

## COMPLETED

| Item | Tests |
|------|-------|
| D28 Amplification/Vulnerability/Sacrifice | 263 |
| D29 Root Combat Toolkit | 286 |
| Warden Data Migration | 286 |
| Passive Panel + Visual UI Pass | — |
| Playtest Fix Pass | 297 |
| Critical Bugfix (weave/fear/network fear) | 312 |
| Sim Harness + Replay | 312 |
| T1 Threshold Targeting | 312 |
| Balance Tuning Pass | 312 |
| Sim CSV Output | 312 |
| Card Icons + Phase Indicator Fix | — |
| Test Coverage Remediation | 355 |
| Root Tightening (passive gating + fear cap) | 369 |
| Ember Warden (base implementation) | 384 |

### Sim History
```
Pre-balance:     100% Clean, weave 20/20, fear 62.8, presence 25.7
Post-balance:    93.2% Clean, 6.4% Weathered, 0.4% Breach, fear 62.9
Post-Root-fix:   94.4% Clean, 5.0% Weathered, 0.6% Breach, fear 39.6
Ember baseline:  0% Clean, 100% Weathered, 0% Breach (by design — pre-patch)
```
