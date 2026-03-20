namespace HollowWardens.Core.Invaders.PaleMarch;

using HollowWardens.Core.Models;

public class OutriderModifier : UnitTypeModifier
{
    public override UnitType UnitType => UnitType.Outrider;
    public override int BaseHp => 2;

    // Ravage: only 1 Corruption, but 2 damage to one Native first
    public override int RavageCorruption => 1;
    public override bool RavageDamagesFirstNativeOnly => true;
    public override int RavageDamageToFirstNative => 2;

    // Rest: advances 1 step instead of healing
    public override bool RestAdvancesInstead => true;

    // Regroup: advances 1 step instead of retreating
    public override bool RegroupAdvancesInstead => true;

    // Always +1 movement on every Advance
    public override int ExtraMovement => 1;
}
