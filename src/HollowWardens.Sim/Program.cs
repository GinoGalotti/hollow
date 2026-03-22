using System.Text;
using System.Text.Json;
using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Data;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders.PaleMarch;
using HollowWardens.Core.Localization;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using HollowWardens.Sim;

// ── Argument parsing ──────────────────────────────────────────────────────────
string? cliSeeds    = null;
string? cliWarden   = null;
string? cliOutput   = null;
string? cliData     = null;
string? cliEncounter = null;
string? profilePath  = null;
bool    verbose      = false;

for (int i = 0; i < args.Length; i++)
{
    if      (args[i] == "--seeds"     && i + 1 < args.Length) cliSeeds    = args[++i];
    else if (args[i] == "--seed"      && i + 1 < args.Length) cliSeeds    = args[++i]; // backward compat: single seed
    else if (args[i] == "--data"      && i + 1 < args.Length) cliData     = args[++i];
    else if (args[i] == "--output"    && i + 1 < args.Length) cliOutput   = args[++i];
    else if (args[i] == "--warden"    && i + 1 < args.Length) cliWarden   = args[++i];
    else if (args[i] == "--encounter" && i + 1 < args.Length) cliEncounter = args[++i];
    else if (args[i] == "--profile"   && i + 1 < args.Length) profilePath  = args[++i];
    else if (args[i] == "--verbose")                           verbose      = true;
}

// ── Load profile (if given) ───────────────────────────────────────────────────
SimProfile? profile = null;
if (profilePath != null)
{
    if (!File.Exists(profilePath))
    {
        Console.Error.WriteLine($"[ERROR] Profile not found: {profilePath}");
        return 1;
    }
    var json     = File.ReadAllText(profilePath);
    var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    profile = JsonSerializer.Deserialize<SimProfile>(json, jsonOpts);
}

// ── Resolve effective seeds (CLI > profile > default 1-500) ──────────────────
string             seedsStr    = cliSeeds ?? profile?.Seeds ?? "1-500";
IReadOnlyList<int> seeds       = ParseSeeds(seedsStr);
int    runs         = seeds.Count;
string wardenArg    = cliWarden   ?? profile?.Warden    ?? "root";
string encounterArg = cliEncounter ?? profile?.Encounter ?? "pale_march_standard";
string outputDir    = cliOutput   ?? profile?.Output    ?? "sim-results";
string? dataPath    = cliData;

// ── Build BalanceConfig from profile overrides ────────────────────────────────
var balance = new BalanceConfig();
if (profile?.BalanceOverrides != null)
    SimProfileApplier.ApplyBalanceOverrides(balance, profile.BalanceOverrides);

// ── Locate warden JSON ────────────────────────────────────────────────────────
if (dataPath == null)
{
    // Walk up from the exe to find project root (contains "data/wardens/" directory)
    var dir = AppContext.BaseDirectory;
    while (dir != null && !Directory.Exists(Path.Combine(dir, "data", "wardens")))
        dir = Path.GetDirectoryName(dir);
    var dataDir = dir != null
        ? Path.Combine(dir, "data", "wardens")
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../data/wardens"));
    dataPath = Path.Combine(dataDir, $"{wardenArg}.json");

    var csvPath = Path.Combine(Path.GetDirectoryName(dataDir) ?? dataDir, "localization", "strings.csv");
    if (File.Exists(csvPath))
        Loc.Load(csvPath, "en");
}

if (!File.Exists(dataPath))
{
    Console.Error.WriteLine($"[ERROR] Warden JSON not found: {dataPath}");
    Console.Error.WriteLine($"Pass --warden <wardenId> or --data <path/to/{wardenArg}.json> to specify the path.");
    return 1;
}

string seedsDisplay = FormatSeedsDisplay(seeds);
Console.WriteLine($"=== HOLLOW WARDENS SIMULATION — {runs} encounters (seeds {seedsDisplay}) ===");
Console.WriteLine($"Warden: {wardenArg} | Encounter: {encounterArg}");
if (profile != null) Console.WriteLine($"Profile: {profile.Name}");
Console.WriteLine();

