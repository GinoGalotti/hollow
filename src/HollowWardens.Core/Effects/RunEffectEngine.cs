namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Models;
using HollowWardens.Core.Run;

/// <summary>
/// Generic engine that applies RunEffect instances to RunState.
/// Shared by events, passive upgrades, and rewards — no game-specific logic here.
/// </summary>
public static class RunEffectEngine
{
    public static void Apply(RunState run, RunEffect effect, Random rng,
                             List<Card>? availableCards = null)
    {
        switch (effect.Type)
        {
            case "heal_weave":
                run.CurrentWeave = Math.Min(run.CurrentWeave + effect.Value, run.MaxWeave);
                break;

            case "heal_max_weave":
                run.MaxWeave += effect.Value;
                break;

            case "reduce_max_weave":
                run.MaxWeave = Math.Max(1, run.MaxWeave - effect.Value);
                run.CurrentWeave = Math.Min(run.CurrentWeave, run.MaxWeave);
                break;

            case "add_tokens":
                run.UpgradeTokens += effect.Value;
                break;

            case "remove_tokens":
                run.UpgradeTokens = Math.Max(0, run.UpgradeTokens - effect.Value);
                break;

            case "add_corruption":
                ApplyCorruption(run, effect, rng);
                break;

            case "cleanse_carryover":
                run.CorruptionCarryover.Clear();
                break;

            case "dissolve_card":
                DissolveCards(run, effect, rng);
                break;

            case "add_card":
                AddCard(run, effect, rng, availableCards);
                break;

            case "recover_card":
                RecoverCard(run, effect);
                break;

            case "unlock_passive":
                if (effect.TargetId != null)
                    run.PermanentlyUnlockedPassives.Add(effect.TargetId);
                break;

            case "upgrade_passive":
                if (effect.TargetId != null && !run.AppliedPassiveUpgradeIds.Contains(effect.TargetId))
                    run.AppliedPassiveUpgradeIds.Add(effect.TargetId);
                break;

            case "upgrade_card":
                if (effect.TargetId != null && !run.AppliedCardUpgradeIds.Contains(effect.TargetId))
                    run.AppliedCardUpgradeIds.Add(effect.TargetId);
                break;

            case "set_elements":
                // Stored for next encounter start — RunState.StartingElements is not yet modelled
                break;

            case "set_balance":
                // Balance override stored for next encounter — handled by caller
                break;

            case "modify_hand_limit":
                // Hand limit modifier — handled by caller
                break;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void ApplyCorruption(RunState run, RunEffect effect, Random rng)
    {
        var territories = effect.Territories;
        if (territories == null || territories.Count == 0) return;

        string territory;
        if (territories.Count == 1 && territories[0] == "random")
        {
            // Pick a random territory from the existing carryover keys, or a fallback
            var keys = run.CorruptionCarryover.Keys.ToList();
            if (keys.Count == 0)
            {
                // Fallback: spread to a standard territory when carryover is empty
                keys = new List<string> { "A1", "A2", "A3", "M1", "M2" };
            }
            territory = keys[rng.Next(keys.Count)];
        }
        else
        {
            territory = territories[rng.Next(territories.Count)];
        }

        run.CorruptionCarryover.TryGetValue(territory, out var current);
        run.CorruptionCarryover[territory] = current + effect.Value;
    }

    private static void DissolveCards(RunState run, RunEffect effect, Random rng)
    {
        // Remove N random cards from the deck (by ID)
        var deck = run.DeckCardIds;
        int count = Math.Min(effect.Value, deck.Count);
        for (int i = 0; i < count; i++)
        {
            int idx = rng.Next(deck.Count);
            var id = deck[idx];
            deck.RemoveAt(idx);
            run.PermanentlyRemovedCardIds.Add(id);
        }
    }

    private static void AddCard(RunState run, RunEffect effect, Random rng,
                                 List<Card>? availableCards)
    {
        if (availableCards == null || availableCards.Count == 0) return;

        // Filter by rarity if specified
        var pool = effect.Rarity != null
            ? availableCards.Where(c => c.Rarity.ToString().Equals(effect.Rarity, StringComparison.OrdinalIgnoreCase)).ToList()
            : availableCards;

        if (pool.Count == 0) return;

        var card = pool[rng.Next(pool.Count)];
        run.DeckCardIds.Add(card.Id);
    }

    private static void RecoverCard(RunState run, RunEffect effect)
    {
        if (effect.TargetId == null) return;
        if (!run.PermanentlyRemovedCardIds.Remove(effect.TargetId)) return;
        run.DeckCardIds.Add(effect.TargetId);
    }
}
