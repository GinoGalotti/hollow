namespace HollowWardens.Tests.Systems;

using HollowWardens.Core.Events;
using HollowWardens.Core.Systems;
using Xunit;

public class DreadSystemTests : IDisposable
{
    private readonly DreadSystem _sut = new();

    public void Dispose() => GameEvents.ClearAll();

    [Fact]
    public void StartsAtLevel1()
    {
        Assert.Equal(1, _sut.DreadLevel);
    }

    [Fact]
    public void AdvancesToLevel2At15()
    {
        _sut.OnFearGenerated(15);
        Assert.Equal(2, _sut.DreadLevel);
    }

    [Fact]
    public void AdvancesToLevel3At30()
    {
        _sut.OnFearGenerated(30);
        Assert.Equal(3, _sut.DreadLevel);
    }

    [Fact]
    public void AdvancesToLevel4At45()
    {
        _sut.OnFearGenerated(45);
        Assert.Equal(4, _sut.DreadLevel);
    }

    [Fact]
    public void DoesNotExceedLevel4()
    {
        _sut.OnFearGenerated(100);
        Assert.Equal(4, _sut.DreadLevel);
    }

    [Fact]
    public void FiresDreadAdvancedEvent()
    {
        var levels = new List<int>();
        GameEvents.DreadAdvanced += l => levels.Add(l);

        _sut.OnFearGenerated(45);

        Assert.Equal(new[] { 2, 3, 4 }, levels);
    }
}
