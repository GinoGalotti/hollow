namespace HollowWardens.Tests.Systems;

using HollowWardens.Core.Events;
using HollowWardens.Core.Systems;
using Xunit;

public class WeaveSystemTests : IDisposable
{
    public void Dispose() => GameEvents.WeaveChanged = null;

    [Fact]
    public void RestoreWeave_ClampsAtMaxWeave()
    {
        var weave = new WeaveSystem(startingWeave: 18, maxWeave: 20);

        weave.Restore(5);

        Assert.Equal(20, weave.CurrentWeave);
    }

    [Fact]
    public void RestoreWeave_AtMax_NoChange()
    {
        var weave = new WeaveSystem(startingWeave: 20, maxWeave: 20);

        weave.Restore(3);

        Assert.Equal(20, weave.CurrentWeave);
    }

    [Fact]
    public void RestoreWeave_BelowMax_AddsNormally()
    {
        var weave = new WeaveSystem(startingWeave: 15, maxWeave: 20);

        weave.Restore(3);

        Assert.Equal(18, weave.CurrentWeave);
    }

    [Fact]
    public void DealDamage_ReducesWeave()
    {
        var weave = new WeaveSystem(startingWeave: 20, maxWeave: 20);

        weave.DealDamage(5);

        Assert.Equal(15, weave.CurrentWeave);
    }

    [Fact]
    public void DealDamage_ClampsAtZero()
    {
        var weave = new WeaveSystem(startingWeave: 3, maxWeave: 20);

        weave.DealDamage(10);

        Assert.Equal(0, weave.CurrentWeave);
    }
}
