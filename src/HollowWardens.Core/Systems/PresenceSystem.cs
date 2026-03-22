namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Map;
using HollowWardens.Core.Models;

public class PresenceSystem : IPresenceSystem
{
    private readonly Func<IEnumerable<Territory>> _territoriesProvider;
    /// <summary>Default max presence; kept as a public constant for test compatibility.</summary>
    public const int MaxPresencePerTerritory = 3;

    private readonly int _maxPresence;

    public PresenceSystem(Func<IEnumerable<Territory>> territoriesProvider, int maxPresencePerTerritory = MaxPresencePerTerritory)
    {
        _territoriesProvider = territoriesProvider;
        _maxPresence = maxPresencePerTerritory;
    }

    public void PlacePresence(Territory territory, int count = 1)
        => territory.PresenceCount = Math.Min(territory.PresenceCount + count, _maxPresence);

    public void RemovePresence(Territory territory, int count = 1)
        => territory.PresenceCount = Math.Max(0, territory.PresenceCount - count);

    public bool IsInRange(string fromTerritoryId, string toTerritoryId, int range)
        => TerritoryGraph.Standard.Distance(fromTerritoryId, toTerritoryId) <= range;

    public List<string> GetTerritoriesInRange(string fromTerritoryId, int range)
        => TerritoryGraph.Standard.AllTerritoryIds
            .Where(id => TerritoryGraph.Standard.Distance(fromTerritoryId, id) <= range)
            .ToList();

    public int CalculateNetworkFear()
    {
        var territories = _territoriesProvider().ToDictionary(t => t.Id);
        int fear = 0;
        foreach (var id in TerritoryGraph.Standard.AllTerritoryIds)
        {
            if (!territories.TryGetValue(id, out var t) || !t.HasPresence) continue;
            foreach (var neighborId in TerritoryGraph.Standard.GetNeighbors(id))
            {
                // Bugfix: count undirected edges only (each pair once, not twice)
                if (string.Compare(id, neighborId, StringComparison.Ordinal) >= 0) continue;
                if (territories.TryGetValue(neighborId, out var neighbor) && neighbor.HasPresence)
                    fear++;
            }
        }
        return fear;
    }
}
