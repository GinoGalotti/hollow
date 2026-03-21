namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Events;
using HollowWardens.Core.Run;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Verifies the initial board state set up by EncounterRunner before the first Vigil:
/// - Natives spawned per NativeSpawns config
/// - Starting Presence placed on I1
/// - No invaders on board before the first Tide
/// </summary>
public class StartingStateTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private static (HollowWardens.Core.Encounter.EncounterState state, EncounterRunner runner) BuildAndRun(
        int tideCount,
        Dictionary<string, int>? nativeSpawns = null)
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: tideCount);
        if (nativeSpawns != null)
            foreach (var (k, v) in nativeSpawns) config.NativeSpawns[k] = v;

        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        var runner = new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver());
        return (state, runner);
    }

    [Fact]
    public void NativeSpawns_PopulatesTerritoriesWithCorrectCount()
    {
        var (state, runner) = BuildAndRun(tideCount: 0,
            nativeSpawns: new() { ["M1"] = 2, ["M2"] = 2, ["I1"] = 2 });

        runner.Run(state, new IntegrationHelpers.IdleStrategy());

        Assert.Equal(2, state.GetTerritory("M1")!.Natives.Count(n => n.IsAlive));
        Assert.Equal(2, state.GetTerritory("M2")!.Natives.Count(n => n.IsAlive));
        Assert.Equal(2, state.GetTerritory("I1")!.Natives.Count(n => n.IsAlive));
    }

    [Fact]
    public void NativeSpawns_NativesHaveCorrectStats()
    {
        var (state, runner) = BuildAndRun(tideCount: 0,
            nativeSpawns: new() { ["M1"] = 1 });

        runner.Run(state, new IntegrationHelpers.IdleStrategy());

        var native = state.GetTerritory("M1")!.Natives.First();
        Assert.Equal(2, native.Hp);
        Assert.Equal(2, native.MaxHp);
        Assert.Equal(3, native.Damage);
    }

    [Fact]
    public void StartingPresence_I1HasPresenceCount1()
    {
        var (state, runner) = BuildAndRun(tideCount: 0);

        // I1 starts with 0 presence (board state creates territories with PresenceCount=0)
        Assert.Equal(0, state.GetTerritory("I1")!.PresenceCount);

        runner.Run(state, new IntegrationHelpers.IdleStrategy());

        Assert.Equal(1, state.GetTerritory("I1")!.PresenceCount);
    }

    [Fact]
    public void BoardStartsWithNoInvaders_BeforeFirstTide()
    {
        // 0-tide run: no waves spawn, so the board should remain empty of invaders
        var (state, runner) = BuildAndRun(tideCount: 0);
        runner.Run(state, new IntegrationHelpers.IdleStrategy());

        int totalInvaders = state.Territories.Sum(t => t.Invaders.Count(i => i.IsAlive));
        Assert.Equal(0, totalInvaders);
    }
}
