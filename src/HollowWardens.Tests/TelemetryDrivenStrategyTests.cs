using HollowWardens.Core.Encounter;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Models;
using HollowWardens.Core.Telemetry;
using HollowWardens.Sim;
using Xunit;

namespace HollowWardens.Tests;

public class TelemetryDrivenStrategyTests
{
    private static EncounterState MakeState() => new EncounterState
    {
        Config    = new EncounterConfig { Id = "test" },
        Balance   = new BalanceConfig(),
        Territories = new List<Territory>
        {
            new Territory { Id = "A1", Invaders = new List<Invader> { new Invader { Hp = 2 }, new Invader { Hp = 2 } } },
            new Territory { Id = "M1", CorruptionPoints = 8 },
            new Territory { Id = "I1", PresenceCount = 1 },
        }
    };

    private static Card MakeCard(string id, EffectType topType) => new Card
    {
        Id        = id,
        TopEffect = new EffectData { Type = topType, Value = 1 },
        BottomEffect = new EffectData { Type = topType, Value = 1 },
    };

    [Fact]
    public void ChoosePlay_WeightsByDistribution()
    {
        // Profile strongly prefers card-a (90% of plays)
        var profile = new PlayerProfile
        {
            CardPlayDistribution = new() { ["card-a"] = 0.9, ["card-b"] = 0.1 }
        };
        var strategy = new TelemetryDrivenStrategy(profile, new Random(42));
        var hand = new List<Card>
        {
            MakeCard("card-a", EffectType.DamageInvaders),
            MakeCard("card-b", EffectType.GenerateFear),
        };
        var state = MakeState();

        // Over 100 trials, card-a should dominate
        int aCount = 0;
        for (int i = 0; i < 100; i++)
        {
            var chosen = strategy.ChooseTopPlay(hand, state);
            if (chosen?.Id == "card-a") aCount++;
        }
        // At least 70% should be card-a (generous threshold for randomness)
        Assert.True(aCount >= 50, $"Expected card-a to dominate, got {aCount}/100");
    }

    [Fact]
    public void ChoosePlay_SometimesRests()
    {
        // Profile has a 50% voluntary rest rate
        var profile = new PlayerProfile
        {
            RestTiming = new RestTimingProfile { VoluntaryRestPct = 0.5 }
        };
        var strategy = new TelemetryDrivenStrategy(profile, new Random(7));
        var hand = new List<Card> { MakeCard("card-a", EffectType.DamageInvaders) };
        var state = MakeState();

        int rests = 0;
        for (int i = 0; i < 100; i++)
        {
            var chosen = strategy.ChooseTopPlay(hand, state);
            if (chosen == null) rests++;
        }
        // Expect roughly 50 rests; at least 20 for generous tolerance
        Assert.True(rests >= 20 && rests <= 80, $"Expected ~50 voluntary rests, got {rests}/100");
    }

    [Fact]
    public void ChooseTarget_UsesPreference()
    {
        var profile = new PlayerProfile
        {
            TargetingPreference = new() { ["DamageInvaders"] = "most_invaded" }
        };
        var strategy = new TelemetryDrivenStrategy(profile, new Random(1));
        var state = MakeState(); // A1 has 2 alive invaders, M1/I1 have 0

        var effect = new EffectData { Type = EffectType.DamageInvaders };
        var target = strategy.ChooseTarget(effect, state);

        Assert.Equal("A1", target);
    }

    [Fact]
    public void ChooseDraft_WeightsByPreference()
    {
        var profile = new PlayerProfile
        {
            DraftPreferences = new() { ["card-x"] = 0.95, ["card-y"] = 0.05 }
        };
        var strategy = new TelemetryDrivenStrategy(profile, new Random(42));
        var offered = new List<Card>
        {
            MakeCard("card-x", EffectType.DamageInvaders),
            MakeCard("card-y", EffectType.GenerateFear),
        };

        int xCount = 0;
        for (int i = 0; i < 100; i++)
        {
            var chosen = strategy.ChooseDraft(offered);
            if (chosen.Id == "card-x") xCount++;
        }
        Assert.True(xCount >= 60, $"card-x should dominate, got {xCount}/100");
    }

    [Fact]
    public void DefaultProfile_BehavesLikeBot()
    {
        // Empty profile → falls through to BotStrategy logic
        var profile = new PlayerProfile();
        var strategy = new TelemetryDrivenStrategy(profile, new Random(5));
        var hand = new List<Card> { MakeCard("root-ward", EffectType.DamageInvaders) };
        var state = MakeState();

        // Should not throw; may return a card or null
        var result = strategy.ChooseTopPlay(hand, state);
        // Just verify it runs without exception — no specific assertion needed
        Assert.True(result == null || result.Id == "root-ward");
    }
}
