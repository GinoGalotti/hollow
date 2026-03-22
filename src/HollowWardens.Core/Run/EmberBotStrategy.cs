namespace HollowWardens.Core.Run;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;

/// <summary>
/// Bot strategy for Ember. Aggressive burst-damage priorities:
/// 1. Spread presence to 3 territories (Ash Trail / Scorched Earth need coverage)
/// 2. Deal damage (Ember's primary role)
/// 3. Generate fear (triggers Phoenix Spark via bottoms)
/// 4. Any non-dormant card (Ember wants to burn through the deck)
/// 5. Cleanse ONLY when corruption >= 7 (about to block presence placement)
/// </summary>
public class EmberBotStrategy : IPlayerStrategy
{
    private int _topsPlayedThisTurn;

    /// <summary>Set before each card choice. Format: "PRIORITY: {rule} — {context}".</summary>
    public string LastDecisionReason { get; private set; } = "";

    public Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state)
    {
        if (_topsPlayedThisTurn >= 2) { _topsPlayedThisTurn = 0; LastDecisionReason = "SKIP: play limit reached"; return null; }

        // Priority 1: spread presence to 3 territories
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

        // Priority 2: damage (Ember's primary role)
        var damageCard = hand.FirstOrDefault(c =>
            !c.IsDormant && c.TopEffect.Type == EffectType.DamageInvaders);
        if (damageCard != null && state.Territories.Any(t => t.Invaders.Any(i => i.IsAlive)))
        {
            var target = state.Territories.OrderByDescending(t => t.Invaders.Count(i => i.IsAlive)).First();
            LastDecisionReason = $"PRIORITY: damage — {target.Id} has {target.Invaders.Count(i => i.IsAlive)} invaders, playing Ember card";
            _topsPlayedThisTurn++; return damageCard;
        }

        // Priority 3: fear generation
        var fearCard = hand.FirstOrDefault(c =>
            !c.IsDormant && c.TopEffect.Type == EffectType.GenerateFear);
        if (fearCard != null)
        {
            LastDecisionReason = "PRIORITY: fear — generating fear (Phoenix Spark may trigger)";
            _topsPlayedThisTurn++; return fearCard;
        }

        // Priority 4: cleanse ONLY when danger territory at 7+ (approaching Level 2).
        // Sweet spot = 3+ territories at Level 1 (3-7 pts): don't cleanse those.
        // Only cleanse presence territories approaching L2 — never fully wipe L1 territories.
        var dangerTerritories = state.Territories
            .Where(t => t.CorruptionPoints >= 7 && t.HasPresence)
            .OrderByDescending(t => t.CorruptionPoints)
            .ToList();
        if (dangerTerritories.Any())
        {
            var cleanseCard = hand.FirstOrDefault(c =>
                !c.IsDormant && (c.TopEffect.Type == EffectType.ReduceCorruption
                              || c.TopEffect.Type == EffectType.Purify));
            if (cleanseCard != null)
            {
                LastDecisionReason = $"PRIORITY: cleanse — {dangerTerritories[0].Id} at {dangerTerritories[0].CorruptionPoints} corruption (approaching Defiled)";
                _topsPlayedThisTurn++; return cleanseCard;
            }
        }

        // Priority 5: any non-dormant card
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
        // Ember wants to burn through the deck aggressively (Phoenix Spark generates Fear on removal)
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
        return null; // auto-assign
    }

    public string? ChooseRestGrowthTarget(EncounterState state)
    {
        // Ember has no rest growth passive — return null
        return null;
    }

    public string? ChooseTarget(EffectData effect, EncounterState state)
    {
        return effect.Type switch
        {
            // Damage: most invaders (maximize Ember Fury + Scorched Earth value)
            EffectType.DamageInvaders => state.Territories
                .Where(t => t.Invaders.Any(i => i.IsAlive))
                .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
                .FirstOrDefault()?.Id,

            // Cleanse: most corrupted territory (keep Level 2 at bay)
            EffectType.ReduceCorruption or EffectType.Purify => state.Territories
                .OrderByDescending(t => t.CorruptionPoints)
                .FirstOrDefault()?.Id,

            // Presence: empty territory adjacent to existing presence, not Defiled
            EffectType.PlacePresence => ChoosePlacementTarget(state),

            _ => state.Territories.FirstOrDefault(t => t.HasPresence)?.Id
                ?? state.Territories.FirstOrDefault()?.Id
        };
    }

    private static string? ChoosePlacementTarget(EncounterState state)
    {
        int blockLevel = state.Warden?.PresenceBlockLevel() ?? 2;
        var presenceIds = state.Territories.Where(t => t.HasPresence).Select(t => t.Id).ToHashSet();
        var candidate = state.Territories
            .Where(t => !t.HasPresence && t.CorruptionLevel < blockLevel
                && state.Graph.GetNeighbors(t.Id).Any(n => presenceIds.Contains(n)))
            .OrderBy(t => t.CorruptionPoints)
            .FirstOrDefault();
        if (candidate != null) return candidate.Id;

        return state.Territories
            .Where(t => t.HasPresence && t.CorruptionLevel < blockLevel)
            .OrderBy(t => t.PresenceCount)
            .FirstOrDefault()?.Id;
    }

    private static int BottomPriority(Card card, EncounterState state)
    {
        return card.BottomEffect.Type switch
        {
            // Heavy damage is Ember's best play
            EffectType.DamageInvaders when state.Territories.Any(t => t.Invaders.Any(i => i.IsAlive)) => 100,
            // Fear generation (Phoenix Spark will fire when the card is consumed)
            EffectType.GenerateFear => 70,
            // Urgent corruption management only
            EffectType.ReduceCorruption when state.Territories.Any(t => t.CorruptionPoints >= 8) => 90,
            EffectType.Purify when state.Territories.Any(t => t.CorruptionPoints >= 8) => 95,
            EffectType.PlacePresence => 50,
            EffectType.RestoreWeave when (state.Weave?.CurrentWeave ?? 20) < 10 => 80,
            _ => 10
        };
    }
}
