namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Events;
using HollowWardens.Core.Models;

public class CorruptionSystem : ICorruptionSystem
{
    public HashSet<string> SacredTerritories { get; } = new();

    public void AddCorruption(Territory territory, int points)
    {
        if (SacredTerritories.Contains(territory.Id)) return;

        // D28 Vulnerability: detect crossing into Desecrated (Level 3)
        var levelBefore = territory.CorruptionLevel;

        territory.CorruptionPoints += points;
        GameEvents.CorruptionChanged?.Invoke(territory, territory.CorruptionPoints, territory.CorruptionLevel);

        // If corruption just crossed into Level 3, presence is destroyed
        if (levelBefore < 3 && territory.CorruptionLevel >= 3)
            GameEvents.TerritoryDesecrated?.Invoke(territory);
    }

    public void ReduceCorruption(Territory territory, int points)
    {
        territory.CorruptionPoints = Math.Max(0, territory.CorruptionPoints - points);
        GameEvents.CorruptionChanged?.Invoke(territory, territory.CorruptionPoints, territory.CorruptionLevel);
    }

    public void PurifyLevel(Territory territory)
    {
        int newPoints = territory.CorruptionLevel switch
        {
            3 => 8,   // drop to start of Level 2 (Defiled)
            2 => 3,   // drop to start of Level 1 (Tainted)
            1 => 0,   // drop to Clean
            _ => territory.CorruptionPoints
        };
        if (newPoints == territory.CorruptionPoints) return;
        territory.CorruptionPoints = newPoints;
        GameEvents.CorruptionChanged?.Invoke(territory, territory.CorruptionPoints, territory.CorruptionLevel);
    }

    public void ApplyPersistence(Territory territory)
    {
        switch (territory.CorruptionLevel)
        {
            case 1:
                territory.CorruptionPoints = 0;
                break;
            case 2:
                territory.CorruptionPoints = 3;
                break;
            // Level 0: unchanged (already clean); Level 3: permanent (no change)
        }
    }
}
