namespace HollowWardens.Tests;

using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Data;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders.PaleMarch;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Deterministic replay tests — verifies that the same seed always produces
/// the same export string, and that replaying an export produces the same final weave.
/// </summary>
public class ReplayTests : IDisposable
{
    private static readonly string WardenJsonPath = GetWardenJsonPath();

    public void Dispose() => GameEvents.ClearAll();

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string GetWardenJsonPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "wardens", "root.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not find repo root (no data/wardens/root.json)");
    }

    private static (EncounterState state, EncounterRunner runner) BuildEncounter(int seed)
    {
        var random      = GameRandom.FromSeed(seed);
        var wardenData  = WardenLoader.Load(WardenJsonPath);
        var territories = HollowWardens.Core.Map.BoardState.CreatePyramid().Territories.Values.ToList();
        var presence    = new PresenceSystem(() => territories);
        var warden      = new RootAbility(presence);
        var dread       = new DreadSystem();
        var config      = EncounterLoader.CreatePaleMarchStandard();

        var state = new EncounterState
        {
            Config      = config,
            Territories = territories,
            Elements    = new ElementSystem(),
            Dread       = dread,
            Weave       = new WeaveSystem(20),
            Combat      = new CombatSystem(),
            Presence    = presence,
            Corruption  = new CorruptionSystem(),
            FearActions = new FearActionSystem(dread, FearActionPool.Build(), random),
            Warden      = warden,
            WardenData  = wardenData,
            Random      = random,
            ActionLog   = new ActionLog()
        };

        var startingCards = wardenData.Cards.Where(c => c.IsStarting).ToList();
        state.Deck = new DeckManager(warden, startingCards, random, shuffle: true);

        var faction    = new PaleMarchFaction();
        var actionDeck = new ActionDeck(faction.BuildPainfulPool(), faction.BuildEasyPool(), random, shuffle: true);
        var cadence    = new CadenceManager(config.Cadence);
        var spawn      = new SpawnManager(config.Waves, random);
        var resolver   = new HollowWardens.Core.Effects.EffectResolver();

        VulnerabilityWiring.WireEvents(presence);

        var runner = new EncounterRunner(actionDeck, cadence, spawn, faction, resolver);
        return (state, runner);
    }

    private static string RunAndExport(int seed)
    {
        GameEvents.ClearAll();
        var (state, runner) = BuildEncounter(seed);
        runner.Run(state, new BotStrategy());
        GameEvents.ClearAll();
        return state.ActionLog.ExportFull(seed);
    }

    private static EncounterState RunEncounter(int seed)
    {
        GameEvents.ClearAll();
        var (state, runner) = BuildEncounter(seed);
        runner.Run(state, new BotStrategy());
        GameEvents.ClearAll();
        return state;
    }

    // ── Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public void SameSeed_ProducesSameExportString()
    {
        var export1 = RunAndExport(seed: 42);
        var export2 = RunAndExport(seed: 42);
        Assert.Equal(export1, export2);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentExportStrings()
    {
        var export1 = RunAndExport(seed: 42);
        var export2 = RunAndExport(seed: 99);
        Assert.NotEqual(export1, export2);
    }

    [Fact]
    public void ExportString_ContainsSeedPrefix()
    {
        var export = RunAndExport(seed: 42);
        Assert.StartsWith("SEED:42|", export);
    }

    [Fact]
    public void ImportFull_RoundTrips_Seed()
    {
        var export = RunAndExport(seed: 42);
        var (seed, _) = ActionLog.ImportFull(export);
        Assert.Equal(42, seed);
    }

    [Fact]
    public void Replay_ProducesSameFinalWeave()
    {
        // Capture the original weave from the same run that produces the export
        GameEvents.ClearAll();
        var (origState, origRunner) = BuildEncounter(42);
        origRunner.Run(origState, new BotStrategy());
        GameEvents.ClearAll();

        int originalWeave = origState.Weave!.CurrentWeave;
        var export = origState.ActionLog.ExportFull(42);

        var (seed, rawActions) = ActionLog.ImportFull(export);
        GameEvents.ClearAll();
        var replayState = new ReplayRunner(seed, rawActions, WardenJsonPath).Replay();
        GameEvents.ClearAll();

        Assert.Equal(originalWeave, replayState.Weave!.CurrentWeave);
    }

    [Fact]
    public void Replay_ProducesSameActionCount()
    {
        // Capture the original action count from the same run
        GameEvents.ClearAll();
        var (origState, origRunner) = BuildEncounter(42);
        origRunner.Run(origState, new BotStrategy());
        GameEvents.ClearAll();

        int originalActionCount = origState.ActionLog.Count;
        var export = origState.ActionLog.ExportFull(42);

        var (seed, rawActions) = ActionLog.ImportFull(export);
        GameEvents.ClearAll();
        var replayState = new ReplayRunner(seed, rawActions, WardenJsonPath).Replay();
        GameEvents.ClearAll();

        Assert.Equal(originalActionCount, replayState.ActionLog.Count);
    }
}
