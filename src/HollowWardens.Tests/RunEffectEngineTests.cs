namespace HollowWardens.Tests;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Run;
using Xunit;

public class RunEffectEngineTests
{
    private static RunState MakeRun(int current = 15, int max = 20) => new()
    {
        CurrentWeave = current,
        MaxWeave = max,
        UpgradeTokens = 0,
        DeckCardIds = new() { "card_a", "card_b", "card_c" },
        PermanentlyRemovedCardIds = new(),
        CorruptionCarryover = new() { ["A1"] = 1, ["M1"] = 0 },
        PermanentlyUnlockedPassives = new(),
        AppliedPassiveUpgradeIds = new(),
    };

    private static readonly Random _rng = new(42);

    [Fact]
    public void HealWeave_ClampsAtMax()
    {
        var run = MakeRun(current: 18, max: 20);
        RunEffectEngine.Apply(run, new RunEffect { Type = "heal_weave", Value = 5 }, _rng);
        Assert.Equal(20, run.CurrentWeave);
    }

    [Fact]
    public void HealMaxWeave_Increases()
    {
        var run = MakeRun(max: 20);
        RunEffectEngine.Apply(run, new RunEffect { Type = "heal_max_weave", Value = 3 }, _rng);
        Assert.Equal(23, run.MaxWeave);
    }

    [Fact]
    public void ReduceMaxWeave_ClampsAt1()
    {
        var run = MakeRun(current: 1, max: 1);
        RunEffectEngine.Apply(run, new RunEffect { Type = "reduce_max_weave", Value = 5 }, _rng);
        Assert.Equal(1, run.MaxWeave);
    }

    [Fact]
    public void AddTokens_Increases()
    {
        var run = MakeRun();
        RunEffectEngine.Apply(run, new RunEffect { Type = "add_tokens", Value = 2 }, _rng);
        Assert.Equal(2, run.UpgradeTokens);
    }

    [Fact]
    public void CleansCarryover_ClearsDict()
    {
        var run = MakeRun();
        run.CorruptionCarryover["A1"] = 3;
        RunEffectEngine.Apply(run, new RunEffect { Type = "cleanse_carryover" }, _rng);
        Assert.Empty(run.CorruptionCarryover);
    }

    [Fact]
    public void UnlockPassive_AddsToList()
    {
        var run = MakeRun();
        RunEffectEngine.Apply(run, new RunEffect { Type = "unlock_passive", TargetId = "dormancy" }, _rng);
        Assert.Contains("dormancy", run.PermanentlyUnlockedPassives);
    }

    [Fact]
    public void DissolveCard_RemovesFromDeck()
    {
        var run = MakeRun();
        int before = run.DeckCardIds.Count;
        RunEffectEngine.Apply(run, new RunEffect { Type = "dissolve_card", Value = 1 }, _rng);
        Assert.Equal(before - 1, run.DeckCardIds.Count);
        Assert.Equal(1, run.PermanentlyRemovedCardIds.Count);
    }

    [Fact]
    public void AddCorruption_RandomTerritory_Works()
    {
        var run = MakeRun();
        run.CorruptionCarryover["A1"] = 0;
        int before = run.CorruptionCarryover.Values.Sum();
        RunEffectEngine.Apply(run, new RunEffect
        {
            Type = "add_corruption",
            Value = 2,
            Territories = new() { "random" }
        }, _rng);
        int after = run.CorruptionCarryover.Values.Sum();
        Assert.Equal(before + 2, after);
    }

    [Fact]
    public void RecoverCard_MovesFromRemovedToDeck()
    {
        var run = MakeRun();
        run.DeckCardIds.Remove("card_b");
        run.PermanentlyRemovedCardIds.Add("card_b");

        RunEffectEngine.Apply(run, new RunEffect { Type = "recover_card", TargetId = "card_b" }, _rng);

        Assert.Contains("card_b", run.DeckCardIds);
        Assert.DoesNotContain("card_b", run.PermanentlyRemovedCardIds);
    }
}
