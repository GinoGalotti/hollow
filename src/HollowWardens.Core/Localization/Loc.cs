namespace HollowWardens.Core.Localization;

/// <summary>
/// Simple key-value localization. Loads from CSV. Falls back to key if missing.
/// </summary>
public static class Loc
{
    private static Dictionary<string, string> _strings = new();
    private static string _currentLocale = "en";

    /// <summary>
    /// Load strings from a CSV file. Format: KEY,en,es,fr,...
    /// First row is header. Each subsequent row: KEY,English,Spanish,French,...
    /// </summary>
    public static void Load(string csvPath, string locale = "en")
    {
        _currentLocale = locale;
        _strings.Clear();

        if (!File.Exists(csvPath)) return;

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length == 0) return;

        // Parse header to find locale column index
        var headers = ParseCsvLine(lines[0]);
        int localeIndex = Array.IndexOf(headers, locale);
        if (localeIndex < 0) localeIndex = 1; // default to first locale (en)

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = ParseCsvLine(lines[i]);
            if (cols.Length <= localeIndex) continue;
            var key = cols[0].Trim();
            var value = cols[localeIndex].Trim().Replace("\\n", "\n");
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                _strings[key] = value;
        }
    }

    /// <summary>
    /// Get a localized string. Returns the key itself if not found (fail visible).
    /// </summary>
    public static string Get(string key) =>
        _strings.TryGetValue(key, out var value) ? value : key;

    /// <summary>
    /// Get with string.Format arguments. E.g., Loc.Get("PHASE_TIDE", tideNum, totalTides)
    /// </summary>
    public static string Get(string key, params object[] args)
    {
        var template = Get(key);
        try { return string.Format(template, args); }
        catch { return template; }
    }

    /// <summary>Check if a key exists.</summary>
    public static bool Has(string key) => _strings.ContainsKey(key);

    /// <summary>Current locale.</summary>
    public static string CurrentLocale => _currentLocale;

    /// <summary>Total loaded strings.</summary>
    public static int Count => _strings.Count;

    /// <summary>Load from a dictionary (for testing).</summary>
    public static void LoadFromDict(Dictionary<string, string> strings)
    {
        _strings = new Dictionary<string, string>(strings);
    }

    /// <summary>Clear all loaded strings.</summary>
    public static void Clear()
    {
        _strings.Clear();
    }

    private static string[] ParseCsvLine(string line)
    {
        // Simple CSV parse — handles quoted fields with commas
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }
            current.Append(c);
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}
