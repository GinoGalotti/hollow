namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Manual cadence pattern P-E-P-E produces the expected action pool for each Tide.
/// </summary>
public class CadencePatternTest : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    [Fact]
    public void PEPEPattern_ProducesCorrectPoolPerTide()
    {
        var pools = new List<ActionPool>();
        GameEvents.ActionCardRevealed += card => pools.Add(card.Pool);

        var cadenceConfig = new CadenceConfig
        {
            Mode = "manual",
            ManualPattern = new[] { "P", "E", "P", "E" }
        };
        var config = IntegrationHelpers.MakeConfig(tideCount: 4, cadence: cadenceConfig);
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver())
            .Run(state, new IntegrationHelpers.IdleStrategy());

        Assert.Equal(4, pools.Count);
        Assert.Equal(ActionPool.Painful, pools[0]);
        Assert.Equal(ActionPool.Easy,    pools[1]);
        Assert.Equal(ActionPool.Painful, pools[2]);
        Assert.Equal(ActionPool.Easy,    pools[3]);
    }

    [Fact]
    public void RuleBasedCadence_MaxStreak1_AlternatesAfterPainful()
    {
        var pools = new List<ActionPool>();
        GameEvents.ActionCardRevealed += card => pools.Add(card.Pool);

        // MaxPainfulStreak=1: after 1 painful, must draw easy
        var cadenceConfig = new CadenceConfig { Mode = "rule_based", MaxPainfulStreak = 1, EasyFrequency = 2 };
        var config = IntegrationHelpers.MakeConfig(tideCount: 4, cadence: cadenceConfig);
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver())
            .Run(state, new IntegrationHelpers.IdleStrategy());

        // Tide 1: Painful (streak=1 → capped)
        // Tide 2: forced Easy (streak=0)
        // Tide 3: Painful
        // Tide 4: forced Easy
        Assert.Equal(ActionPool.Painful, pools[0]);
        Assert.Equal(ActionPool.Easy,    pools[1]);
        Assert.Equal(ActionPool.Painful, pools[2]);
        Assert.Equal(ActionPool.Easy,    pools[3]);
    }
}
