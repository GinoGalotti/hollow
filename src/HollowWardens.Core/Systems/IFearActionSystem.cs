namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Models;

public interface IFearActionSystem
{
    int QueuedCount { get; }
    void OnFearSpent(int amount);
    List<FearActionData> RevealAndDequeue();
    void OnDreadAdvanced(int newLevel);  // retroactive upgrade
}
