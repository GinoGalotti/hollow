namespace HollowWardens.Core.Invaders;

using HollowWardens.Core.Models;

public abstract class InvaderFaction
{
    public abstract string FactionId { get; }
    public abstract string DisplayName { get; }
    public abstract IReadOnlyList<UnitType> UnitTypes { get; }
    public abstract IReadOnlyList<ActionCard> BuildPainfulPool();
    public abstract IReadOnlyList<ActionCard> BuildEasyPool();
    public abstract UnitTypeModifier GetModifier(UnitType unitType);

    /// <summary>HP bonus applied to all created invaders. Set from BalanceConfig.InvaderHpBonus.</summary>
    public int HpBonus { get; set; } = 0;

    public virtual Invader CreateUnit(UnitType unitType, string territoryId)
    {
        var modifier = GetModifier(unitType);
        int hp = modifier.BaseHp + HpBonus;
        return new Invader
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            UnitType = unitType,
            MaxHp = hp,
            Hp = hp,
            TerritoryId = territoryId,
            FactionId = FactionId
        };
    }
}
