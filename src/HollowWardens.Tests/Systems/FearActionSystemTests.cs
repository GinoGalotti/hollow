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
}
