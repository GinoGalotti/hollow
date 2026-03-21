namespace HollowWardens.Tests.Integration;

using HollowWardens.Core;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Tide 1 ramp-up: FearActions, Activate, CounterAttack, and Escalate are skipped.
/// Only Advance, Arrive, and Preview run on Tide 1.
/// </summary>
public class TideRampTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    [Fact]
    public void Tide1_FearActions_AreNotRevealed()
    {
        // Queue a fear action, run Tide 1, verify it was NOT revealed (stays queued)
        var cadenceConfig = new CadenceConfig { Mode = "manual", ManualPattern = new[] { "P" } };
        var config = IntegrationHelpers.MakeConfig(tideCount: 1, cadence: cadenceConfig);
        var (state, _, _, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        // Pre-queue a fear action by calling OnFearSpent directly
        state.FearActions?.OnFearSpent(5); // queues 1 action

        int revealedCount = 0;
        GameEvents.FearActionRevealed += _ => revealedCount++;

        var marchCard = new ActionCard { Id = "pm_march", Name = "March", Pool = ActionPool.Painful, AdvanceModifier = 1 };
        var actionDeck = new ActionDeck(new[] { marchCard }, new[] { marchCard }, rng: GameRandom.FromSeed(0), shuffle: false);
        var cadence = new CadenceManager(cadenceConfig);
        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());

        tideRunner.ExecuteTide(1, state);

        Assert.Equal(0, revealedCount);         // No fear actions revealed on Tide 1
        Assert.Equal(1, state.FearActions!.QueuedCount); // Still queued for Tide 2
    }

    [Fact]
    public void Tide1_Activate_IsSkipped_NoCorruptionFromInvaders()
    {
        // Place an Ironclad (Ravage: +3 Corruption) in M1 with natives.
        // Tide 1 should skip Activate, so M1 should gain no corruption.
        var cadenceConfig = new CadenceConfig { Mode = "manual", ManualPattern = new[] { "P" } };
        var config = IntegrationHelpers.MakeConfig(tideCount: 1, cadence: cadenceConfig);
        var (state, _, _, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        var ironclad = faction.CreateUnit(UnitType.Ironclad, "M1");
        state.GetTerritory("M1")!.Invaders.Add(ironclad);

        int corruptionBefore = state.GetTerritory("M1")!.CorruptionPoints;

        var marchCard = new ActionCard { Id = "pm_ravage", Name = "Ravage", Pool = ActionPool.Painful, AdvanceModifier = 1 };
        var actionDeck = new ActionDeck(new[] { marchCard }, new[] { marchCard }, rng: GameRandom.FromSeed(0), shuffle: false);
        var tideRunner = new TideRunner(actionDeck, new CadenceManager(cadenceConfig), spawn, faction, new EffectResolver());

        tideRunner.ExecuteTide(1, state);

        Assert.Equal(corruptionBefore, state.GetTerritory("M1")!.CorruptionPoints);
    }

    [Fact]
    public void Tide1_Escalate_IsSkipped_NoEscalationCardAdded()
    {
        // Config has escalation at Tide 1 — it should be ignored on Tide 1 ramp-up.
        var cadenceConfig = new CadenceConfig { Mode = "manual", ManualPattern = new[] { "P" } };
        var config = IntegrationHelpers.MakeConfig(tideCount: 1, cadence: cadenceConfig);
        config.EscalationSchedule.Add(new HollowWardens.Core.Encounter.EscalationEntry
        {
            Tide = 1, CardId = "test_escalation", Pool = ActionPool.Painful
        });

        var (state, _, _, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        int escalationCardsAdded = 0;
        GameEvents.ActionCardRevealed += c => { if (c.IsEscalation) escalationCardsAdded++; };

        var marchCard = new ActionCard { Id = "pm_march", Name = "March", Pool = ActionPool.Painful, AdvanceModifier = 1 };
        var actionDeck = new ActionDeck(new[] { marchCard }, new[] { marchCard }, rng: GameRandom.FromSeed(0), shuffle: false);
        var tideRunner = new TideRunner(actionDeck, new CadenceManager(cadenceConfig), spawn, faction, new EffectResolver());

        tideRunner.ExecuteTide(1, state);

        Assert.Equal(0, escalationCardsAdded);
    }

    [Fact]
    public void Tide2_RunsAllSteps_FearActionsRevealed()
    {
        // Queue fear actions, run Tide 2, verify they ARE revealed
        var cadenceConfig = new CadenceConfig { Mode = "manual", ManualPattern = new[] { "P", "P" } };
        var config = IntegrationHelpers.MakeConfig(tideCount: 2, cadence: cadenceConfig);
        var (state, _, _, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(10), new RootAbility(), config);

        state.FearActions?.OnFearSpent(5); // queue 1 action before Tide 2

        int revealedCount = 0;
        GameEvents.FearActionRevealed += _ => revealedCount++;

        var marchCard = new ActionCard { Id = "pm_march", Name = "March", Pool = ActionPool.Painful, AdvanceModifier = 1 };
        var actionDeck = new ActionDeck(new[] { marchCard }, new[] { marchCard }, rng: GameRandom.FromSeed(0), shuffle: false);
        var cadence = new CadenceManager(cadenceConfig);
        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());

        tideRunner.ExecuteTide(1, state); // ramp-up tide — no reveals
        tideRunner.ExecuteTide(2, state); // full tide — reveals queued actions

        Assert.Equal(1, revealedCount);
    }
}
