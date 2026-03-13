using System.Collections.Generic;

// Hardcoded 3×3 territory grid with 4-directional adjacency and BFS pathfinding.
// Row 0 (entry):  E1 - E2 - E3
// Row 1 (middle): M1 - M2 - M3
// Row 2 (inner):  S1 - SS - S2  (SS = Sacred Site)
public class TerritoryGraph
{
    private static readonly Dictionary<string, List<string>> _adjacency = new()
    {
        ["E1"] = new List<string> { "E2", "M1" },
        ["E2"] = new List<string> { "E1", "E3", "M2" },
        ["E3"] = new List<string> { "E2", "M3" },
        ["M1"] = new List<string> { "E1", "M2", "S1" },
        ["M2"] = new List<string> { "E2", "M1", "M3", "SS" },
        ["M3"] = new List<string> { "E3", "M2", "S2" },
        ["S1"] = new List<string> { "M1", "SS" },
        ["SS"] = new List<string> { "M2", "S1", "S2" },
        ["S2"] = new List<string> { "M3", "SS" },
    };

    public IReadOnlyCollection<string> AllIds => _adjacency.Keys;

    public IReadOnlyList<string> GetNeighbors(string id) =>
        _adjacency.TryGetValue(id, out var neighbors) ? neighbors : System.Array.Empty<string>();

    // Multi-target BFS. Returns path[1] (first step toward nearest target), or null if
    // fromId is already in targetIds or no path exists.
    public string? NextStepToward(string fromId, IEnumerable<string> targetIds)
    {
        var targets = new HashSet<string>(targetIds);
        if (targets.Contains(fromId)) return null;
        if (!_adjacency.ContainsKey(fromId)) return null;

        var prev = new Dictionary<string, string> { [fromId] = fromId };
        var queue = new Queue<string>();
        queue.Enqueue(fromId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in _adjacency[current])
            {
                if (prev.ContainsKey(neighbor)) continue;
                prev[neighbor] = current;
                if (targets.Contains(neighbor))
                {
                    // Reconstruct: walk back to find the node whose parent is fromId
                    var node = neighbor;
                    while (prev[node] != fromId)
                        node = prev[node];
                    return node;
                }
                queue.Enqueue(neighbor);
            }
        }
        return null;
    }
}
