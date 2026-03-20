namespace HollowWardens.Core.Models;

public class Native
{
    public int Hp { get; set; }
    public int MaxHp { get; set; } = 2;
    public int Damage { get; set; } = 3;
    public int ShieldValue { get; set; }
    public string TerritoryId { get; set; } = string.Empty;
    public bool IsAlive => Hp > 0;
}
