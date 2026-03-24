namespace HollowWardens.Core.Systems;

public interface IWeaveSystem
{
    int CurrentWeave { get; }
    int MaxWeave { get; }
    void DealDamage(int amount);
    void Restore(int amount);
    bool IsGameOver { get; }
}
