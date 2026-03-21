namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Models;

public interface IFearActionSystem
{
    int QueuedCount { get; }
    void OnFearSpent(int amount);
    List<FearActionData> RevealAndDequeue();
    /// <summary>Returns and clears the queue WITHOUT firing FearActionRevealed events (player-driven reveal).</summary>
    List<FearActionData> DrainQueue();
    void OnDreadAdvanced(int newLevel);  // retroactive upgrade
}
