namespace HollowWardens.Core.Events;

using System.Text.Json;

/// <summary>
/// Loads EventData from data/events/*.json and provides tag/warden filtering.
/// </summary>
public static class EventLoader
{
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Loads all event files from data/events/.</summary>
    public static List<EventData> LoadAll()
    {
        var dir = FindDataDir("events");
        if (dir == null) return new();

        return Directory.GetFiles(dir, "*.json")
            .Select(LoadOne)
            .Where(e => e != null)
            .Select(e => e!)
            .ToList();
    }

    /// <summary>
    /// Filters events by required tags and optional warden filter.
    /// ALL required tags must be present in the event's tags list.
    /// Events with warden_filter=null are universal (pass any wardenId).
    /// </summary>
    public static List<EventData> Filter(
        List<EventData> events,
        List<string>? requiredTags = null,
        string? wardenId = null)
    {
        return events.Where(e =>
        {
            if (requiredTags != null && requiredTags.Count > 0)
                if (!requiredTags.All(t => e.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
                    return false;

            if (wardenId != null && e.WardenFilter != null)
                if (!e.WardenFilter.Equals(wardenId, StringComparison.OrdinalIgnoreCase))
                    return false;

            return true;
        }).ToList();
    }

    private static EventData? LoadOne(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<EventData>(json, _opts);
        }
        catch { return null; }
    }

    private static string? FindDataDir(string subdir)
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "data", subdir);
            if (Directory.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }
}
