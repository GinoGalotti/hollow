namespace HollowWardens.Core.Systems;

public interface IDreadSystem
{
    int DreadLevel { get; }
    int TotalFearGenerated { get; }
    void OnFearGenerated(int amount);
    // Dread level advances automatically inside OnFearGenerated
}
