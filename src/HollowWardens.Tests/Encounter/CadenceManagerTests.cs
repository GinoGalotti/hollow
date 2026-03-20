namespace HollowWardens.Tests.Encounter;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using Xunit;

public class CadenceManagerTests
{
    [Fact]
    public void PepePattern_MaxStreak1()
    {
        var config = new CadenceConfig { Mode = "rule_based", MaxPainfulStreak = 1 };
        var sut = new CadenceManager(config);

        Assert.Equal(ActionPool.Painful, sut.NextPool());
        Assert.Equal(ActionPool.Easy,    sut.NextPool());
        Assert.Equal(ActionPool.Painful, sut.NextPool());
        Assert.Equal(ActionPool.Easy,    sut.NextPool());
    }

    [Fact]
    public void PpePattern_MaxStreak2()
    {
        var config = new CadenceConfig { Mode = "rule_based", MaxPainfulStreak = 2 };
        var sut = new CadenceManager(config);

        Assert.Equal(ActionPool.Painful, sut.NextPool());
        Assert.Equal(ActionPool.Painful, sut.NextPool());
        Assert.Equal(ActionPool.Easy,    sut.NextPool());
    }

    [Fact]
    public void PppePattern_MaxStreak3()
    {
        var config = new CadenceConfig { Mode = "rule_based", MaxPainfulStreak = 3 };
        var sut = new CadenceManager(config);

        Assert.Equal(ActionPool.Painful, sut.NextPool());
        Assert.Equal(ActionPool.Painful, sut.NextPool());
        Assert.Equal(ActionPool.Painful, sut.NextPool());
        Assert.Equal(ActionPool.Easy,    sut.NextPool());
    }

    [Fact]
    public void ManualOverride_Respected()
    {
        var config = new CadenceConfig
        {
            Mode          = "manual",
            ManualPattern = new[] { "P", "E", "P", "P", "E" }
        };
        var sut = new CadenceManager(config);

        Assert.Equal(ActionPool.Painful, sut.NextPool());
        Assert.Equal(ActionPool.Easy,    sut.NextPool());
        Assert.Equal(ActionPool.Painful, sut.NextPool());
        Assert.Equal(ActionPool.Painful, sut.NextPool());
        Assert.Equal(ActionPool.Easy,    sut.NextPool());
    }

    [Fact]
    public void StreakResets_AfterEasy()
    {
        var config = new CadenceConfig { Mode = "rule_based", MaxPainfulStreak = 2 };
        var sut = new CadenceManager(config);

        sut.NextPool(); // P, streak = 1
        sut.NextPool(); // P, streak = 2
        sut.NextPool(); // E, streak = 0

        // After the Easy, streak is 0 — next draw must be Painful
        Assert.Equal(ActionPool.Painful, sut.NextPool());
        Assert.Equal(1, sut.PainfulStreak);
    }
}
