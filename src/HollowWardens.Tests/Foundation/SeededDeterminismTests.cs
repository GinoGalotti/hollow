namespace HollowWardens.Tests.Foundation;

using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;
using Xunit;

public class SeededDeterminismTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Card MakeCard(string id) => new() { Id = id, Name = id };
    private static List<Card> MakeCards(int count) =>
        Enumerable.Range(1, count).Select(i => MakeCard($"card{i}")).ToList();

    private static ActionCard MakePainful(string id) =>
        new() { Id = id, Name = id, Pool = ActionPool.Painful, AdvanceModifier = 1 };

    // Warden whose rest-dissolve returns Dissolved (not Dormant) so InsertRandom isn't called
    private class DissolveWarden : IWardenAbility
    {
        public string WardenId => "test";
        public BottomResult OnBottomPlayed(Card card, EncounterTier tier) => BottomResult.Dissolved;
        public BottomResult OnRestDissolve(Card card) => BottomResult.Dissolved;
        public void OnResolution(EncounterState state) { }
        public int CalculatePassiveFear() => 0;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void DeckShuffleDeterministic()
    {
        // Two DeckManagers with the same seed: after Rest (which shuffles), draw order is identical.
        var warden = new DissolveWarden();
        int seed   = 99;

        var dm1 = new DeckManager(warden, MakeCards(10), GameRandom.FromSeed(seed), shuffle: false);
        var dm2 = new DeckManager(warden, MakeCards(10), GameRandom.FromSeed(seed), shuffle: false);

        // Play all tops into discard, then Rest (triggers seeded shuffle)
        dm1.RefillHand(); foreach (var c in dm1.Hand.ToList()) dm1.PlayTop(c);
        dm1.RefillHand(); foreach (var c in dm1.Hand.ToList()) dm1.PlayTop(c);
        dm1.Rest();

        dm2.RefillHand(); foreach (var c in dm2.Hand.ToList()) dm2.PlayTop(c);
        dm2.RefillHand(); foreach (var c in dm2.Hand.ToList()) dm2.PlayTop(c);
        dm2.Rest();

        // Refill both and compare card sequence
        dm1.RefillHand();
        dm2.RefillHand();

        var hand1 = dm1.Hand.Select(c => c.Id).ToList();
        var hand2 = dm2.Hand.Select(c => c.Id).ToList();

        Assert.Equal(hand1, hand2);
    }

    [Fact]
    public void SpawnWaveSelectionDeterministic()
    {
        var wave = new SpawnWave
        {
            TurnNumber    = 1,
            ArrivalPoints = new() { "A1" },
            Options       = new()
            {
                new SpawnWaveOption { Weight = 50, Units = new() { ["A1"] = new() { UnitType.Marcher   } } },
                new SpawnWaveOption { Weight = 50, Units = new() { ["A1"] = new() { UnitType.Ironclad  } } }
            }
        };

        var sm1 = new SpawnManager(new[] { wave }, GameRandom.FromSeed(77));
        var sm2 = new SpawnManager(new[] { wave }, GameRandom.FromSeed(77));

        var opt1 = sm1.RevealComposition(wave);
        var opt2 = sm2.RevealComposition(wave);

        Assert.NotNull(opt1);
        Assert.NotNull(opt2);
        Assert.Equal(opt1.Units["A1"][0], opt2.Units["A1"][0]);
    }

    [Fact]
    public void ActionDeckDrawDeterministic()
    {
        // Three painful cards; drawing 4 times forces a reshuffle on the 4th draw.
        // Both decks with the same seed must produce identical draw sequences.
        var cards = new[]
        {
            MakePainful("p1"),
            MakePainful("p2"),
            MakePainful("p3"),
        };

        var deck1 = new ActionDeck(cards, Array.Empty<ActionCard>(), GameRandom.FromSeed(11), shuffle: true);
        var deck2 = new ActionDeck(cards, Array.Empty<ActionCard>(), GameRandom.FromSeed(11), shuffle: true);

        var draws1 = Enumerable.Range(0, 4).Select(_ => deck1.Draw(ActionPool.Painful).Id).ToList();
        var draws2 = Enumerable.Range(0, 4).Select(_ => deck2.Draw(ActionPool.Painful).Id).ToList();

        Assert.Equal(draws1, draws2);
    }
}
