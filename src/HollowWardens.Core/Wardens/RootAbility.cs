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
/// - On resolution: Assimilation (B6) — base: each territory with stacked Presence ≥ threshold spawns 1 Native;
///   upgraded (assimilation_u1): also converts invaders → Natives in ≥2-presence + ≥2-native territories.
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
    /// Assimilation (B6 redesign — two tiers):
    ///
    /// Base (always active when assimilation is active):
    ///   At Resolution, each territory with Presence ≥ AssimilationSpawnThreshold spawns 1 Native.
    ///   "Stack presence → the forest grows." No invader conversion at base level.
    ///
    /// Upgraded (assimilation_u1):
    ///   After the spawn pass, territories with ≥2 Presence + ≥2 Natives + invaders
    ///   convert floor(min(presence, natives) / 2) invaders → Natives (weakest first,
    ///   HP = max(1, invader.MaxHp / 2)). This is the D42 conversion logic, now gated
    ///   behind the upgrade so the base mechanic can seed the native army first.
    /// </summary>
    public void OnResolution(EncounterState state)
    {
        if (Gating != null && !Gating.IsActive("assimilation")) return;

        int spawnThreshold = _config?.AssimilationSpawnThreshold ?? 3;
        int nativeHp       = _config?.DefaultNativeHp ?? 2;

        // ── Base: spawn 1 native per territory with stacked presence ≥ threshold ──
        foreach (var territory in state.Territories.Where(t => t.PresenceCount >= spawnThreshold).ToList())
        {
            territory.Natives.Add(new Native
            {
                Hp          = nativeHp,
                MaxHp       = nativeHp,
                Damage      = 1,
                TerritoryId = territory.Id,
            });
        }

        // ── Upgrade: also convert invaders → natives in ≥2-presence + ≥2-native territories ──
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
}
