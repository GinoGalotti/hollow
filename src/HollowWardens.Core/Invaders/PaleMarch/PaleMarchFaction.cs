namespace HollowWardens.Core.Invaders.PaleMarch;

using HollowWardens.Core.Models;

public class PaleMarchFaction : InvaderFaction
{
    public override string FactionId => "pale_march";
    public override string DisplayName => "The Pale March";

    public override IReadOnlyList<UnitType> UnitTypes { get; } =
        new[] { UnitType.Marcher, UnitType.Ironclad, UnitType.Outrider, UnitType.Pioneer };

    private static readonly Dictionary<UnitType, UnitTypeModifier> Modifiers = new()
    {
        [UnitType.Marcher]  = new MarcherModifier(),
        [UnitType.Ironclad] = new IroncladModifier(),
        [UnitType.Outrider] = new OutriderModifier(),
        [UnitType.Pioneer]  = new PioneerModifier(),
    };

    public override UnitTypeModifier GetModifier(UnitType unitType) => Modifiers[unitType];

    public override IReadOnlyList<ActionCard> BuildPainfulPool() => new[]
    {
        new ActionCard { Id = "pm_ravage", Name = "Ravage", Pool = ActionPool.Painful, AdvanceModifier = 1 },
        new ActionCard { Id = "pm_march",  Name = "March",  Pool = ActionPool.Painful, AdvanceModifier = 2 },
    };

    public override IReadOnlyList<ActionCard> BuildEasyPool() => new[]
    {
        new ActionCard { Id = "pm_rest",    Name = "Rest",    Pool = ActionPool.Easy, AdvanceModifier = 1 },
        new ActionCard { Id = "pm_settle",  Name = "Settle",  Pool = ActionPool.Easy, AdvanceModifier = 0 },
        new ActionCard { Id = "pm_regroup", Name = "Regroup", Pool = ActionPool.Easy, AdvanceModifier = 0 },
    };

    // Escalation cards — added to the deck via ActionDeck.AddEscalationCard at the appropriate Tide
    public static ActionCard BuildCorrupt() =>
        new() { Id = "pm_corrupt", Name = "Corrupt", Pool = ActionPool.Painful, AdvanceModifier = 1, IsEscalation = true };

    public static ActionCard BuildFortify() =>
        new() { Id = "pm_fortify", Name = "Fortify", Pool = ActionPool.Painful, AdvanceModifier = 0, IsEscalation = true };
}
