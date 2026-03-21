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
    public void RootTier1_PlacesPresence_AtRangeOneFromExistingPresence()
    {
        var state = BuildState();
        var resolver = new ThresholdResolver();

        // I1 has presence; M1 and M2 are adjacent and should receive the new presence
        int presenceBefore = state.Territories.Sum(t => t.PresenceCount);

        resolver.AutoResolve(Element.Root, 1, state);

        int presenceAfter = state.Territories.Sum(t => t.PresenceCount);
        Assert.Equal(presenceBefore + 1, presenceAfter);

        // The new presence must be adjacent to I1 (M1 or M2)
        bool placedNearI1 = state.GetTerritory("M1")!.PresenceCount > 0
                         || state.GetTerritory("M2")!.PresenceCount > 0;
        Assert.True(placedNearI1);
    }

    [Fact]
    public void MistTier1_RestoresOneWeave()
    {
        var state = BuildState(weave: 15);
        var resolver = new ThresholdResolver();

        resolver.AutoResolve(Element.Mist, 1, state);

        Assert.Equal(16, state.Weave!.CurrentWeave);
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
    public void VoidTier1_DamagesLowestHpInvader()
    {
        var state = BuildState();
        var (_, _, _, _, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(),
                IntegrationHelpers.MakeConfig(tideCount: 1));

        // Place a Marcher (HP 3) and an Outrider (HP 2) — Void T1 should hit the Outrider
        var marcher  = faction.CreateUnit(UnitType.Marcher,  "M1");
        var outrider = faction.CreateUnit(UnitType.Outrider, "M2");
        state.GetTerritory("M1")!.Invaders.Add(marcher);
        state.GetTerritory("M2")!.Invaders.Add(outrider);

        var resolver = new ThresholdResolver();
        resolver.AutoResolve(Element.Void, 1, state);

        // Outrider (lowest HP) should take 1 damage
        Assert.Equal(outrider.MaxHp - 1, outrider.Hp);
        // Marcher should be untouched
        Assert.Equal(marcher.MaxHp, marcher.Hp);
    }
}
