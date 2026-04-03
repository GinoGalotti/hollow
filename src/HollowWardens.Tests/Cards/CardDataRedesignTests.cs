namespace HollowWardens.Tests.Cards;

using HollowWardens.Core.Data;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using Xunit;

/// <summary>Tests for Task 5: Card Data Redesign — Fast/Slow timing, new effect types.</summary>
public class CardDataRedesignTests
{
    private static string GetRootPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "wardens", "root.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("data/wardens/root.json not found");
    }

    private static string GetEmberPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "wardens", "ember.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("data/wardens/ember.json not found");
    }

    // ── Root timing assignments ─────────────────────────────────────────────

    [Theory]
    [InlineData("root_001")] // Tendrils — cleanse is time-sensitive
    [InlineData("root_003")] // Earthen Mending — cleanse before corruption ticks
    [InlineData("root_004")] // Shiver — fear before Tide
    [InlineData("root_005")] // Forest Remembers — fear/move before Tide
    [InlineData("root_008")] // Stir the Sleeping — spawn before Tide acts
    public void Root_FastCards_AreTaggedFast(string cardId)
    {
        var warden = WardenLoader.Load(GetRootPath());
        var card = warden.Cards.Single(c => c.Id == cardId);
        Assert.Equal(CardTiming.Fast, card.TopTiming);
    }

    [Theory]
    [InlineData("root_002")] // Deep Roots — placement resolves after seeing Tide
    [InlineData("root_006")] // Spreading Growth — expansion after Tide
    [InlineData("root_007")] // Healing Earth — sustain after Tide
    [InlineData("root_009")] // Living Wall — reactive
    [InlineData("root_025")] // Grasping Roots — damage + push is reactive
    public void Root_SlowCards_AreTaggedSlow(string cardId)
    {
        var warden = WardenLoader.Load(GetRootPath());
        var card = warden.Cards.Single(c => c.Id == cardId);
        Assert.Equal(CardTiming.Slow, card.TopTiming);
    }

    // ── Ember timing assignments ────────────────────────────────────────────

    [Theory]
    [InlineData("ember_001")] // Flame Burst — FAST damage before Tide
    [InlineData("ember_004")] // Smoke Screen — FAST fear before Tide
    [InlineData("ember_005")] // Stoke the Fire — FAST cleanse
    [InlineData("ember_007")] // Heat Shimmer — FAST weave regen
    public void Ember_FastCards_AreTaggedFast(string cardId)
    {
        var warden = WardenLoader.Load(GetEmberPath());
        var card = warden.Cards.Single(c => c.Id == cardId);
        Assert.Equal(CardTiming.Fast, card.TopTiming);
    }

    [Theory]
    [InlineData("ember_002")] // Kindle — SLOW placement
    [InlineData("ember_003")] // Burning Ground — SLOW AoE + corruption
    [InlineData("ember_006")] // Ember Spread — SLOW expansion
    [InlineData("ember_008")] // Conflagration — SLOW finisher
    public void Ember_SlowCards_AreTaggedSlow(string cardId)
    {
        var warden = WardenLoader.Load(GetEmberPath());
        var card = warden.Cards.Single(c => c.Id == cardId);
        Assert.Equal(CardTiming.Slow, card.TopTiming);
    }

    // ── AddCorruption on Ember cards ────────────────────────────────────────

    [Fact]
    public void BurningGround_Bottom_HasAddCorruptionSecondary()
    {
        var warden = WardenLoader.Load(GetEmberPath());
        var card = warden.Cards.Single(c => c.Id == "ember_003");
        Assert.NotNull(card.BottomSecondary);
        Assert.Equal(EffectType.AddCorruption, card.BottomSecondary!.Type);
        Assert.Equal(2, card.BottomSecondary.Value);
    }

    [Fact]
    public void EmberSpread_Bottom_HasAddCorruptionSecondary()
    {
        var warden = WardenLoader.Load(GetEmberPath());
        var card = warden.Cards.Single(c => c.Id == "ember_006");
        Assert.NotNull(card.BottomSecondary);
        Assert.Equal(EffectType.AddCorruption, card.BottomSecondary!.Type);
        Assert.Equal(1, card.BottomSecondary.Value);
    }

    // ── New effect types resolve without crashing ───────────────────────────

    private static EncounterState MakeState(params Territory[] territories)
    {
        var state = new EncounterState
        {
            Corruption = new CorruptionSystem(),
            Graph = TerritoryGraph.Create("standard")
        };
        foreach (var t in territories)
            state.Territories.Add(t);
        return state;
    }

    [Fact]
    public void AddCorruptionEffect_AddsCorruptionToTerritory()
    {
        var t = new Territory { Id = "I1", CorruptionPoints = 0 };
        var state = MakeState(t);
        var effect = new AddCorruptionEffect(new EffectData { Type = EffectType.AddCorruption, Value = 3 });

        effect.Resolve(state, new TargetInfo { TerritoryId = "I1" });

        Assert.Equal(3, t.CorruptionPoints);
    }

    [Fact]
    public void PullInvadersEffect_PullsFromAdjacent()
    {
        var state = new EncounterState
        {
            Corruption = new CorruptionSystem(),
            Graph = TerritoryGraph.Create("standard")
        };
        // Standard layout: I1 heart, M1 mid, M2 mid, A1 arrival, A2 arrival, A3 arrival
        foreach (var id in state.Graph.AllTerritoryIds)
            state.Territories.Add(new Territory { Id = id });

        // Place invader in A1 (adjacent to M1)
        var tA1 = state.GetTerritory("A1")!;
        var tM1 = state.GetTerritory("M1")!;
        var invader = new Invader { Hp = 3, MaxHp = 3, TerritoryId = "A1" };
        tA1.Invaders.Add(invader);

        var effect = new PullInvadersEffect(new EffectData { Type = EffectType.PullInvaders, Value = 5 });
        effect.Resolve(state, new TargetInfo { TerritoryId = "M1" });

        // Invader should have moved from A1 to M1
        Assert.Empty(tA1.Invaders);
        Assert.Single(tM1.Invaders);
    }

    [Fact]
    public void CorruptionDetonateEffect_DealsDamageEqualToCorruptionAndCleanses()
    {
        var t = new Territory { Id = "A1", CorruptionPoints = 6 };
        var invader = new Invader { Hp = 10, MaxHp = 10, TerritoryId = "A1" };
        t.Invaders.Add(invader);
        var state = MakeState(t);

        var effect = new CorruptionDetonateEffect(new EffectData { Type = EffectType.CorruptionDetonate, Value = 1 });
        effect.Resolve(state, new TargetInfo { TerritoryId = "A1" });

        Assert.Equal(4, invader.Hp);           // 10 - 6 = 4
        Assert.Equal(0, t.CorruptionPoints);   // cleansed
    }

    [Fact]
    public void EffectResolver_ResolvesNewEffectTypes()
    {
        var resolver = new EffectResolver();
        Assert.NotNull(resolver.Resolve(new EffectData { Type = EffectType.AddCorruption, Value = 1 }));
        Assert.NotNull(resolver.Resolve(new EffectData { Type = EffectType.PullInvaders, Value = 2 }));
        Assert.NotNull(resolver.Resolve(new EffectData { Type = EffectType.CorruptionDetonate, Value = 1 }));
    }
}
