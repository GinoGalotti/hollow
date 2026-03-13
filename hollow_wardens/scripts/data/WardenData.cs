using Godot;
using Godot.Collections;

[GlobalClass]
public partial class WardenData : Resource
{
    [Export] public string Id { get; set; } = "";          // "root", "ember", "veil"
    [Export] public string WardenName { get; set; } = "";
    [Export] public string Archetype { get; set; } = "";   // e.g. "Tank / Control"
    [Export] public int StartingHandSize { get; set; }
    [Export] public int MaxHandSize { get; set; }
    [Export] public Array<CardData> StartingDeck { get; set; } = new();
    [Export] public string DissolveDescription { get; set; } = "";
    [Export] public string ResolutionStyle { get; set; } = "";
}
