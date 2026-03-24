namespace HollowWardens.Tests.Integration;

using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Data;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders.PaleMarch;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Regression tests for the warden-selection → encounter-start flow.
/// These tests catch the most common breakage: warden JSON changes, encounter
/// config changes, or system init changes that prevent the game from starting.
///
/// Coverage:
/// - Both wardens load from JSON with the expected starting deck
/// - All 10 warden × encounter combos construct without throwing
/// - After initialization, the board is in the correct starting state
/// - The encounter actually runs (completes without exception) for both wardens
/// - A chain first-encounter builds cleanly
/// </summary>
public class EncounterStartTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    // ── Helper: locate a repo-relative file ──────────────────────────────────

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            $"Could not locate '{relativePath}' by walking up from {AppContext.BaseDirectory}");
    }

    // ── Helper: build a real encounter state from actual warden JSON ──────────
    //
    // Mirrors GameBridge.BuildEncounter() in the Godot layer but without any
    // Godot dependencies. Uses minimal fear pools (same pattern as IntegrationHelpers).

    private static (EncounterState state, EncounterRunner runner) BuildRealEncounter(
        string wardenId, string encounterId)
    {
        var wardenPath  = FindRepoFile($"data/wardens/{wardenId}.json");
        var wardenData  = WardenLoader.Load(wardenPath);
        var startCards  = wardenData.Cards.Where(c => c.IsStarting).ToList();

        var config      = EncounterLoader.Create(encounterId);
        var graph       = TerritoryGraph.Create(config.BoardLayout);
        var territories = BoardState.Create(graph).Territories.Values.ToList();

        var balance     = new BalanceConfig();
        var dread       = new DreadSystem(balance);
        var fearPools   = new Dictionary<int, List<FearActionData>>
        {
            [1] = new() { new() { Id = "fa_d1", DreadLevel = 1, Effect = new() { Type = EffectType.GenerateFear, Value = 0 } } },
            [2] = new() { new() { Id = "fa_d2", DreadLevel = 2, Effect = new() { Type = EffectType.GenerateFear, Value = 0 } } },
        };
        var presence    = new PresenceSystem(() => territories);

        IWardenAbility wardenAbility = wardenId switch
        {
            "root"  => new RootAbility(presence),
            "ember" => new EmberAbility(),
            _       => throw new ArgumentException($"Unknown warden: {wardenId}")
        };

        var state = new EncounterState
        {
            Config      = config,
            Territories = territories,
            Elements    = new ElementSystem(),
            Dread       = dread,
            Weave       = new WeaveSystem(20, 20),
            Combat      = new CombatSystem(),
            Presence    = presence,
            Corruption  = new CorruptionSystem(),
            FearActions = new FearActionSystem(dread, fearPools, GameRandom.FromSeed(42)),
            Warden      = wardenAbility,
            WardenData  = wardenData,
            Balance     = balance,
        };

        state.Deck = new DeckManager(
            wardenAbility, startCards,
            rng: GameRandom.FromSeed(42),
            handLimit: wardenData.HandLimit);

        var faction    = new PaleMarchFaction();
        var actionDeck = new ActionDeck(
            faction.BuildPainfulPool(), faction.BuildEasyPool(),
            rng: GameRandom.FromSeed(0), shuffle: false);
        var cadence = new CadenceManager(config.Cadence);
        var spawn   = new SpawnManager(config.Waves, rng: GameRandom.FromSeed(0));
        var runner  = new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver());

        return (state, runner);
    }

    // ── Both wardens load from JSON with the correct starting deck ────────────

    [Theory]
    [InlineData("root",  10)]
    [InlineData("ember",  8)]
    public void WardenJson_LoadsWithCorrectStartingCardCount(string wardenId, int expectedStarting)
    {
        var path   = FindRepoFile($"data/wardens/{wardenId}.json");
        var warden = WardenLoader.Load(path);

        Assert.Equal(wardenId, warden.WardenId);
        Assert.Equal(expectedStarting, warden.Cards.Count(c => c.IsStarting));
        Assert.True(warden.HandLimit > 0, "HandLimit must be positive");
    }

    // ── All 10 warden × encounter combos build without throwing ──────────────

    [Theory]
    [InlineData("root",  "pale_march_standard")]
    [InlineData("root",  "pale_march_scouts")]
    [InlineData("root",  "pale_march_siege")]
    [InlineData("root",  "pale_march_elite")]
    [InlineData("root",  "pale_march_frontier")]
    [InlineData("ember", "pale_march_standard")]
    [InlineData("ember", "pale_march_scouts")]
    [InlineData("ember", "pale_march_siege")]
    [InlineData("ember", "pale_march_elite")]
    [InlineData("ember", "pale_march_frontier")]
    public void AllWardenEncounterCombos_BuildWithoutThrowing(string wardenId, string encounterId)
    {
        var ex = Record.Exception(() => BuildRealEncounter(wardenId, encounterId));
        Assert.Null(ex);
    }

    // ── Board is in the correct starting state after initialization ───────────

    [Theory]
    [InlineData("root")]
    [InlineData("ember")]
    public void EncounterStart_StandardBoard_HasExpectedTerritories(string wardenId)
    {
        var (state, _) = BuildRealEncounter(wardenId, "pale_march_standard");

        Assert.NotEmpty(state.Territories);
        Assert.NotNull(state.GetTerritory("A1"));
        Assert.NotNull(state.GetTerritory("M1"));
        Assert.NotNull(state.GetTerritory("I1"));
    }

    [Theory]
    [InlineData("root")]
    [InlineData("ember")]
    public void EncounterStart_AfterInit_StartingPresencePlacedOnI1(string wardenId)
    {
        var (state, runner) = BuildRealEncounter(wardenId, "pale_march_standard");

        // 0-tide run: only initial setup executes (natives + presence placed, no tides)
        state.Config.TideCount = 0;
        runner.Run(state, new IntegrationHelpers.IdleStrategy());

        Assert.True(
            state.GetTerritory("I1")!.PresenceCount > 0,
            $"Expected starting presence on I1 for warden '{wardenId}' after init");
    }

    // ── The encounter actually runs start-to-finish without exception ─────────

    [Theory]
    [InlineData("root",  "pale_march_standard")]
    [InlineData("ember", "pale_march_standard")]
    public void EncounterStart_FullRun_CompletesWithValidResult(string wardenId, string encounterId)
    {
        var (state, runner) = BuildRealEncounter(wardenId, encounterId);

        EncounterResult result = default;
        var ex = Record.Exception(
            () => result = runner.Run(state, new IntegrationHelpers.IdleStrategy()));

        Assert.Null(ex);
        Assert.True(
            result is EncounterResult.Clean or EncounterResult.Weathered or EncounterResult.Breach,
            $"Expected a valid EncounterResult for {wardenId}+{encounterId}, got {result}");
    }

    // ── Chain start: first encounter in a chain builds cleanly ───────────────

    [Theory]
    [InlineData("root")]
    [InlineData("ember")]
    public void ChainStart_FirstEncounterInChain_BuildsWithoutThrowing(string wardenId)
    {
        // Default chain: Standard → Scouts → Elite (mirrors WardenSelectController defaults)
        string[] chain = { "pale_march_standard", "pale_march_scouts", "pale_march_elite" };

        var ex = Record.Exception(() => BuildRealEncounter(wardenId, chain[0]));
        Assert.Null(ex);
    }
}
