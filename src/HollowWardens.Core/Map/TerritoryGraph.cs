namespace HollowWardens.Core.Map;

using HollowWardens.Core.Models;

public static class TerritoryGraph
{
    private static readonly Dictionary<string, List<string>> Adjacency = new()
    {
        ["A1"] = new() { "A2", "M1" },
        ["A2"] = new() { "A1", "A3", "M1", "M2" },
        ["A3"] = new() { "A2", "M2" },
        ["M1"] = new() { "A1", "A2", "M2", "I1" },
        ["M2"] = new() { "A2", "A3", "M1", "I1" },
        ["I1"] = new() { "M1", "M2" },
    };

    public static readonly string[] AllTerritoryIds = { "A1", "A2", "A3", "M1", "M2", "I1" };

    public static IReadOnlyList<string> GetNeighbors(string id)
        => Adjacency.TryGetValue(id, out var n) ? n : new List<string>();

    public static bool IsAdjacent(string a, string b)
        => Adjacency.TryGetValue(a, out var n) && n.Contains(b);

    public static bool CanAttackHeart(string id) => id == "I1";

    public static TerritoryRow GetRow(string id) => id[0] switch
    {
        'A' => TerritoryRow.Arrival,
        'M' => TerritoryRow.Middle,
        'I' => TerritoryRow.Inner,
        _ => throw new ArgumentException($"Unknown territory: {id}")
    };

    public static int Distance(string from, string to)
    {
        if (from == to) return 0;
        // BFS
        var visited = new HashSet<string> { from };
        var queue = new Queue<(string Id, int Dist)>();
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
}
