namespace HollowWardens.Core.Map;

using HollowWardens.Core.Models;

/// <summary>
/// Instance-based board graph. Use TerritoryGraph.Create(layoutId) to get a layout.
/// Standard (3-2-1), Wide (4-3-2-1), Narrow (2-1-1), TwinPeaks (3-2-2-1).
/// </summary>
public class TerritoryGraph
{
    private readonly Dictionary<string, List<string>> _adjacency;

    public string HeartId { get; }
    public string[] AllTerritoryIds { get; }
    public string[] ArrivalIds { get; }

    private TerritoryGraph(Dictionary<string, List<string>> adjacency, string heartId)
    {
        _adjacency      = adjacency;
        HeartId         = heartId;
        AllTerritoryIds = adjacency.Keys.OrderBy(k => k).ToArray();
        ArrivalIds      = AllTerritoryIds.Where(id => GetRow(id) == TerritoryRow.Arrival).ToArray();
    }

    public IReadOnlyList<string> GetNeighbors(string id)
        => _adjacency.TryGetValue(id, out var n) ? n : new List<string>();

    public bool IsAdjacent(string a, string b)
        => _adjacency.TryGetValue(a, out var n) && n.Contains(b);

    public bool CanAttackHeart(string id)
        => GetNeighbors(id).Contains(HeartId) || id == HeartId;

    /// <summary>Derives territory row from its ID prefix character.</summary>
    public static TerritoryRow GetRow(string id) => id[0] switch
    {
        'A' => TerritoryRow.Arrival,
        'M' => TerritoryRow.Middle,
        'B' => TerritoryRow.Bridge,
        'I' => TerritoryRow.Inner,
        _ => throw new ArgumentException($"Unknown territory: {id}")
    };

    public int Distance(string from, string to)
    {
        if (from == to) return 0;
        var visited = new HashSet<string> { from };
        var queue   = new Queue<(string Id, int Dist)>();
        queue.Enqueue((from, 0));
        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            foreach (var neighbor in GetNeighbors(current))
            {
                if (neighbor == to) return dist + 1;
                if (visited.Add(neighbor))
                    queue.Enqueue((neighbor, dist + 1));
            }
        }
        return int.MaxValue;
    }

    // ── Static factory ─────────────────────────────────────────────────────────

    public static TerritoryGraph Create(string layoutId) => layoutId switch
    {
        "standard"   => Standard,
        "wide"       => Wide,
        "narrow"     => Narrow,
        "twin_peaks" => TwinPeaks,
        _            => Standard
    };

    // ── Singleton layouts ──────────────────────────────────────────────────────

    public static readonly TerritoryGraph Standard  = CreateStandard();
    public static readonly TerritoryGraph Wide      = CreateWide();
    public static readonly TerritoryGraph Narrow    = CreateNarrow();
    public static readonly TerritoryGraph TwinPeaks = CreateTwinPeaks();

    // ── Layout creators ────────────────────────────────────────────────────────

    /// <summary>Standard 3-2-1 pyramid (6 territories).</summary>
    private static TerritoryGraph CreateStandard() => new(new Dictionary<string, List<string>>
    {
        ["A1"] = new() { "A2", "M1" },
        ["A2"] = new() { "A1", "A3", "M1", "M2" },
        ["A3"] = new() { "A2", "M2" },
        ["M1"] = new() { "A1", "A2", "M2", "I1" },
        ["M2"] = new() { "A2", "A3", "M1", "I1" },
        ["I1"] = new() { "M1", "M2" },
    }, "I1");

    /// <summary>Wide 4-3-2-1 (10 territories): A1-A4, M1-M3, B1-B2, I1.</summary>
    private static TerritoryGraph CreateWide() => new(new Dictionary<string, List<string>>
    {
        ["A1"] = new() { "A2", "M1" },
        ["A2"] = new() { "A1", "A3", "M1", "M2" },
        ["A3"] = new() { "A2", "A4", "M2", "M3" },
        ["A4"] = new() { "A3", "M3" },
        ["M1"] = new() { "A1", "A2", "B1" },
        ["M2"] = new() { "A2", "A3", "B1", "B2" },
        ["M3"] = new() { "A3", "A4", "B2" },
        ["B1"] = new() { "M1", "M2", "I1" },
        ["B2"] = new() { "M2", "M3", "I1" },
        ["I1"] = new() { "B1", "B2" },
    }, "I1");

    /// <summary>Narrow 2-1-1 (4 territories): A1, A2, M1, I1.</summary>
    private static TerritoryGraph CreateNarrow() => new(new Dictionary<string, List<string>>
    {
        ["A1"] = new() { "A2", "M1" },
        ["A2"] = new() { "A1", "M1" },
        ["M1"] = new() { "A1", "A2", "I1" },
        ["I1"] = new() { "M1" },
    }, "I1");

    /// <summary>
    /// TwinPeaks 3-2-2-1 (8 territories): A1-A3, M1-M2, B1-B2, I1.
    /// M1 and M2 are NOT adjacent — two separate paths.
    /// </summary>
    private static TerritoryGraph CreateTwinPeaks() => new(new Dictionary<string, List<string>>
    {
        ["A1"] = new() { "A2", "M1" },
        ["A2"] = new() { "A1", "A3", "M1", "M2" },
        ["A3"] = new() { "A2", "M2" },
        ["M1"] = new() { "A1", "A2", "B1" },   // M1↔M2 NOT adjacent
        ["M2"] = new() { "A2", "A3", "B2" },   // M1↔M2 NOT adjacent
        ["B1"] = new() { "M1", "I1" },
        ["B2"] = new() { "M2", "I1" },
        ["I1"] = new() { "B1", "B2" },
    }, "I1");
}
