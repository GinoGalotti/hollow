namespace HollowWardens.Core.Run;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;

/// <summary>
/// Simple bot for simulation. Plays a spread-presence, damage-tops, cleanse-bottoms strategy.
/// Not optimal — just good enough to generate meaningful balance data.
/// </summary>
public class BotStrategy : IPlayerStrategy
{
    private int _topsPlayedThisTurn;

    /// <summary>Set before each card choice. Format: "PRIORITY: {rule} — {context}".</summary>
    public string LastDecisionReason { get; private set; } = "";

    public Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state)
    {
        if (_topsPlayedThisTurn >= 2) { _topsPlayedThisTurn = 0; LastDecisionReason = "SKIP: play limit reached"; return null; }

        // Priority 1: Place presence if we have < 3 territories with presence
        int presenceTerritories = state.Territories.Count(t => t.HasPresence);
        if (presenceTerritories < 3)
        {
            var presenceCard = hand.FirstOrDefault(c =>
                !c.IsDormant && c.TopEffect.Type == EffectType.PlacePresence);
            if (presenceCard != null)
            {
                LastDecisionReason = $"PRIORITY: presence_expansion — only {presenceTerritories} presence territories, need 3+";
                _topsPlayedThisTurn++; return presenceCard;
            }
        }

        // Priority 1.5: Soft unlock — prioritize Shadow cards to unlock Network Slow
        if (state.PassiveGating != null && !state.PassiveGating.IsActive("network_slow"))
        {
            var shadowCard = hand.FirstOrDefault(c =>
                !c.IsDormant && c.Elements.Contains(Element.Shadow));
            if (shadowCard != null)
            {
                LastDecisionReason = "PRIORITY: passive_unlock — playing Shadow to unlock network_slow";
                _topsPlayedThisTurn++; return shadowCard;
            }
        }

        // Priority 2: Damage if any territory has invaders in range
        var damageCard = hand.FirstOrDefault(c =>
            !c.IsDormant && c.TopEffect.Type == EffectType.DamageInvaders);
        if (damageCard != null && state.Territories.Any(t => t.Invaders.Any(i => i.IsAlive)))
        {
            var target = state.Territories.OrderByDescending(t => t.Invaders.Count(i => i.IsAlive)).First();
            LastDecisionReason = $"PRIORITY: damage — {target.Id} has {target.Invaders.Count(i => i.IsAlive)} invaders";
            _topsPlayedThisTurn++; return damageCard;
        }

        // Priority 3: Cleanse if any territory has corruption >= 5
        var cleanseCard = hand.FirstOrDefault(c =>
            !c.IsDormant && c.TopEffect.Type == EffectType.ReduceCorruption);
        if (cleanseCard != null && state.Territories.Any(t => t.CorruptionPoints >= 5))
        {
            var worst = state.Territories.OrderByDescending(t => t.CorruptionPoints).First();
            LastDecisionReason = $"PRIORITY: cleanse — {worst.Id} at {worst.CorruptionPoints} corruption";
            _topsPlayedThisTurn++; return cleanseCard;
        }

        // Priority 4: Fear generation
        var fearCard = hand.FirstOrDefault(c =>
            !c.IsDormant && c.TopEffect.Type == EffectType.GenerateFear);
        if (fearCard != null)
        {
            LastDecisionReason = "PRIORITY: fear — no urgent threats, generating fear";
            _topsPlayedThisTurn++; return fearCard;
        }

        // Priority 5: Any non-dormant card
        var anyCard = hand.FirstOrDefault(c => !c.IsDormant);
        if (anyCard != null)
        {
            LastDecisionReason = $"PRIORITY: any_card — playing {anyCard.Id}";
            _topsPlayedThisTurn++; return anyCard;
        }

        _topsPlayedThisTurn = 0;
        LastDecisionReason = "SKIP: all cards dormant, no playable options";
        return null;
    }

    public Card? ChooseBottomPlay(IReadOnlyList<Card> hand, EncounterState state)
    {
        // Play the highest-value bottom available
        var best = hand
            .Where(c => !c.IsDormant)
            .OrderByDescending(c => BottomPriority(c, state))
            .FirstOrDefault();
        if (best != null)
            LastDecisionReason = $"PRIORITY: best_bottom — {best.BottomEffect.Type} (priority {BottomPriority(best, state)})";
        else
            LastDecisionReason = "SKIP: no playable bottom";
        return best;
    }

    public Dictionary<Invader, int>? AssignCounterDamage(
        Territory territory, int damagePool, EncounterState state)
    {
        // Auto-assign: null tells EncounterRunner to use CombatSystem.AutoAssignCounterAttack
        return null;
    }

    public string? ChooseRestGrowthTarget(EncounterState state)
    {
        return state.Territories
            .Where(t => t.HasPresence && t.CorruptionLevel < 2)
            .OrderByDescending(t => t.PresenceCount)
            .FirstOrDefault()?.Id;
    }

    public string? ChooseTarget(EffectData effect, EncounterState state)
    {
        return effect.Type switch
        {
            // Damage: target territory with most invaders
            EffectType.DamageInvaders => state.Territories
                .Where(t => t.Invaders.Any(i => i.IsAlive))
                .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
                .FirstOrDefault()?.Id,

            // Cleanse: target most corrupted territory
            EffectType.ReduceCorruption or EffectType.Purify => state.Territories
                .OrderByDescending(t => t.CorruptionPoints)
                .FirstOrDefault()?.Id,

            // Presence: target territory adjacent to existing presence, not Defiled
            EffectType.PlacePresence => ChoosePlacementTarget(state),

            // Default: first territory with presence
            _ => state.Territories.FirstOrDefault(t => t.HasPresence)?.Id
                ?? state.Territories.FirstOrDefault()?.Id
        };
    }

    private static string? ChoosePlacementTarget(EncounterState state)
    {
        // Prefer empty territory adjacent to presence, not Defiled
        var presenceIds = state.Territories.Where(t => t.HasPresence).Select(t => t.Id).ToHashSet();
        var candidate = state.Territories
            .Where(t => !t.HasPresence && t.CorruptionLevel < 2
                && TerritoryGraph.GetNeighbors(t.Id).Any(n => presenceIds.Contains(n)))
            .OrderBy(t => t.CorruptionPoints)
            .FirstOrDefault();
        if (candidate != null) return candidate.Id;

        // Fallback: reinforce existing presence
        return state.Territories
            .Where(t => t.HasPresence && t.CorruptionLevel < 2)
            .OrderBy(t => t.PresenceCount)
            .FirstOrDefault()?.Id;
    }

    private static int BottomPriority(Card card, EncounterState state)
    {
        return card.BottomEffect.Type switch
        {
            EffectType.DamageInvaders when state.Territories.Any(t => t.Invaders.Any(i => i.IsAlive)) => 100,
            EffectType.ReduceCorruption when state.Territories.Any(t => t.CorruptionPoints >= 8) => 90,
            EffectType.GenerateFear => 60,
            EffectType.PlacePresence => 50,
            EffectType.RestoreWeave when (state.Weave?.CurrentWeave ?? 20) < 10 => 80,
            EffectType.AwakeDormant => 40,
            _ => 10
        };
    }
}
