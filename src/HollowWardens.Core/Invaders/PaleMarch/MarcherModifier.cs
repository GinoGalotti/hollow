namespace HollowWardens.Core.Invaders.PaleMarch;

using HollowWardens.Core.Models;

public class MarcherModifier : UnitTypeModifier
{
    public override UnitType UnitType => UnitType.Marcher;
    public override int BaseHp => 4;  // was 3
}
