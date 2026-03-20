namespace HollowWardens.Core.Models;

public class SpawnWave
{
    public int TurnNumber { get; set; }
    public List<string> ArrivalPoints { get; set; } = new();  // territory IDs
    public List<SpawnWaveOption> Options { get; set; } = new();
}

public class SpawnWaveOption
{
    public int Weight { get; set; }
    public Dictionary<string, List<UnitType>> Units { get; set; } = new();
    // key = territory ID, value = list of unit types to spawn there
}
