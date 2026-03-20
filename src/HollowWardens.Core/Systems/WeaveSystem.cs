namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Events;

public class WeaveSystem : IWeaveSystem
{
    private int _weave;

    public WeaveSystem(int startingWeave = 20)
    {
        _weave = startingWeave;
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
        GameEvents.WeaveChanged?.Invoke(_weave);
    }
}
