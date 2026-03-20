namespace HollowWardens.Tests.Systems;

using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using Xunit;

public class CorruptionSystemTests : IDisposable
{
    private readonly CorruptionSystem _sut = new();

    private static Territory MakeTerritory() => new() { Id = "A1" };

    public void Dispose() => GameEvents.ClearAll();

    [Fact]
    public void ThreePointsReachesLevel1()
    {
        var t = MakeTerritory();
        _sut.AddCorruption(t, 3);
        Assert.Equal(1, t.CorruptionLevel);
    }

    [Fact]
    public void EightPointsReachesLevel2()
    {
        var t = MakeTerritory();
        _sut.AddCorruption(t, 8);
        Assert.Equal(2, t.CorruptionLevel);
    }

    [Fact]
    public void FifteenPointsReachesLevel3()
    {
        var t = MakeTerritory();
        _sut.AddCorruption(t, 15);
        Assert.Equal(3, t.CorruptionLevel);
    }

    [Fact]
    public void ReduceCorruptionClampsAtZero()
    {
        var t = MakeTerritory();
        _sut.AddCorruption(t, 2);
        _sut.ReduceCorruption(t, 5);
        Assert.Equal(0, t.CorruptionPoints);
    }

    [Fact]
    public void PurifyDropsOneLevel()
    {
        var t = MakeTerritory();
        _sut.AddCorruption(t, 15); // Level 3 (Desecrated)
        _sut.PurifyLevel(t);
        Assert.Equal(2, t.CorruptionLevel);
        Assert.Equal(8, t.CorruptionPoints); // start of Level 2

        _sut.PurifyLevel(t); // Level 2 → Level 1
        Assert.Equal(1, t.CorruptionLevel);
        Assert.Equal(3, t.CorruptionPoints); // start of Level 1

        _sut.PurifyLevel(t); // Level 1 → Clean
        Assert.Equal(0, t.CorruptionLevel);
        Assert.Equal(0, t.CorruptionPoints);
    }

    [Fact]
    public void PersistenceLevel1ResetsToZero()
    {
        var t = MakeTerritory();
        _sut.AddCorruption(t, 5); // Level 1
        _sut.ApplyPersistence(t);
        Assert.Equal(0, t.CorruptionPoints);
    }

    [Fact]
    public void PersistenceLevel2BecomesThreePoints()
    {
        var t = MakeTerritory();
        _sut.AddCorruption(t, 10); // Level 2
        _sut.ApplyPersistence(t);
        Assert.Equal(3, t.CorruptionPoints);
        Assert.Equal(1, t.CorruptionLevel);
    }

    [Fact]
    public void PersistenceLevel3Permanent()
    {
        var t = MakeTerritory();
        _sut.AddCorruption(t, 20); // Level 3
        _sut.ApplyPersistence(t);
        Assert.Equal(3, t.CorruptionLevel);
        Assert.Equal(20, t.CorruptionPoints);
    }
}
