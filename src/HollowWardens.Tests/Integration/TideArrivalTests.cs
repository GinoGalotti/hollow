namespace HollowWardens.Tests.Integration;

using HollowWardens.Core;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Verifies the tide arrival wave offset:
/// SpawnInitialWave spawns Wave 1 before Tide 1.
/// Tide N's Arrive step spawns Wave N+1 (not Wave N).
/// </summary>
public class TideArrivalTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private static SpawnWave MakeWave(int turnNumber, string territoryId, UnitType unitType) =>
        new()
        {
            TurnNumber = turnNumber,
            Options = new List<SpawnWaveOption>
            {
                new() { Weight = 1, Units = new Dictionary<string, List<UnitType>> { [territoryId] = new() { unitType } } }
            }
        };

    private static ActionCard MarchCard() =>
        new() { Id = "pm_march", Name = "March", Pool = ActionPool.Painful, AdvanceModifier = 1 };

    [Fact]
    public void Tide1_SpawnsWave2_NotWave1()
    {
        // Wave 1 = Marcher (pre-spawn); Wave 2 = Ironclad (Tide 1 Arrive step)
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        config.Waves.Add(MakeWave(1, "A1", UnitType.Marcher));
        config.Waves.Add(MakeWave(2, "A1", UnitType.Ironclad));

        var (state, _, cadence, _, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        var spawn = new SpawnManager(config.Waves, GameRandom.FromSeed(0));
        var marchCard = MarchCard();
        var actionDeck = new ActionDeck(new[] { marchCard }, new[] { marchCard }, rng: GameRandom.FromSeed(0), shuffle: false);
        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());

        tideRunner.SpawnInitialWave(state); // Wave 1 → Marcher arrives

        var arrivedDuringTide = new List<UnitType>();
        GameEvents.InvaderArrived += (inv, _) => arrivedDuringTide.Add(inv.UnitType);

        tideRunner.ExecuteTide(1, state); // Arrive step → Wave 2 (Ironclad)

        Assert.Contains(UnitType.Ironclad, arrivedDuringTide);
        Assert.DoesNotContain(UnitType.Marcher, arrivedDuringTide); // Wave 1 not re-spawned
    }

    [Fact]
    public void Tide2_SpawnsWave3()
    {
        // Wave 2 = Marcher (Tide 1 Arrive); Wave 3 = Ironclad (Tide 2 Arrive)
        var config = IntegrationHelpers.MakeConfig(tideCount: 2);
        config.Waves.Add(MakeWave(2, "A1", UnitType.Marcher));
        config.Waves.Add(MakeWave(3, "A1", UnitType.Ironclad));

        var (state, _, cadence, _, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        var spawn = new SpawnManager(config.Waves, GameRandom.FromSeed(0));
        var marchCard = MarchCard();
        var actionDeck = new ActionDeck(new[] { marchCard }, new[] { marchCard }, rng: GameRandom.FromSeed(0), shuffle: false);
        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());

        tideRunner.ExecuteTide(1, state); // Arrive → Wave 2 (Marcher)

        var arrivedOnTide2 = new List<UnitType>();
        GameEvents.InvaderArrived += (inv, _) => arrivedOnTide2.Add(inv.UnitType);

        tideRunner.ExecuteTide(2, state); // Arrive → Wave 3 (Ironclad)

        Assert.Contains(UnitType.Ironclad, arrivedOnTide2);
        Assert.DoesNotContain(UnitType.Marcher, arrivedOnTide2);
    }

    [Fact]
    public void NoWaveDefined_GracefulSkip()
    {
        // No waves defined — Tide 1 Arrive step (tries Wave 2) should silently skip
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);

        var (state, _, cadence, _, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        var spawn = new SpawnManager(Array.Empty<SpawnWave>(), GameRandom.FromSeed(0));
        var marchCard = MarchCard();
        var actionDeck = new ActionDeck(new[] { marchCard }, new[] { marchCard }, rng: GameRandom.FromSeed(0), shuffle: false);
        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());

        int invadersBefore = state.Territories.Sum(t => t.Invaders.Count);

        var ex = Record.Exception(() => tideRunner.ExecuteTide(1, state));

        Assert.Null(ex);
        Assert.Equal(invadersBefore, state.Territories.Sum(t => t.Invaders.Count));
    }
}
