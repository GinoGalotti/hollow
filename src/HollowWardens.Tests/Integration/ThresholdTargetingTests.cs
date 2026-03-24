namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Models;
using Xunit;

/// <summary>
/// Verifies ThresholdResolver.NeedsTarget and GetTargetEffect after D41:
/// Root T1/T2, Ash T1/T2, and Gale T1/T2 require player territory selection.
/// T3 effects and untargeted elements (Mist, Shadow, Void) do not.
/// </summary>
public class ThresholdTargetingTests
{
    [Fact]
    public void NeedsTarget_RootT1_ReturnsTrue()
        => Assert.True(ThresholdResolver.NeedsTarget(Element.Root, 1));

    [Fact]
    public void NeedsTarget_RootT2_ReturnsTrue()
        => Assert.True(ThresholdResolver.NeedsTarget(Element.Root, 2));

    [Fact]
    public void NeedsTarget_RootT3_ReturnsFalse()
        => Assert.False(ThresholdResolver.NeedsTarget(Element.Root, 3));

    [Fact]
    public void NeedsTarget_AshT1T2_ReturnTrue()
    {
        Assert.True(ThresholdResolver.NeedsTarget(Element.Ash, 1));
        Assert.True(ThresholdResolver.NeedsTarget(Element.Ash, 2));
    }

    [Fact]
    public void NeedsTarget_AshT3_ReturnsTrue()
        => Assert.True(ThresholdResolver.NeedsTarget(Element.Ash, 3));

    [Fact]
    public void NeedsTarget_GaleT1T2_ReturnTrue()
    {
        Assert.True(ThresholdResolver.NeedsTarget(Element.Gale, 1));
        Assert.True(ThresholdResolver.NeedsTarget(Element.Gale, 2));
    }

    [Fact]
    public void NeedsTarget_GaleT3_ReturnsFalse()
        => Assert.False(ThresholdResolver.NeedsTarget(Element.Gale, 3));

    [Fact]
    public void NeedsTarget_UntargetedElements_ReturnFalse()
    {
        Assert.False(ThresholdResolver.NeedsTarget(Element.Mist,   1));
        Assert.False(ThresholdResolver.NeedsTarget(Element.Shadow, 1));
        Assert.False(ThresholdResolver.NeedsTarget(Element.Void,   1));
    }

    [Fact]
    public void GetTargetEffect_RootT1_ReturnsReduceCorruption()
    {
        var effect = ThresholdResolver.GetTargetEffect(Element.Root, 1);

        Assert.NotNull(effect);
        Assert.Equal(EffectType.ReduceCorruption, effect!.Type);
        Assert.Equal(3, effect.Value);
    }

    [Fact]
    public void GetTargetEffect_RootT2_ReturnsPlacePresenceRange1()
    {
        var effect = ThresholdResolver.GetTargetEffect(Element.Root, 2);

        Assert.NotNull(effect);
        Assert.Equal(EffectType.PlacePresence, effect!.Type);
        Assert.Equal(1, effect.Range);
    }

    [Fact]
    public void GetTargetEffect_AshT1_ReturnsDamageInvaders()
    {
        var effect = ThresholdResolver.GetTargetEffect(Element.Ash, 1);

        Assert.NotNull(effect);
        Assert.Equal(EffectType.DamageInvaders, effect!.Type);
    }

    [Fact]
    public void GetTargetEffect_AshT2_ReturnsDamageInvaders()
    {
        var effect = ThresholdResolver.GetTargetEffect(Element.Ash, 2);

        Assert.NotNull(effect);
        Assert.Equal(EffectType.DamageInvaders, effect!.Type);
    }

    [Fact]
    public void GetTargetEffect_GaleT1T2_ReturnPushInvaders()
    {
        Assert.Equal(EffectType.PushInvaders, ThresholdResolver.GetTargetEffect(Element.Gale, 1)!.Type);
        Assert.Equal(EffectType.PushInvaders, ThresholdResolver.GetTargetEffect(Element.Gale, 2)!.Type);
    }

    [Fact]
    public void GetTargetEffect_AshT3_ReturnsDamageInvaders()
    {
        var effect = ThresholdResolver.GetTargetEffect(Element.Ash, 3);
        Assert.NotNull(effect);
        Assert.Equal(EffectType.DamageInvaders, effect!.Type);
    }

    [Fact]
    public void GetTargetEffect_GaleT3AndRootT3_ReturnNull()
    {
        Assert.Null(ThresholdResolver.GetTargetEffect(Element.Gale, 3));
        Assert.Null(ThresholdResolver.GetTargetEffect(Element.Root, 3));
    }

    [Fact]
    public void GetTargetEffect_UntargetedElements_ReturnNull()
    {
        Assert.Null(ThresholdResolver.GetTargetEffect(Element.Mist,   1));
        Assert.Null(ThresholdResolver.GetTargetEffect(Element.Shadow, 1));
        Assert.Null(ThresholdResolver.GetTargetEffect(Element.Void,   1));
    }
}