// ── Run encounters ─────────────────────────────────────────────────────────
var allStats   = new List<SimStats>();
var allExports = new List<(SimStats stats, string export, int seed)>();

int encounterNum = 0;
foreach (int seed in seeds)
{
    encounterNum++;
    var (state, runner, stats, collector) = BuildEncounter(seed, dataPath, balance.Clone(), wardenArg, encounterArg, profile);

    collector.WireEvents();

    IPlayerStrategy strategy = wardenArg == "ember" ? new EmberBotStrategy() : new BotStrategy();

    // --verbose: log first 5 encounters; remaining breaches logged after result
    VerboseLogger? vlog = null;
    bool logThisEncounter = verbose && encounterNum <= 5;
    if (logThisEncounter)
    {
        var logsDir = Path.Combine(outputDir, "logs");
        vlog = new VerboseLogger(state, Path.Combine(logsDir, $"encounter_{seed}.txt"), strategy);
        vlog.WireEvents();
    }

    var result = runner.Run(state, strategy);
    collector.Finalize(result);
    collector.UnwireEvents();

    // For verbose: also log breach encounters beyond first 5
    if (verbose && !logThisEncounter && result == EncounterResult.Breach)
    {
        var logsDir = Path.Combine(outputDir, "logs");
        // Replay isn't possible after the fact — note the breach seed for reference
        var breachNote = new StringBuilder();
        breachNote.AppendLine($"=== BREACH ENCOUNTER — seed {seed} ===");
        breachNote.AppendLine("(Full verbose log not available — encounter ran before verbose was triggered)");
        breachNote.AppendLine($"Export: {state.ActionLog.ExportFull(seed)}");
        Directory.CreateDirectory(logsDir);
        File.WriteAllText(Path.Combine(logsDir, $"encounter_{seed}_breach.txt"), breachNote.ToString(), System.Text.Encoding.UTF8);
    }

    if (vlog != null)
    {
        vlog.Finalize(result);
        vlog.UnwireEvents();
    }

    GameEvents.ClearAll();

    allStats.Add(stats);
    allExports.Add((stats, state.ActionLog.ExportFull(seed), seed));

    // Print breach exports immediately
    if (stats.Result == EncounterResult.Breach)
    {
        var export = state.ActionLog.ExportFull(seed);
        Console.WriteLine($"\n[BREACH] Encounter {encounterNum} (seed {seed}) export:");
        Console.WriteLine(export);
    }
}

// ── Balance report ─────────────────────────────────────────────────────────
int total          = allStats.Count;
int cleanCount     = allStats.Count(s => s.Result == EncounterResult.Clean);
int weatheredCount = allStats.Count(s => s.Result == EncounterResult.Weathered);
int breachCount    = allStats.Count(s => s.Result == EncounterResult.Breach);

double Pct(int n) => total == 0 ? 0 : Math.Round(n * 100.0 / total, 1);
double Avg(Func<SimStats, double> f) => total == 0 ? 0 : Math.Round(allStats.Average(f), 2);

// Build summary into a StringBuilder so it can go to both console and file
var summary   = new StringBuilder();
var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
summary.AppendLine($"=== HOLLOW WARDENS SIMULATION — {runs} encounters (seeds {seedsDisplay}) ===");
summary.AppendLine($"Warden: {wardenArg} | Encounter: {encounterArg}");
if (profile != null) summary.AppendLine($"Profile: {profile.Name}");
summary.AppendLine($"Timestamp: {timestamp}");
summary.AppendLine($"Output directory: {Path.GetFullPath(outputDir)}");
summary.AppendLine();

summary.AppendLine("OUTCOMES:");
summary.AppendLine($"  Clean:     {cleanCount} ({Pct(cleanCount)}%)");
summary.AppendLine($"  Weathered: {weatheredCount} ({Pct(weatheredCount)}%)");
summary.AppendLine($"  Breach:    {breachCount} ({Pct(breachCount)}%)");
summary.AppendLine();

