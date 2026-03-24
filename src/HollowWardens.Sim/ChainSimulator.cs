namespace HollowWardens.Sim;

using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Data;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders.PaleMarch;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Turn;
using HollowWardens.Core.Wardens;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Telemetry;

/// <summary>
/// Simulates a full roguelike run through a realm: multiple encounters with
/// carryover, rewards, and between-encounter events. Bot strategy is data-driven
/// via BotChainConfig.
/// </summary>
public static class ChainSimulator
{
    /// <summary>
    /// Runs a single chain (full roguelike run) for the given seed.
    /// </summary>
    public static ChainRunResult RunChain(
        int seed,
        string wardenId,
        string wardenJsonPath,
        string realmId,
        BalanceConfig? baseBalance = null,
        BotChainConfig? botConfig = null,
        TelemetryCollector? telemetry = null)
    {
        botConfig ??= BotChainConfig.Default;

        var rng       = new Random(seed);
        var realm     = RealmLoader.Load(realmId);
        var wardenData = WardenLoader.Load(wardenJsonPath);

        var run = CreateRunState(wardenId, wardenData);
        if (baseBalance?.MaxWeave is int mw) run.MaxWeave = mw;

        telemetry?.StartRun(wardenId, "chain_sim", realmId, seed);

        var realmRunner = new RealmRunner(run, realm, rng);
        var result      = new ChainRunResult { Seed = seed };

        int stageNum = 0;
        while (!realmRunner.IsRunComplete())
        {
            var currentNode = realmRunner.GetCurrentNode();
            if (currentNode.Type == "complete") break;

            // Determine encounter ID (support force_encounters override)
            string encId = currentNode.EncounterId ?? "pale_march_standard";

            var config  = EncounterLoader.Create(encId);
            var balance = baseBalance?.Clone() ?? new BalanceConfig();

            var (state, runner) = BuildEncounterForChain(seed + stageNum, wardenJsonPath, balance, wardenId, config, wardenData);

            // Apply carryover from RunState into encounter
            var carryover = RunStateToCarryover(run);
            EncounterRunner.ApplyCarryover(state, carryover);

            // Run encounter
            IPlayerStrategy strategy = (IPlayerStrategy)(wardenId == "ember" ? new EmberBotStrategy() : new BotStrategy());
            IPlayerStrategy wrappedStrategy = telemetry != null
                ? new TelemetryBotWrapper(strategy, telemetry)
                : strategy;

            telemetry?.StartEncounter(config.Id, config.BoardLayout, state.Balance.MaxWeave);
            if (telemetry != null)
            {
                GameEvents.InvaderDefeated += _ => telemetry.OnInvaderKilled();
                GameEvents.FearGenerated += amt => telemetry.OnFearGenerated(amt);
                GameEvents.HeartDamageDealt += _ => telemetry.OnHeartDamage();
                GameEvents.TideCompleted += t => telemetry.SetTide(t);
            }

            var encounterResult = runner.Run(state, wrappedStrategy);
            telemetry?.EndEncounter(state, encounterResult.ToString().ToLowerInvariant(), null);
            GameEvents.ClearAll();

            // Extract carryover and update RunState
            var newCarryover = state.ExtractCarryover();
            ApplyCarryoverToRunState(run, newCarryover);

            // Snapshot max weave after carryover (for arc reporting)
            result.MaxWeaveHistory.Add(run.MaxWeave);

            // Compute rewards
            var reward = RunRewardCalculator.Calculate(
                encounterResult, run.CurrentWeave, run.MaxWeave, wardenId, config);
            run.UpgradeTokens += reward.UpgradeTokens;
            result.TokensEarned += reward.UpgradeTokens;

            run.EncounterResults.Add(encounterResult.ToString().ToLowerInvariant());
            run.CompletedEncounterIds.Add(config.Id);
            result.EncounterResults.Add(encounterResult);
            result.StagesCompleted++;
            stageNum++;

            // Pick next node
            var availableNodes = realmRunner.GetAvailableNextNodes();
            if (availableNodes.Count == 0) break;

            var chosenNode = PickNode(availableNodes, botConfig.PathStrategy);

            // Process the chosen node
            ProcessNode(chosenNode, run, realmRunner, rng, botConfig, result, telemetry);

            realmRunner.AdvanceToNode(chosenNode.Id);
        }

        result.FinalWeave    = run.CurrentWeave;
        result.FinalMaxWeave = run.MaxWeave;
        int requiredStages = realm.Stages.Count(s => !s.IsOptional);
        result.FullClear          = result.StagesCompleted >= requiredStages;
        result.ReachedOptionalStage = result.StagesCompleted > requiredStages;
        result.VisitedNodeIds = run.VisitedNodeIds.ToList();

        telemetry?.EndRun(run, result.FullClear ? "complete" : "failed");

        return result;
    }

