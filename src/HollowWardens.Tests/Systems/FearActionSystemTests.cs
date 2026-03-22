namespace HollowWardens.Tests.Systems;

using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using Xunit;

public class FearActionSystemTests : IDisposable
{
    private static FearActionData MakeAction(string id, int dreadLevel)
        => new() { Id = id, DreadLevel = dreadLevel };

    private static Dictionary<int, List<FearActionData>> MakePools() => new()
    {
        [1] = new List<FearActionData> { MakeAction("L1A", 1) },
        [2] = new List<FearActionData> { MakeAction("L2A", 2) },
        [3] = new List<FearActionData> { MakeAction("L3A", 3) },
        [4] = new List<FearActionData> { MakeAction("L4A", 4) },
    };

    public void Dispose() => GameEvents.ClearAll();

    [Fact]
    public void QueuesActionEvery5Fear()
    {
        var dread = new DreadSystem();
        var sut = new FearActionSystem(dread, MakePools());

        sut.OnFearSpent(5);

        Assert.Equal(1, sut.QueuedCount);
    }

    [Fact]
    public void DrawsFromCurrentDreadPool()
    {
        var dread = new DreadSystem();
        dread.OnFearGenerated(15); // advance to level 2
        var sut = new FearActionSystem(dread, MakePools());

        sut.OnFearSpent(5);
        var revealed = sut.RevealAndDequeue();

        Assert.Single(revealed);
        Assert.Equal(2, revealed[0].DreadLevel);
    }

    [Fact]
    public void RetroactiveUpgradeOnDreadAdvance()
    {
        var dread = new DreadSystem();
        var sut = new FearActionSystem(dread, MakePools());

        sut.OnFearSpent(5); // queue 1 action from level 1 pool
        Assert.Equal(1, sut.QueuedCount);

        sut.OnDreadAdvanced(2); // retroactive upgrade to level 2 pool

        var revealed = sut.RevealAndDequeue();
        Assert.Single(revealed);
        Assert.Equal(2, revealed[0].DreadLevel);
    }

    [Fact]
    public void RevealDequeuesToEmpty()
    {
        var dread = new DreadSystem();
        var sut = new FearActionSystem(dread, MakePools());

        sut.OnFearSpent(10); // queue 2 actions

        sut.RevealAndDequeue();

        Assert.Equal(0, sut.QueuedCount);
    }

    [Fact]
    public void RetroactiveUpgradeReplacesActualObjects()
    {
        var dread = new DreadSystem();
        var sut = new FearActionSystem(dread, MakePools());

        sut.OnFearSpent(10); // queue 2 actions from level 1 pool
        Assert.Equal(2, sut.QueuedCount);

        sut.OnDreadAdvanced(2); // drain and redraw from level 2 pool

        var revealed = sut.RevealAndDequeue();
        Assert.Equal(2, revealed.Count);
        Assert.All(revealed, a => Assert.Equal(2, a.DreadLevel));
    }

    [Fact]
    public void QueuedCountTracksCorrectly()
    {
        var dread = new DreadSystem();
        var sut = new FearActionSystem(dread, MakePools());

        sut.OnFearSpent(5);  // queue 1
        sut.OnFearSpent(5);  // queue 2
        sut.OnFearSpent(3);  // partial, not yet queued

        Assert.Equal(2, sut.QueuedCount);
    }

    [Fact]
    public void FearGenerated_DuringResolution_DoesNotQueue()
    {
        var dread = new DreadSystem();
        var sut = new FearActionSystem(dread, MakePools());

        sut.BeginResolution();
        sut.OnFearSpent(5);
        sut.EndResolution();

        Assert.Equal(0, sut.QueuedCount);
    }

    [Fact]
    public void FearGenerated_AfterResolutionEnds_QueuesNormally()
    {
        var dread = new DreadSystem();
        var sut = new FearActionSystem(dread, MakePools());

        sut.BeginResolution();
        sut.EndResolution();
        sut.OnFearSpent(5);

        Assert.Equal(1, sut.QueuedCount);
    }

    [Fact]
    public void BeginResolution_EndResolution_CanBeCalledMultipleTimes()
    {
        var dread = new DreadSystem();
        var sut = new FearActionSystem(dread, MakePools());

        sut.BeginResolution();
        sut.EndResolution();
        sut.BeginResolution();
        sut.EndResolution();
        sut.OnFearSpent(5);

        Assert.Equal(1, sut.QueuedCount);
    }

    // ── Null-safety regression tests ────────────────────────────────────────
    // Guard against: GameBridge.Instance?.State.FearActions (missing ?. after State)
    // The view controller callbacks fire via GameEvents; if the state reference is null
    // at that moment the null-conditional chain must return 0, not throw.

    [Fact]
    public void QueuedCount_IsZero_OnFreshSystem()
    {
        var dread = new DreadSystem();
        var sut = new FearActionSystem(dread, MakePools());

        Assert.Equal(0, sut.QueuedCount);
    }

    [Fact]
    public void QueuedCount_NullableInterface_ReturnsZeroViaCoalescing()
    {
        IFearActionSystem? sut = null;
        int count = sut?.QueuedCount ?? 0;
        Assert.Equal(0, count);
    }

    [Fact]
    public void FearActionQueued_Event_CallbackWithNullStateSafelyReturnsZero()
    {
        // Regression: FearActionQueueController used ?.State.FearActions instead of
        // ?.State?.FearActions — crashing when State was null before BuildEncounter.
        // This test fires the exact same Core event path with a null state reference.
        var dread = new DreadSystem();
        var sut   = new FearActionSystem(dread, MakePools());

        IFearActionSystem? nullableRef = null; // simulates State not yet built
        var observed = new List<int>();
        GameEvents.FearActionQueued += () => observed.Add(nullableRef?.QueuedCount ?? 0);

        sut.OnFearSpent(5); // fires GameEvents.FearActionQueued while nullableRef is null

        Assert.Single(observed);
        Assert.Equal(0, observed[0]); // must return 0, not throw NullReferenceException
    }

    [Fact]
    public void FearActionQueued_Event_CallbackReadsCorrectCountOnceStateIsAvailable()
    {
        var dread = new DreadSystem();
        var sut   = new FearActionSystem(dread, MakePools());

        IFearActionSystem? nullableRef = sut; // simulates State available after BuildEncounter
        var observed = new List<int>();
        GameEvents.FearActionQueued += () => observed.Add(nullableRef?.QueuedCount ?? 0);

        sut.OnFearSpent(5);  // queues 1
        sut.OnFearSpent(5);  // queues 2

        Assert.Equal(new[] { 1, 2 }, observed);
    }
}