summary.AppendLine("SURVIVAL:");
summary.AppendLine($"  Avg tides completed:  {Avg(s => s.TidesCompleted)}");
summary.AppendLine($"  Avg final weave:      {Avg(s => s.FinalWeave)} / 20");
summary.AppendLine($"  Game overs (weave 0): {allStats.Count(s => s.FinalWeave <= 0)}");
summary.AppendLine();

summary.AppendLine("COMBAT:");
summary.AppendLine($"  Avg invaders killed:     {Avg(s => s.InvadersKilled)}");
summary.AppendLine($"  Avg natives killed:      {Avg(s => s.NativesKilled)}");
summary.AppendLine($"  Avg heart damage events: {Avg(s => s.HeartDamageEvents)}");
summary.AppendLine();

summary.AppendLine("CORRUPTION:");
summary.AppendLine($"  Avg peak corruption (single territory): {Avg(s => s.PeakCorruption)}");
summary.AppendLine($"  Desecration events (L3 reached):        {allStats.Sum(s => s.DesecrationEvents)} total across all encounters");
summary.AppendLine($"  Avg total corruption at final tide:     {Avg(s => s.TideSnapshots.LastOrDefault()?.TotalCorruption ?? 0)}");
summary.AppendLine();

summary.AppendLine("PRESENCE & SACRIFICE:");
summary.AppendLine($"  Avg total presence at final tide: {Avg(s => s.TideSnapshots.LastOrDefault()?.TotalPresence ?? 0)}");
summary.AppendLine($"  Avg sacrifices per encounter:     {Avg(s => s.SacrificeCount)}");
summary.AppendLine();

summary.AppendLine("FEAR:");
summary.AppendLine($"  Avg fear generated per encounter: {Avg(s => s.TotalFearGenerated)}");
summary.AppendLine();

// Per-tide averages
int maxTides = allStats.Max(s => s.TideSnapshots.Count);
if (maxTides > 0)
{
    summary.AppendLine("PER-TIDE AVERAGES (across all encounters):");
    for (int t = 1; t <= maxTides; t++)
    {
        int tIdx = t;
        var snapshots = allStats
            .Select(s => s.TideSnapshots.FirstOrDefault(snap => snap.Tide == tIdx))
            .Where(snap => snap != null)
            .ToList();
        if (snapshots.Count == 0) continue;
        double avgWeave      = Math.Round(snapshots.Average(snap => snap!.Weave), 1);
        double avgInvaders   = Math.Round(snapshots.Average(snap => snap!.TotalInvadersAlive), 1);
        double avgPresence   = Math.Round(snapshots.Average(snap => snap!.TotalPresence), 1);
        double avgCorruption = Math.Round(snapshots.Average(snap => snap!.TotalCorruption), 1);
        summary.AppendLine($"  Tide {t}: weave={avgWeave} invaders={avgInvaders} presence={avgPresence} corruption={avgCorruption}");
    }
    summary.AppendLine();
}

// Closest call export
var closest = allExports.OrderBy(r => r.stats.FinalWeave).First();
summary.AppendLine($"[CLOSEST CALL] Encounter with lowest weave ({closest.stats.FinalWeave}) — seed {closest.seed}:");
summary.AppendLine(closest.export);

Console.Write(summary.ToString());

// ── Write output files ─────────────────────────────────────────────────────
Directory.CreateDirectory(outputDir);

// summary.txt
File.WriteAllText(Path.Combine(outputDir, "summary.txt"), summary.ToString(), Encoding.UTF8);

