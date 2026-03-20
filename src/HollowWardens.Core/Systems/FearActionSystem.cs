namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Events;
using HollowWardens.Core.Models;

public class FearActionSystem : IFearActionSystem
{
    private readonly IDreadSystem _dread;
    private readonly Dictionary<int, List<FearActionData>> _pools;
    private readonly Queue<FearActionData> _queue = new();
    private int _fearBuffer = 0;

    public FearActionSystem(IDreadSystem dread, Dictionary<int, List<FearActionData>> pools)
    {
        _dread = dread;
        _pools = pools;
    }

    public int QueuedCount => _queue.Count;

    public void OnFearSpent(int amount)
    {
        _fearBuffer += amount;
        while (_fearBuffer >= 5)
        {
            _fearBuffer -= 5;
            _queue.Enqueue(DrawFromPool(_dread.DreadLevel));
            GameEvents.FearActionQueued?.Invoke();
        }
    }

    public List<FearActionData> RevealAndDequeue()
    {
        var revealed = new List<FearActionData>(_queue);
        _queue.Clear();
        foreach (var action in revealed)
            GameEvents.FearActionRevealed?.Invoke(action);
        return revealed;
    }

    public void OnDreadAdvanced(int newLevel)
    {
        int count = _queue.Count;
        _queue.Clear();
        for (int i = 0; i < count; i++)
            _queue.Enqueue(DrawFromPool(newLevel));
        GameEvents.DreadUpgradeApplied?.Invoke();
    }

    private FearActionData DrawFromPool(int dreadLevel)
    {
        for (int level = dreadLevel; level >= 1; level--)
        {
            if (_pools.TryGetValue(level, out var pool) && pool.Count > 0)
                return pool[Random.Shared.Next(pool.Count)];
        }
        return new FearActionData { DreadLevel = dreadLevel };
    }
}
