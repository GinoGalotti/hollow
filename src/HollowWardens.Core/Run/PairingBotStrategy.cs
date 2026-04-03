namespace HollowWardens.Core.Run;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Turn;

/// <summary>
/// Bot strategy that uses the pairing system. Scores all N×(N-1) card orientations and picks
/// the highest-scoring pair each turn.
///
/// Scoring criteria per pair orientation (TopCard, BottomCard):
///   - FastTopBonus:       +10 if top is Fast (resolves before Tide — proactive value)
///   - TopEffectValue:     top effect value × 2
///   - BottomEffectValue:  bottom effect value × 3 (bottoms are stronger but at risk)
///   - ElementSynergy:     +2 per element shared across both cards (builds thresholds)
///   - BottomRiskPenalty:  -BottomEffect.Value if bottom value > HighValueThreshold
///                         (avoid risking key cards on rest)
/// </summary>
public class PairingBotStrategy : IPlayerStrategy
{
    /// <summary>Minimum bottom effect value above which a card is penalised for risking as bottom.</summary>
    public int HighValueThreshold { get; set; } = 5;

    /// <summary>Bonus applied to pairs where the top card is Fast (resolves before Tide).</summary>
    public int FastTopBonus { get; set; } = 10;

    /// <summary>Rest when hand has this many or fewer playable cards.</summary>
    public int RestHandThreshold { get; set; } = 2;

    /// <summary>Reroll if dissolved bottom value exceeds this.</summary>
    public int RerollValueThreshold { get; set; } = 8;

    /// <summary>Reroll only if weave exceeds this.</summary>
    public int RerollWeaveThreshold { get; set; } = 6;

    /// <summary>Flat score bonus applied to any PlacePresence effect (overrides raw value scoring).</summary>
    public int PlacePresenceBonus { get; set; } = 8;

    /// <summary>Set after each ChoosePair for debugging.</summary>
    public string LastDecisionReason { get; private set; } = "";

    // ── Pairing ───────────────────────────────────────────────────────────────

    public bool UsesPairingSystem => true;

    public CardPair? ChoosePair(IReadOnlyList<Card> hand, EncounterState state)
    {
        var playable = hand.Where(c => !c.IsDormant).ToList();
        if (playable.Count < 2) return null;

        CardPair? bestPair = null;
        int bestScore = int.MinValue;

        // Score all N×(N-1) orientations
        for (int ti = 0; ti < playable.Count; ti++)
        {
            for (int bi = 0; bi < playable.Count; bi++)
            {
                if (ti == bi) continue;
                var top = playable[ti];
                var bot = playable[bi];
                int score = ScorePair(top, bot, state);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPair = new CardPair(top, bot);
                    LastDecisionReason = $"Pair ({top.Id} top / {bot.Id} bottom) scored {score}";
                }
            }
        }

