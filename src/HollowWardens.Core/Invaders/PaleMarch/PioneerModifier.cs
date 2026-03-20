namespace HollowWardens.Core.Invaders.PaleMarch;

using HollowWardens.Core.Models;

public class PioneerModifier : UnitTypeModifier
{
    public override UnitType UnitType => UnitType.Pioneer;
    public override int BaseHp => 2;

    // After any activate, if 2+ Marchers in territory, place Infrastructure
    public override bool PlacesInfrastructureAfterActivate => true;

    // Fortify: fortification also gives +1 Corruption on all future Ravage
    public override bool FortifyGrantsFutureCorruption => true;
}
