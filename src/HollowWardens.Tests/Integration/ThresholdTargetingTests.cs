namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Models;
using Xunit;

/// <summary>
/// Verifies ThresholdResolver.NeedsTarget and GetTargetEffect:
/// Root T1 requires player target (PlacePresence range 1); all other tiers/elements do not.
/// </summary>
public class ThresholdTargetingTests
{
    [Fact]
    public void NeedsTarget_RootT1_ReturnsTrue()
        => Assert.True(ThresholdResolver.NeedsTarget(Element.Root, 1));

    [Fact]
    public void NeedsTarget_RootT2_ReturnsFalse()
        => Assert.False(ThresholdResolver.NeedsTarget(Element.Root, 2));

    [Fact]
    public void NeedsTarget_RootT3_ReturnsFalse()
        => Assert.False(ThresholdResolver.NeedsTarget(Element.Root, 3));

    [Fact]
    public void NeedsTarget_NonRootElements_ReturnFalse()
    {
        Assert.False(ThresholdResolver.NeedsTarget(Element.Mist,   1));
        Assert.False(ThresholdResolver.NeedsTarget(Element.Shadow, 1));
        Assert.False(ThresholdResolver.NeedsTarget(Element.Ash,    1));
        Assert.False(ThresholdResolver.NeedsTarget(Element.Gale,   1));
        Assert.False(ThresholdResolver.NeedsTarget(Element.Void,   1));
    }

    [Fact]
    public void GetTargetEffect_RootT1_ReturnsPlacePresenceRange1()
    {
        var effect = ThresholdResolver.GetTargetEffect(Element.Root, 1);

        Assert.NotNull(effect);
        Assert.Equal(EffectType.PlacePresence, effect!.Type);
        Assert.Equal(1, effect.Range);
    }

    [Fact]
    public void GetTargetEffect_RootT2_ReturnsNull()
        => Assert.Null(ThresholdResolver.GetTargetEffect(Element.Root, 2));
}
