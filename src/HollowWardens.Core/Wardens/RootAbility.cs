namespace HollowWardens.Core.Wardens;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;

/// <summary>
/// Warden ability for The Root.
/// - Playing a bottom: card goes Dormant (shuffled back into draw pile, unplayable).
/// - Playing the bottom of an already-Dormant card on Boss: permanently removed.
/// - Rest-dissolve: card goes Dormant instead of being removed from the encounter.
/// - Passive fear: 1 Fear per directed Presence-adjacency pair (via PresenceSystem).
/// - On resolution: Assimilation — for each territory with Presence, remove ALL
///   invaders from each adjacent territory and reduce its Corruption by 1 point.
/// </summary>
public class RootAbility : IWardenAbility
{
    private readonly IPresenceSystem? _presence;

    public RootAbility() { }

    public RootAbility(IPresenceSystem presence)
    {
        _presence = presence;
    }

    public string WardenId => "root";

    public BottomResult OnBottomPlayed(Card card, EncounterTier tier)
    {
        if (card.IsDormant && tier == EncounterTier.Boss)
            return BottomResult.PermanentlyRemoved;

        return BottomResult.Dormant;
    }

    public BottomResult OnRestDissolve(Card card) => BottomResult.Dormant;

    /// <summary>
    /// Assimilation: for each territory where Root has Presence, remove ALL alive
    /// invaders from each adjacent territory and reduce that territory's Corruption by 1 point.
    /// </summary>
    public void OnResolution(EncounterState state)
    {
        foreach (var territory in state.Territories.Where(t => t.HasPresence).ToList())
        {
            foreach (var neighborId in TerritoryGraph.GetNeighbors(territory.Id))
            {
                var neighbor = state.GetTerritory(neighborId);
                if (neighbor == null) continue;

                // Remove all alive invaders
                var toRemove = neighbor.Invaders.Where(i => i.IsAlive).ToList();
                foreach (var invader in toRemove)
                {
                    neighbor.Invaders.Remove(invader);
                    GameEvents.InvaderDefeated?.Invoke(invader);
                }

                // Corruption -1 per assimilation (reduce by 1 point)
                if (neighbor.CorruptionPoints > 0)
                    state.Corruption?.ReduceCorruption(neighbor, 1);
            }
        }
    }

    /// <summary>
    /// Network Fear: 1 Fear per directed Presence→Presence adjacency edge.
    /// Two adjacent Presence territories contribute 2 (one per direction).
    /// </summary>
    public int CalculatePassiveFear() =>
        _presence?.CalculateNetworkFear() ?? 0;

    /// <summary>
    /// D29 Network Slow: −1 movement for invaders in territories adjacent to 2+ Presence territories.
    /// </summary>
    public int GetMovementPenalty(string territoryId, IEnumerable<Territory> allTerritories)
    {
        var territories = allTerritories.ToDictionary(t => t.Id);
        var neighbors = TerritoryGraph.GetNeighbors(territoryId);
        int presenceNeighborCount = neighbors.Count(n =>
            territories.TryGetValue(n, out var t) && t.HasPresence);
        return presenceNeighborCount >= 2 ? 1 : 0;
    }

    /// <summary>
    /// D29 Presence Provocation: Natives in Presence territories counter-attack on every invader action.
    /// </summary>
    public bool ProvokesNatives(Territory territory) => territory.HasPresence;

    /// <summary>
    /// D29 Rest Growth: Place 1 free Presence on any territory with existing Presence.
    /// Blocked by D28 vulnerability if territory is Defiled (corruption level 2+).
    /// </summary>
    public void OnRest(EncounterState state, string? targetTerritoryId)
    {
        if (targetTerritoryId == null) return;
        var territory = state.GetTerritory(targetTerritoryId);
        if (territory == null || !territory.HasPresence) return;

        // D28 Vulnerability: Defiled blocks placement
        if (territory.CorruptionLevel >= 2) return;

        state.Presence?.PlacePresence(territory, 1);
    }
}