        return bestPair;
    }

    private int ScorePair(Card top, Card bottom, EncounterState state)
    {
        int score = 0;

        // Fast top bonus — resolve before invaders act
        if (top.TopTiming == CardTiming.Fast)
            score += FastTopBonus;

        // Effect values (type-aware: PlacePresence is high-value even when nominal value is 1)
        score += ScoreEffect(top.TopEffect, state) * 2;
        score += ScoreEffect(bottom.BottomEffect, state) * 3;

        // Element synergy — shared elements build the same threshold faster
        var topElements = top.Elements.ToHashSet();
        foreach (var e in bottom.Elements)
        {
            if (topElements.Contains(e))
                score += 2;
        }

        // Bottom risk penalty — penalise putting high-value cards at rest risk
        if (bottom.BottomEffect.Value > HighValueThreshold)
            score -= bottom.BottomEffect.Value;

        return score;
    }

    private int ScoreEffect(EffectData effect, EncounterState state)
    {
        return effect.Type switch
        {
            // Presence placement is high-value regardless of nominal count
            EffectType.PlacePresence => PlacePresenceBonus,

            // Weave restoration is urgent when below half
            EffectType.RestoreWeave => (state.Weave?.CurrentWeave ?? 20) < 10
                ? effect.Value * 3
                : effect.Value,

            // Corruption reduction is worthless if there's nothing to reduce
            EffectType.ReduceCorruption or EffectType.Purify =>
                state.Territories.Any(t => t.CorruptionPoints > 0) ? effect.Value : 0,

            _ => effect.Value
        };
    }

    // ── Rest ─────────────────────────────────────────────────────────────────

    public bool ShouldRest(IReadOnlyList<Card> hand, EncounterState state)
        => hand.Count(c => !c.IsDormant) <= RestHandThreshold;

    // ── Reroll ───────────────────────────────────────────────────────────────

    public bool ShouldReroll(Card dissolved, EncounterState state)
        => dissolved.BottomEffect.Value > RerollValueThreshold
        && (state.Weave?.CurrentWeave ?? 0) > RerollWeaveThreshold;

    // ── Offering (Root passive) ───────────────────────────────────────────────

    /// <summary>
    /// Use the Elemental Offering on turn 1 if a card would push Root to a threshold.
    /// Default: only offer if we'd gain 2+ of the primary element (Root) and are below T1.
    /// </summary>
    public bool ShouldUseOffering(Card card, IReadOnlyList<Card> hand, EncounterState state)
    {
        if (state.RootOfferingUsedThisCycle) return false;

        // Only use on turn 1 of the cycle (max value from compounding)
        // Heuristic: if offering the card would add ≥2 primary elements and we're below T1
        var primaryElement = state.Warden?.WardenId == "root" ? Element.Root : Element.Ash;
        int gain = card.Elements.Count(e => e == primaryElement);
        if (gain < 2) return false;

        // Check we're below T1 (below 4 elements — default T1 threshold)
        if (state.Elements == null) return false;
        int current = state.Elements.Get(primaryElement);
        int t1 = state.Balance?.GetThreshold(primaryElement, 1) ?? 4;
        return current < t1;
    }

    // ── Targeting ─────────────────────────────────────────────────────────────

    public string? ChooseRestGrowthTarget(EncounterState state)
        => state.Territories
            .Where(t => t.HasPresence && t.CorruptionLevel < 2)
            .OrderByDescending(t => t.PresenceCount)
            .FirstOrDefault()?.Id;

    public string? ChooseTarget(EffectData effect, EncounterState state)
        => effect.Type switch
        {
            EffectType.PlacePresence => ChoosePlacePresenceTarget(state),

            EffectType.ReduceCorruption or EffectType.Purify => state.Territories
                .OrderByDescending(t => t.CorruptionPoints)
                .FirstOrDefault()?.Id,

            EffectType.DamageInvaders => state.Territories
                .Where(t => t.Invaders.Any(i => i.IsAlive))
                .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
                .FirstOrDefault()?.Id,

            EffectType.PushInvaders => state.Territories
                .Where(t => t.Invaders.Any(i => i.IsAlive))
                .OrderBy(t => state.Graph?.Distance(t.Id, state.Graph?.HeartId ?? "") ?? int.MaxValue)
                .ThenByDescending(t => t.Invaders.Count(i => i.IsAlive))
                .FirstOrDefault()?.Id,

            _ => state.Territories.FirstOrDefault(t => t.HasPresence)?.Id
                ?? state.Territories.FirstOrDefault()?.Id
        };

    private static string? ChoosePlacePresenceTarget(EncounterState state)
    {
        // Expand to an adjacent unoccupied territory
        var presenceIds = state.Territories.Where(t => t.HasPresence).Select(t => t.Id).ToHashSet();
        return state.Territories
            .Where(t => !t.HasPresence && t.CorruptionLevel < 2
                && state.Graph != null
                && state.Graph.GetNeighbors(t.Id).Any(n => presenceIds.Contains(n)))
            .OrderBy(t => t.CorruptionPoints)
            .FirstOrDefault()?.Id
            // Fallback: deepen an existing presence territory
            ?? state.Territories
                .Where(t => t.HasPresence && t.CorruptionLevel < 2)
                .OrderByDescending(t => t.PresenceCount)
                .FirstOrDefault()?.Id;
    }

    // ── Legacy interface (unused by pairing system, stubs to satisfy interface) ──

    public Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state) => null;
    public Card? ChooseBottomPlay(IReadOnlyList<Card> hand, EncounterState state) => null;

    public Dictionary<Invader, int>? AssignCounterDamage(Territory territory, int damagePool, EncounterState state)
        => null;
}