    /// <summary>
    /// Processes a post-encounter node (event, rest, merchant, corruption).
    /// Can be called directly for testing.
    /// </summary>
    public static void ProcessNode(
        MapNode node, RunState run, RealmRunner realmRunner,
        Random rng, BotChainConfig config, ChainRunResult result,
        TelemetryCollector? telemetry = null)
    {
        switch (node.Type)
        {
            case "event":
            {
                var evt = realmRunner.DrawEventForNode(node);
                if (evt != null)
                {
                    int weaveBefore = run.CurrentWeave;
                    int tokensBefore = run.UpgradeTokens;
                    int optIdx = PickEventOption(evt, run, config.EventStrategy);
                    EventRunner.ResolveOption(run, evt, optIdx, rng);
                    telemetry?.RecordEvent(evt.Id, evt.Type, optIdx, null,
                        weaveBefore, run.CurrentWeave, tokensBefore, run.UpgradeTokens);
                    result.EventsResolved++;
                }
                break;
            }
            case "rest":
                ApplyRestStop(run, config.RestStopStrategy);
                break;
            case "corruption":
            {
                // Auto-apply option 0 (accept corruption) unless tokens allow mitigation
                var evt = realmRunner.DrawEventForNode(node);
                if (evt != null)
                {
                    int optIdx = run.UpgradeTokens > 0 && evt.Options.Count > 1 ? 1 : 0;
                    EventRunner.ResolveOption(run, evt, optIdx, rng);
                    result.EventsResolved++;
                }
                break;
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RunState CreateRunState(string wardenId, WardenData wardenData)
    {
        var startingCards = wardenData.Cards.Where(c => c.IsStarting).Select(c => c.Id).ToList();
        return new RunState
        {
            WardenId      = wardenId,
            RealmId       = "realm_1",
            MaxWeave      = 20,
            CurrentWeave  = 20,
            DeckCardIds   = startingCards,
            UpgradeTokens = 0,
        };
    }

    private static BoardCarryover RunStateToCarryover(RunState run) => new()
    {
        FinalWeave              = run.CurrentWeave,
        MaxWeave                = run.MaxWeave,
        CorruptionCarryover     = new Dictionary<string, int>(run.CorruptionCarryover),
        PermanentlyRemovedCards = run.PermanentlyRemovedCardIds.ToList(),
        DreadLevel              = run.DreadLevel,
        TotalFearGenerated      = run.TotalFearGenerated,
    };

    private static void ApplyCarryoverToRunState(RunState run, BoardCarryover carryover)
    {
        run.CurrentWeave              = carryover.FinalWeave;
        run.MaxWeave                  = carryover.MaxWeave;
        run.CorruptionCarryover       = new Dictionary<string, int>(carryover.CorruptionCarryover);
        run.PermanentlyRemovedCardIds = carryover.PermanentlyRemovedCards.ToList();
        run.DreadLevel                = carryover.DreadLevel;
        run.TotalFearGenerated       += carryover.TotalFearGenerated;
    }

    private static MapNode PickNode(List<MapNode> nodes, string strategy) => strategy switch
    {
        "first"    => nodes.First(),
        "last"     => nodes.Last(),
        "balanced" => nodes[nodes.Count / 2],
        _          => nodes[nodes.Count / 2]
    };

    private static int PickEventOption(EventData evt, RunState run, string strategy)
    {
        if (strategy == "safe")
        {
            // Prefer options with heal effects
            for (int i = 0; i < evt.Options.Count; i++)
            {
                if (evt.Options[i].Effects.Any(e => e.Type is "heal_weave" or "heal_max_weave"))
                    return i;
            }
            // No heal found — prefer options that don't contain harmful effects
            string[] harmful = ["reduce_max_weave", "dissolve_card"];
            for (int i = 0; i < evt.Options.Count; i++)
            {
                if (!evt.Options[i].Effects.Any(e => harmful.Contains(e.Type)))
                    return i;
            }
        }
        return 0;
    }

    private static void ApplyRestStop(RunState run, RestStopStrategy? strategy)
    {
        strategy ??= new RestStopStrategy();

        double pct = run.MaxWeave > 0 ? run.CurrentWeave * 100.0 / run.MaxWeave : 100;

        if (pct < strategy.HealThresholdPercent)
        {
            run.CurrentWeave = Math.Min(run.CurrentWeave + 3, run.MaxWeave);
        }
        else if (run.MaxWeave < strategy.PreferMaxWeaveHealBelow)
        {
            run.MaxWeave++;
        }
        // Otherwise: upgrade card — no-op in sim (card upgrade tracking is separate)
    }

    private static (EncounterState state, EncounterRunner runner)
        BuildEncounterForChain(int seed, string wardenJsonPath, BalanceConfig balance, string wardenId,
            EncounterConfig config, WardenData? cachedWardenData = null)
    {
        var random     = GameRandom.FromSeed(seed);
        var wardenData = cachedWardenData ?? WardenLoader.Load(wardenJsonPath);
        var graph      = TerritoryGraph.Create(config.BoardLayout);
        var territories = BoardState.Create(graph).Territories.Values.ToList();
        var presence   = new PresenceSystem(() => territories, balance.MaxPresencePerTerritory);
        var dread      = new DreadSystem(balance);

        IWardenAbility warden = wardenData.WardenId switch
        {
            "root"  => new RootAbility(presence, balance),
            "ember" => new EmberAbility(),
            _       => new RootAbility(presence, balance)
        };

        var gating = new PassiveGating(wardenData.WardenId);
        if (warden is RootAbility rootAbility)
            rootAbility.Gating = gating;

        var faction   = new PaleMarchFaction();
        faction.HpBonus = balance.InvaderHpBonus;

        int handLimit = wardenData.HandLimit;
        if (config.HandLimitOverride.HasValue)
            handLimit = config.HandLimitOverride.Value;

        var state = new EncounterState
        {
            Config      = config,
            Graph       = graph,
            Territories = territories,
            Elements    = new ElementSystem(balance),
            Dread       = dread,
            Weave       = new WeaveSystem(balance.StartingWeave, balance.MaxWeave),
            Combat      = new CombatSystem(),
            Presence    = presence,
            Corruption  = new CorruptionSystem(),
            FearActions = new FearActionSystem(dread, FearActionPool.Build(), random, balance),
            Warden      = warden,
            WardenData  = wardenData,
            Random      = random,
            ActionLog   = new ActionLog(),
            PassiveGating = gating,
            Balance     = balance
        };

        var startingCards = wardenData.Cards.Where(c => c.IsStarting).ToList();
        state.Deck = new DeckManager(warden, startingCards, random, handLimit, shuffle: true);

        var actionDeck = new ActionDeck(faction.BuildPainfulPool(), faction.BuildEasyPool(), random, shuffle: true);
        var cadence    = new CadenceManager(config.Cadence);
        var spawn      = new SpawnManager(config.Waves, random);
        var resolver   = new EffectResolver();

        VulnerabilityWiring.WireEvents(presence);

        var runner = new EncounterRunner(actionDeck, cadence, spawn, faction, resolver);
        return (state, runner);
    }
}

/// <summary>Run outcome for a single chain (full roguelike run).</summary>
public class ChainRunResult
{
    public int Seed { get; set; }
    public int StagesCompleted { get; set; }
    public List<EncounterResult> EncounterResults { get; set; } = new();
    public int FinalWeave { get; set; }
    public int FinalMaxWeave { get; set; }
    /// <summary>Max weave snapshot after each encounter (index 0 = after E1, 1 = after E2, etc.).</summary>
    public List<int> MaxWeaveHistory { get; set; } = new();
    public int TokensEarned { get; set; }
    public bool FullClear { get; set; }
    public bool ReachedOptionalStage { get; set; }
    public int EventsResolved { get; set; }
    public List<string> VisitedNodeIds { get; set; } = new();
}

/// <summary>Data-driven bot heuristics for between-encounter decisions.</summary>
public class BotChainConfig
{
    public List<string> DraftPriority { get; set; } = new();
    public string UpgradePriority { get; set; } = "highest_value_damage_card";
    public bool PassiveUpgradeOverUnlock { get; set; } = true;
    public string EventStrategy { get; set; } = "safe";
    public string PathStrategy { get; set; } = "balanced";
    public RestStopStrategy RestStopStrategy { get; set; } = new();

    public static BotChainConfig Default => new()
    {
        DraftPriority = new() { "DamageInvaders", "PlacePresence", "ReduceCorruption", "GenerateFear", "RestoreWeave" },
        EventStrategy = "safe",
        PathStrategy  = "balanced",
    };
}

public class RestStopStrategy
{
    public int HealThresholdPercent { get; set; } = 80;
    public int PreferMaxWeaveHealBelow { get; set; } = 16;
    public string Otherwise { get; set; } = "upgrade_card";
}
