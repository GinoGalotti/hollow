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
        var state    = BuildState();
        var resolver = new ThresholdResolver();
        int presenceBefore = state.Territories.Sum(t => t.PresenceCount);

        resolver.OnThresholdTriggered(Element.Root, 1, state);
        resolver.Resolve(Element.Root, 1, state);

        Assert.Empty(resolver.Pending);
        Assert.Equal(presenceBefore + 1, state.Territories.Sum(t => t.PresenceCount));
    }

    [Fact]
    public void PlayerResolves_Tier2_ReducesCorruptionInHighestPresenceTerritory()
    {
        // Root T2: Reduce Corruption by 3 in the territory with presence and highest corruption
        var state    = BuildState();
        var resolver = new ThresholdResolver();

        state.GetTerritory("I1")!.CorruptionPoints = 7; // I1 has presence=1 (from BuildState)

        resolver.OnThresholdTriggered(Element.Root, 2, state);
        resolver.Resolve(Element.Root, 2, state);

        Assert.Empty(resolver.Pending);
        Assert.Equal(4, state.GetTerritory("I1")!.CorruptionPoints); // 7 - 3
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
}
