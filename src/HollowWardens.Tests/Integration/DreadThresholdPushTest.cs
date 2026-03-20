namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Queue 2 fear actions at Dread 1, generate enough fear to push to Dread 2,
/// verify both queued actions upgrade to Dread 2 pool.
/// </summary>
public class DreadThresholdPushTest : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private static (DreadSystem dread, FearActionSystem fearActions) BuildSystems()
    {
        var dread = new DreadSystem();
        var pools = new Dictionary<int, List<FearActionData>>
        {
            [1] = new() { new() { Id = "d1_action", DreadLevel = 1, Effect = new() { Type = EffectType.GenerateFear, Value = 0 } } },
            [2] = new() { new() { Id = "d2_action", DreadLevel = 2, Effect = new() { Type = EffectType.GenerateFear, Value = 0 } } },
        };
        return (dread, new FearActionSystem(dread, pools));
    }

    [Fact]
    public void QueuedActions_UpgradeWhenDreadAdvances()
    {
        var (dread, fearActions) = BuildSystems();

        // Wire: DreadAdvanced → retroactive upgrade
        GameEvents.DreadAdvanced += level => fearActions.OnDreadAdvanced(level);

        // Queue 2 actions at Dread 1 (spend 10 fear)
        fearActions.OnFearSpent(10);
        Assert.Equal(2, fearActions.QueuedCount);
        Assert.Equal(1, dread.DreadLevel);

        // Generate enough total fear to push to Dread 2 (threshold = 15)
        dread.OnFearGenerated(15);  // fires DreadAdvanced(2) → upgrade

        Assert.Equal(2, dread.DreadLevel);
        Assert.Equal(2, fearActions.QueuedCount); // still 2, just upgraded

        // Reveal: both should be from Dread 2 pool
        var revealed = fearActions.RevealAndDequeue();
        Assert.Equal(2, revealed.Count);
        Assert.All(revealed, a => Assert.Equal(2, a.DreadLevel));
    }

    [Fact]
    public void QueuedActions_DontUpgrade_IfDreadStaysAtLevel1()
    {
        var (dread, fearActions) = BuildSystems();
        GameEvents.DreadAdvanced += level => fearActions.OnDreadAdvanced(level);

        fearActions.OnFearSpent(10); // queue 2 Dread 1 actions

        // Only 14 total fear — not enough to advance Dread (threshold=15)
        dread.OnFearGenerated(14);
        Assert.Equal(1, dread.DreadLevel);

        var revealed = fearActions.RevealAndDequeue();
        Assert.All(revealed, a => Assert.Equal(1, a.DreadLevel));
    }
}
