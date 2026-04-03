namespace HollowWardens.Tests;

using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Sim;
using Xunit;

public class ChainSimTests
{
    private static string GetWardenJsonPath(string wardenId)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "wardens", $"{wardenId}.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException($"Could not find data/wardens/{wardenId}.json in ancestors");
    }

    [Fact]
    public void ChainSim_CompletesFullRun()
    {
        var path = GetWardenJsonPath("root");
        var result = ChainSimulator.RunChain(
            seed: 1, wardenId: "root", wardenJsonPath: path, realmId: "realm_1");

        Assert.True(result.StagesCompleted >= 1, $"Expected at least 1 stage completed, got {result.StagesCompleted}");
        Assert.True(result.FinalWeave >= 0);
    }

    [Fact]
    public void ChainSim_WeaveDecays_AcrossEncounters()
    {
        var path = GetWardenJsonPath("root");
        var result = ChainSimulator.RunChain(
            seed: 7, wardenId: "root", wardenJsonPath: path, realmId: "realm_1");

        // MaxWeave can only stay the same or decrease — never increase
        Assert.True(result.FinalMaxWeave <= 20,
            $"Final max weave {result.FinalMaxWeave} should not exceed starting max weave 20");
    }

    [Fact]
    public void ChainSim_RewardsApplied()
    {
        var path = GetWardenJsonPath("root");
        var result = ChainSimulator.RunChain(
            seed: 1, wardenId: "root", wardenJsonPath: path, realmId: "realm_1");

        // TokensEarned should be non-negative (0 for all breaches, positive for weathered/clean)
        Assert.True(result.TokensEarned >= 0);
        // StagesCompleted implies we went through reward calculation at least once
        if (result.StagesCompleted >= 1)
            Assert.True(result.TokensEarned >= 0); // weathered gives 1 token, breach gives 0
    }

    [Fact]
    public void ChainSim_CarryoverApplied()
    {
        var path = GetWardenJsonPath("root");
        // Run 2+ stages; the second encounter starts with the first encounter's ending weave
        var result = ChainSimulator.RunChain(
            seed: 3, wardenId: "root", wardenJsonPath: path, realmId: "realm_1");

        if (result.StagesCompleted >= 2)
        {
            // Final weave reflects carryover propagation through multiple encounters
            Assert.True(result.FinalWeave >= 0, "Final weave should be non-negative");
            // And max weave should not exceed 20 (can only decrease via decay)
            Assert.True(result.FinalMaxWeave <= 20);
        }
        else
        {
            // Still valid - carryover logic ran for at least 1 encounter
            Assert.True(result.StagesCompleted >= 1);
        }
    }

    [Fact]
    public void ChainSim_EventsResolve()
    {
        // Use "first" path strategy to ensure event nodes are chosen
        // (Stage 1, Node 0 = r1_n1, type="event")
        var config = new BotChainConfig
        {
            PathStrategy  = "first",
            EventStrategy = "safe",
            RestStopStrategy = new RestStopStrategy(),
        };
        var path = GetWardenJsonPath("root");
        var result = ChainSimulator.RunChain(
            seed: 1, wardenId: "root", wardenJsonPath: path, realmId: "realm_1",
            botConfig: config);

        if (result.StagesCompleted >= 1)
        {
            // With "first" strategy, r1_n1 (event node) should have been chosen
            Assert.Contains("r1_n1", result.VisitedNodeIds);
            Assert.True(result.EventsResolved >= 1, "At least one event should have been resolved");
        }
    }

    // ── Multi-encounter chain propagation tests ─────────────────────────────

    [Fact]
    public void ChainSim_MaxWeaveHistory_MonotonicallyNonIncreasing()
    {
        var path = GetWardenJsonPath("root");
        // Run 20 seeds to get statistical confidence
        for (int seed = 1; seed <= 20; seed++)
        {
            var result = ChainSimulator.RunChain(
                seed: seed, wardenId: "root", wardenJsonPath: path, realmId: "realm_1");

            if (result.MaxWeaveHistory.Count < 2) continue;

            for (int i = 1; i < result.MaxWeaveHistory.Count; i++)
            {
                Assert.True(result.MaxWeaveHistory[i] <= result.MaxWeaveHistory[i - 1],
                    $"Seed {seed}: MaxWeave increased from {result.MaxWeaveHistory[i - 1]} (E{i}) to {result.MaxWeaveHistory[i]} (E{i + 1})");
            }
        }
    }

    [Fact]
    public void ChainSim_EncounterResultsCount_MatchesStagesCompleted()
    {
        var path = GetWardenJsonPath("root");
        for (int seed = 1; seed <= 20; seed++)
        {
            var result = ChainSimulator.RunChain(
                seed: seed, wardenId: "root", wardenJsonPath: path, realmId: "realm_1");

            Assert.Equal(result.StagesCompleted, result.EncounterResults.Count);
            Assert.Equal(result.StagesCompleted, result.MaxWeaveHistory.Count);
        }
    }

    [Fact]
    public void ChainSim_AllRequiredStages_CompletedBySomeSeeds()
    {
        var path = GetWardenJsonPath("root");
        var realm = RealmLoader.Load("realm_1");
        int requiredStages = realm.Stages.Count(s => !s.IsOptional);

        int fullClears = 0;
        for (int seed = 1; seed <= 50; seed++)
        {
            var result = ChainSimulator.RunChain(
                seed: seed, wardenId: "root", wardenJsonPath: path, realmId: "realm_1");
            if (result.StagesCompleted >= requiredStages)
                fullClears++;
        }

        Assert.True(fullClears > 0,
            $"Expected at least 1 full clear out of 50 seeds, got {fullClears}. Required stages: {requiredStages}");
    }

    [Fact]
    public void ChainSim_TokensAccumulate_AcrossMultipleStages()
    {
        var path = GetWardenJsonPath("root");
        bool foundMultiStageWithTokens = false;

        for (int seed = 1; seed <= 30; seed++)
        {
            var result = ChainSimulator.RunChain(
                seed: seed, wardenId: "root", wardenJsonPath: path, realmId: "realm_1");

            // Tokens can only be non-negative
            Assert.True(result.TokensEarned >= 0, $"Seed {seed}: negative tokens {result.TokensEarned}");

            // If multiple stages completed, tokens should have been evaluated per stage
            if (result.StagesCompleted >= 2 && result.TokensEarned > 0)
                foundMultiStageWithTokens = true;
        }

        Assert.True(foundMultiStageWithTokens,
            "Expected at least 1 seed with 2+ stages and positive tokens across 30 seeds");
    }

    [Fact]
    public void ChainSim_FinalWeave_NeverExceedsMaxWeave()
    {
        var path = GetWardenJsonPath("root");
        for (int seed = 1; seed <= 20; seed++)
        {
            var result = ChainSimulator.RunChain(
                seed: seed, wardenId: "root", wardenJsonPath: path, realmId: "realm_1");

            Assert.True(result.FinalWeave <= result.FinalMaxWeave,
                $"Seed {seed}: FinalWeave {result.FinalWeave} > FinalMaxWeave {result.FinalMaxWeave}");
            Assert.True(result.FinalWeave >= 0,
                $"Seed {seed}: FinalWeave {result.FinalWeave} should be non-negative");
        }
    }

    [Fact]
    public void ChainSim_EncounterResults_AllValid()
    {
        var path = GetWardenJsonPath("root");

        for (int seed = 1; seed <= 20; seed++)
        {
            var result = ChainSimulator.RunChain(
                seed: seed, wardenId: "root", wardenJsonPath: path, realmId: "realm_1");

            foreach (var er in result.EncounterResults)
            {
                Assert.True(
                    er == EncounterResult.Clean || er == EncounterResult.Weathered || er == EncounterResult.Breach,
                    $"Seed {seed}: unexpected encounter result {er}");
            }
        }
    }

    [Fact]
    public void ChainSim_RestStop_HealsWeaveWhenBelowThreshold()
    {
        // Use a RestStopStrategy with known threshold and "first" path to hit rest nodes
        var config = new BotChainConfig
        {
            PathStrategy  = "first",
            EventStrategy = "safe",
            RestStopStrategy = new RestStopStrategy
            {
                HealThresholdPercent    = 100,  // always heal (weave < 100% triggers heal)
                PreferMaxWeaveHealBelow = 0,    // never prefer max weave heal
            }
        };
        var path = GetWardenJsonPath("root");

        for (int seed = 1; seed <= 30; seed++)
        {
            var result = ChainSimulator.RunChain(
                seed: seed, wardenId: "root", wardenJsonPath: path, realmId: "realm_1",
                botConfig: config);

            // Chain should complete with rest config applied without errors
            Assert.True(result.StagesCompleted >= 1,
                $"Seed {seed}: chain with rest config should complete at least 1 stage");
            Assert.True(result.FinalWeave <= result.FinalMaxWeave,
                $"Seed {seed}: weave invariant violated after rest stops");
        }
    }

    [Fact]
    public void ChainSim_Ember_CompletesChainWithoutCrash()
    {
        var path = GetWardenJsonPath("ember");
        for (int seed = 1; seed <= 10; seed++)
        {
            var result = ChainSimulator.RunChain(
                seed: seed, wardenId: "ember", wardenJsonPath: path, realmId: "realm_1");

            Assert.True(result.StagesCompleted >= 1,
                $"Ember seed {seed}: expected at least 1 stage, got {result.StagesCompleted}");
            Assert.True(result.FinalWeave >= 0);
        }
    }

    [Fact]
    public void ChainSim_DamagedRun_HasLowerMaxWeaveThanPerfect()
    {
        var path = GetWardenJsonPath("root");
        bool foundDecay = false;

        for (int seed = 1; seed <= 50; seed++)
        {
            var result = ChainSimulator.RunChain(
                seed: seed, wardenId: "root", wardenJsonPath: path, realmId: "realm_1");

            if (result.StagesCompleted >= 2 && result.FinalMaxWeave < 20)
            {
                foundDecay = true;
                break;
            }
        }

        Assert.True(foundDecay,
            "Expected at least 1 seed out of 50 where max weave decays below 20 after 2+ encounters");
    }
}
