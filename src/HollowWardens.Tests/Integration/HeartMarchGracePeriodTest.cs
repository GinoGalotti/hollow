namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// An invader that enters I1 during Advance does NOT deal heart damage that same Tide,
/// but DOES deal heart damage on the next Tide.
/// </summary>
public class HeartMarchGracePeriodTest : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    [Fact]
    public void InvaderEntersI1_NoHeartDamage_SameTide()
    {
        // Place a Marcher in M1 (1 step from I1). Use March action (advanceModifier=2).
        // After Advance, Marcher should be in I1 but not deal heart damage this Tide.

        var cadenceConfig = new CadenceConfig
        {
            Mode = "manual",
            ManualPattern = new[] { "P" }   // always Painful → "march" or "ravage"
        };
        var config = IntegrationHelpers.MakeConfig(tideCount: 1, cadence: cadenceConfig);
        var (state, _, _, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        // Place a Marcher in M1
        var marcher = faction.CreateUnit(UnitType.Marcher, "M1");
        state.GetTerritory("M1")!.Invaders.Add(marcher);

        int weaveBeforeMarch = state.Weave!.CurrentWeave;
        int heartDamageEvents = 0;
        GameEvents.HeartDamageDealt += _ => heartDamageEvents++;

        // Build action deck with only March (advanceModifier=2, moves invaders 2 steps)
        var marchCard = new ActionCard { Id = CombatSystem.MarchId, Name = "March", Pool = ActionPool.Painful, AdvanceModifier = 2 };
        var actionDeck = new ActionDeck(new[] { marchCard }, new[] { marchCard }, rng: new Random(0), shuffle: false);
        var cadence = new CadenceManager(cadenceConfig);
        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());

        tideRunner.ExecuteTide(1, state);

        // Marcher should have moved from M1 to I1 (1 step needed, 2 allowed)
        Assert.Equal("I1", marcher.TerritoryId);
        // But NO heart damage on the same tide it arrived
        Assert.Equal(0, heartDamageEvents);
        Assert.Equal(weaveBeforeMarch, state.Weave.CurrentWeave);
    }

    [Fact]
    public void InvaderInI1_BeforeAdvance_DoesMarchOnHeart()
    {
        // Place a Marcher already in I1. After Advance (grace check uses pre-advance snapshot),
        // it should deal heart damage.

        var cadenceConfig = new CadenceConfig
        {
            Mode = "manual",
            ManualPattern = new[] { "P" }
        };
        var config = IntegrationHelpers.MakeConfig(tideCount: 1, cadence: cadenceConfig);
        var (state, _, _, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        var marcher = faction.CreateUnit(UnitType.Marcher, "I1");
        state.GetTerritory("I1")!.Invaders.Add(marcher);

        int heartDamageEvents = 0;
        GameEvents.HeartDamageDealt += _ => heartDamageEvents++;

        var marchCard = new ActionCard { Id = CombatSystem.MarchId, Name = "March", Pool = ActionPool.Painful, AdvanceModifier = 1 };
        var actionDeck = new ActionDeck(new[] { marchCard }, new[] { marchCard }, rng: new Random(0), shuffle: false);
        var cadence = new CadenceManager(cadenceConfig);
        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());

        tideRunner.ExecuteTide(1, state);

        // The marcher was in I1 BEFORE the advance → heart damage fires
        Assert.Equal(1, heartDamageEvents);
        Assert.True(state.Weave!.CurrentWeave < 20, "Heart should have taken damage");
    }
}
