using Godot;

[GlobalClass]
public partial class CardEffect : Resource
{
    public enum EffectType
    {
        PlacePresence, MovePresence,
        GenerateFear,
        ReduceCorruption, Purify,
        DamageInvaders, PushInvaders, RoutInvaders,
        RestoreWeave,
        PredictTide,
        Conditional,    // Threshold/if-then effects — needs EffectCondition
        Custom,         // Warden-specific, resolved in Warden subclass
        AwakeDormant    // Root-specific: awaken Value dormant cards from hand (0 = all)
    }

    [Export] public EffectType Type { get; set; }
    [Export] public int Value { get; set; }
    [Export] public int Range { get; set; }             // Territory steps from nearest Presence
    [Export] public EffectCondition? Condition { get; set; } // Optional, for Dusk threshold effects
    [Export] public string DescriptionKey { get; set; } = "";
}
