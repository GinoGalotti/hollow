namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Verifies that TideRunner executes steps in exact order.
/// Tide 1 is a ramp-up tide: only Advance → Arrive → Preview run.
/// Tide 2+ run the full sequence: FearActions → Activate → CounterAttack → Advance → Arrive → Escalate → Preview.
/// </summary>
public class TideSequenceOrderTest : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    [Fact]
    public void Tide1_RampUp_OnlyRunsAdvanceArrivePreview()
    {
        var steps = new List<TideStep>();
        GameEvents.TideStepStarted += step => steps.Add(step);

        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver())
            .Run(state, new IntegrationHelpers.IdleStrategy());

        Assert.Equal(new[] { TideStep.Advance, TideStep.Arrive, TideStep.Preview }, steps);
    }

    [Fact]
    public void Tide2_RunsFullSequence()
    {
        var steps = new List<TideStep>();
        GameEvents.TideStepStarted += step => steps.Add(step);

        var config = IntegrationHelpers.MakeConfig(tideCount: 2);
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver())
            .Run(state, new IntegrationHelpers.IdleStrategy());

        // Tide 1: Advance, Arrive, Preview
        // Tide 2: FearActions, Activate, [CounterAttack only if action is Ravage/Corrupt], Advance, Arrive, Escalate, Preview
        // With default seed the Tide-2 card is non-provocative, so CounterAttack is skipped.
        var expectedOrder = new[]
        {
            TideStep.Advance, TideStep.Arrive, TideStep.Preview,      // Tide 1 ramp-up
            TideStep.FearActions, TideStep.Activate,                   // Tide 2 (no CounterAttack: card is non-provocative)
            TideStep.Advance, TideStep.Arrive, TideStep.Escalate, TideStep.Preview
        };

        Assert.Equal(expectedOrder, steps);
    }

    [Fact]
    public void Tide2_RavageCard_IncludesCounterAttackStep()
    {
        var steps = new List<TideStep>();
        GameEvents.TideStepStarted += step => steps.Add(step);

        var config = IntegrationHelpers.MakeConfig(tideCount: 2);
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        // Use TideRunner directly with a preloaded ravage card to guarantee provocation fires
        var tideRunner = new HollowWardens.Core.Encounter.TideRunner(
            actionDeck, cadence, spawn, faction, new EffectResolver());
        tideRunner.CounterAttackHandler = (_, __, ___) => null;
        tideRunner.PreloadPreview(new HollowWardens.Core.Models.ActionCard
        {
            Id = "pm_ravage", Name = "Ravage",
            Pool = HollowWardens.Core.Models.ActionPool.Painful, AdvanceModifier = 1
        });
        state.Combat = new HollowWardens.Core.Systems.CombatSystem();

        tideRunner.ExecuteTide(2, state); // Tide 2 with ravage preloaded — should fire CounterAttack

        Assert.Contains(TideStep.CounterAttack, steps);
    }

    [Fact]
    public void ActionCardRevealedFiresBeforeFirstStep()
    {
        bool actionRevealed = false;
        bool firstStepFired = false;

        GameEvents.ActionCardRevealed += _ => actionRevealed = true;
        GameEvents.TideStepStarted += _ =>
        {
            if (!firstStepFired)
                firstStepFired = actionRevealed;
        };

        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver())
            .Run(state, new IntegrationHelpers.IdleStrategy());

        Assert.True(actionRevealed);
        Assert.True(firstStepFired, "ActionCardRevealed should fire before first TideStep");
    }
}
