namespace HollowWardens.Core.Invaders;

using HollowWardens.Core.Models;

public abstract class UnitTypeModifier
{
    public abstract UnitType UnitType { get; }
    public abstract int BaseHp { get; }
    public virtual int BaseMovement => 1;

    // Ravage
    public virtual int RavageCorruption => 2;
    public virtual int RavageDamagePerNative => 1;
    public virtual bool RavageDamagesFirstNativeOnly => false;
    public virtual int RavageDamageToFirstNative => 0;

    // Corrupt
    public virtual int CorruptNativesKilled => 1;

    // Rest
    public virtual bool RestIsFullHeal => false;
    public virtual bool RestAdvancesInstead => false;

    // Regroup
    public virtual bool RegroupAdvancesInstead => false;

    // Movement
    public virtual int ExtraMovement => 0;
    public virtual bool MovesEveryOtherAdvance => false;

    // Pioneer
    public virtual bool PlacesInfrastructureAfterActivate => false;
    public virtual bool FortifyGrantsFutureCorruption => false;
}
