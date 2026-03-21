namespace HollowWardens.Tests.Integration;

using HollowWardens.Core;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Verifies the full fear generation chain:
/// GenerateFearEffect → GameEvents.FearGenerated → FearActionSystem (OnFearSpent)
///                   → state.Dread.OnFearGenerated → (threshold) → GameEvents.DreadAdvanced
///                   → FearActionSystem (OnDreadAdvanced)
/// </summary>
public class FearWiringTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    // Manually subscribe the fear chain as EncounterRunner.WireEvents does
    private static (Action unsubscribe, EncounterState state) Wire(EncounterState state)
    {
        Action<int> onFear = amount => state.FearActions?.OnFearSpent(amount);
        Action<int> onDread = level => state.FearActions?.OnDreadAdvanced(level);
        GameEvents.FearGenerated += onFear;
        GameEvents.DreadAdvanced += onDread;

        return (() => { GameEvents.FearGenerated -= onFear; GameEvents.DreadAdvanced -= onDread; }, state);
    }

    [Fact]
    public void FearGenerated_AccumulatesInDread()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, _, _, _, _) = IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        // Generate 14 fear — should NOT advance Dread (threshold is 15)
        var effect = new GenerateFearEffect(new EffectData { Type = EffectType.GenerateFear, Value = 14 });
        effect.Resolve(state, new TargetInfo());

        Assert.Equal(14, state.Dread!.TotalFearGenerated);
        Assert.Equal(1, state.Dread.DreadLevel);
    }

    [Fact]
    public void DreadAdvances_AtThreshold15()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, _, _, _, _) = IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        // Generate exactly 15 fear — Dread should advance to level 2
        var effect = new GenerateFearEffect(new EffectData { Type = EffectType.GenerateFear, Value = 15 });
        effect.Resolve(state, new TargetInfo());

        Assert.Equal(2, state.Dread!.DreadLevel);
    }

    [Fact]
    public void FearActions_QueuedAt5FearIncrements()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, _, _, _, _) = IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);
        var (unsubscribe, _) = Wire(state);

        try
        {
            // 10 fear = 2 fear actions queued
            GameEvents.FearGenerated?.Invoke(10);

            Assert.Equal(2, state.FearActions!.QueuedCount);
        }
        finally { unsubscribe(); }
    }

    [Fact]
    public void DreadAdvanced_UpgradesQueuedFearActions()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, _, _, _, _) = IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);
        var (unsubscribe, _) = Wire(state);

        try
        {
            // Queue 1 action at Dread 1
            GameEvents.FearGenerated?.Invoke(5);
            Assert.Equal(1, state.FearActions!.QueuedCount);

            // Advance Dread — DreadAdvanced fires → FearActions.OnDreadAdvanced upgrades queued actions
            int upgradeCount = 0;
            GameEvents.DreadUpgradeApplied += () => upgradeCount++;
            GameEvents.DreadAdvanced?.Invoke(2);

            Assert.Equal(1, upgradeCount);
        }
        finally { unsubscribe(); }
    }

    [Fact]
    public void FearGenerated_EventFiresAfterDreadUpdated()
    {
        // Regression: GenerateFearEffect previously fired FearGenerated BEFORE updating Dread,
        // causing DreadBarController to read a stale TotalFearGenerated.
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, _, _, _, _) = IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        int totalWhenEventFired = -1;
        GameEvents.FearGenerated += _ => totalWhenEventFired = state.Dread!.TotalFearGenerated;

        var effect = new GenerateFearEffect(new EffectData { Type = EffectType.GenerateFear, Value = 3 });
        effect.Resolve(state, new TargetInfo());

        Assert.Equal(3, totalWhenEventFired); // Dread must be updated BEFORE the event fires
    }

    [Fact]
    public void FullChain_GenerateFearEffect_WiresThrough_BothSystems()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, _, _, _, _) = IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);
        var (unsubscribe, _) = Wire(state);

        try
        {
            // 5 fear: Dread accumulates, FearActions queues 1 action
            var effect = new GenerateFearEffect(new EffectData { Type = EffectType.GenerateFear, Value = 5 });
            effect.Resolve(state, new TargetInfo());

            Assert.Equal(5, state.Dread!.TotalFearGenerated);   // Dread received via direct call
            Assert.Equal(1, state.FearActions!.QueuedCount);     // FearActions received via event
        }
        finally { unsubscribe(); }
    }
}
