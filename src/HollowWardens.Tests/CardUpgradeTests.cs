namespace HollowWardens.Tests;

using HollowWardens.Core.Cards;
using HollowWardens.Core.Data;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Models;
using Xunit;

public class CardUpgradeTests
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

    private static Card MakeCardWithUpgrades() => new()
    {
        Id           = "test_card",
        Name         = "Test Card",
        Elements     = new[] { Element.Root },
        TopEffect    = new EffectData { Type = EffectType.DamageInvaders, Value = 2, Range = 1 },
        BottomEffect = new EffectData { Type = EffectType.GenerateFear, Value = 4, Range = 0 },
        UpgradeSlots = new()
        {
            new() { Id = "test_u1", Slot = "top",      Field = "value",  From = 2, To = 3, Cost = 1 },
            new() { Id = "test_u2", Slot = "bottom",   Field = "range",  From = 0, To = 1, Cost = 1 },
            new() { Id = "test_u3", Slot = "elements", Action = "add",   Element = "Shadow", Cost = 1 },
        }
    };

    [Fact]
    public void ApplyUpgrade_ValueBump_ChangesTopValue()
    {
        var card = MakeCardWithUpgrades();
        bool result = CardUpgrader.ApplyUpgrade(card, "test_u1");
        Assert.True(result);
        Assert.Equal(3, card.TopEffect.Value);
        Assert.Equal(EffectType.DamageInvaders, card.TopEffect.Type);
    }

    [Fact]
    public void ApplyUpgrade_AddElement_AddsToArray()
    {
        var card = MakeCardWithUpgrades();
        bool result = CardUpgrader.ApplyUpgrade(card, "test_u3");
        Assert.True(result);
        Assert.Contains(Element.Shadow, card.Elements);
        Assert.Equal(2, card.Elements.Length);
    }

    [Fact]
    public void ApplyUpgrade_AlreadyApplied_ReturnsFalse()
    {
        var card = MakeCardWithUpgrades();
        Assert.True(CardUpgrader.ApplyUpgrade(card, "test_u1"));
        Assert.False(CardUpgrader.ApplyUpgrade(card, "test_u1"));
    }

    [Fact]
    public void ApplyUpgrade_UnknownId_ReturnsFalse()
    {
        var card = MakeCardWithUpgrades();
        bool result = CardUpgrader.ApplyUpgrade(card, "nonexistent_upgrade");
        Assert.False(result);
    }

    [Fact]
    public void WardenLoader_ParsesUpgradeSlots()
    {
        var warden = WardenLoader.Load(GetWardenJsonPath("root"));
        var card = warden.Cards.First(c => c.Id == "root_001");
        Assert.NotEmpty(card.UpgradeSlots);
        Assert.Equal("root_001_u1", card.UpgradeSlots[0].Id);
        Assert.Equal("top", card.UpgradeSlots[0].Slot);
        Assert.Equal("value", card.UpgradeSlots[0].Field);
        Assert.Equal(3, card.UpgradeSlots[0].To);
    }

    [Fact]
    public void ApplyUpgrade_TrackedInAppliedIds()
    {
        var card = MakeCardWithUpgrades();
        CardUpgrader.ApplyUpgrade(card, "test_u1");
        CardUpgrader.ApplyUpgrade(card, "test_u3");
        Assert.Contains("test_u1", card.AppliedUpgradeIds);
        Assert.Contains("test_u3", card.AppliedUpgradeIds);
        Assert.DoesNotContain("test_u2", card.AppliedUpgradeIds);
    }
}
