namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Models;

public interface ICorruptionSystem
{
    void AddCorruption(Territory territory, int points);
    void ReduceCorruption(Territory territory, int points);
    void PurifyLevel(Territory territory);
    void ApplyPersistence(Territory territory);  // between encounters
    HashSet<string> SacredTerritories { get; }
}
