using System.Text.RegularExpressions;
using HollowWardens.Core;
using HollowWardens.Core.Encounter;
using Xunit;

namespace HollowWardens.Tests;

public class GameVersionTests
{
    [Fact]
    public void Version_IsNotEmpty()
    {
        Assert.Equal("0.8.0", GameVersion.Version);
    }

    [Fact]
    public void Full_ContainsPlus()
    {
        Assert.Matches(@"^0\.8\.0\+\d+$", GameVersion.Full);
    }

    [Fact]
    public void BalanceHash_IsDeterministic()
    {
        var config = new BalanceConfig();
        Assert.Equal(config.GetHash(), config.GetHash());
    }

    [Fact]
    public void BalanceHash_ChangesWhenConfigChanges()
    {
        var a = new BalanceConfig();
        var b = new BalanceConfig { MaxWeave = 99 };
        Assert.NotEqual(a.GetHash(), b.GetHash());
    }

    [Fact]
    public void BalanceHash_Is12Chars()
    {
        var hash = new BalanceConfig().GetHash();
        Assert.Equal(12, hash.Length);
    }
}
