namespace HollowWardens.Core.Run;

using System.Text.Json;
using System.Text.Json.Serialization;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

/// <summary>
/// Roguelike reward tier calculator.
/// Reads tier config from EncounterConfig.RewardTiers, computes which tier (1/2/3)
/// the player achieved, and returns reward parameters from data/rewards/reward_tiers.json.
/// No per-encounter logic — all defined in JSON.
/// </summary>
public static class RunRewardCalculator
{
    private static RewardTierDefinitions? _tierDefs;

    public static RewardResult Calculate(
        EncounterResult result,
        int currentWeave,
        int maxWeave,
        string wardenId,
        EncounterConfig config)
    {
        EnsureTierDefsLoaded();

        // Determine which tier was achieved
        int tier = ComputeTier(result, currentWeave, maxWeave, wardenId, config);

        // Read tier parameters
        var tierKey = $"tier{tier}";
        if (_tierDefs == null || !_tierDefs.TryGetValue(tierKey, out var def))
            def = new RewardTierDefinition { DraftChoices = 2, UpgradeTokens = 0 };

        return new RewardResult
        {
            RewardTier    = tier,
            DraftChoices  = def.DraftChoices,
            UpgradeTokens = def.UpgradeTokens,
            CanRemoveCard = def.CanRemoveCard,
            CanChooseHeal = def.CanChooseHeal,
            DraftPoolTag  = def.DraftPoolTag
        };
    }

    private static int ComputeTier(
        EncounterResult result,
        int currentWeave,
        int maxWeave,
        string wardenId,
        EncounterConfig config)
    {
        // Default if no per-warden config
        var tierConfig = config.RewardTiers?.GetValueOrDefault(wardenId)
            ?? new RewardTierConfig { Tier1MinResult = "clean", Tier2MinResult = "weathered" };

        int weavePercent = maxWeave > 0 ? (int)((currentWeave * 100.0) / maxWeave) : 0;
        string resultStr = result.ToString().ToLowerInvariant();

        // Try Tier 1
        if (MeetsThreshold(resultStr, weavePercent, tierConfig.Tier1MinResult, tierConfig.Tier1MinWeavePercent))
            return 1;

        // Try Tier 2
        if (MeetsThreshold(resultStr, weavePercent, tierConfig.Tier2MinResult, tierConfig.Tier2MinWeavePercent))
            return 2;

        return 3;
    }

    private static bool MeetsThreshold(
        string resultStr, int weavePercent,
        string minResult, int? minWeavePercent)
    {
        // Result ordering: clean > weathered > breach
        if (!ResultMeets(resultStr, minResult)) return false;
        if (minWeavePercent.HasValue && weavePercent < minWeavePercent.Value) return false;
        return true;
    }

    private static bool ResultMeets(string result, string minResult) => (result, minResult) switch
    {
        ("clean", "clean")     => true,
        ("clean", "weathered") => true,
        ("clean", "breach")    => true,
        ("weathered", "weathered") => true,
        ("weathered", "breach")    => true,
        ("breach", "breach")       => true,
        _ => false
    };

    private static void EnsureTierDefsLoaded()
    {
        if (_tierDefs != null) return;

        var path = FindDataPath("rewards", "reward_tiers.json");
        if (path == null)
        {
            _tierDefs = new RewardTierDefinitions();
            return;
        }

        var json = File.ReadAllText(path);
        _tierDefs = JsonSerializer.Deserialize<RewardTierDefinitions>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new RewardTierDefinitions();
    }

    private static string? FindDataPath(string subdir, string filename)
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "data", subdir, filename);
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    // JSON model
    private class RewardTierDefinitions : Dictionary<string, RewardTierDefinition> { }

    private class RewardTierDefinition
    {
        [JsonPropertyName("draft_choices")]  public int DraftChoices  { get; set; } = 2;
        [JsonPropertyName("upgrade_tokens")] public int UpgradeTokens { get; set; } = 0;
        [JsonPropertyName("can_remove_card")] public bool CanRemoveCard { get; set; } = false;
        [JsonPropertyName("can_choose_heal")] public bool CanChooseHeal { get; set; } = false;
        [JsonPropertyName("draft_pool_tag")] public string DraftPoolTag { get; set; } = "tier3";
    }
}

/// <summary>Result returned by RunRewardCalculator.Calculate.</summary>
public class RewardResult
{
    public int    RewardTier    { get; set; }
    public int    DraftChoices  { get; set; }
    public int    UpgradeTokens { get; set; }
    public bool   CanRemoveCard { get; set; }
    public bool   CanChooseHeal { get; set; }
    public string DraftPoolTag  { get; set; } = "";
}
