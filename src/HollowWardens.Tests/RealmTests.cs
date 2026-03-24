namespace HollowWardens.Tests;

using HollowWardens.Core.Run;
using Xunit;

public class RealmTests
{
    private static RealmData LoadRealm() => RealmLoader.Load("realm_1");

    [Fact]
    public void RealmLoader_LoadsRealm1()
    {
        var realm = LoadRealm();
        Assert.Equal("realm_1", realm.Id);
        Assert.Equal(4, realm.Stages.Count);
    }

    [Fact]
    public void RealmRunner_StartsAtStage1()
    {
        var realm = LoadRealm();
        var run = new RunState { CurrentNodeIndex = 0 };
        var runner = new RealmRunner(run, realm);

        var node = runner.GetCurrentNode();
        Assert.Equal("encounter", node.Type);
        Assert.Equal("pale_march_standard", node.EncounterId);
    }

    [Fact]
    public void RealmRunner_AvailableNodes_ReturnsPathOptions()
    {
        var realm = LoadRealm();
        var run = new RunState { CurrentNodeIndex = 0 };
        var runner = new RealmRunner(run, realm);

        var nodes = runner.GetAvailableNextNodes();
        // Stage 1 has 3 post-encounter nodes: r1_n1, r1_n2, r1_n3
        Assert.Equal(3, nodes.Count);
        Assert.Contains(nodes, n => n.Id == "r1_n1");
        Assert.Contains(nodes, n => n.Id == "r1_n2");
        Assert.Contains(nodes, n => n.Id == "r1_n3");
    }

    [Fact]
    public void RealmRunner_AdvanceNode_UpdatesRunState()
    {
        var realm = LoadRealm();
        var run = new RunState { CurrentNodeIndex = 0 };
        var runner = new RealmRunner(run, realm);

        runner.AdvanceToNode("r1_n1");

        Assert.Contains("r1_n1", run.VisitedNodeIds);
        Assert.Equal(1, run.CurrentNodeIndex);
    }

    [Fact]
    public void RealmRunner_IsComplete_AfterFinalStage()
    {
        var realm = LoadRealm();
        var run = new RunState { CurrentNodeIndex = 4 }; // past all 4 stages
        var runner = new RealmRunner(run, realm);

        Assert.True(runner.IsRunComplete());
    }

    [Fact]
    public void DrawEvent_FiltersByTags_AndWarden()
    {
        var realm = LoadRealm();
        var run = new RunState { WardenId = "root", CurrentNodeIndex = 0 };
        var runner = new RealmRunner(run, realm, new Random(42));

        // r1_n1 is type=event with tags=["stage_1"]
        var node = realm.Stages[0].PostEncounterNodes.First(n => n.Type == "event");
        var evt = runner.DrawEventForNode(node);

        Assert.NotNull(evt);
        // Should be ember-exclusive events
        Assert.True(evt.WardenFilter == null || evt.WardenFilter.Equals("root", StringComparison.OrdinalIgnoreCase));
        // Should have at least one of the node's tags
        if (node.Tags.Count > 0)
            Assert.True(node.Tags.Any(t => evt.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)));
    }

    [Fact]
    public void OptionalStage_RequiresTier1()
    {
        var realm = LoadRealm();

        // At CurrentNodeIndex=3 (about to enter optional stage 4)
        // Without tier 1 (breach) → run is complete, can't enter stage 4
        var runBreach = new RunState { CurrentNodeIndex = 3, EncounterResults = new() { "breach" } };
        var runnerBreach = new RealmRunner(runBreach, realm);
        Assert.True(runnerBreach.IsRunComplete());

        // With tier 1 (clean) → stage 4 is accessible, run NOT complete yet
        var runClean = new RunState { CurrentNodeIndex = 3, EncounterResults = new() { "clean" } };
        var runnerClean = new RealmRunner(runClean, realm);
        Assert.False(runnerClean.IsRunComplete());
    }
}
