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

    public virtual Invader CreateUnit(UnitType unitType, string territoryId)
    {
        var modifier = GetModifier(unitType);
        return new Invader
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            UnitType = unitType,
            MaxHp = modifier.BaseHp,
            Hp = modifier.BaseHp,
            TerritoryId = territoryId,
            FactionId = FactionId
        };
    }
}
