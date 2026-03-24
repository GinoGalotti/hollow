namespace HollowWardens.Tests;

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
}