// encounters.csv
var encountersCsv = new StringBuilder();
encountersCsv.AppendLine("seed,result,tides_completed,final_weave,max_weave,invaders_killed,natives_killed,heart_damage_events,peak_corruption,total_corruption_at_end,total_presence_at_end,sacrifices,total_fear_generated,dread_level,cards_removed,final_corruption_json,export_string");
foreach (var (stats, export, seed) in allExports)
{
    var escapedExport = "\"" + export.Replace("\"", "\"\"") + "\"";
    int corrAtEnd = stats.TideSnapshots.LastOrDefault()?.TotalCorruption ?? 0;
    int presAtEnd = stats.TideSnapshots.LastOrDefault()?.TotalPresence ?? 0;
    // Carryover columns
    var carryover = stats.FinalCarryover;
    int dreadLevel   = carryover?.DreadLevel ?? 1;
    int cardsRemoved = carryover?.PermanentlyRemovedCards.Count ?? 0;
    string corrJson  = carryover?.CorruptionCarryover.Count > 0
        ? "\"" + System.Text.Json.JsonSerializer.Serialize(carryover.CorruptionCarryover).Replace("\"", "\"\"") + "\""
        : "\"{}\"";
    encountersCsv.AppendLine(
        $"{seed},{stats.Result},{stats.TidesCompleted},{stats.FinalWeave},20," +
        $"{stats.InvadersKilled},{stats.NativesKilled},{stats.HeartDamageEvents}," +
        $"{stats.PeakCorruption},{corrAtEnd},{presAtEnd}," +
        $"{stats.SacrificeCount},{stats.TotalFearGenerated},{dreadLevel},{cardsRemoved},{corrJson},{escapedExport}");
}
File.WriteAllText(Path.Combine(outputDir, "encounters.csv"), encountersCsv.ToString(), Encoding.UTF8);

// per-tide.csv
var perTideCsv = new StringBuilder();
perTideCsv.AppendLine("seed,tide,weave,alive_invaders,total_presence,total_corruption,fear_generated_this_tide,invaders_killed_this_tide,invaders_arrived_this_tide");
foreach (var (stats, _, seed) in allExports)
{
    foreach (var snap in stats.TideSnapshots)
    {
        perTideCsv.AppendLine(
            $"{seed},{snap.Tide},{snap.Weave},{snap.TotalInvadersAlive}," +
            $"{snap.TotalPresence},{snap.TotalCorruption}," +
            $"{snap.FearGeneratedThisTide},{snap.InvadersKilledThisTide},{snap.InvadersArrivedThisTide}");
    }
}
File.WriteAllText(Path.Combine(outputDir, "per-tide.csv"), perTideCsv.ToString(), Encoding.UTF8);

Console.WriteLine($"Results written to {Path.GetFullPath(outputDir)}/");
if (verbose) Console.WriteLine($"Verbose logs written to {Path.GetFullPath(Path.Combine(outputDir, "logs"))}/");

return 0;

// ── Helpers ────────────────────────────────────────────────────────────────

static IReadOnlyList<int> ParseSeeds(string s)
{
    s = s.Trim();
    if (s.Contains('-'))
    {
        var parts = s.Split('-', 2);
        int start = int.Parse(parts[0]);
        int end   = int.Parse(parts[1]);
        return Enumerable.Range(start, end - start + 1).ToList();
    }
    return s.Split(',').Select(x => int.Parse(x.Trim())).ToArray();
}

static string FormatSeedsDisplay(IReadOnlyList<int> seeds)
{
    if (seeds.Count == 0) return "(none)";
    if (seeds.Count == 1) return seeds[0].ToString();
    return $"{seeds[0]}\u2013{seeds[seeds.Count - 1]}";  // en-dash
}

