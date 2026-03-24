namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Cards;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Aggressive bottom play in turns 1-3 depletes the deck faster,
/// resulting in smaller Resolution hands than conservative play.
/// </summary>
public class FrontloadingPenaltyTest : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private class GenericWarden : IWardenAbility
    {
        public string WardenId => "generic";
        public BottomResult OnBottomPlayed(Card card, EncounterTier tier) =>
            tier == EncounterTier.Boss ? BottomResult.PermanentlyRemoved : BottomResult.Dissolved;
        public BottomResult OnRestDissolve(Card card) => BottomResult.Dissolved;
        public void OnResolution(HollowWardens.Core.Encounter.EncounterState state) { }
        public int CalculatePassiveFear(HollowWardens.Core.Encounter.EncounterState state) => 0;
    }

    /// <summary>Plays N bottoms in Dusk, no tops.</summary>
    private class AggressiveBottomStrategy : IPlayerStrategy
    {
        private readonly int _bottomsPerTurn;
        private int _playedThisDusk;

        public AggressiveBottomStrategy(int bottomsPerTurn) => _bottomsPerTurn = bottomsPerTurn;

        public Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state) => null;

        public Card? ChooseBottomPlay(IReadOnlyList<Card> hand, EncounterState state)
        {
            if (_playedThisDusk >= _bottomsPerTurn) { _playedThisDusk = 0; return null; }
            var c = hand.FirstOrDefault(x => !x.IsDormant);
            if (c != null) _playedThisDusk++;
            return c;
        }

        public Dictionary<Invader, int>? AssignCounterDamage(Territory t, int p, EncounterState s) => null;
    }

    [Fact]
    public void AggressiveBottomPlay_LeavesSmallResolutionHand()
    {
        // Aggressive: 2 bottoms per turn. After 3 tides (with rests), deck is thin.
        // Resolution should have fewer cards than a passive run.
        var config = IntegrationHelpers.MakeConfig(tideCount: 3);
        var deck = IntegrationHelpers.MakeCards(10);
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(deck, new GenericWarden(), config);

        int resolutionHandSize = -1;
        GameEvents.ResolutionTurnStarted += turn =>
        {
            if (turn == 1)
                resolutionHandSize = state.Deck?.Hand.Count ?? -1;
        };

        new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver())
            .Run(state, new AggressiveBottomStrategy(bottomsPerTurn: 2));

        // With 10 cards, 2 bottoms per turn, 3 tides + forced rests,
        // the deck should be thinner than 5 (full hand) by resolution.
        Assert.True(resolutionHandSize < 5,
            $"Expected resolution hand < 5 but got {resolutionHandSize}. Frontloading should deplete deck.");
    }

    [Fact]
    public void ConservativePlay_HasFullerResolutionHand()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 3);
        var deck = IntegrationHelpers.MakeCards(10);
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(deck, new GenericWarden(), config);

        int resolutionHandSize = -1;
        GameEvents.ResolutionTurnStarted += turn =>
        {
            if (turn == 1)
                resolutionHandSize = state.Deck?.Hand.Count ?? -1;
        };

        // Idle strategy: no card plays at all
        new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver())
            .Run(state, new IntegrationHelpers.IdleStrategy());

        // Without any plays, deck stays full → hand at resolution should be 5
        Assert.Equal(5, resolutionHandSize);
    }
}
