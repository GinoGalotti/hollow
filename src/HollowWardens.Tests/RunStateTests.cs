namespace HollowWardens.Tests;

using HollowWardens.Core.Run;
using Xunit;

public class RunStateTests
{
    [Fact]
    public void NewRunState_HasDefaults()
    {
        var run = new RunState();
        Assert.Equal("root", run.WardenId);
        Assert.Equal("realm_1", run.RealmId);
        Assert.Equal(20, run.MaxWeave);
        Assert.Equal(20, run.CurrentWeave);
        Assert.Equal(1, run.DreadLevel);
        Assert.Equal(0, run.UpgradeTokens);
        Assert.Empty(run.DeckCardIds);
        Assert.Empty(run.VisitedNodeIds);
        Assert.Empty(run.CorruptionCarryover);
    }

    [Fact]
    public void Clone_IsIndependent()
    {
        var run = new RunState { WardenId = "ember", MaxWeave = 18 };
        run.DeckCardIds.Add("card_1");
        run.CorruptionCarryover["A1"] = 3;

        var clone = run.Clone();

        // Mutate original after cloning
        run.MaxWeave = 15;
        run.DeckCardIds.Add("card_2");
        run.CorruptionCarryover["A2"] = 5;

        Assert.Equal(18, clone.MaxWeave);
        Assert.Single(clone.DeckCardIds);
        Assert.DoesNotContain("A2", clone.CorruptionCarryover.Keys);
    }

    [Fact]
    public void Clone_CopiesAllFields()
    {
        var run = new RunState
        {
            WardenId           = "ember",
            RealmId            = "realm_2",
            Seed               = 42,
            CurrentNodeIndex   = 3,
            MaxWeave           = 18,
            CurrentWeave       = 15,
            DreadLevel         = 2,
            TotalFearGenerated = 30,
            UpgradeTokens      = 2
        };
        run.VisitedNodeIds.Add("n1");
        run.CompletedEncounterIds.Add("pale_march_standard");
        run.EncounterResults.Add("Clean");
        run.DeckCardIds.Add("root_001");
        run.PermanentlyRemovedCardIds.Add("root_002");
        run.AppliedCardUpgradeIds.Add("root_001_u1");
        run.AppliedPassiveUpgradeIds.Add("network_fear_u1");
        run.PermanentlyUnlockedPassives.Add("rest_growth");
        run.CorruptionCarryover["A1"] = 3;

        var clone = run.Clone();

        Assert.Equal("ember", clone.WardenId);
        Assert.Equal("realm_2", clone.RealmId);
        Assert.Equal(42, clone.Seed);
        Assert.Equal(3, clone.CurrentNodeIndex);
        Assert.Equal(18, clone.MaxWeave);
        Assert.Equal(15, clone.CurrentWeave);
        Assert.Equal(2, clone.DreadLevel);
        Assert.Equal(30, clone.TotalFearGenerated);
        Assert.Equal(2, clone.UpgradeTokens);
        Assert.Contains("n1", clone.VisitedNodeIds);
        Assert.Contains("pale_march_standard", clone.CompletedEncounterIds);
        Assert.Contains("Clean", clone.EncounterResults);
        Assert.Contains("root_001", clone.DeckCardIds);
        Assert.Contains("root_002", clone.PermanentlyRemovedCardIds);
        Assert.Contains("root_001_u1", clone.AppliedCardUpgradeIds);
        Assert.Contains("network_fear_u1", clone.AppliedPassiveUpgradeIds);
        Assert.Contains("rest_growth", clone.PermanentlyUnlockedPassives);
        Assert.Equal(3, clone.CorruptionCarryover["A1"]);
    }
}