// ── Builder ────────────────────────────────────────────────────────────────
static (EncounterState state, EncounterRunner runner, SimStats stats, SimStatsCollector collector)
    BuildEncounter(int seed, string wardenJsonPath, BalanceConfig balance, string wardenId, string encounterId, SimProfile? profile)
{
    var random     = GameRandom.FromSeed(seed);
    var wardenData = WardenLoader.Load(wardenJsonPath);

    // Apply warden overrides (cards, hand limit, starting presence)
    if (profile?.WardenOverrides != null)
        SimProfileApplier.ApplyWardenOverrides(wardenData, profile.WardenOverrides);

    var config      = EncounterLoader.Create(encounterId);
    var graph       = HollowWardens.Core.Map.TerritoryGraph.Create(config.BoardLayout);
    var territories = HollowWardens.Core.Map.BoardState.Create(graph).Territories.Values.ToList();
    var presence    = new PresenceSystem(() => territories, balance.MaxPresencePerTerritory);
    var dread       = new DreadSystem(balance);

    // Apply encounter overrides (tide count, native spawns, escalation, levers)
    if (profile?.EncounterOverrides != null)
        SimProfileApplier.ApplyEncounterOverrides(config, profile.EncounterOverrides);

    // Re-create board if encounter overrides changed the layout
    if (profile?.EncounterOverrides?.BoardLayout != null)
    {
        graph = HollowWardens.Core.Map.TerritoryGraph.Create(config.BoardLayout);
        var newBoard = HollowWardens.Core.Map.BoardState.Create(graph);
        territories.Clear();
        territories.AddRange(newBoard.Territories.Values);
    }

    IWardenAbility warden = wardenData.WardenId switch
    {
        "root"  => new RootAbility(presence, balance),
        "ember" => new EmberAbility(),
        _       => new RootAbility(presence, balance)
    };

    var gating = new PassiveGating(wardenData.WardenId);
    if (warden is RootAbility rootAbility)
        rootAbility.Gating = gating;

    var faction = new PaleMarchFaction();
    faction.HpBonus = balance.InvaderHpBonus;

    // Apply hand limit override from encounter config
    int handLimit = wardenData.HandLimit;
    if (config.HandLimitOverride.HasValue)
        handLimit = config.HandLimitOverride.Value;

    var state = new EncounterState
    {
        Config        = config,
        Graph         = graph,
        Territories   = territories,
        Elements      = new ElementSystem(balance),
        Dread         = dread,
        Weave         = new WeaveSystem(balance.StartingWeave, balance.MaxWeave),
        Combat        = new CombatSystem(),
        Presence      = presence,
        Corruption    = new CorruptionSystem(),
        FearActions   = new FearActionSystem(dread, FearActionPool.Build(), random, balance),
        Warden        = warden,
        WardenData    = wardenData,
        Random        = random,
        ActionLog     = new ActionLog(),
        PassiveGating = gating,
        Balance       = balance
    };

    // Apply starting corruption from encounter config
    if (config.StartingCorruption != null)
        SimProfileApplier.ApplyStartingCorruption(state, config.StartingCorruption);

    // Apply post-state profile overrides
    if (profile?.EncounterOverrides?.StartingCorruption != null)
        SimProfileApplier.ApplyStartingCorruption(state, profile.EncounterOverrides.StartingCorruption);
    if (profile?.WardenOverrides?.StartingElements != null)
        SimProfileApplier.ApplyStartingElements(state, profile.WardenOverrides.StartingElements);
    if (profile?.WardenOverrides != null)
        SimProfileApplier.ApplyPassiveOverrides(gating, profile.WardenOverrides);

    var startingCards = wardenData.Cards.Where(c => c.IsStarting).ToList();
    state.Deck = new DeckManager(warden, startingCards, random, handLimit, shuffle: true);

    // Apply board carryover overrides (starting weave, corruption, removed cards)
    // Must come after Deck is built so PermanentlyRemove works
    if (profile?.BoardCarryover != null)
        SimProfileApplier.ApplyBoardCarryoverOverride(state, profile.BoardCarryover);

    var actionDeck = new ActionDeck(faction.BuildPainfulPool(), faction.BuildEasyPool(), random, shuffle: true);
    var cadence    = new CadenceManager(config.Cadence);
    var spawn      = new SpawnManager(config.Waves, random);
    var resolver   = new HollowWardens.Core.Effects.EffectResolver();

    VulnerabilityWiring.WireEvents(presence);

    var runner    = new EncounterRunner(actionDeck, cadence, spawn, faction, resolver);
    var simStats  = new SimStats();
    var collector = new SimStatsCollector(simStats, state);

    return (state, runner, simStats, collector);
}
