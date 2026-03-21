namespace HollowWardens.Core.Models;

public class Invader
{
    public string Id { get; set; } = string.Empty;
    public UnitType UnitType { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int ShieldValue { get; set; }
    public string TerritoryId { get; set; } = string.Empty;
    public string FactionId { get; set; } = string.Empty;
    public bool AlternateMoveTurn { get; set; }  // Ironclad: moves every other Advance
    // D29: SlowInvaders effect flag — halves movement, reset each Tide
    public bool IsSlowed { get; set; }
    public bool IsAlive => Hp > 0;
}
