using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class WardenRoot : Warden
{
    // Dormancy: first dissolve → Dormant in deck; second dissolve → permanently removed
    public override void OnDissolve(CardData card)
    {
        if (card.IsDormant)
        {
            // Second dissolution: fully consumed
            DissolvedThisEncounter.Remove(card);
            if (!PermanentlyRemoved.Contains(card))
                PermanentlyRemoved.Add(card);
        }
        else
        {
            // First dissolution: enter Dormancy (overrides Boss tier too)
            DissolvedThisEncounter.Remove(card);
            PermanentlyRemoved.Remove(card);
            card.IsDormant = true;
            Deck?.AddToBottom(card);
        }
    }

    // Network Fear: fires at Tide start via TurnManager.EndVigil
    public override void OnTideStart()
    {
        if (GameState.Instance == null) return;
        var graph = new TerritoryGraph();
        var presenceIds = GameState.Instance.Territories
            .Where(kv => kv.Value.PresenceCount > 0)
            .Select(kv => kv.Key)
            .ToHashSet();

        int totalFear = 0;
        foreach (var id in presenceIds)
            totalFear += graph.GetNeighbors(id).Count(n => presenceIds.Contains(n));

        if (totalFear > 0)
            GameState.Instance.ModifyFear(totalFear);
    }

    // Assimilation: Presence tokens absorb adjacent invaders → Corruption -1
    public override void OnResolutionStart(List<TerritoryState> territories)
    {
        if (GameState.Instance == null) return;
        var graph = new TerritoryGraph();

        foreach (var territory in territories)
        {
            if (territory.PresenceCount <= 0) continue;
            foreach (var neighborId in graph.GetNeighbors(territory.Id))
            {
                if (!GameState.Instance.Territories.TryGetValue(neighborId, out var neighbor)) continue;
                if (neighbor.InvaderUnits.Count == 0) continue;

                foreach (var invader in neighbor.InvaderUnits.ToList())
                {
                    neighbor.InvaderUnits.Remove(invader);
                    GameState.Instance.ActiveInvaders.Remove(invader);
                }
                neighbor.Corruption = Math.Max(0, neighbor.Corruption - 1);
                EventBus.Instance?.EmitSignal(EventBus.SignalName.CorruptionChanged,
                    neighborId, neighbor.Corruption);
            }
        }
    }
}
