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
/// - Network Fear: 1 Fear per invader in a territory adjacent to ≥3 Presence territories, capped at NetworkFearCap.
/// - Network Slow: invaders in territories adjacent to ≥3 Presence territories have −1 Advance movement.
/// - On tide start: Assimilation (B6) — pick ONE presence territory, spawn natives based on presence count
///   and AssimilationSpawnMode (linear / scaled / half).
/// - On resolution: Assimilation upgrade (assimilation_u1) — territories with ≥2 Presence + ≥2 Natives + invaders
///   convert floor(min(presence, natives) / 2) invaders → Natives (weakest first, HP = max(1, invader.MaxHp / 2)).
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

    /// <summary>Optional gating — controls which passives are active.</summary>
    public PassiveGating? Gating { get; set; }

    public BottomResult OnBottomPlayed(Card card, EncounterTier tier)
    {
        if (card.IsDormant && tier == EncounterTier.Boss)
            return BottomResult.PermanentlyRemoved;

        return BottomResult.Dormant;
    }

    public BottomResult OnRestDissolve(Card card) => BottomResult.Dormant;

    /// <summary>
    /// Assimilation — base (B6 tide-start spawn):
    ///   Pick the presence territory with the most adjacent invaders (tie-break: most presence).
    ///   Spawn natives there based on AssimilationSpawnMode:
    ///     linear: count = presence
    ///     scaled:  count = 1 + floor(presence / 2)   [default]
    ///     half:    count = ceil(presence / 2)
    ///   Natives spawned at tide start can counter-attack that same tide if Provocation is active.
    /// </summary>
    public void OnTideStart(EncounterState state)
    {
        if (Gating != null && !Gating.IsActive("assimilation")) return;

        var territory = ChooseSpawnTerritory(state);
        if (territory == null) return;

        string mode      = _config?.AssimilationSpawnMode ?? "scaled";
        int    spawnCount = CalcSpawnCount(territory.PresenceCount, mode);
        int    nativeHp   = _config?.DefaultNativeHp ?? 2;

        for (int i = 0; i < spawnCount; i++)
        {
            territory.Natives.Add(new Native
            {
                Hp          = nativeHp,
                MaxHp       = nativeHp,
                Damage      = 1,
                TerritoryId = territory.Id,
            });
        }
    }

    /// <summary>
    /// Assimilation upgrade (assimilation_u1 — Resolution):
    ///   After the final tide, territories with ≥2 Presence + ≥2 Natives + invaders
    ///   convert floor(min(presence, natives) / 2) invaders → Natives (weakest first).
    ///   The base spawn (OnTideStart) builds the native army this upgrade needs.
    /// </summary>
    public void OnResolution(EncounterState state)
    {
        if (!(Gating?.IsUpgraded("assimilation_u1") ?? false)) return;

        foreach (var territory in state.Territories.Where(t => t.PresenceCount >= 2).ToList())
        {
            int aliveNatives = territory.Natives.Count(n => n.IsAlive);
            if (aliveNatives < 2) continue;

            int conversions = Math.Min(territory.PresenceCount, aliveNatives) / 2;
            if (conversions <= 0) continue;

            var toConvert = territory.Invaders
                .Where(i => i.IsAlive)
                .OrderBy(i => i.Hp)  // convert weakest first
                .Take(conversions)
                .ToList();

            foreach (var invader in toConvert)
            {
                territory.Invaders.Remove(invader);
                GameEvents.InvaderDefeated?.Invoke(invader);

                territory.Natives.Add(new Native
                {
                    Hp          = Math.Max(1, invader.MaxHp / 2),
                    MaxHp       = Math.Max(1, invader.MaxHp / 2),
                    Damage      = 1,
                    TerritoryId = territory.Id,
                });
            }
        }
    }

    /// <summary>
    /// Network Fear: for each invader in a territory adjacent to ≥3 Presence territories,
    /// generate 1 Fear. Total capped at NetworkFearCap (default 3).
    /// Rewards wide network building — spreading across many territories.
    /// </summary>
    public int CalculatePassiveFear(EncounterState state)
    {
        var territories = state.Territories.ToDictionary(t => t.Id);
        int fear = 0;

        foreach (var territory in state.Territories)
        {
            int aliveInvaders = territory.Invaders.Count(i => i.IsAlive);
            if (aliveInvaders == 0) continue;

            int presenceNeighborCount = TerritoryGraph.Standard.GetNeighbors(territory.Id)
                .Count(n => territories.TryGetValue(n, out var t) && t.HasPresence);

            if (presenceNeighborCount >= 3)
                fear += aliveInvaders;
        }

        return Math.Min(fear, _config?.NetworkFearCap ?? 3);
    }

    /// <summary>
    /// Network Slow: invaders in a territory adjacent to ≥3 Presence territories have −1 movement.
    /// Requires dense wide spread to trigger — prevents accidental early-game shutdown.
    /// Upgrade (network_slow_u1) increases penalty to −2.
    /// </summary>
    public int GetMovementPenalty(string territoryId, IEnumerable<Territory> allTerritories)
    {
        if (Gating != null && !Gating.IsActive("network_slow")) return 0;
        var territories = allTerritories.ToDictionary(t => t.Id);
        if (!territories.TryGetValue(territoryId, out _)) return 0;

        var neighbors = TerritoryGraph.Standard.GetNeighbors(territoryId);
        int presenceNeighborCount = neighbors.Count(n =>
            territories.TryGetValue(n, out var t) && t.HasPresence);

        if (presenceNeighborCount < 3) return 0;

        // Upgrade: increases penalty from 1 to 2
        return (Gating?.IsUpgraded("network_slow_u1") ?? false) ? 2 : 1;
    }

    /// <summary>
    /// Presence Provocation: Natives in Presence territories counter-attack on every invader action.
    /// Upgrade (presence_provocation_u1) extends to range 1 from Presence territories.
    /// </summary>
    public bool ProvokesNatives(Territory territory)
    {
        if (Gating != null && !Gating.IsActive("presence_provocation")) return false;
        return territory.HasPresence;
    }

    /// <summary>
    /// Rest Growth: Place 1 free Presence on any territory with existing Presence.
    /// Upgrade (rest_growth_u1) places 2 Presence instead of 1.
    /// </summary>
    public void OnRest(EncounterState state, string? targetTerritoryId)
    {
        if (Gating != null && !Gating.IsActive("rest_growth")) return;
        if (targetTerritoryId == null) return;
        var territory = state.GetTerritory(targetTerritoryId);
        if (territory == null || !territory.HasPresence) return;

        // D28/D31 Vulnerability: use warden's own tolerance threshold
        if (territory.CorruptionLevel >= (state.Warden?.PresenceBlockLevel() ?? 2)) return;

        int count = (Gating?.IsUpgraded("rest_growth_u1") ?? false) ? 2 : 1;
        state.Presence?.PlacePresence(territory, count);
    }

    /// <summary>
    /// Bot heuristic: choose the presence territory with the most invaders in adjacent territories.
    /// Tie-break: most presence (maximises spawn count and conversion potential).
    /// </summary>
    private static Territory? ChooseSpawnTerritory(EncounterState state)
    {
        var territoryMap = state.Territories.ToDictionary(t => t.Id);
        return state.Territories
            .Where(t => t.HasPresence)
            .OrderByDescending(t =>
                TerritoryGraph.Standard.GetNeighbors(t.Id)
                    .Sum(n => territoryMap.TryGetValue(n, out var neighbor)
                        ? neighbor.Invaders.Count(i => i.IsAlive)
                        : 0))
            .ThenByDescending(t => t.PresenceCount)
            .FirstOrDefault();
    }

    /// <summary>
    /// Returns native spawn count for the given presence and mode.
    /// linear: count = presence
    /// scaled:  count = 1 + floor(presence / 2)   [default]
    /// half:    count = ceil(presence / 2)
    /// </summary>
    private static int CalcSpawnCount(int presenceCount, string mode) => mode switch
    {
        "linear" => presenceCount,
        "half"   => (int)Math.Ceiling(presenceCount / 2.0),
        _        => 1 + presenceCount / 2, // "scaled" default
    };
}
