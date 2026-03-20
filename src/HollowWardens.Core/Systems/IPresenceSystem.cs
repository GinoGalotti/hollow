namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Models;

public interface IPresenceSystem
{
    void PlacePresence(Territory territory, int count = 1);
    void RemovePresence(Territory territory, int count = 1);
    bool IsInRange(string fromTerritoryId, string toTerritoryId, int range);
    List<string> GetTerritoriesInRange(string fromTerritoryId, int range);
    int CalculateNetworkFear();  // Root passive: adjacency counting
}
