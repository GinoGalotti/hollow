using Godot;

// Runtime invader unit — not a Resource; lives in TerritoryState.InvaderUnits
public partial class InvaderUnit : Node
{
    public string TerritoryId { get; set; } = "";
    public int Hp { get; set; }
    public InvaderData? Data { get; set; }

    public bool IsDefeated => Hp <= 0;

    public void TakeDamage(int amount)
    {
        Hp = System.Math.Max(0, Hp - amount);
    }
}
