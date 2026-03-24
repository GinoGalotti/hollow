namespace HollowWardens.Core.Run;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;

/// <summary>
/// Bot strategy for Root. Prioritizes stacking presence in fewer territories
/// over wide spread, enabling Assimilation (presence ≥ stackTarget → spawn native).
///
/// Phase 1 (spread): expand to spreadTarget territories.
/// Phase 2 (stack): once spread is met, stack toward stackTarget presence per territory
///                  before expanding further. Rest growth targets the territory closest
///                  to the stack threshold.
/// </summary>
public class RootTallStrategy : IPlayerStrategy
{
    private int _topsPlayedThisTurn;

    /// <summary>Expand to this many territories before stacking.</summary>
    private readonly int _spreadTarget;

    /// <summary>Stack presence toward this count per territory (matches AssimilationSpawnThreshold).</summary>
    private readonly int _stackTarget;

    /// <summary>Set before each card choice. Format: "PRIORITY: {rule} — {context}".</summary>
    public string LastDecisionReason { get; private set; } = "";

    public RootTallStrategy(int spreadTarget = 3, int stackTarget = 3)
    {
        _spreadTarget = spreadTarget;
        _stackTarget  = stackTarget;
    }

    public Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state)
    {
        if (_topsPlayedThisTurn >= 2)
        {
            _topsPlayedThisTurn = 0;
            LastDecisionReason = "SKIP: play limit reached";
            return null;
        }

        int presenceTerritories = state.Territories.Count(t => t.HasPresence);

        // Priority 1: spread to spreadTarget territories first
        if (presenceTerritories < _spreadTarget)
        {
            var presenceCard = hand.FirstOrDefault(c =>
                !c.IsDormant && c.TopEffect.Type == EffectType.PlacePresence);
            if (presenceCard != null)
            {
                LastDecisionReason = $"PRIORITY: presence_expansion — {presenceTerritories}/{_spreadTarget} spread territories";
                _topsPlayedThisTurn++; return presenceCard;
            }
        }

        // Priority 1.5: soft unlock — play Shadow to unlock network_slow
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

        // Priority 1.7: stack toward stackTarget once spread is met
        if (presenceTerritories >= _spreadTarget)
        {
            bool needsStacking = state.Territories.Any(t =>
                t.HasPresence && t.PresenceCount < _stackTarget && t.CorruptionLevel < 2);
            if (needsStacking)
            {
                var presenceCard = hand.FirstOrDefault(c =>
                    !c.IsDormant && c.TopEffect.Type == EffectType.PlacePresence);
                if (presenceCard != null)
                {
                    var target = ChooseStackTarget(state) ?? "?";
                    LastDecisionReason = $"PRIORITY: stack_presence — building toward {_stackTarget} in {target}";
                    _topsPlayedThisTurn++; return presenceCard;
                }
            }
        }

        // Priority 2: damage if territory has invaders
        var damageCard = hand.FirstOrDefault(c =>
            !c.IsDormant && c.TopEffect.Type == EffectType.DamageInvaders);
        if (damageCard != null && state.Territories.Any(t => t.Invaders.Any(i => i.IsAlive)))
        {
            var target = state.Territories.OrderByDescending(t => t.Invaders.Count(i => i.IsAlive)).First();
            LastDecisionReason = $"PRIORITY: damage — {target.Id} has {target.Invaders.Count(i => i.IsAlive)} invaders";
            _topsPlayedThisTurn++; return damageCard;
        }

        // Priority 3: cleanse if corruption >= 5
        var cleanseCard = hand.FirstOrDefault(c =>
            !c.IsDormant && c.TopEffect.Type == EffectType.ReduceCorruption);
        if (cleanseCard != null && state.Territories.Any(t => t.CorruptionPoints >= 5))
        {
            var worst = state.Territories.OrderByDescending(t => t.CorruptionPoints).First();
            LastDecisionReason = $"PRIORITY: cleanse — {worst.Id} at {worst.CorruptionPoints} corruption";
            _topsPlayedThisTurn++; return cleanseCard;
        }

        // Priority 4: fear generation
        var fearCard = hand.FirstOrDefault(c =>
            !c.IsDormant && c.TopEffect.Type == EffectType.GenerateFear);
        if (fearCard != null)
        {
            LastDecisionReason = "PRIORITY: fear — generating fear";
            _topsPlayedThisTurn++; return fearCard;
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
        Territory territory, int damagePool, EncounterState state) => null;

    public string? ChooseRestGrowthTarget(EncounterState state)
    {
        // Target the territory closest to stackTarget (highest presence, below threshold, not Defiled)
        return state.Territories
            .Where(t => t.HasPresence && t.PresenceCount < _stackTarget && t.CorruptionLevel < 2)
            .OrderByDescending(t => t.PresenceCount)
            .FirstOrDefault()?.Id
            ?? state.Territories
                .Where(t => t.HasPresence && t.CorruptionLevel < 2)
                .OrderByDescending(t => t.PresenceCount)
                .FirstOrDefault()?.Id;
    }

    public string? ChooseTarget(EffectData effect, EncounterState state)
    {
        return effect.Type switch
        {
            EffectType.DamageInvaders => state.Territories
                .Where(t => t.Invaders.Any(i => i.IsAlive))
                .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
                .FirstOrDefault()?.Id,

            EffectType.ReduceCorruption or EffectType.Purify => state.Territories
                .OrderByDescending(t => t.CorruptionPoints)
                .FirstOrDefault()?.Id,

            EffectType.PlacePresence => ChooseStackTarget(state) ?? ChooseSpreadTarget(state),

            _ => state.Territories.FirstOrDefault(t => t.HasPresence)?.Id
                ?? state.Territories.FirstOrDefault()?.Id
        };
    }

    /// <summary>Pick the presence territory closest to stackTarget, not yet at it, not Defiled.</summary>
    private string? ChooseStackTarget(EncounterState state)
    {
        return state.Territories
            .Where(t => t.HasPresence && t.PresenceCount < _stackTarget && t.CorruptionLevel < 2)
            .OrderByDescending(t => t.PresenceCount)
            .FirstOrDefault()?.Id;
    }

    /// <summary>Fallback spread: adjacent empty territory, not Defiled.</summary>
    private string? ChooseSpreadTarget(EncounterState state)
    {
        var presenceIds = state.Territories.Where(t => t.HasPresence).Select(t => t.Id).ToHashSet();
        return state.Territories
            .Where(t => !t.HasPresence && t.CorruptionLevel < 2
                && state.Graph.GetNeighbors(t.Id).Any(n => presenceIds.Contains(n)))
            .OrderBy(t => t.CorruptionPoints)
            .FirstOrDefault()?.Id
            ?? state.Territories
                .Where(t => t.HasPresence && t.CorruptionLevel < 2)
                .OrderByDescending(t => t.PresenceCount)
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
