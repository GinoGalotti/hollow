namespace HollowWardens.Core.Map;

using HollowWardens.Core.Models;

public class BoardState
{
    private readonly Dictionary<string, Territory> _territories;
    private readonly TerritoryGraph _graph;

    private BoardState(Dictionary<string, Territory> territories, TerritoryGraph graph)
    {
        _territories = territories;
        _graph       = graph;
    }

    public IReadOnlyDictionary<string, Territory> Territories => _territories;
    public TerritoryGraph Graph => _graph;

    public Territory Get(string id) => _territories[id];

    public IEnumerable<Territory> GetByRow(TerritoryRow row)
        => _territories.Values.Where(t => t.Row == row);

    public IEnumerable<Territory> GetInRange(string fromId, int range)
        => _graph.AllTerritoryIds
            .Where(id => _graph.Distance(fromId, id) <= range && _territories.ContainsKey(id))
            .Select(id => _territories[id]);

    /// <summary>Create a board from any layout graph.</summary>
    public static BoardState Create(TerritoryGraph graph)
    {
        var territories = graph.AllTerritoryIds
            .Select(id => new Territory
            {
                Id           = id,
                Row          = TerritoryGraph.GetRow(id),
                IsEntryPoint = TerritoryGraph.GetRow(id) == TerritoryRow.Arrival
            })
            .ToDictionary(t => t.Id);
        return new BoardState(territories, graph);
    }

    /// <summary>Standard 3-2-1 pyramid — backward-compat shorthand.</summary>
    public static BoardState CreatePyramid() => Create(TerritoryGraph.Standard);
}
