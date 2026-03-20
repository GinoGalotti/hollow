namespace HollowWardens.Tests.Invaders;

using HollowWardens.Core.Invaders.PaleMarch;
using HollowWardens.Core.Models;
using Xunit;

public class UnitModifierTests
{
    // --- Ironclad ---

    [Fact]
    public void Ironclad_RavageCorruption_IsPlusTwoBase()
    {
        var mod = new IroncladModifier();
        // Design: +1 Corruption on Ravage (base 2 → 3)
        Assert.Equal(3, mod.RavageCorruption);
    }

    [Fact]
    public void Ironclad_Rest_IsFullHeal()
    {
        var mod = new IroncladModifier();
        Assert.True(mod.RestIsFullHeal);
    }

    [Fact]
    public void Ironclad_Corrupt_KillsTwoNatives()
    {
        var mod = new IroncladModifier();
        Assert.Equal(2, mod.CorruptNativesKilled);
    }

    [Fact]
    public void Ironclad_MovesEveryOtherAdvance()
    {
        var mod = new IroncladModifier();
        Assert.True(mod.MovesEveryOtherAdvance);
    }

    // --- Outrider ---

    [Fact]
    public void Outrider_HasPlusOneExtraMovement()
    {
        var mod = new OutriderModifier();
        Assert.Equal(1, mod.ExtraMovement);
    }

    [Fact]
    public void Outrider_RestAdvancesInstead()
    {
        var mod = new OutriderModifier();
        Assert.True(mod.RestAdvancesInstead);
    }

    [Fact]
    public void Outrider_RegroupAdvancesInstead()
    {
        var mod = new OutriderModifier();
        Assert.True(mod.RegroupAdvancesInstead);
    }

    [Fact]
    public void Outrider_RavageCorruption_IsOne()
    {
        var mod = new OutriderModifier();
        Assert.Equal(1, mod.RavageCorruption);
    }

    [Fact]
    public void Outrider_DamagesFirstNativeOnly()
    {
        var mod = new OutriderModifier();
        Assert.True(mod.RavageDamagesFirstNativeOnly);
        Assert.Equal(2, mod.RavageDamageToFirstNative);
    }

    // --- Pioneer ---

    [Fact]
    public void Pioneer_PlacesInfrastructureAfterActivate()
    {
        var mod = new PioneerModifier();
        Assert.True(mod.PlacesInfrastructureAfterActivate);
    }

    [Fact]
    public void Pioneer_FortifyGrantsFutureCorruption()
    {
        var mod = new PioneerModifier();
        Assert.True(mod.FortifyGrantsFutureCorruption);
    }

    // --- Marcher ---

    [Fact]
    public void Marcher_HasNoSpecialModifiers()
    {
        var mod = new MarcherModifier();

        Assert.Equal(3, mod.BaseHp);
        Assert.Equal(2, mod.RavageCorruption);     // default
        Assert.Equal(0, mod.ExtraMovement);
        Assert.False(mod.MovesEveryOtherAdvance);
        Assert.False(mod.RestIsFullHeal);
        Assert.False(mod.RestAdvancesInstead);
        Assert.False(mod.RegroupAdvancesInstead);
        Assert.False(mod.PlacesInfrastructureAfterActivate);
    }
}
