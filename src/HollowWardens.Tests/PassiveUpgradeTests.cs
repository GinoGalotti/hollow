namespace HollowWardens.Tests;

using HollowWardens.Core.Data;
using HollowWardens.Core.Wardens;
using Xunit;

public class PassiveUpgradeTests
{
    private static string GetWardenJsonPath(string warden = "root")
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "wardens", $"{warden}.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not find repo root");
    }

    [Fact]
    public void UpgradePassive_SetsFlag()
    {
        var gating = new PassiveGating("root");
        bool result = gating.UpgradePassive("network_fear_u1");
        Assert.True(result);
        Assert.True(gating.IsUpgraded("network_fear_u1"));
    }

    [Fact]
    public void UpgradePassive_AlreadyUpgraded_ReturnsFalse()
    {
        var gating = new PassiveGating("root");
        Assert.True(gating.UpgradePassive("network_fear_u1"));
        Assert.False(gating.UpgradePassive("network_fear_u1"));
    }

    [Fact]
    public void WardenLoader_ParsesPassiveUpgrades()
    {
        var warden = WardenLoader.Load(GetWardenJsonPath("root"));
        var passive = warden.Passives.First(p => p.Id == "network_fear");
        Assert.NotNull(passive.Upgrade);
        Assert.Equal("network_fear_u1", passive.Upgrade!.Id);
        Assert.NotEmpty(passive.Upgrade.Effects);
        Assert.Equal("set_balance", passive.Upgrade.Effects[0].Type);
    }

    [Fact]
    public void UpgradedPassives_TrackedCorrectly()
    {
        var gating = new PassiveGating("ember");
        gating.UpgradePassive("ash_trail_u1");
        gating.UpgradePassive("ember_fury_u1");

        Assert.Contains("ash_trail_u1", gating.UpgradedPassives);
        Assert.Contains("ember_fury_u1", gating.UpgradedPassives);
        Assert.DoesNotContain("heat_wave_u1", gating.UpgradedPassives);
        Assert.Equal(2, gating.UpgradedPassives.Count);
    }
}
