namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Events;

public class WeaveSystem : IWeaveSystem
{
    private int _weave;
    private readonly int _maxWeave;

    public WeaveSystem(int startingWeave = 20, int? maxWeave = null)
    {
        _weave = startingWeave;
        _maxWeave = maxWeave ?? 20; // Bugfix: max defaults to 20, not startingWeave
    }

    public int CurrentWeave => _weave;
    public bool IsGameOver => _weave <= 0;

    public void DealDamage(int amount)
    {
        _weave = Math.Max(0, _weave - amount);
        GameEvents.WeaveChanged?.Invoke(_weave);
    }

    public void Restore(int amount)
    {
        _weave += amount;
        _weave = Math.Min(_weave, _maxWeave); // Bugfix: cannot exceed maximum
        GameEvents.WeaveChanged?.Invoke(_weave);
    }
}
