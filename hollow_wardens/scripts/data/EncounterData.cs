using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EncounterData : Resource
{
    public enum EncounterTier { Standard, Elite, Boss }

    [Export] public string Id { get; set; } = "";
    [Export] public EncounterTier Tier { get; set; }
    [Export] public InvaderData? Faction { get; set; }
    [Export] public int TideSteps { get; set; }         // Total Tide steps before Resolution
    [Export] public int ResolutionTurns { get; set; }   // Max Resolution turns allowed
    [Export] public Array<SpawnEvent> SpawnPattern { get; set; } = new();
    [Export] public Array<EscalateEvent> EscalationSchedule { get; set; } = new();
    [Export] public bool IsEclipse { get; set; } = false;
    [Export] public Dictionary StartingCorruption { get; set; } = new(); // territoryId → corruptionLevel
}
