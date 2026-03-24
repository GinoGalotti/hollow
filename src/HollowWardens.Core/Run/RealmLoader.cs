namespace HollowWardens.Core.Run;

using System.Text.Json;

/// <summary>
/// Loads RealmData from data/realms/{realmId}.json.
/// </summary>
public static class RealmLoader
{
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public static RealmData Load(string realmId)
    {
        var path = FindDataPath("realms", $"{realmId}.json");
        if (path == null)
            throw new FileNotFoundException($"Realm data file not found: {realmId}");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<RealmData>(json, _opts)
            ?? throw new InvalidDataException($"Failed to parse realm file: {realmId}");
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
