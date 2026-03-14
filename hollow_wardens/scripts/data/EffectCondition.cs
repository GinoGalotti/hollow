using Godot;

// Condition evaluated at effect resolution time (used by Dusk threshold effects)
[GlobalClass]
public partial class EffectCondition : Resource
{
    public enum ConditionType
    {
        TerritoriesRavagedThisTide,
        SacredSiteThreatenedThisTide,
        FearTokensOnTerritory,
        InvaderCountOnTerritory,
        AlwaysTrue
    }

    [Export] public ConditionType Type { get; set; }
    [Export] public int Threshold { get; set; }         // Minimum value to meet condition
    [Export] public string DescriptionKey { get; set; } = "";
}
