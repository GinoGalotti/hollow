namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;

public class DreadSystem : IDreadSystem
{
    private readonly int[] _thresholds;

    private int _dreadLevel = 1;
    private int _totalFearGenerated = 0;

    public DreadSystem(BalanceConfig? config = null)
    {
        _thresholds = config != null
            ? new[] { config.DreadThreshold1, config.DreadThreshold2, config.DreadThreshold3 }
            : new[] { 15, 30, 45 };
    }

    public int DreadLevel => _dreadLevel;
    public int TotalFearGenerated => _totalFearGenerated;

    public void OnFearGenerated(int amount)
    {
        _totalFearGenerated += amount;
        CheckAdvancement();
    }

    private void CheckAdvancement()
    {
        while (_dreadLevel < 4 && _totalFearGenerated >= _thresholds[_dreadLevel - 1])
        {
            _dreadLevel++;
            GameEvents.DreadAdvanced?.Invoke(_dreadLevel);
        }
    }
}
