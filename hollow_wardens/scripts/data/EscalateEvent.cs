using Godot;

// An escalation event triggered every 3 Tide steps
[GlobalClass]
public partial class EscalateEvent : Resource
{
    public enum EscalateType
    {
        AddUnitType,        // Adds a new invader unit variant
        AddBehaviorModifier // Applies a movement/attack modifier
    }

    [Export] public int TideStep { get; set; }          // Which Tide step triggers this
    [Export] public EscalateType Type { get; set; }
    [Export] public string DescriptionKey { get; set; } = "";
}
