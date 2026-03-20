namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Events;

public class DreadSystem : IDreadSystem
{
    private static readonly int[] Thresholds = { 15, 30, 45 };

    private int _dreadLevel = 1;
    private int _totalFearGenerated = 0;

    public int DreadLevel => _dreadLevel;
    public int TotalFearGenerated => _totalFearGenerated;

    public void OnFearGenerated(int amount)
    {
        _totalFearGenerated += amount;
        CheckAdvancement();
    }

    private void CheckAdvancement()
    {
        while (_dreadLevel < 4 && _totalFearGenerated >= Thresholds[_dreadLevel - 1])
        {
            _dreadLevel++;
            GameEvents.DreadAdvanced?.Invoke(_dreadLevel);
        }
    }
}
