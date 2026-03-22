namespace HollowWardens.Tests;

using HollowWardens.Core.Data;
using HollowWardens.Core.Models;
using Xunit;

/// <summary>
/// Tests that ember.json loads correctly via WardenLoader.
/// </summary>
public class EmberLoaderTests
{
    private static string GetEmberJsonPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "wardens", "ember.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not find repo root (no data/wardens/ember.json found in ancestors)");
    }

    private static readonly string _path = GetEmberJsonPath();

    [Fact]
    public void EmberLoad_WardenId_IsEmber()
    {
        var warden = WardenLoader.Load(_path);
        Assert.Equal("ember", warden.WardenId);
    }

    [Fact]
    public void EmberLoad_Cards_TotalCount()
    {
        var warden = WardenLoader.Load(_path);
        Assert.Equal(18, warden.Cards.Count); // 8 starting + 10 draft
    }

    [Fact]
    public void EmberLoad_StartingCards_Is8()
    {
        var warden = WardenLoader.Load(_path);
        Assert.Equal(8, warden.Cards.Count(c => c.IsStarting));
    }

    [Fact]
    public void EmberLoad_Passives_Count()
    {
        var warden = WardenLoader.Load(_path);
        Assert.Equal(7, warden.Passives.Count); // D31: +controlled_burn passive
    }

    [Fact]
    public void EmberLoad_ElementAffinity_IsAsh_Shadow_Gale()
    {
        var warden = WardenLoader.Load(_path);
        Assert.Equal("Ash",    warden.ElementAffinity.Primary);
        Assert.Equal("Shadow", warden.ElementAffinity.Secondary);
        Assert.Equal("Gale",   warden.ElementAffinity.Tertiary);
    }
}
