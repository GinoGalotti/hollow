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
    /// <summary>Causes the next queued Fear Action to draw from one Dread Level higher than current.</summary>
    void ElevateNextDraw();
    /// <summary>Bugfix: prevents new fear actions from being queued during resolution (avoids infinite loop).</summary>
    void BeginResolution();
    void EndResolution();
}
