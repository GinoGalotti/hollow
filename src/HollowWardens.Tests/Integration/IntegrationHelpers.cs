namespace HollowWardens.Tests.Integration;

using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders.PaleMarch;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;

/// <summary>
/// Shared helpers for integration tests.
/// </summary>
internal static class IntegrationHelpers
{
    internal static Card MakeCard(string id, Element[]? elements = null) => new()
    {
        Id = id, Name = id,
        Elements = elements ?? Array.Empty<Element>(),
        TopEffect = new() { Type = EffectType.PlacePresence },
        BottomEffect = new() { Type = EffectType.PlacePresence }
    };

    internal static Card MakeFearCard(string id, int fearValue = 1) => new()
    {
        Id = id, Name = id,
        Elements = Array.Empty<Element>(),
        TopEffect = new() { Type = EffectType.GenerateFear, Value = fearValue },
        BottomEffect = new() { Type = EffectType.GenerateFear, Value = fearValue }
    };

    internal static List<Card> MakeCards(int count) =>
        Enumerable.Range(1, count).Select(i => MakeCard($"card{i}")).ToList();

    internal static EncounterConfig MakeConfig(
        int tideCount = 7,
        EncounterTier tier = EncounterTier.Standard,
        CadenceConfig? cadence = null) => new()
    {
        Id = "test_enc",
        Tier = tier,
        FactionId = "pale_march",
        TideCount = tideCount,
        Cadence = cadence ?? new CadenceConfig { Mode = "rule_based", MaxPainfulStreak = 1, EasyFrequency = 2 }
    };

    internal static (
        EncounterState state,
        ActionDeck actionDeck,
        CadenceManager cadence,
        SpawnManager spawn,
        PaleMarchFaction faction
    ) Build(
        List<Card> deck,
        IWardenAbility warden,
        EncounterConfig config,
        int weave = 20,
        bool shuffleDeck = false,
        int handLimit = 5)
    {
        var territories = BoardState.CreatePyramid().Territories.Values.ToList();
        var dread = new DreadSystem();
        var fearPools = new Dictionary<int, List<FearActionData>>
        {
            [1] = new() { new() { Id = "fa_d1", DreadLevel = 1, Effect = new() { Type = EffectType.GenerateFear, Value = 0 } } },
            [2] = new() { new() { Id = "fa_d2", DreadLevel = 2, Effect = new() { Type = EffectType.GenerateFear, Value = 0 } } },
        };
        var presence = new PresenceSystem(() => territories);

        var state = new EncounterState
        {
            Config = config,
            Territories = territories,
            Elements = new ElementSystem(),
            Dread = dread,
            Weave = new WeaveSystem(weave),
            Combat = new CombatSystem(),
            Presence = presence,
            Corruption = new CorruptionSystem(),
            FearActions = new FearActionSystem(dread, fearPools, GameRandom.FromSeed(42)),
            Warden = warden
        };

        state.Deck = new DeckManager(warden, deck, rng: GameRandom.FromSeed(42), handLimit: handLimit, shuffle: shuffleDeck);

        var faction = new PaleMarchFaction();
        var actionDeck = new ActionDeck(faction.BuildPainfulPool(), faction.BuildEasyPool(), rng: GameRandom.FromSeed(0), shuffle: false);
        var cadence = new CadenceManager(config.Cadence);
        var spawn = new SpawnManager(config.Waves, rng: GameRandom.FromSeed(0));

        return (state, actionDeck, cadence, spawn, faction);
    }

    /// <summary>Strategy that never plays any cards.</summary>
    internal sealed class IdleStrategy : IPlayerStrategy
    {
        public Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state) => null;
        public Card? ChooseBottomPlay(IReadOnlyList<Card> hand, EncounterState state) => null;
        public Dictionary<Invader, int>? AssignCounterDamage(Territory territory, int pool, EncounterState state) => null;
    }

    /// <summary>Strategy that always plays tops only (first non-dormant card).</summary>
    internal sealed class PlayTopsStrategy : IPlayerStrategy
    {
        private readonly int _maxPerTurn;
        private int _playedThisTurn;

        public PlayTopsStrategy(int maxPerTurn = 1) => _maxPerTurn = maxPerTurn;

        public Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state)
        {
            if (_playedThisTurn >= _maxPerTurn) { _playedThisTurn = 0; return null; }
            var card = hand.FirstOrDefault(c => !c.IsDormant);
            if (card != null) _playedThisTurn++;
            return card;
        }

        public Card? ChooseBottomPlay(IReadOnlyList<Card> hand, EncounterState state)
        { _playedThisTurn = 0; return null; }

        public Dictionary<Invader, int>? AssignCounterDamage(Territory territory, int pool, EncounterState state) => null;
    }

    /// <summary>Strategy that plays one top in Vigil and one bottom in Dusk.</summary>
    internal sealed class OneTopOneBottomStrategy : IPlayerStrategy
    {
        private bool _playedTop;
        private bool _playedBottom;

        public Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state)
        {
            if (_playedTop) { _playedTop = false; return null; }
            var c = hand.FirstOrDefault(x => !x.IsDormant);
            if (c != null) _playedTop = true;
            return c;
        }

        public Card? ChooseBottomPlay(IReadOnlyList<Card> hand, EncounterState state)
        {
            if (_playedBottom) { _playedBottom = false; return null; }
            var c = hand.FirstOrDefault(x => !x.IsDormant);
            if (c != null) _playedBottom = true;
            return c;
        }

        public Dictionary<Invader, int>? AssignCounterDamage(Territory territory, int pool, EncounterState state) => null;
    }
}
