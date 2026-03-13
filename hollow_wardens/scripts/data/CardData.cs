using Godot;

[GlobalClass]
public partial class CardData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string CardName { get; set; } = "";
    [Export] public string WardenId { get; set; } = "";  // "root", "ember", "veil", "" (shared)
    [Export] public CardEffect? VigilEffect { get; set; }    // Top action
    [Export] public CardEffect? DuskEffect { get; set; }     // Bottom action
    [Export] public CardEffect? DissolveEffect { get; set; } // Default: place presence bypassing range
    [Export] public int Cost { get; set; }                   // Used for Ember's Fear pulse on dissolve
    [Export] public bool IsDormant { get; set; } = false;    // Root-specific state
    [Export] public Texture2D? ArtTexture { get; set; }
}
