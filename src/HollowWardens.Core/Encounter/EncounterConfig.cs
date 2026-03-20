namespace HollowWardens.Core.Encounter;

using HollowWardens.Core.Models;

public class EncounterConfig
{
    public string Id { get; set; } = string.Empty;
    public EncounterTier Tier { get; set; }
    public string FactionId { get; set; } = string.Empty;
    public int TideCount { get; set; }
    public int ResolutionTurns => Tier switch
    {
        EncounterTier.Standard => 2,
        EncounterTier.Elite => 3,
        EncounterTier.Boss => 1,
        _ => 2
    };
    public CadenceConfig Cadence { get; set; } = new();
    public Dictionary<string, int> NativeSpawns { get; set; } = new();
    public List<SpawnWave> Waves { get; set; } = new();
    public List<EscalationEntry> EscalationSchedule { get; set; } = new();
}

public class CadenceConfig
{
    public string Mode { get; set; } = "rule_based";  // or "manual"
    public int MaxPainfulStreak { get; set; } = 1;
    public int EasyFrequency { get; set; } = 2;
    public string[]? ManualPattern { get; set; }  // ["P","E","P","P","E",...]
}

public class EscalationEntry
{
    public int Tide { get; set; }
    public string CardId { get; set; } = string.Empty;
    public ActionPool Pool { get; set; }
}
