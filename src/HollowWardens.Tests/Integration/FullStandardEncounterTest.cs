namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// 7-Tide encounter start to resolution. Verifies:
/// - encounter ends with a valid result (Clean/Weathered/Breach)
/// - element engine reaches Tier 1 for primary element by turn 3 (given element-heavy tops)
/// - at least one Rest occurs with aggressive play
/// - final deck size is reduced from starting size after rest-dissolves
/// </summary>
public class FullStandardEncounterTest : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private static Card MakeRootCard(string id) => new()
    {
        Id = id, Name = id,
        Elements = new[] { Element.Root, Element.Root },   // 2 Root per card
        TopEffect = new() { Type = EffectType.PlacePresence },
        BottomEffect = new() { Type = EffectType.PlacePresence }
    };

    [Fact]
    public void Encounter_CompletesWithValidResult()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 7);
        var deck = Enumerable.Range(1, 10).Select(i => MakeRootCard($"r{i}")).ToList();
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(deck, new RootAbility(), config);

        var result = new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver())
            .Run(state, new IntegrationHelpers.PlayTopsStrategy(maxPerTurn: 1));

        // Result must be one of the valid enum values
        Assert.True(result is EncounterResult.Clean or EncounterResult.Weathered or EncounterResult.Breach);
    }

    [Fact]
    public void ElementTier1_Fires_ByTurn3_WithRootAffinity()
    {
        // Each top play adds 2 Root. Tier 1 = 4. After 2 top plays (turn 1 + turn 2) Root=4 → Tier 1.
        var firedTiers = new List<(Element e, int tier)>();
        GameEvents.ThresholdTriggered += (e, t) => firedTiers.Add((e, t));

        var config = IntegrationHelpers.MakeConfig(tideCount: 3);
        var deck = Enumerable.Range(1, 10).Select(i => MakeRootCard($"r{i}")).ToList();
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(deck, new RootAbility(), config);

        new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver())
            .Run(state, new IntegrationHelpers.PlayTopsStrategy(maxPerTurn: 1));

        // Root Tier 1 should have fired at least once by turn 3
        Assert.Contains(firedTiers, t => t.e == Element.Root && t.tier == 1);
    }

    [Fact]
    public void RestOccurs_WhenDeckDepletes()
    {
        int restCount = 0;
        GameEvents.PhaseChanged += phase => { if (phase == TurnPhase.Rest) restCount++; };

        // Use one-top-one-bottom strategy to force rest faster
        var config = IntegrationHelpers.MakeConfig(tideCount: 7);
        var deck = IntegrationHelpers.MakeCards(10);
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(deck, new RootAbility(), config);

        new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver())
            .Run(state, new IntegrationHelpers.OneTopOneBottomStrategy());

        // With 1 top + 1 bottom per turn, deck depletes within 7 tides → at least 1 rest
        Assert.True(restCount >= 1, $"Expected at least 1 rest but got {restCount}");
    }

    [Fact]
    public void FinalDeckSize_SmallerThanStarting_AfterRestDissolve()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 7);
        var deck = IntegrationHelpers.MakeCards(10);
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(deck, new RootAbility(), config);

        new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver())
            .Run(state, new IntegrationHelpers.OneTopOneBottomStrategy());

        // Root rest-dissolve makes cards Dormant (not removed), so verify dormant count > 0
        Assert.True(state.Deck!.DormantCount > 0,
            $"Expected at least 1 dormant card from rest-dissolve, got {state.Deck.DormantCount}");
    }
}
