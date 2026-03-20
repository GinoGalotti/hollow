namespace HollowWardens.Core.Map;

using HollowWardens.Core.Models;

public class BoardState
{
    private readonly Dictionary<string, Territory> _territories;

    private BoardState(Dictionary<string, Territory> territories)
    {
        _territories = territories;
    }

    public IReadOnlyDictionary<string, Territory> Territories => _territories;

    public Territory Get(string id) => _territories[id];

    public IEnumerable<Territory> GetByRow(TerritoryRow row)
        => _territories.Values.Where(t => t.Row == row);

    public IEnumerable<Territory> GetInRange(string fromId, int range)
        => TerritoryGraph.AllTerritoryIds
            .Where(id => TerritoryGraph.Distance(fromId, id) <= range && _territories.ContainsKey(id))
            .Select(id => _territories[id]);

    public static BoardState CreatePyramid()
    {
        var territories = TerritoryGraph.AllTerritoryIds
            .Select(id => new Territory
            {
                Id = id,
                Row = TerritoryGraph.GetRow(id),
                IsEntryPoint = TerritoryGraph.GetRow(id) == TerritoryRow.Arrival
            })
            .ToDictionary(t => t.Id);
        return new BoardState(territories);
    }
}
