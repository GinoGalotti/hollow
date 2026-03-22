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
    private readonly BalanceConfig? _config;

    public RootAbility() { }

    public RootAbility(IPresenceSystem presence, BalanceConfig? config = null)
    {
        _presence = presence;
        _config = config;
    }

    public string WardenId => "root";

    /// <summary>Optional gating — when set, locks passives until thresholds unlock them.</summary>
    public PassiveGating? Gating { get; set; }

    public BottomResult OnBottomPlayed(Card card, EncounterTier tier)
    {
        if (card.IsDormant && tier == EncounterTier.Boss)
            return BottomResult.PermanentlyRemoved;

        return BottomResult.Dormant;
    }

    public BottomResult OnRestDissolve(Card card) => BottomResult.Dormant;

    /// <summary>
    /// D30 Assimilation nerf: for each territory with Presence, remove up to
    /// PresenceCount invaders (weakest first) from each adjacent territory.
    /// Each removed invader reduces Corruption by 1. Stacking presence = stronger clear.
    /// </summary>
    public void OnResolution(EncounterState state)
    {
        foreach (var territory in state.Territories.Where(t => t.HasPresence).ToList())
        {
            foreach (var neighborId in TerritoryGraph.GetNeighbors(territory.Id))
            {
                var neighbor = state.GetTerritory(neighborId);
                if (neighbor == null) continue;

                // Remove up to PresenceCount invaders (weakest first)
                var toRemove = neighbor.Invaders
                    .Where(i => i.IsAlive)
                    .OrderBy(i => i.Hp)
                    .Take(territory.PresenceCount)
                    .ToList();

                foreach (var invader in toRemove)
                {
                    neighbor.Invaders.Remove(invader);
                    GameEvents.InvaderDefeated?.Invoke(invader);
                }

                // Corruption -1 per removed invader
                if (toRemove.Count > 0)
                    state.Corruption?.ReduceCorruption(neighbor, toRemove.Count);
            }
        }
    }

    /// <summary>
    /// Network Fear: 1 Fear per undirected Presence adjacency edge, capped at 4 per Tide.
    /// Cap is a balance knob on Root — not a system-wide rule.
    /// </summary>
    public int CalculatePassiveFear() =>
        Math.Min(_presence?.CalculateNetworkFear() ?? 0, _config?.NetworkFearCap ?? 4);

    /// <summary>
    /// D30 Network Slow redesign: slow only when adjacent presence territories strictly
    /// outnumber the invaders in the target territory. Dense presence traps lone scouts;
    /// a wave of 3 marchers pushes through 2 presence neighbors (2 ≤ 3, no slow).
    /// </summary>
    public int GetMovementPenalty(string territoryId, IEnumerable<Territory> allTerritories)
    {
        if (Gating != null && !Gating.IsActive("network_slow")) return 0;
        var territories = allTerritories.ToDictionary(t => t.Id);
        if (!territories.TryGetValue(territoryId, out var territory)) return 0;

        var neighbors = TerritoryGraph.GetNeighbors(territoryId);
        int presenceNeighborCount = neighbors.Count(n =>
            territories.TryGetValue(n, out var t) && t.HasPresence);

        int invaderCount = territory.Invaders.Count(i => i.IsAlive);

        // No invaders = no slow needed. Presence must strictly outnumber invaders.
        if (invaderCount == 0 || presenceNeighborCount <= invaderCount) return 0;
        return 1;
    }

    /// <summary>
    /// D29 Presence Provocation: Natives in Presence territories counter-attack on every invader action.
    /// </summary>
    public bool ProvokesNatives(Territory territory)
    {
        if (Gating != null && !Gating.IsActive("presence_provocation")) return false;
        return territory.HasPresence;
    }

    /// <summary>
    /// D29 Rest Growth: Place 1 free Presence on any territory with existing Presence.
    /// Blocked by D28 vulnerability if territory is Defiled (corruption level 2+).
    /// </summary>
    public void OnRest(EncounterState state, string? targetTerritoryId)
    {
        if (Gating != null && !Gating.IsActive("rest_growth")) return;
        if (targetTerritoryId == null) return;
        var territory = state.GetTerritory(targetTerritoryId);
        if (territory == null || !territory.HasPresence) return;

        // D28/D31 Vulnerability: use warden's own tolerance threshold
        if (territory.CorruptionLevel >= (state.Warden?.PresenceBlockLevel() ?? 2)) return;

        state.Presence?.PlacePresence(territory, 1);
    }
}
