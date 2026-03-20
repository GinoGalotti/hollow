namespace HollowWardens.Core.Invaders.PaleMarch;

using HollowWardens.Core.Models;

public class IroncladModifier : UnitTypeModifier
{
    public override UnitType UnitType => UnitType.Ironclad;
    public override int BaseHp => 5;

    // Ravage: +1 Corruption (base 2 → 3)
    public override int RavageCorruption => 3;

    // Rest: full heal instead of half
    public override bool RestIsFullHeal => true;

    // Corrupt: kills 2 Natives instead of 1
    public override int CorruptNativesKilled => 2;

    // Moves every other Advance step
    public override bool MovesEveryOtherAdvance => true;
}
