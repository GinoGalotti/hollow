namespace HollowWardens.Tests;

using HollowWardens.Core.Data;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using Xunit;

public class RewardTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static EncounterConfig StandardConfig() => EncounterLoader.CreatePaleMarchStandard();
    private static EncounterConfig SiegeConfig()    => EncounterLoader.CreatePaleMarchSiege();

    // ── Reward Tier Tests ─────────────────────────────────────────────────────

    [Fact]
    public void Root_Standard_Clean_GetsTier1()
    {
        var result = RunRewardCalculator.Calculate(
            EncounterResult.Clean, currentWeave: 18, maxWeave: 20,
            wardenId: "root", config: StandardConfig());

        Assert.Equal(1, result.RewardTier);
    }

    [Fact]
    public void Root_Standard_Weathered_GetsTier2()
    {
        var result = RunRewardCalculator.Calculate(
            EncounterResult.Weathered, currentWeave: 10, maxWeave: 20,
            wardenId: "root", config: StandardConfig());

        Assert.Equal(2, result.RewardTier);
    }

    [Fact]
    public void Root_Standard_Breach_GetsTier3()
    {
        var result = RunRewardCalculator.Calculate(
            EncounterResult.Breach, currentWeave: 5, maxWeave: 20,
            wardenId: "root", config: StandardConfig());

        Assert.Equal(3, result.RewardTier);
    }

    [Fact]
    public void Ember_Siege_Weathered_HighWeave_GetsTier1()
    {
        // Siege ember: Tier1MinResult="weathered", Tier1MinWeavePercent=60
        // 14/20 = 70% >= 60 → should be tier 1
        var result = RunRewardCalculator.Calculate(
            EncounterResult.Weathered, currentWeave: 14, maxWeave: 20,
            wardenId: "ember", config: SiegeConfig());

        Assert.Equal(1, result.RewardTier);
    }

    [Fact]
    public void Tier1_Gets3Choices_1Token_CardRemoval()
    {
        var result = RunRewardCalculator.Calculate(
            EncounterResult.Clean, currentWeave: 20, maxWeave: 20,
            wardenId: "root", config: StandardConfig());

        Assert.Equal(1, result.RewardTier);
        Assert.Equal(3, result.DraftChoices);
        Assert.Equal(1, result.UpgradeTokens);
        Assert.True(result.CanRemoveCard);
    }

    [Fact]
    public void Tier3_Gets2Choices_NoToken_HealOption()
    {
        var result = RunRewardCalculator.Calculate(
            EncounterResult.Breach, currentWeave: 2, maxWeave: 20,
            wardenId: "root", config: StandardConfig());

        Assert.Equal(3, result.RewardTier);
        Assert.Equal(2, result.DraftChoices);
        Assert.Equal(0, result.UpgradeTokens);
        Assert.True(result.CanChooseHeal);
    }

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
    public void DraftPool_FiltersCorrectRarities()
    {
        // Load root cards and check pool filtering
        var rootJsonPath = GetWardenJsonPath("root");
        var cards = WardenLoader.LoadCards(rootJsonPath);

        // Tier 1, stage 1 allows dormant + awakened (not ancient)
        var pool = DraftPool.GetPool(cards, wardenId: "root", poolTag: "tier1", stage: 1);

        Assert.All(pool, c => Assert.True(
            c.Rarity == CardRarity.Dormant || c.Rarity == CardRarity.Awakened,
            $"Card {c.Id} has rarity {c.Rarity} which is not in tier1/stage_1 pool"));

        // No starting cards
        Assert.All(pool, c => Assert.False(c.IsStarting));

        // Tier 3, stage 1 allows dormant only
        var restrictedPool = DraftPool.GetPool(cards, wardenId: "root", poolTag: "tier3", stage: 1);
        Assert.All(restrictedPool, c => Assert.Equal(CardRarity.Dormant, c.Rarity));
    }
}
