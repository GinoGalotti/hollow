namespace HollowWardens.Core.Encounter;

using HollowWardens.Core.Models;

public class CadenceManager
{
    private readonly CadenceConfig _config;
    private int _painfulStreak;
    private int _patternIndex;

    public CadenceManager(CadenceConfig config)
    {
        _config = config;
    }

    public int PainfulStreak => _painfulStreak;

    /// <summary>
    /// Determines the pool for the next Tide draw and updates internal cadence state.
    /// </summary>
    public ActionPool NextPool()
    {
        if (_config.Mode == "manual" && _config.ManualPattern is { Length: > 0 } pattern)
        {
            var entry = pattern[_patternIndex % pattern.Length];
            _patternIndex++;
            var pool = entry == "P" ? ActionPool.Painful : ActionPool.Easy;
            if (pool == ActionPool.Easy) _painfulStreak = 0;
            else _painfulStreak++;
            return pool;
        }

        // Rule-based: force Easy after max consecutive Painful draws
        if (_painfulStreak >= _config.MaxPainfulStreak)
        {
            _painfulStreak = 0;
            return ActionPool.Easy;
        }

        _painfulStreak++;
        return ActionPool.Painful;
    }
}
