namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Verifies that ThresholdResolver.AutoResolve correctly handles Tier 1 effects
/// for each element: Root (presence), Mist (weave), Shadow (fear), Void (damage).
/// </summary>
public class ThresholdResolverTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private static EncounterState BuildState(int weave = 20)
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, _, _, _, _) = IntegrationHelpers.Build(
            IntegrationHelpers.MakeCards(5), new RootAbility(), config, weave: weave);
        // Place starting presence on I1 so Root T1 has a source
        state.GetTerritory("I1")!.PresenceCount = 1;
        return state;
    }

    [Fact]
    public void RootTier1_ReducesCorruptionByThree_InHighestCorruptPresenceTerritory()
    {
        var state = BuildState();
        // I1 has presence (set in BuildState); give it some corruption
        state.GetTerritory("I1")!.CorruptionPoints = 8;
        var resolver = new ThresholdResolver();

        resolver.AutoResolve(Element.Root, 1, state);

        Assert.Equal(5, state.GetTerritory("I1")!.CorruptionPoints);
    }

    [Fact]
    public void RootTier1_DoesNothing_WhenNoPresenceTerritoriesHaveCorruption()
    {
        var state = BuildState();
        // I1 has presence but 0 corruption — nothing to reduce
        var resolver = new ThresholdResolver();

        resolver.AutoResolve(Element.Root, 1, state);

        // No corruption anywhere, no change
        Assert.Equal(0, state.Territories.Sum(t => t.CorruptionPoints));
    }

    [Fact]
    public void MistTier1_RestoresTwoWeave()
    {
        var state = BuildState(weave: 15);
        var resolver = new ThresholdResolver();

        resolver.AutoResolve(Element.Mist, 1, state);

        Assert.Equal(17, state.Weave!.CurrentWeave);
    }

    [Fact]
    public void ShadowTier1_GeneratesTwoFear()
    {
        var state = BuildState();
        var resolver = new ThresholdResolver();

        int fearBefore = state.Dread!.TotalFearGenerated;
        resolver.AutoResolve(Element.Shadow, 1, state);

        Assert.Equal(fearBefore + 2, state.Dread.TotalFearGenerated);
    }

    [Fact]
    public void VoidTier1_DealThreeDamageToLowestHpInvader()
    {
        var state = BuildState();
        var (_, _, _, _, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(),
                IntegrationHelpers.MakeConfig(tideCount: 1));

        // Marcher (HP 4) and Outrider (HP 3) — Void T1 targets lowest HP = Outrider, deals 3 damage
        var marcher  = faction.CreateUnit(UnitType.Marcher,  "M1");
        var outrider = faction.CreateUnit(UnitType.Outrider, "M2");
        state.GetTerritory("M1")!.Invaders.Add(marcher);
        state.GetTerritory("M2")!.Invaders.Add(outrider);

        var resolver = new ThresholdResolver();
        resolver.AutoResolve(Element.Void, 1, state);

        // Outrider (lowest HP=3) takes 3 damage → dead
        Assert.False(outrider.IsAlive);
        // Marcher (HP=4) untouched
        Assert.Equal(marcher.MaxHp, marcher.Hp);
    }
}
