namespace HollowWardens.Core.Run;

using System.Text.Json;
using HollowWardens.Core.Models;

/// <summary>
/// Filters a warden's card pool by the rarity tags defined in data/rewards/draft_pools.json
/// for the current tier and stage.
/// </summary>
public static class DraftPool
{
    private static Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>? _pools;

    /// <summary>
    /// Returns all non-starting cards for <paramref name="wardenId"/> whose rarity
    /// matches the allowed rarities for the given tier/stage combination.
    /// </summary>
    public static List<Card> GetPool(
        List<Card> allCards,
        string wardenId,
        string poolTag,
        int stage)
    {
        EnsurePoolsLoaded();

        var stageKey = $"stage_{stage}";
        var allowedRarities = GetAllowedRarities(poolTag, wardenId, stageKey);

        return allCards
            .Where(c => !c.IsStarting && allowedRarities.Contains(RarityTag(c.Rarity)))
            .ToList();
    }

    private static HashSet<string> GetAllowedRarities(string poolTag, string wardenId, string stageKey)
    {
        if (_pools == null) return new HashSet<string> { "dormant" };

        if (_pools.TryGetValue(poolTag, out var byWarden)
            && byWarden.TryGetValue(wardenId, out var byStage)
            && byStage.TryGetValue(stageKey, out var rarities))
        {
            return new HashSet<string>(rarities, StringComparer.OrdinalIgnoreCase);
        }

        // Fallback: dormant only
        return new HashSet<string> { "dormant" };
    }

    private static string RarityTag(CardRarity rarity) => rarity switch
    {
        CardRarity.Dormant  => "dormant",
        CardRarity.Awakened => "awakened",
        CardRarity.Ancient  => "ancient",
        _                   => "dormant"
    };

    private static void EnsurePoolsLoaded()
    {
        if (_pools != null) return;

        var path = FindDataPath("rewards", "draft_pools.json");
        if (path == null)
        {
            _pools = new();
            return;
        }

        var json = File.ReadAllText(path);
        _pools = JsonSerializer.Deserialize<
            Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new();
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
}
