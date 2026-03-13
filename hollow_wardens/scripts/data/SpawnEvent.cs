using Godot;

// A single spawn event within an encounter's SpawnPattern
[GlobalClass]
public partial class SpawnEvent : Resource
{
    [Export] public int TideStep { get; set; }          // Which Tide step this spawns on
    [Export] public string TerritoryId { get; set; } = "";  // Entry territory id
    [Export] public int Count { get; set; } = 1;        // Number of invader units
}
