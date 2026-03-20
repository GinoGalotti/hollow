namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Verifies that TideRunner executes steps in exact order:
/// FearActions → Activate → CounterAttack → Advance → Arrive → Escalate → Preview
/// </summary>
public class TideSequenceOrderTest : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    [Fact]
    public void TideStepsFire_InCorrectOrder()
    {
        var steps = new List<TideStep>();
        GameEvents.TideStepStarted += step => steps.Add(step);

        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var deck = IntegrationHelpers.MakeCards(10);
        var warden = new RootAbility();
        var (state, actionDeck, cadence, spawn, faction) = IntegrationHelpers.Build(deck, warden, config);

        var runner = new EncounterRunner(actionDeck, cadence, spawn, faction, new EffectResolver());
        runner.Run(state, new IntegrationHelpers.IdleStrategy());

        var expectedOrder = new[]
        {
            TideStep.FearActions,
            TideStep.Activate,
            TideStep.CounterAttack,
            TideStep.Advance,
            TideStep.Arrive,
            TideStep.Escalate,
            TideStep.Preview
        };

        Assert.Equal(expectedOrder, steps.Take(7));
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
