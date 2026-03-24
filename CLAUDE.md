# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## AI Guidance

* Ignore GEMINI.md and GEMINI-*.md files
* To save main context space, for code searches, inspections, troubleshooting or analysis, use code-searcher subagent where appropriate - giving the subagent full context background for the task(s) you assign it.
* ALWAYS read and understand relevant files before proposing code edits. Do not speculate about code you have not inspected. If the user references a specific file/path, you MUST open and inspect it before explaining or proposing fixes. Be rigorous and persistent in searching code for key facts. Thoroughly review the style, conventions, and abstractions of the codebase before implementing new features or abstractions.
* After receiving tool results, carefully reflect on their quality and determine optimal next steps before proceeding. Use your thinking to plan and iterate based on this new information, and then take the best next action.
* After completing a task that involves tool use, provide a quick summary of what you've done.
* For maximum efficiency, whenever you need to perform multiple independent operations, invoke all relevant tools simultaneously rather than sequentially.
* Before you finish, please verify your solution
* Do what has been asked; nothing more, nothing less.
* NEVER create files unless they're absolutely necessary for achieving your goal.
* ALWAYS prefer editing an existing file to creating a new one.
* NEVER proactively create documentation files (*.md) or README files. Only create documentation files if explicitly requested by the User.
* If you create any temporary new files, scripts, or helper files for iteration, clean up these files by removing them at the end of the task.
* When you update or modify core context files, also update markdown documentation and memory bank
* When asked to commit changes, exclude CLAUDE.md and CLAUDE-*.md referenced memory bank system files from any commits. Never delete these files.

<investigate_before_answering>
Never speculate about code you have not opened. If the user references a specific file, you MUST read the file before answering. Make sure to investigate and read relevant files BEFORE answering questions about the codebase. Never make any claims about code before investigating unless you are certain of the correct answer - give grounded and hallucination-free answers.
</investigate_before_answering>

<do_not_act_before_instructions>
Do not jump into implementatation or changes files unless clearly instructed to make changes. When the user's intent is ambiguous, default to providing information, doing research, and providing recommendations rather than taking action. Only proceed with edits, modifications, or implementations when the user explicitly requests them.
</do_not_act_before_instructions>

<use_parallel_tool_calls>
If you intend to call multiple tools and there are no dependencies between the tool calls, make all of the independent tool calls in parallel. Prioritize calling tools simultaneously whenever the actions can be done in parallel rather than sequentially. For example, when reading 3 files, run 3 tool calls in parallel to read all 3 files into context at the same time. Maximize use of parallel tool calls where possible to increase speed and efficiency. However, if some tool calls depend on previous calls to inform dependent values like the parameters, do NOT call these tools in parallel and instead call them sequentially. Never use placeholders or guess missing parameters in tool calls.
</use_parallel_tool_calls>

## Memory Bank System

This project uses a structured memory bank system with specialized context files. Always check these files for relevant information before starting work:

### Core Context Files

* **CLAUDE-activeContext.md** - Current session state, goals, and progress (if exists)
* **CLAUDE-patterns.md** - Established code patterns and conventions (if exists)
* **CLAUDE-decisions.md** - Architecture decisions and rationale (if exists)
* **CLAUDE-balance.md** - Balance decisions and sim evidence (if exists)
* **CLAUDE-troubleshooting.md** - Common issues and proven solutions (if exists)
* **CLAUDE-config-variables.md** - Configuration variables reference (if exists)
* **CLAUDE-temp.md** - Temporary scratch pad (only read when referenced)

**Important:** Always reference the active context file first to understand what's currently being worked on and maintain session continuity.

### Memory Bank System Backups

When asked to backup Memory Bank System files, you will copy the core context files above and @.claude settings directory to directory @/path/to/backup-directory. If files already exist in the backup directory, you will overwrite them.

## Claude Code Official Documentation

When working on Claude Code features (hooks, skills, subagents, MCP servers, etc.), use the `claude-docs-consultant` skill to selectively fetch official documentation from docs.claude.com.

## Project Overview

