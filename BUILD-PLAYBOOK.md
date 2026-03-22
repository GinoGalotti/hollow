# Hollow Wardens — Build Playbook

Quick reference for running, testing, and replaying the game.

---

## Running tests

```bash
dotnet test src/HollowWardens.Tests/
```

Expected: 297+ tests passing.

---

## Running the simulation

```bash
dotnet run --project src/HollowWardens.Sim/ -- --seeds 1-100 --warden root
```

Runs encounters for seeds 1–100 (100 total) and prints balance statistics.

### Running with a SimProfile

```bash
dotnet run --project src/HollowWardens.Sim/ -- --profile sim-profiles/example-tough-root.json
```

### Creating a SimProfile

Copy an example from `sim-profiles/` and modify. Key sections:
- `warden_overrides`: change cards, passives, starting elements
- `encounter_overrides`: change tides, corruption, waves
- `balance_overrides`: change any BalanceConfig field (use snake_case keys)

### A/B Testing

Run the same seed range with different profiles:

```bash
dotnet run --project src/HollowWardens.Sim/ -- --profile sim-profiles/variant-a.json --output sim-results/variant-a/
dotnet run --project src/HollowWardens.Sim/ -- --profile sim-profiles/variant-b.json --output sim-results/variant-b/
```

Compare `sim-results/variant-a/summary.txt` vs `sim-results/variant-b/summary.txt`

---

## Exporting encounter state

In Godot during gameplay:

- **F5** — copies encounter export string to clipboard
- **F6** — prints export string to console
- **Copy State** button (top-left when debug log is open)

---

## Replaying an encounter

- Paste the export string into "Paste saved state here..." field in Godot
- Click **Load** to replay

---

## Launching the game

```bash
& "D:\Downloads\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe" --path "D:\Workspace\hollow\hollow_wardens"
```

---

## Debug shortcuts (in-game)

| Key | Action |
|-----|--------|
| `D` | Toggle debug log overlay |
| `P` | Print encounter seed + action log to console |
| `Space` | Advance phase / confirm |
| `R` | Rest |
| `Escape` | Cancel targeting |
| `F5` | Copy encounter export string to clipboard |
| `F6` | Print export string to console |
