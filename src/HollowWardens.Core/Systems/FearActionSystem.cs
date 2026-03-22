namespace HollowWardens.Core.Systems;

using HollowWardens.Core;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;

public class FearActionSystem : IFearActionSystem
{
    private readonly IDreadSystem _dread;
    private readonly Dictionary<int, List<FearActionData>> _pools;
    private readonly GameRandom _rng;
    private readonly int _fearPerAction;
    private readonly Queue<FearActionData> _queue = new();
    private int _fearBuffer = 0;
    private int _nextDrawElevation = 0;
    private bool _resolvingActions;

    public FearActionSystem(
        IDreadSystem dread,
        Dictionary<int, List<FearActionData>> pools,
        GameRandom? rng = null,
        BalanceConfig? config = null)
    {
        _dread = dread;
        _pools = pools;
        _rng = rng ?? GameRandom.NewRandom();
        _fearPerAction = config?.FearPerAction ?? 5;
    }

    public int QueuedCount => _queue.Count;

    public void BeginResolution() => _resolvingActions = true;
    public void EndResolution()   => _resolvingActions = false;

    public void OnFearSpent(int amount)
    {
        if (_resolvingActions) return; // Bugfix: don't queue during resolution (prevents infinite loop)
        _fearBuffer += amount;
        while (_fearBuffer >= _fearPerAction)
        {
            _fearBuffer -= _fearPerAction;
            int drawLevel = _dread.DreadLevel + _nextDrawElevation;
            _nextDrawElevation = 0; // consume elevation for this one draw only
            _queue.Enqueue(DrawFromPool(drawLevel));
            GameEvents.FearActionQueued?.Invoke();
        }
    }

    public void ElevateNextDraw() => _nextDrawElevation = 1;

    public List<FearActionData> RevealAndDequeue()
    {
        var revealed = new List<FearActionData>(_queue);
        _queue.Clear();
        foreach (var action in revealed)
            GameEvents.FearActionRevealed?.Invoke(action);
        return revealed;
    }

    /// <summary>Returns and clears the queue WITHOUT firing FearActionRevealed (for player-driven reveal).</summary>
    public List<FearActionData> DrainQueue()
    {
        var drained = new List<FearActionData>(_queue);
        _queue.Clear();
        return drained;
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
                return pool[_rng.Next(pool.Count)];
        }
        return new FearActionData { DreadLevel = dreadLevel };
    }
}
