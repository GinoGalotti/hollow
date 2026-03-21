namespace HollowWardens.Tests.Integration;

using HollowWardens.Core;
using HollowWardens.Core.Data;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders.PaleMarch;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using Xunit;

/// <summary>
/// Verifies that SpawnInitialWave populates the A-row before the first Vigil,
/// and that Tide 1's Arrive step does not re-spawn Wave 1.
/// </summary>
public class InitialWaveTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private static (EncounterState state, TideRunner runner) Build()
    {
        var config     = EncounterLoader.CreatePaleMarchStandard();
        var territories = BoardState.CreatePyramid().Territories.Values.ToList();
        var random     = GameRandom.FromSeed(42);

        var state = new EncounterState
        {
            Config     = config,
            Territories = territories,
            Weave      = new WeaveSystem(20),
            Combat     = new CombatSystem(),
            Corruption = new CorruptionSystem(),
        };

        var faction    = new PaleMarchFaction();
        var actionDeck = new ActionDeck(faction.BuildPainfulPool(), faction.BuildEasyPool(), random, shuffle: false);
        var cadence    = new CadenceManager(config.Cadence);
        var spawn      = new SpawnManager(config.Waves, random);
        var resolver   = new EffectResolver();
        var runner     = new TideRunner(actionDeck, cadence, spawn, faction, resolver);

        return (state, runner);
    }

    [Fact]
    public void SpawnInitialWave_ARowHasInvaders()
    {
        var (state, runner) = Build();

        runner.SpawnInitialWave(state);

        int aRowTotal = state.Territories
            .Where(t => t.Row == TerritoryRow.Arrival)
            .Sum(t => t.Invaders.Count(i => i.IsAlive));

        Assert.True(aRowTotal > 0,
            $"Expected invaders in A-row after SpawnInitialWave, got {aRowTotal}");
    }

    [Fact]
    public void SpawnInitialWave_TideRunnerDoesNotRespawnWave1()
    {
        var (state, runner) = Build();

        runner.SpawnInitialWave(state);
        int invadersBefore = state.Territories.SelectMany(t => t.Invaders).Count(i => i.IsAlive);

        // Tide 1: Fear/Activate/CounterAttack/Escalate skipped; only Advance + Arrive + Preview run.
        // Arrive calls SpawnWaveForTide(1, state) which must be a no-op (guard: _initialWaveSpawned).
        runner.ExecuteTide(1, state);

        // Advance only moves invaders; Arrive is guarded → total count unchanged.
        int invadersAfter = state.Territories.SelectMany(t => t.Invaders).Count(i => i.IsAlive);
        Assert.Equal(invadersBefore, invadersAfter);
    }
}
