namespace HollowWardens.Tests;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Systems;
using Xunit;

/// <summary>
/// Tests for BotStrategy — verifies decision-making for card selection and targeting.
/// </summary>
public class BotStrategyTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Territory MakeTerritory(string id, int presence = 0, int corruption = 0)
    {
        var row = id[0] switch { 'A' => TerritoryRow.Arrival, 'M' => TerritoryRow.Middle, _ => TerritoryRow.Inner };
        return new Territory { Id = id, Row = row, PresenceCount = presence, CorruptionPoints = corruption };
    }

    private static EncounterState MakeState(params Territory[] territories)
    {
        var state = new EncounterState();
        state.Territories.AddRange(territories);
        state.Presence   = new PresenceSystem(() => state.Territories);
        state.Corruption = new CorruptionSystem();
        state.Weave      = new WeaveSystem(20);
        return state;
    }

    private static Card MakeCard(string id, EffectType topType, EffectType bottomType,
        int topRange = 1, int bottomRange = 1, bool dormant = false)
        => new()
        {
            Id         = id,
            Name       = id,
            IsDormant  = dormant,
            TopEffect  = new EffectData { Type = topType,    Value = 2, Range = topRange    },
            BottomEffect = new EffectData { Type = bottomType, Value = 2, Range = bottomRange }
        };

    private static Invader MakeInvader(string tId)
        => new() { Hp = 3, MaxHp = 3, UnitType = UnitType.Marcher, TerritoryId = tId };

    // ── Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public void ChooseTopPlay_ReturnsCard_WhenPlayableCardsExist()
    {
        var state = MakeState(
            MakeTerritory("M1", presence: 2),
            MakeTerritory("M2", presence: 2),
            MakeTerritory("I1", presence: 2));
        var card = MakeCard("c1", EffectType.DamageInvaders, EffectType.GenerateFear);
        state.GetTerritory("M1")!.Invaders.Add(MakeInvader("M1"));
        var hand = new List<Card> { card };

        var bot    = new BotStrategy();
        var chosen = bot.ChooseTopPlay(hand, state);

        Assert.NotNull(chosen);
    }

    [Fact]
    public void ChooseTopPlay_ReturnsNull_WhenAllCardsDormant()
    {
        var state = MakeState(MakeTerritory("I1", presence: 3));
        var hand = new List<Card>
        {
            MakeCard("c1", EffectType.DamageInvaders, EffectType.GenerateFear, dormant: true),
            MakeCard("c2", EffectType.PlacePresence,  EffectType.RestoreWeave, dormant: true),
        };

        var bot    = new BotStrategy();
        var chosen = bot.ChooseTopPlay(hand, state);

        Assert.Null(chosen);
    }

    [Fact]
    public void ChooseTopPlay_PrioritizesPresence_WhenFewerThan3PresenceTerritories()
    {
        // Only 1 territory with presence — should pick PlacePresence card first
        var state = MakeState(
            MakeTerritory("A1"),
            MakeTerritory("M1", presence: 2),
            MakeTerritory("I1"));

        var presenceCard = MakeCard("p1", EffectType.PlacePresence, EffectType.GenerateFear);
        var damageCard   = MakeCard("d1", EffectType.DamageInvaders, EffectType.GenerateFear);
        state.GetTerritory("M1")!.Invaders.Add(MakeInvader("M1"));
        var hand = new List<Card> { damageCard, presenceCard };

        var bot    = new BotStrategy();
        var chosen = bot.ChooseTopPlay(hand, state);

        Assert.Equal("p1", chosen?.Id);
    }

    [Fact]
    public void ChooseTopPlay_PrioritizesDamage_WhenInvadersPresent_And3OrMorePresenceTerritories()
    {
        // 3 territories with presence → no presence card priority
        var state = MakeState(
            MakeTerritory("A1", presence: 1),
            MakeTerritory("M1", presence: 1),
            MakeTerritory("I1", presence: 1));
        state.GetTerritory("A1")!.Invaders.Add(MakeInvader("A1"));

        var damageCard = MakeCard("d1", EffectType.DamageInvaders, EffectType.GenerateFear);
        var fearCard   = MakeCard("f1", EffectType.GenerateFear,   EffectType.RestoreWeave);
        var hand = new List<Card> { fearCard, damageCard };

        var bot    = new BotStrategy();
        var chosen = bot.ChooseTopPlay(hand, state);

        Assert.Equal("d1", chosen?.Id);
    }

    [Fact]
    public void ChooseTarget_DamageInvaders_PicksMostInvadedTerritory()
    {
        var state = MakeState(
            MakeTerritory("A1", presence: 1),
            MakeTerritory("A2"),
            MakeTerritory("M1"));
        // A2 has 3 invaders, A1 has 1
        state.GetTerritory("A1")!.Invaders.Add(MakeInvader("A1"));
        state.GetTerritory("A2")!.Invaders.AddRange(new[] {
            MakeInvader("A2"), MakeInvader("A2"), MakeInvader("A2")
        });

        var bot    = new BotStrategy();
        var effect = new EffectData { Type = EffectType.DamageInvaders, Value = 2, Range = 2 };
        var target = bot.ChooseTarget(effect, state);

        Assert.Equal("A2", target);
    }

    [Fact]
    public void ChooseTarget_ReduceCorruption_PicksMostCorruptedTerritory()
    {
        var state = MakeState(
            MakeTerritory("M1", presence: 1, corruption: 3),
            MakeTerritory("M2", corruption: 8),
            MakeTerritory("I1", corruption: 1));

        var bot    = new BotStrategy();
        var effect = new EffectData { Type = EffectType.ReduceCorruption, Value = 3, Range = 2 };
        var target = bot.ChooseTarget(effect, state);

        Assert.Equal("M2", target);
    }

    [Fact]
    public void ChooseTarget_PlacePresence_PrefersAdjacentNonDefiledTerritory()
    {
        // I1 has presence; adjacent are M1 and M2 (TerritoryGraph)
        var state = MakeState(
            MakeTerritory("A1"),
            MakeTerritory("A2"),
            MakeTerritory("A3"),
            MakeTerritory("M1"),
            MakeTerritory("M2"),
            MakeTerritory("I1", presence: 2));

        var bot    = new BotStrategy();
        var effect = new EffectData { Type = EffectType.PlacePresence, Value = 1, Range = 1 };
        var target = bot.ChooseTarget(effect, state);

        // Should be M1 or M2 (adjacent to I1, no presence, not Defiled)
        Assert.True(target == "M1" || target == "M2",
            $"Expected M1 or M2 but got '{target}'");
    }

    [Fact]
    public void ChooseRestGrowthTarget_ReturnsHighestPresenceNonDefiledTerritory()
    {
        var state = MakeState(
            MakeTerritory("M1", presence: 1),
            MakeTerritory("M2", presence: 3),
            MakeTerritory("I1", presence: 2));

        var bot    = new BotStrategy();
        var target = bot.ChooseRestGrowthTarget(state);

        Assert.Equal("M2", target);
    }

    [Fact]
    public void ChooseRestGrowthTarget_ReturnsNull_WhenAllPresenceTerritoriesDefiled()
    {
        // CorruptionLevel 2 = Defiled when CorruptionPoints >= 8
        // BotStrategy skips territories with CorruptionLevel >= 2
        var state = MakeState(
            MakeTerritory("M1", presence: 2, corruption: 10),  // level 2 = Defiled
            MakeTerritory("M2", presence: 1, corruption: 8));   // level 2 = Defiled

        var bot    = new BotStrategy();
        var target = bot.ChooseRestGrowthTarget(state);

        Assert.Null(target);
    }
}
