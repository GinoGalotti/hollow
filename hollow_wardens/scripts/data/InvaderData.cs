using Godot;

[GlobalClass]
public partial class InvaderData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string FactionNameKey { get; set; } = "";
    [Export] public int MaxHp { get; set; }
    [Export] public int MoveSpeed { get; set; } = 1;        // Territory steps per Advance
    [Export] public int WeaveDrainPassive { get; set; } = 0; // Per turn drain (Pale March: per 3+ units)
    [Export] public string DreadDescKey { get; set; } = "";
}
