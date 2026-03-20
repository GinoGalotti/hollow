namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Map;
using HollowWardens.Core.Models;

public class PresenceSystem : IPresenceSystem
{
    private readonly Func<IEnumerable<Territory>> _territoriesProvider;

    public PresenceSystem(Func<IEnumerable<Territory>> territoriesProvider)
    {
        _territoriesProvider = territoriesProvider;
    }

    public void PlacePresence(Territory territory, int count = 1)
        => territory.PresenceCount += count;

    public void RemovePresence(Territory territory, int count = 1)
        => territory.PresenceCount = Math.Max(0, territory.PresenceCount - count);

    public bool IsInRange(string fromTerritoryId, string toTerritoryId, int range)
        => TerritoryGraph.Distance(fromTerritoryId, toTerritoryId) <= range;

    public List<string> GetTerritoriesInRange(string fromTerritoryId, int range)
        => TerritoryGraph.AllTerritoryIds
            .Where(id => TerritoryGraph.Distance(fromTerritoryId, id) <= range)
            .ToList();

    public int CalculateNetworkFear()
    {
        var territories = _territoriesProvider().ToDictionary(t => t.Id);
        int fear = 0;
        foreach (var id in TerritoryGraph.AllTerritoryIds)
        {
            if (!territories.TryGetValue(id, out var t) || !t.HasPresence) continue;
            foreach (var neighborId in TerritoryGraph.GetNeighbors(id))
            {
                if (territories.TryGetValue(neighborId, out var neighbor) && neighbor.HasPresence)
                    fear++;
            }
        }
        return fear;
    }
}
