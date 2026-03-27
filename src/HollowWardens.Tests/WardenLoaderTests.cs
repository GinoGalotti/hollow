namespace HollowWardens.Tests;

using HollowWardens.Core.Data;
using HollowWardens.Core.Effects;
using Xunit;

public class WardenLoaderTests
{
    private static string GetWardenJsonPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "wardens", "root.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not find repo root (no data/wardens/root.json found in ancestors)");
    }

    private static readonly string _path = GetWardenJsonPath();

    [Fact]
    public void FullLoad_ReturnsCorrectMetadata()
    {
        var warden = WardenLoader.Load(_path);
        Assert.Equal("root", warden.WardenId);
        Assert.Equal("The Root", warden.Name);
        Assert.Equal(5, warden.HandLimit);
    }

    [Fact]
    public void StartingPresence_IsI1Count1()
    {
        var warden = WardenLoader.Load(_path);
        Assert.Equal("I1", warden.StartingPresence.Territory);
        Assert.Equal(1, warden.StartingPresence.Count);
    }

    [Fact]
    public void Passives_Count_IsSix()
    {
        var warden = WardenLoader.Load(_path);
        Assert.Equal(6, warden.Passives.Count);
    }

    [Fact]
    public void Passive_NetworkFear_HasCorrectTriggerAndMechanic()
    {
        var warden   = WardenLoader.Load(_path);
        var passive  = warden.Passives.Single(p => p.Id == "network_fear");
        Assert.Equal("turn_start", passive.Trigger);
        Assert.Equal("network_fear", passive.Mechanic);
    }

    [Fact]
    public void Cards_TotalCount_Is25()
    {
        var warden = WardenLoader.Load(_path);
        Assert.Equal(25, warden.Cards.Count);
    }

    [Fact]
    public void Cards_StartingCount_Is10()
    {
        var warden = WardenLoader.Load(_path);
        Assert.Equal(10, warden.Cards.Count(c => c.IsStarting));
    }

    [Fact]
    public void CardSwap_Root025_IsStarting_Root011_IsNot()
    {
        var warden = WardenLoader.Load(_path);
        var r025   = warden.Cards.Single(c => c.Id == "root_025");
        var r011   = warden.Cards.Single(c => c.Id == "root_011");
        Assert.True(r025.IsStarting);
        Assert.False(r011.IsStarting);
    }

    [Fact]
    public void EffectParsing_Root025_TopIsDamageRange1_BottomIsPushInvadersWithReduceCorruption()
    {
        // B6: card 025 bottom changed from DamageInvaders+SlowInvaders → PushInvaders+ReduceCorruption
        var warden = WardenLoader.Load(_path);
        var card   = warden.Cards.Single(c => c.Id == "root_025");
        Assert.Equal(EffectType.DamageInvaders, card.TopEffect.Type);
        Assert.Equal(2, card.TopEffect.Value);
        Assert.Equal(1, card.TopEffect.Range);
        Assert.Equal(EffectType.PushInvaders, card.BottomEffect.Type);
        Assert.NotNull(card.BottomSecondary);
        Assert.Equal(EffectType.ReduceCorruption, card.BottomSecondary!.Type);
    }

    [Fact]
    public void ElementAffinity_IsRoot_Mist_Shadow()
    {
        var warden = WardenLoader.Load(_path);
        Assert.Equal("Root",   warden.ElementAffinity.Primary);
        Assert.Equal("Mist",   warden.ElementAffinity.Secondary);
        Assert.Equal("Shadow", warden.ElementAffinity.Tertiary);
    }

    [Fact]
    public void ConvenienceMethods_ReturnSameCountAsFullLoad()
    {
        var warden    = WardenLoader.Load(_path);
        var cards     = WardenLoader.LoadCards(_path);
        var passives  = WardenLoader.LoadPassives(_path);
        Assert.Equal(warden.Cards.Count,   cards.Count);
        Assert.Equal(warden.Passives.Count, passives.Count);
    }

    [Fact]
    public void Load_NonExistentFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => WardenLoader.Load("/nonexistent/path/warden.json"));
    }
}