This repo is the working directory for **Hollow Wardens** — a card roguelike built in Godot 4.x (.NET / C#). See `master.md` for the full game design document and technical architecture.

- **Engine:** Godot 4.6.1 (.NET build) — exe at `D:\Downloads\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe`
- **Language:** C# (primary) — do NOT mix GDScript
- **Project dir:** `hollow_wardens/` (subdirectory)
- **Status:** Phase 6+ active development — 432 tests passing, functional prototype playable

## Project Architecture

| Layer | Path | Notes |
|-------|------|-------|
| Pure C# logic | `src/HollowWardens.Core/` | No Godot dependencies — all game logic, encounter engine, card system |
| xUnit tests | `src/HollowWardens.Tests/` | Run with `dotnet test`, no Godot required |
| Godot UI layer | `hollow_wardens/godot/` | Bridge + view controllers only — delegates to Core |
| Old scripts (backup) | `hollow_wardens/scripts_old_backup/` | **DO NOT USE** — pending deletion |

## Card Data Authoring

Card definitions are **decoupled from code**. Edit JSON only — no generation step needed.

| File | Role |
|------|------|
| `data/cards-root.json` | Source of truth — v2.1 schema, 10 starting + 14 draft cards |
| `src/HollowWardens.Core/Data/CardLoader.cs` | Loads cards at runtime from JSON |

**To update cards:** edit `data/cards-root.json` directly. Changes are picked up at runtime.
**To add a new Warden's cards:** create `data/cards-{warden}.json` following the v2.1 schema.
See `master.md §11.6` for full schema documentation and effect type reference.

## Running Tests

After any code changes, run:

```bash
# Unit + integration tests (no Godot needed)
cd src/HollowWardens.Tests && dotnet test

# Godot build check
cd hollow_wardens && dotnet build HollowWardens.csproj

# Launch game
& "D:\Downloads\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe" --path "D:\Workspace\hollow\hollow_wardens"
```

## Key Design Documents

| File | Purpose |
|------|---------|
| `master.md` | Full game design document — updated through D27 |
| `CLAUDE-decisions.md` | Architecture decisions D1–D36 with rationale |
| `CLAUDE-balance.md` | Balance decisions B1+ with sim evidence |
| `ARCHITECTURE-v2.md` | Pure C# architecture plan and class structure |
| `BUILD-PLAYBOOK.md` | Agent prompts for parallel development |
| `data/cards-root.json` | Root warden card definitions v2.1 |
| `data/localization/strings.csv` | English localization strings (80+ keys, CSV format) |

## Debug Shortcuts (In-Game)

| Key | Action |
|-----|--------|
| `` ` `` | Toggle dev console (type `/help` for command list) |
| `D` | Toggle debug log overlay |
| `P` | Print encounter seed + action log to console |
| `Space` | Advance phase / confirm |
| `R` | Rest |
| `Escape` | Cancel targeting |

## Dev Console Commands (`` ` `` to open)

**Actually wired** (work now during playtesting):

| Command | Effect |
|---------|--------|
| `/help` | List all commands |
| `/add_presence <territory> [n]` | Add n presence tokens (default 1). e.g. `/add_presence I1 2` |
| `/kill_all` | Remove all invaders from the board |
| `/export` | Print encounter state / action log |
| `/run_info` | Print mode, warden, weave, tide, run stage |
| `/set_weave <n>` | Set current weave (via GameBridge) |
| `/set_corruption <territory> <pts>` | Set corruption on a territory |
| `/give_tokens <n>` | Add upgrade tokens to the current run |
| `/end_encounter [clean\|weathered\|breach]` | Force-end the encounter, return to menu |

**Parsed but not yet wired in console UI** (stubs — show "coming soon"):
`/set_max_weave`, `/set_element`, `/set_dread`, `/spawn`, `/add_card`, `/upgrade_card`,
`/unlock_passive`, `/upgrade_passive`, `/trigger_event`, `/skip_tide`, `/encounter`, `/restart`

> Territory IDs: `I1`, `M1`, `M2`, `A1`, `A2`, `A3`
> Element names: `Fire`, `Ash`, `Root`, `Stone`, `Water`, `Wind`

## Environment Architecture

This repo is a Claude Code configuration environment. The "codebase" is primarily the `.claude/` directory. Future Claude instances should understand what tools are available before starting work.

### Subagents (`.claude/agents/`)

| Agent | When to use |
|-------|-------------|
| `code-searcher` | Codebase search, function/class location, security analysis, CoD mode for token-efficient searches |
| `memory-bank-synchronizer` | Sync CLAUDE-*.md memory files when code has changed significantly |
| `get-current-datetime` | Timestamps (Brisbane/AEST timezone) |
| `ux-design-expert` | UI/UX design guidance, Tailwind CSS, Highcharts data visualization |
| `zai-cli` | Z.AI GLM 4.7 perspective on code analysis |
| `codex-cli` | OpenAI Codex (GPT-5.2) perspective on code analysis |

### Skills (`.claude/skills/`)

| Skill | When to use |
|-------|-------------|
| `claude-docs-consultant` | Fetch official docs.claude.com pages for Claude Code features |
| `consult-zai` | Dual-AI analysis: Z.AI + code-searcher together |
| `consult-codex` | Dual-AI analysis: Codex + code-searcher together |

### Slash Commands (`.claude/commands/`)

| Command | Purpose |
|---------|---------|
| `/update-memory-bank` | Update CLAUDE.md and all CLAUDE-*.md memory files |
| `/cleanup-context` | Reduce token bloat in memory bank files |
| `/ccusage-daily` | Claude Code daily usage cost summary |
| `/apply-thinking-to` | Enhance a prompt with extended thinking |
| `/convert-to-todowrite-tasklist-prompt` | Convert a prompt to parallel TodoWrite task format |
| `/security-audit` | Full security audit of the codebase |
| `/check-best-practices` | Check code against best practices |
| `/secure-prompts` | Analyze prompts for injection vulnerabilities |
| `/refactor-code` | Refactoring analysis |
| `/explain-architecture-pattern` | Explain an architecture pattern |
| `/create-readme-section` | Generate a README section |
| `/create-release-note` | Generate a release note |

### MCP Servers

| Server | Purpose |
|--------|---------|
| `context7` | Up-to-date library documentation (active — use `mcp__context7__query-docs`) |
| `cf-docs` | Cloudflare platform documentation |
| `chrome-devtools` | Browser automation via Chrome DevTools Protocol |

## Search Commands

Prefer `rg` and `fd` over `grep`/`find`/`tree` (not installed on Windows):

```bash
rg "search_term"          # Content search (FASTEST)
rg --files                # List files (respects .gitignore)
rg --files | rg "pattern" # Find files by name
fd "filename"             # Find files by name pattern
fd . -t f                 # All files recursively
fd . -t d                 # All directories
fd -e gd                  # All GDScript files (once Godot project exists)
```
