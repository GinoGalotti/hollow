namespace HollowWardens.Core.Wardens;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;

/// <summary>
/// Ember warden ability. Burst-damage glass cannon.
/// Flame Out: bottoms are always permanently removed (no Dormancy).
/// Ash Trail: tide-start corruption + damage in presence territories.
/// Scorched Earth: resolution damage = total corruption in presence territories, then halve.
/// Heat Wave (gated): on Rest, deal 2 damage to all invaders in all presence territories.
/// </summary>
public class EmberAbility : IWardenAbility
{
    public string WardenId => "ember";

    // Flame Out: bottoms are always permanent removal
    public BottomResult OnBottomPlayed(Card card, EncounterTier tier)
        => BottomResult.PermanentlyRemoved;

    // Rest-dissolve: also permanent (Ember burns everything)
    public BottomResult OnRestDissolve(Card card)
        => BottomResult.PermanentlyRemoved;

    // Ash Trail: at Tide start, each presence territory gains 1 Corruption and takes 1 damage to all invaders
    public void OnTideStart(EncounterState state)
    {
        foreach (var territory in state.Territories.Where(t => t.HasPresence).ToList())
        {
            state.Corruption?.AddCorruption(territory, 1);

            foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
            {
                invader.Hp = Math.Max(0, invader.Hp - 1);
                if (!invader.IsAlive)
                    GameEvents.InvaderDefeated?.Invoke(invader);
            }
        }

        // Controlled Burn: 3+ territories at Level 1 → generate 2 Fear
        if (state.PassiveGating == null || state.PassiveGating.IsActive("controlled_burn"))
        {
            int l1Count = state.Territories.Count(t => t.CorruptionLevel == 1);
            if (l1Count >= 3)
            {
                state.Dread?.OnFearGenerated(2);
                GameEvents.FearGenerated?.Invoke(2);
            }
        }
    }

    // Scorched Earth resolution:
    // 1. Damage = total corruption across presence territories → deal to all alive invaders (weakest first)
    // 2. Smart cleanse: L0/L1 → fully cleanse; L2 → halve (round down); L3 → no change (Desecrated is permanent)
    public void OnResolution(EncounterState state)
    {
        var presenceTerritories = state.Territories.Where(t => t.HasPresence).ToList();
        int totalCorruption = presenceTerritories.Sum(t => t.CorruptionPoints);

        if (totalCorruption > 0)
        {
            // Distribute damage board-wide, lowest HP first
            var allInvaders = state.Territories
                .SelectMany(t => t.Invaders.Where(i => i.IsAlive))
                .OrderBy(i => i.Hp)
                .ToList();

            int remaining = totalCorruption;
            foreach (var invader in allInvaders)
            {
                if (remaining <= 0) break;
                int dmg = Math.Min(remaining, invader.Hp);
                invader.Hp = Math.Max(0, invader.Hp - dmg);
                remaining -= dmg;
                if (!invader.IsAlive)
                    GameEvents.InvaderDefeated?.Invoke(invader);
            }
        }

        // Smart cleanse: based on corruption level at time of resolution
        foreach (var territory in presenceTerritories)
        {
            switch (territory.CorruptionLevel)
            {
                case 0:
                case 1:
                    // Level 0-1: fully cleanse
                    if (territory.CorruptionPoints > 0)
                        state.Corruption?.ReduceCorruption(territory, territory.CorruptionPoints);
                    break;
                case 2:
                    // Level 2: halve (round down)
                    int halved = territory.CorruptionPoints / 2;
                    int reduction = territory.CorruptionPoints - halved;
                    if (reduction > 0)
                        state.Corruption?.ReduceCorruption(territory, reduction);
                    break;
                // Level 3: no change (Desecrated is permanent)
            }
        }
    }

    // Ember has no passive fear
    public int CalculatePassiveFear() => 0;

    // D31: Ember tolerates Defiled (L2). Only Desecrated (L3, 15+ pts) blocks placement.
    public int PresenceBlockLevel() => 3;

    // Heat Wave (gated): on Rest, deal 2 damage to all invaders in all presence territories
    public void OnRest(EncounterState state, string? targetTerritoryId)
    {
        if (state.PassiveGating != null && !state.PassiveGating.IsActive("heat_wave"))
            return;

        foreach (var territory in state.Territories.Where(t => t.HasPresence).ToList())
        {
            foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
            {
                invader.Hp = Math.Max(0, invader.Hp - 2);
                if (!invader.IsAlive)
                    GameEvents.InvaderDefeated?.Invoke(invader);
            }
        }
    }
}
