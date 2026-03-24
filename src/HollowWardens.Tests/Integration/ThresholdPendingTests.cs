namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Verifies the player-driven threshold pending queue:
/// OnThresholdTriggered adds to pending (all tiers), Resolve removes and executes,
/// ClearUnresolved fires ThresholdExpired and clears the queue.
/// </summary>
public class ThresholdPendingTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private static EncounterState BuildState()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, _, _, _, _) = IntegrationHelpers.Build(
            IntegrationHelpers.MakeCards(5), new RootAbility(), config);
        state.GetTerritory("I1")!.PresenceCount = 1;
        return state;
    }

    [Fact]
    public void OnThresholdTriggered_Tier1_GoesToPending()
    {
        var state    = BuildState();
        var resolver = new ThresholdResolver();

        resolver.OnThresholdTriggered(Element.Root, 1, state);

        Assert.Single(resolver.Pending);
        Assert.Equal((Element.Root, 1), resolver.Pending[0]);
    }

    [Fact]
    public void OnThresholdTriggered_Tier2_GoesToPending()
    {
        var state    = BuildState();
        var resolver = new ThresholdResolver();

        resolver.OnThresholdTriggered(Element.Shadow, 2, state);

        Assert.Single(resolver.Pending);
        Assert.Equal((Element.Shadow, 2), resolver.Pending[0]);
    }

    [Fact]
    public void PlayerResolves_Tier1_ExecutesEffect()
    {
        // Root T1: Reduce Corruption by 3 in highest-corruption territory with presence
        var state    = BuildState();
        var resolver = new ThresholdResolver();
        state.GetTerritory("I1")!.CorruptionPoints = 5; // I1 has presence (from BuildState)

        resolver.OnThresholdTriggered(Element.Root, 1, state);
        resolver.Resolve(Element.Root, 1, state);

        Assert.Empty(resolver.Pending);
        Assert.Equal(2, state.GetTerritory("I1")!.CorruptionPoints); // 5 - 3
    }

    [Fact]
    public void PlayerResolves_Tier2_PlacesPresenceAdjacentToExistingPresence()
    {
        // Root T2: Place 1 Presence at range 1
        var state    = BuildState();
        var resolver = new ThresholdResolver();
        int presenceBefore = state.Territories.Sum(t => t.PresenceCount);

        resolver.OnThresholdTriggered(Element.Root, 2, state);
        resolver.Resolve(Element.Root, 2, state);

        Assert.Empty(resolver.Pending);
        Assert.Equal(presenceBefore + 1, state.Territories.Sum(t => t.PresenceCount));
    }

    [Fact]
    public void UnresolvedThreshold_ClearsAtEndOfDusk()
    {
        var state    = BuildState();
        var resolver = new ThresholdResolver();
        var expired  = new List<(Element, int)>();
        GameEvents.ThresholdExpired += (e, t) => expired.Add((e, t));

        resolver.OnThresholdTriggered(Element.Mist, 1, state);
        resolver.OnThresholdTriggered(Element.Ash, 1, state);
        resolver.ClearUnresolved();

        Assert.Empty(resolver.Pending);
        Assert.Equal(2, expired.Count);
        Assert.Contains((Element.Mist, 1), expired);
        Assert.Contains((Element.Ash, 1), expired);
    }

    [Fact]
    public void MultiplePending_ResolveInAnyOrder()
    {
        var state    = BuildState();
        var resolver = new ThresholdResolver();

        resolver.OnThresholdTriggered(Element.Mist,   1, state);
        resolver.OnThresholdTriggered(Element.Shadow, 1, state);
        resolver.Resolve(Element.Shadow, 1, state); // resolve second first

        Assert.Single(resolver.Pending);
        Assert.Equal(Element.Mist, resolver.Pending[0].element);
    }

    // ── D41: Targeting expansion ──────────────────────────────────────────────

    [Theory]
    [InlineData(Element.Ash,  1)]
    [InlineData(Element.Ash,  2)]
    [InlineData(Element.Ash,  3)]  // Ash T3 now presence-scaled — requires target
    [InlineData(Element.Gale, 1)]
    [InlineData(Element.Gale, 2)]
    [InlineData(Element.Root, 1)]
    [InlineData(Element.Root, 2)]
    public void NeedsTarget_ReturnsTrue(Element element, int tier)
    {
        Assert.True(ThresholdResolver.NeedsTarget(element, tier));
    }

    [Theory]
    [InlineData(Element.Mist,   1)]
    [InlineData(Element.Mist,   2)]
    [InlineData(Element.Shadow, 1)]
    [InlineData(Element.Void,   1)]
    [InlineData(Element.Gale,   3)]
    public void NeedsTarget_ReturnsFalse(Element element, int tier)
    {
        Assert.False(ThresholdResolver.NeedsTarget(element, tier));
    }

    [Fact]
    public void AshT1_WithTarget_DamagesSpecificTerritory()
    {
        var state    = BuildState();
        var resolver = new ThresholdResolver();

        var a1 = state.GetTerritory("A1")!;
        var m1 = state.GetTerritory("M1")!;
        a1.Invaders.Add(new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 2, MaxHp = 2, TerritoryId = "A1" });
        m1.Invaders.Add(new Invader { Id = "i2", UnitType = UnitType.Marcher, Hp = 2, MaxHp = 2, TerritoryId = "M1" });

        resolver.OnThresholdTriggered(Element.Ash, 1, state);
        resolver.Resolve(Element.Ash, 1, state, targetTerritoryId: "A1");

        Assert.Equal(1, a1.Invaders[0].Hp); // damaged
        Assert.Equal(2, m1.Invaders[0].Hp); // untouched
    }

    [Fact]
    public void AshT2_WithTarget_DamagesSpecificTerritoryAndAddsCorruption()
    {
        var state    = BuildState();
        var resolver = new ThresholdResolver();

        var m1 = state.GetTerritory("M1")!;
        var a1 = state.GetTerritory("A1")!;
        a1.Invaders.Add(new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 3, MaxHp = 3, TerritoryId = "A1" });
        m1.Invaders.Add(new Invader { Id = "i2", UnitType = UnitType.Marcher, Hp = 3, MaxHp = 3, TerritoryId = "M1" });

        resolver.OnThresholdTriggered(Element.Ash, 2, state);
        resolver.Resolve(Element.Ash, 2, state, targetTerritoryId: "M1");

        Assert.Equal(1, m1.Invaders[0].Hp); // 3 - 2 = 1
        Assert.Equal(3, a1.Invaders[0].Hp); // untouched
        Assert.True(m1.CorruptionPoints > 0); // corruption rider applied
    }

    [Fact]
    public void GaleT2_WithTarget_PushesInvadersFromSpecificTerritory()
    {
        var state    = BuildState();
        var resolver = new ThresholdResolver();

        var m1 = state.GetTerritory("M1")!;
        m1.Invaders.Add(new Invader { Id = "i1", UnitType = UnitType.Marcher, Hp = 2, MaxHp = 2, TerritoryId = "M1" });

        resolver.OnThresholdTriggered(Element.Gale, 2, state);
        resolver.Resolve(Element.Gale, 2, state, targetTerritoryId: "M1");

        // Invader pushed out of M1 (toward spawn/A-row)
        Assert.Empty(m1.Invaders.Where(i => i.IsAlive));
    }

    [Fact]
    public void RootT2_WithTarget_PlacesPresenceInChosenTerritory()
    {
        var state    = BuildState();
        var resolver = new ThresholdResolver();

        resolver.OnThresholdTriggered(Element.Root, 2, state);
        resolver.Resolve(Element.Root, 2, state, targetTerritoryId: "A1");

        Assert.True(state.GetTerritory("A1")!.HasPresence);
    }

    [Fact]
    public void RootT1_WithTarget_ReducesCorruptionInChosenTerritory()
    {
        var state    = BuildState();
        var resolver = new ThresholdResolver();

        var a1 = state.GetTerritory("A1")!;
        var i1 = state.GetTerritory("I1")!;
        // Give both territories presence and corruption so the auto pick would choose A1 (higher)
        a1.PresenceCount    = 1;
        a1.CorruptionPoints = 10;
        i1.CorruptionPoints = 5; // player chooses I1 instead

        resolver.OnThresholdTriggered(Element.Root, 1, state);
        resolver.Resolve(Element.Root, 1, state, targetTerritoryId: "I1");

        Assert.Equal(2, i1.CorruptionPoints);  // 5 - 3
        Assert.Equal(10, a1.CorruptionPoints); // untouched
    }
}
