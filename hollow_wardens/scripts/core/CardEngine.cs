using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Routes card plays to Vigil/Dusk effects, resolves those effects against game state,
// and handles dissolution. Plain C# class — no Godot lifecycle.
public class CardEngine
{
    public TurnManager? TurnManager { get; set; }
    public TerritoryGraph? Graph { get; set; }

    public bool TryPlayCard(CardData card, TurnManager.TurnPhase phase, string? targetId = null, string? sourceId = null)
    {
        if (GameState.Instance == null) return false;
        if (TurnManager == null) return false;
        if (phase != TurnManager.TurnPhase.Vigil && phase != TurnManager.TurnPhase.Dusk) return false;
        if (!TurnManager.CanPlayCard(phase)) return false;

        var warden = GameState.Instance.CurrentWarden;
        if (warden == null) return false;
        if (warden.Hand == null || !warden.Hand.Cards.Contains(card)) return false;
        if (card.IsDormant) return false;

        var effect = phase == TurnManager.TurnPhase.Vigil ? card.VigilEffect : card.DuskEffect;
        if (effect == null) return false;
        if (!CanResolveEffect(effect, targetId)) return false;

        warden.Hand.RemoveCard(card);
        ResolveEffect(effect, targetId, sourceId);
        warden.Discard.Add(card);
        TurnManager.RecordCardPlayed(phase);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.CardPlayed, card, (int)phase);
        return true;
    }

    // Caller must remove card from Hand before calling.
    public void DissolveCard(CardData card, string? targetId = null)
    {
        var warden = GameState.Instance?.CurrentWarden;
        if (warden == null) return;

        var effect = card.DissolveEffect ?? new CardEffect
        {
            Type = CardEffect.EffectType.PlacePresence,
            Value = 1,
            Range = 0
        };

        ResolveEffect(effect, targetId);

        var tier = GameState.Instance!.EncounterTier;
        if (tier == EncounterData.EncounterTier.Boss)
            warden.PermanentlyRemoved.Add(card);
        else
            warden.DissolvedThisEncounter.Add(card);

        warden.OnDissolve(card);

        if (warden.PermanentlyRemoved.Contains(card))
            EventBus.Instance?.EmitSignal(EventBus.SignalName.CardPermanentlyRemoved, card);
        else if (warden.DissolvedThisEncounter.Contains(card))
            EventBus.Instance?.EmitSignal(EventBus.SignalName.CardDissolved, card, (int)tier);
        // Dormant: card moved to Deck — no signal (Phase 6 UI will add CardDormant signal)
    }

    internal bool CanResolveEffect(CardEffect effect, string? targetId)
    {
        switch (effect.Type)
        {
            case CardEffect.EffectType.PlacePresence:
            case CardEffect.EffectType.MovePresence:
            case CardEffect.EffectType.GenerateFear:
            case CardEffect.EffectType.ReduceCorruption:
            case CardEffect.EffectType.Purify:
            case CardEffect.EffectType.DamageInvaders:
            case CardEffect.EffectType.PushInvaders:
            case CardEffect.EffectType.RoutInvaders:
                if (targetId == null) return false;
                if (GameState.Instance == null || !GameState.Instance.Territories.ContainsKey(targetId)) return false;
                if (!IsInRange(targetId, effect.Range)) return false;
                return true;
            default: // RestoreWeave, PredictTide, Custom, Conditional
                return true;
        }
    }

    internal void ResolveEffect(CardEffect effect, string? targetId, string? sourceId = null)
    {
        switch (effect.Type)
        {
            case CardEffect.EffectType.PlacePresence:
            {
                if (targetId == null || GameState.Instance == null) break;
                var territory = GameState.Instance.Territories[targetId];
                territory.PresenceCount += effect.Value;
                var wardenId = GameState.Instance.CurrentWarden?.WardenData?.Id ?? "";
                EventBus.Instance?.EmitSignal(EventBus.SignalName.PresencePlaced, targetId, wardenId);
                break;
            }
            case CardEffect.EffectType.GenerateFear:
                GameState.Instance?.ModifyFear(effect.Value);
                break;
            case CardEffect.EffectType.ReduceCorruption:
            {
                if (targetId == null || GameState.Instance == null) break;
                var territory = GameState.Instance.Territories[targetId];
                territory.Corruption = Math.Max(0, territory.Corruption - effect.Value);
                EventBus.Instance?.EmitSignal(EventBus.SignalName.CorruptionChanged, targetId, territory.Corruption);
                break;
            }
            case CardEffect.EffectType.DamageInvaders:
            {
                if (targetId == null || GameState.Instance == null) break;
                var territory = GameState.Instance.Territories[targetId];
                var units = territory.InvaderUnits.OrderBy(u => u.Hp).ToList();
                int remaining = effect.Value;
                foreach (var unit in units)
                {
                    if (remaining <= 0) break;
                    int dmg = Math.Min(remaining, unit.Hp);
                    unit.TakeDamage(dmg);
                    remaining -= dmg;
                    if (unit.IsDefeated)
                    {
                        territory.InvaderUnits.Remove(unit);
                        GameState.Instance.ActiveInvaders.Remove(unit);
                        EventBus.Instance?.EmitSignal(EventBus.SignalName.InvaderDefeated, targetId);
                    }
                }
                break;
            }
            case CardEffect.EffectType.RestoreWeave:
                GameState.Instance?.ModifyWeave(effect.Value);
                break;
            case CardEffect.EffectType.AwakeDormant:
            {
                var warden = GameState.Instance?.CurrentWarden;
                if (warden?.Hand == null) break;
                var dormant = warden.Hand.Cards.Where(c => c.IsDormant).ToList();
                int count = effect.Value == 0 ? dormant.Count : Math.Min(effect.Value, dormant.Count);
                foreach (var c in dormant.Take(count))
                    c.IsDormant = false;
                break;
            }
            default:
                GD.Print($"[CardEngine] Effect type {effect.Type} is stubbed — no-op");
                break;
        }
    }

    internal bool IsInRange(string targetId, int range)
    {
        if (range == 0) return true;
        if (GameState.Instance == null) return false;
        var presenceTerritories = GameState.Instance.Territories
            .Where(kv => kv.Value.PresenceCount > 0)
            .Select(kv => kv.Key)
            .ToList();
        if (presenceTerritories.Count == 0) return false;
        return GetMinDistance(targetId, presenceTerritories) <= range;
    }

    // BFS from fromId; returns distance to nearest id in toIds, or int.MaxValue if unreachable.
    internal int GetMinDistance(string fromId, IEnumerable<string> toIds)
    {
        if (Graph == null) return int.MaxValue;
        var targets = new HashSet<string>(toIds);
        if (targets.Contains(fromId)) return 0;

        var visited = new HashSet<string> { fromId };
        var queue = new Queue<(string id, int dist)>();
        queue.Enqueue((fromId, 0));

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            foreach (var neighbor in Graph.GetNeighbors(current))
            {
                if (visited.Contains(neighbor)) continue;
                visited.Add(neighbor);
                if (targets.Contains(neighbor)) return dist + 1;
                queue.Enqueue((neighbor, dist + 1));
            }
        }
        return int.MaxValue;
    }
}
