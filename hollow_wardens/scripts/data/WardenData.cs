using Godot;
using Godot.Collections;

[GlobalClass]
public partial class WardenData : Resource
{
    [Export] public string Id { get; set; } = "";          // "root", "ember", "veil"
    [Export] public string WardenNameKey { get; set; } = "";
    [Export] public string ArchetypeKey { get; set; } = "";   // e.g. "WARDEN_ROOT_ARCHETYPE"
    [Export] public int StartingHandSize { get; set; }
    [Export] public int MaxHandSize { get; set; }
    [Export] public Array<CardData> StartingDeck { get; set; } = new();
    [Export] public string DissolveDescKey { get; set; } = "";
    [Export] public string ResolutionDescKey { get; set; } = "";
}
