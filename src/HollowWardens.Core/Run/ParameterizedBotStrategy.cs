namespace HollowWardens.Core.Run;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

/// <summary>
/// Phase-aware, scoring-based bot strategy driven by StrategyParams.
/// Replaces hard-coded decision logic with configurable parameters optimisable by HillClimber.
///
/// Scoring formula (top play):
///   card_score = priority_score×10 + element_value×3 + target_quality×2 + urgency_bonus×20
/// where:
///   priority_score = 6 - phase_priority_rank  (rank 1 = score 5, rank 6 = score 0)
///   element_value  = sum of proximity-to-threshold per element (0–3 per element)
///   target_quality = effectiveness of best available target for this effect (0–5)
///   urgency_bonus  = 1 if this card addresses an active urgency condition, else 0
/// </summary>
public class ParameterizedBotStrategy : IPlayerStrategy
{
    private int _topsPlayedThisTurn;

    public StrategyParams Params { get; }

    /// <summary>Set before each card choice. Format: "SCORE: {effect} = {score}".</summary>
    public string LastDecisionReason { get; private set; } = "";

    public ParameterizedBotStrategy(StrategyParams? parameters = null)
    {
        Params = parameters ?? StrategyDefaults.Root;
    }

    // ── Top Play ──────────────────────────────────────────────────────────────

    public Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state)
    {
        if (_topsPlayedThisTurn >= 2)
        {
            _topsPlayedThisTurn = 0;
            LastDecisionReason = "SKIP: play limit reached";
            return null;
        }

        var playable = hand.Where(c => !c.IsDormant).ToList();
        if (!playable.Any())
        {
            _topsPlayedThisTurn = 0;
            LastDecisionReason = "SKIP: all cards dormant";
            return null;
        }

        int bestScore = int.MinValue;
        Card? bestCard = null;
        string bestReason = "";

        foreach (var card in playable)
        {
            var (score, reason) = ScoreTopCard(card, state);
            if (score > bestScore)
            {
                bestScore = score;
                bestCard = card;
                bestReason = reason;
            }
        }

        if (bestCard != null)
        {
            LastDecisionReason = bestReason;
            _topsPlayedThisTurn++;
            return bestCard;
        }

        _topsPlayedThisTurn = 0;
        LastDecisionReason = "SKIP: no positive-scoring card";
        return null;
    }

    private (int score, string reason) ScoreTopCard(Card card, EncounterState state)
    {
        bool earlyPhase = state.CurrentTide <= Params.PhaseTransitionTide;
        var effectType = card.TopEffect.Type;

        int priorityScore = (6 - GetTopPriorityRank(effectType, earlyPhase)) * 10;
        int elementValue  = ComputeElementValue(card.Elements, state) * 3;
        int targetScore   = ComputeTargetQuality(effectType, card.TopEffect, state) * 2;
        int urgencyBonus  = IsUrgentForEffect(effectType, state) ? 20 : 0;
        int passiveBonus  = ComputePassiveUnlockBonus(card, earlyPhase, state);
        int threatBonus   = ComputeThreatBonus(card, state);

        int total = priorityScore + elementValue + targetScore + urgencyBonus + passiveBonus + threatBonus;
        string phase = earlyPhase ? "early" : "late";
        string reason = $"SCORE: {effectType} = {total} (priority={priorityScore} elem={elementValue} target={targetScore} urgency={urgencyBonus} passive={passiveBonus} threat={threatBonus}) [{phase}]";
        return (total, reason);
    }

    private int GetTopPriorityRank(EffectType type, bool earlyPhase)
    {
        return type switch
        {
            EffectType.PlacePresence =>
                earlyPhase ? Params.EarlyPresencePriority : Params.LatePresencePriority,
            EffectType.DamageInvaders =>
                earlyPhase ? Params.EarlyDamagePriority : Params.LateDamagePriority,
            EffectType.ReduceCorruption or EffectType.Purify =>
                earlyPhase ? Params.EarlyCleansePriority : Params.LateCleansePriority,
            EffectType.GenerateFear =>
                earlyPhase ? Params.EarlyFearPriority : Params.LateFearPriority,
            EffectType.RestoreWeave =>
                earlyPhase ? Params.EarlyWeavePriority : Params.LateWeavePriority,
            EffectType.SpawnNatives =>
                earlyPhase ? Params.EarlySpawnNativesPriority : Params.LateSpawnNativesPriority,
            EffectType.MoveNatives =>
                earlyPhase ? Params.EarlyMoveNativesPriority : Params.LateMoveNativesPriority,
            _ => 6
        };
    }

    // ── Bottom Play ───────────────────────────────────────────────────────────

    public Card? ChooseBottomPlay(IReadOnlyList<Card> hand, EncounterState state)
    {
        var playable = hand.Where(c => !c.IsDormant).ToList();
        if (!playable.Any())
        {
            LastDecisionReason = "SKIP: no playable bottom";
            return null;
        }

        int bestScore = int.MinValue;
        Card? bestCard = null;

        foreach (var card in playable)
        {
            int score = ScoreBottomCard(card, state);
            if (score > bestScore) { bestScore = score; bestCard = card; }
        }

        if (bestCard != null)
            LastDecisionReason = $"PRIORITY: best_bottom — {bestCard.BottomEffect.Type} (score {bestScore})";
        else
            LastDecisionReason = "SKIP: no playable bottom";
        return bestCard;
    }

    private int ScoreBottomCard(Card card, EncounterState state)
    {
        int weight       = GetBottomWeight(card.BottomEffect.Type, state);
        int elementBonus = ComputeElementValue(card.Elements, state) * 5;
        int targetScore  = ComputeTargetQuality(card.BottomEffect.Type, card.BottomEffect, state) * 5;
        return weight + elementBonus + targetScore;
    }

    private int GetBottomWeight(EffectType type, EncounterState state)
    {
        bool hasInvaders    = state.Territories.Any(t => t.Invaders.Any(i => i.IsAlive));
        bool highCorruption = state.Territories.Any(t => t.CorruptionPoints >= 8);
        bool lowWeave       = (state.Weave?.CurrentWeave ?? 20) < Params.WeaveUrgencyThreshold;

        bool hasPresenceWithAdjInvaders = hasInvaders && state.Territories.Any(t =>
            t.HasPresence && (t.Invaders.Any(i => i.IsAlive) ||
                state.Graph.GetNeighbors(t.Id).Any(n => state.GetTerritory(n)?.Invaders.Any(i => i.IsAlive) ?? false)));

        return type switch
        {
            EffectType.DamageInvaders when hasInvaders                          => Params.BottomDamageWeight,
            EffectType.ReduceCorruption or EffectType.Purify when highCorruption => Params.BottomCleanseWeight,
            EffectType.GenerateFear                                              => Params.BottomFearWeight,
            EffectType.PlacePresence                                             => Params.BottomPresenceWeight,
            EffectType.RestoreWeave when lowWeave                               => Params.BottomWeaveWeight,
            EffectType.RestoreWeave                                             => Params.BottomWeaveWeight / 2,
            EffectType.DamageInvaders                                           => Params.BottomDamageWeight / 3,
            EffectType.ReduceCorruption or EffectType.Purify                    => Params.BottomCleanseWeight / 4,
            EffectType.SpawnNatives when hasPresenceWithAdjInvaders             => Params.BottomSpawnNativesWeight,
            EffectType.SpawnNatives                                             => Params.BottomSpawnNativesWeight / 3,
            EffectType.PushInvaders when hasInvaders                           => Params.BottomPushInvadersWeight,
            EffectType.PushInvaders                                            => Params.BottomPushInvadersWeight / 4,
            _                                                                    => 10
        };
    }

    // ── Counter-Attack ────────────────────────────────────────────────────────

    public Dictionary<Invader, int>? AssignCounterDamage(
        Territory territory, int damagePool, EncounterState state)
    {
        if (Params.Targeting.TargetWeakestFirst)
            return null;  // auto-assign (lowest HP first) — same result, less allocation

        // Focus fire: assign damage to highest-MaxHp invaders first
        var assignments = new Dictionary<Invader, int>();
        int remaining = damagePool;
        foreach (var invader in territory.Invaders
            .Where(i => i.IsAlive)
            .OrderByDescending(i => i.MaxHp))
        {
            if (remaining <= 0) break;
            int dmg = Math.Min(remaining, invader.Hp);
            assignments[invader] = dmg;
            remaining -= dmg;
        }
        return assignments.Count > 0 ? assignments : null;
    }

    // ── Rest Growth ───────────────────────────────────────────────────────────

    public string? ChooseRestGrowthTarget(EncounterState state)
    {
        return state.Territories
            .Where(t => t.HasPresence && t.PresenceCount < Params.StackTarget && t.CorruptionLevel < 2)
            .OrderByDescending(t => t.PresenceCount)
            .FirstOrDefault()?.Id
            ?? state.Territories
                .Where(t => t.HasPresence && t.CorruptionLevel < 2)
                .OrderByDescending(t => t.PresenceCount)
                .FirstOrDefault()?.Id;
    }

    // ── Target Selection ──────────────────────────────────────────────────────

    public string? ChooseTarget(EffectData effect, EncounterState state)
    {
        return effect.Type switch
        {
            EffectType.DamageInvaders                    => ChooseDamageTarget(state, effect.Value),
            EffectType.ReduceCorruption or EffectType.Purify => ChooseCleanseTarget(state),
            EffectType.PlacePresence                     => ChoosePlacementTarget(state),
            EffectType.MoveNatives                       => ChooseMoveNativesSource(state),
            EffectType.SpawnNatives                      => ChooseSpawnNativesTarget(state),
            EffectType.PushInvaders                      => ChoosePushTarget(state),
            _ => state.Territories.FirstOrDefault(t => t.HasPresence)?.Id
                 ?? state.Territories.FirstOrDefault()?.Id
        };
    }

    // ── Threshold Resolution ──────────────────────────────────────────────────

    public void ResolvePendingThresholds(ThresholdResolver resolver, EncounterState state)
    {
        // Resolve damage-dealing thresholds first (kill before placements fire)
        var ordered = resolver.Pending
            .OrderByDescending(p => IsDamageThreshold(p.element) ? 1 : 0)
            .ToList();

        foreach (var (element, tier) in ordered)
        {
            string? targetId = ThresholdResolver.NeedsTarget(element, tier)
                ? ChooseThresholdTarget(element, tier, state)
                : null;
            resolver.Resolve(element, tier, state, targetId);
        }
    }

    private static bool IsDamageThreshold(Element element)
        => element == Element.Ash || element == Element.Void;

    private string? ChooseThresholdTarget(Element element, int tier, EncounterState state)
    {
        return (element, tier) switch
        {
            (Element.Ash,  1) => ChooseAshT1Target(state),
            (Element.Ash,  2) => ChooseAshT2Target(state),
            (Element.Ash,  3) => ChooseAshT3Target(state),
            (Element.Root, 1) => ChooseRootT1Target(state),
            (Element.Root, 2) => ChoosePlacementTarget(state),
            (Element.Gale, 1) or (Element.Gale, 2) => ChoosePushTarget(state),
            _ => null
        };
    }

    // ── Scoring Helpers ───────────────────────────────────────────────────────

    private int ComputeElementValue(Element[] elements, EncounterState state)
    {
        if (state.Elements == null || elements.Length == 0) return 0;
        int total = 0;
        foreach (var element in elements)
        {
            int current = state.Elements.Get(element);
            int dist    = DistanceToNextThreshold(element, current, state);
            // 3 if 1 away, 2 if 2 away, 1 if 3 away, 0 if 4+
            total += Math.Max(0, 4 - dist);
        }
        return total;
    }

    private static int DistanceToNextThreshold(Element element, int current, EncounterState state)
    {
        int t1 = state.Balance.GetThreshold(element, 1);
        int t2 = state.Balance.GetThreshold(element, 2);
        int t3 = state.Balance.GetThreshold(element, 3);
        if (current < t1) return t1 - current;
        if (current < t2) return t2 - current;
        if (current < t3) return t3 - current;
        return int.MaxValue / 2;
    }

    private int ComputeTargetQuality(EffectType type, EffectData effect, EncounterState state)
    {
        return type switch
        {
            EffectType.DamageInvaders                    => DamageTargetQuality(state, effect.Value),
            EffectType.ReduceCorruption or EffectType.Purify => CleanseTargetQuality(state),
            EffectType.PlacePresence                     => PresenceTargetQuality(state),
            EffectType.GenerateFear                      => 2,
            EffectType.RestoreWeave                      => WeaveTargetQuality(state),
            EffectType.SpawnNatives                      => SpawnNativesTargetQuality(state),
            EffectType.MoveNatives                       => MoveNativesTargetQuality(state),
            EffectType.PushInvaders                      => PushInvadersTargetQuality(state),
            _ => 1
        };
    }

    private int DamageTargetQuality(EncounterState state, int damageValue)
    {
        var targets = state.Territories.Where(t => t.Invaders.Any(i => i.IsAlive)).ToList();
        if (!targets.Any()) return 0;

        int maxCount = targets.Max(t => t.Invaders.Count(i => i.IsAlive));
        int score = Math.Min(3, maxCount);

        if (state.CurrentTide >= Params.HeartThreatTide &&
            targets.Any(t => state.Graph.Distance(t.Id, state.Graph.HeartId) <= 1))
            score = Math.Min(5, score + 2);

        return score;
    }

    private static int CleanseTargetQuality(EncounterState state)
    {
        int maxCorruption = state.Territories.Max(t => t.CorruptionPoints);
        return maxCorruption switch
        {
            0      => 0,
            <= 2   => 1,
            <= 5   => 2,
            <= 7   => 3,
            <= 10  => 4,
            _      => 5
        };
    }

    private int PresenceTargetQuality(EncounterState state)
    {
        int presenceCount = state.Territories.Count(t => t.HasPresence);
        if (presenceCount < Params.SpreadTarget) return 4;  // spreading is high value
        if (state.Territories.Any(t => t.HasPresence && t.PresenceCount < Params.StackTarget))
            return 3;  // stacking still needed
        return 1;
    }

    private int WeaveTargetQuality(EncounterState state)
    {
        int weave = state.Weave?.CurrentWeave ?? 20;
        return weave switch
        {
            <= 5  => 5,
            <= 8  => 4,
            <= 11 => 3,
            <= 14 => 2,
            _     => 1
        };
    }

    private int SpawnNativesTargetQuality(EncounterState state)
    {
        // Score: how many invaders are reachable (same territory + adjacent) from the best presence territory
        int best = state.Territories
            .Where(t => t.HasPresence)
            .Select(t =>
                t.Invaders.Count(i => i.IsAlive) +
                state.Graph.GetNeighbors(t.Id)
                    .Sum(n => state.GetTerritory(n)?.Invaders.Count(i => i.IsAlive) ?? 0))
            .DefaultIfEmpty(0)
            .Max();
        return best switch { 0 => 0, 1 => 1, 2 => 2, 3 => 3, <= 5 => 4, _ => 5 };
    }

    private int MoveNativesTargetQuality(EncounterState state)
    {
        // Score: native_count_in_source × invader_count_in_adjacent_destination
        int best = state.TerritoriesWithNatives()
            .Select(t =>
            {
                int natives = t.Natives.Count(n => n.IsAlive);
                int adjInvaders = state.Graph.GetNeighbors(t.Id)
                    .Sum(n => state.GetTerritory(n)?.Invaders.Count(i => i.IsAlive) ?? 0);
                return natives * adjInvaders;
            })
            .DefaultIfEmpty(0)
            .Max();
        return best switch { 0 => 0, <= 2 => 1, <= 4 => 2, <= 6 => 3, <= 9 => 4, _ => 5 };
    }

    private int PushInvadersTargetQuality(EncounterState state)
    {
        // Score: proximity_to_heart × invader_count (pushing M-row/I1 invaders is high value)
        int best = state.TerritoriesWithInvaders()
            .Select(t =>
            {
                int invaders = t.Invaders.Count(i => i.IsAlive);
                int heartDist = state.Graph.Distance(t.Id, state.Graph.HeartId);
                int proximity = Math.Max(0, 3 - heartDist);  // 3 if adjacent, 2 if two hops, 1 if three, 0 otherwise
                return proximity * invaders;
            })
            .DefaultIfEmpty(0)
            .Max();
        return best switch { 0 => 0, <= 2 => 1, <= 4 => 2, <= 6 => 3, <= 8 => 4, _ => 5 };
    }

    private bool IsUrgentForEffect(EffectType type, EncounterState state)
    {
        return type switch
        {
            EffectType.RestoreWeave =>
                (state.Weave?.CurrentWeave ?? 20) < Params.WeaveUrgencyThreshold,
            EffectType.DamageInvaders =>
                HasDamageUrgency(state),
            EffectType.ReduceCorruption or EffectType.Purify =>
                HasCleanseUrgency(state),
            _ => false
        };
    }

    private bool HasDamageUrgency(EncounterState state)
    {
        if (state.CurrentTide < Params.HeartThreatTide) return false;
        return state.Territories.Any(t =>
            t.Invaders.Count(i => i.IsAlive) >= Params.DamageUrgencyInvaderCount &&
            state.Graph.Distance(t.Id, state.Graph.HeartId) <= 1);
    }

    private bool HasCleanseUrgency(EncounterState state)
        => state.Territories.Any(t => t.CorruptionPoints >= Params.CleanseUrgencyCorruption);

    private int ComputePassiveUnlockBonus(Card card, bool earlyPhase, EncounterState state)
    {
        if (state.PassiveGating == null || state.PassiveGating.IsActive("network_slow"))
            return 0;
        if (!card.Elements.Contains(Element.Shadow))
            return 0;
        int rank = earlyPhase ? Params.EarlyPassiveUnlockPriority : Params.LatePassiveUnlockPriority;
        return (6 - rank) * 10;
    }

    private int ComputeThreatBonus(Card card, EncounterState state)
    {
        // If current tide's action card is a fast march, boost A-row damage priority
        if (card.TopEffect.Type != EffectType.DamageInvaders) return 0;
        if ((state.CurrentActionCard?.AdvanceModifier ?? 0) < 2) return 0;

        int aRowInvaders = state.Territories
            .Where(t => t.Id.StartsWith("A") && t.Invaders.Any(i => i.IsAlive))
            .Sum(t => t.Invaders.Count(i => i.IsAlive));

        return Params.Targeting.ThreatRowWeight * aRowInvaders;
    }

    // ── Territory Selection ───────────────────────────────────────────────────

    private string? ChooseDamageTarget(EncounterState state, int damageValue)
    {
        var targets = state.Territories.Where(t => t.Invaders.Any(i => i.IsAlive)).ToList();
        if (!targets.Any()) return null;

        return targets
            .OrderByDescending(t => DamageTargetScore(t, state, damageValue))
            .First().Id;
    }

    private int DamageTargetScore(Territory territory, EncounterState state, int damageValue)
    {
        int score = 0;
        int invaderCount = territory.Invaders.Count(i => i.IsAlive);

        if (Params.Targeting.PreferKillsOverDamage && damageValue > 0)
        {
            int kills = territory.Invaders.Count(i =>
                i.IsAlive && i.Hp <= damageValue && damageValue >= i.ShieldValue);
            score += kills * 30;
        }

        score += invaderCount * 10;

        int distFromHeart = state.Graph.Distance(territory.Id, state.Graph.HeartId);

        if (Params.Targeting.PreferArrivalRow && territory.Id.StartsWith("A"))
            score += Params.Targeting.ThreatRowWeight * 10;
        else if (!Params.Targeting.PreferArrivalRow && distFromHeart <= 1)
            score += Params.Targeting.ThreatRowWeight * 15;

        return score;
    }

    private string? ChooseCleanseTarget(EncounterState state)
    {
        IEnumerable<Territory> candidates = state.Territories.Where(t => t.CorruptionPoints > 0);

        if (Params.Targeting.CleansePreferPresence)
            candidates = candidates.Where(t => t.HasPresence);

        var list = candidates.ToList();
        if (!list.Any())
            list = state.Territories.Where(t => t.CorruptionPoints > 0).ToList();
        if (!list.Any()) return null;

        if (Params.Targeting.CleansePreferNearThreshold)
            return list.OrderByDescending(CleanseNearThresholdScore).FirstOrDefault()?.Id;

        if (Params.Targeting.CleansePreferHighest)
            return list.OrderByDescending(t => t.CorruptionPoints).FirstOrDefault()?.Id;

        return list.OrderByDescending(t => t.CorruptionPoints).FirstOrDefault()?.Id;
    }

    private static int CleanseNearThresholdScore(Territory t)
    {
        const int L1 = 3, L2 = 8;
        int pts = t.CorruptionPoints;
        if (pts >= L2) return 100 - (pts - L2);   // already L2, high priority
        if (pts >= L1) return 50 + (pts - L1);     // approaching L2
        return pts;                                  // approaching L1
    }

    private string? ChoosePlacementTarget(EncounterState state)
    {
        int blockLevel  = state.Warden?.PresenceBlockLevel() ?? 2;
        var presenceIds = state.Territories.Where(t => t.HasPresence).Select(t => t.Id).ToHashSet();
        int presenceCount = presenceIds.Count;

        // Prioritise stacking if PreferTallOverWide and stacking is needed
        if (Params.Targeting.PresencePreferStack && Params.PreferTallOverWide)
        {
            // If PresencePreferThreshold: prefer territory where +1 presence reaches StackTarget
            if (Params.Targeting.PresencePreferThreshold)
            {
                var atThreshold = state.Territories
                    .FirstOrDefault(t => t.HasPresence
                        && t.PresenceCount == Params.StackTarget - 1
                        && t.CorruptionLevel < blockLevel);
                if (atThreshold != null) return atThreshold.Id;
            }

            var stackTarget = state.Territories
                .Where(t => t.HasPresence && t.PresenceCount < Params.StackTarget && t.CorruptionLevel < blockLevel)
                .OrderByDescending(t => t.PresenceCount)
                .FirstOrDefault();
            if (stackTarget != null && presenceCount >= Params.SpreadTarget)
                return stackTarget.Id;
        }

        // Spread if below SpreadTarget
        if (presenceCount < Params.SpreadTarget)
        {
            var spreadTarget = state.Territories
                .Where(t => !t.HasPresence && t.CorruptionLevel < blockLevel
                    && state.Graph.GetNeighbors(t.Id).Any(n => presenceIds.Contains(n)))
                .OrderBy(t => t.CorruptionPoints)
                .FirstOrDefault();
            if (spreadTarget != null) return spreadTarget.Id;
        }

        // Fallback: stack in highest-presence territory
        return state.Territories
            .Where(t => t.HasPresence && t.CorruptionLevel < blockLevel)
            .OrderByDescending(t => t.PresenceCount)
            .FirstOrDefault()?.Id;
    }

    private string? ChooseMoveNativesSource(EncounterState state)
    {
        var withAdjacentInvaders = state.Territories
            .Where(t => t.Natives.Any(n => n.IsAlive) &&
                state.Graph.GetNeighbors(t.Id).Any(n =>
                    state.GetTerritory(n)?.Invaders.Any(i => i.IsAlive) ?? false))
            .OrderByDescending(t => t.Natives.Count(n => n.IsAlive))
            .FirstOrDefault();
        if (withAdjacentInvaders != null) return withAdjacentInvaders.Id;

        return state.Territories
            .Where(t => t.Natives.Any(n => n.IsAlive))
            .OrderByDescending(t => t.Natives.Count(n => n.IsAlive))
            .FirstOrDefault()?.Id;
    }

    private string? ChooseSpawnNativesTarget(EncounterState state)
    {
        return state.Territories
            .Where(t => t.HasPresence)
            .OrderByDescending(t =>
                (state.Graph?.GetNeighbors(t.Id) ?? Enumerable.Empty<string>())
                    .Sum(n => state.GetTerritory(n)?.Invaders.Count(i => i.IsAlive) ?? 0))
            .ThenByDescending(t => t.PresenceCount)
            .FirstOrDefault()?.Id;
    }

    private string? ChoosePushTarget(EncounterState state)
    {
        return state.Territories
            .Where(t => t.Invaders.Any(i => i.IsAlive))
            .OrderBy(t => state.Graph?.Distance(t.Id, state.Graph.HeartId) ?? int.MaxValue)
            .ThenByDescending(t => t.Invaders.Count(i => i.IsAlive))
            .FirstOrDefault()?.Id;
    }

    // ── Threshold-specific targets ────────────────────────────────────────────

    private string? ChooseAshT1Target(EncounterState state)
    {
        if (Params.Targeting.AshT1PreferMostInvaders)
            return state.TerritoriesWithInvaders()
                .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
                .FirstOrDefault()?.Id;

        // Prefer low-HP invaders for kills
        return state.TerritoriesWithInvaders()
            .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive && i.Hp <= 1))
            .ThenByDescending(t => t.Invaders.Count(i => i.IsAlive))
            .FirstOrDefault()?.Id;
    }

    private string? ChooseAshT2Target(EncounterState state)
    {
        var targets = state.TerritoriesWithInvaders().ToList();
        if (!targets.Any()) return null;

        if (!Params.Targeting.AshT2PreferHighCorruption)
            // Avoid territories already at high corruption (Ash T2 adds more)
            return targets
                .OrderBy(t => t.CorruptionPoints)
                .ThenByDescending(t => t.Invaders.Count(i => i.IsAlive))
                .First().Id;

        return targets
            .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
            .First().Id;
    }

    private string? ChooseAshT3Target(EncounterState state)
    {
        if (Params.Targeting.AshT3PreferHighPresence)
            return state.Territories
                .Where(t => t.HasPresence && t.Invaders.Any(i => i.IsAlive))
                .OrderByDescending(t => t.PresenceCount)
                .ThenByDescending(t => t.Invaders.Count(i => i.IsAlive))
                .FirstOrDefault()?.Id
                ?? state.Territories
                    .Where(t => t.HasPresence)
                    .OrderByDescending(t => t.PresenceCount)
                    .FirstOrDefault()?.Id;

        return state.TerritoriesWithInvaders()
            .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
            .FirstOrDefault()?.Id;
    }

    private string? ChooseRootT1Target(EncounterState state)
    {
        if (Params.Targeting.RootT1PreferNearThreshold)
            return state.Territories
                .Where(t => t.HasPresence && t.CorruptionPoints > 0)
                .OrderByDescending(CleanseNearThresholdScore)
                .FirstOrDefault()?.Id;

        return state.Territories
            .Where(t => t.HasPresence && t.CorruptionPoints > 0)
            .OrderByDescending(t => t.CorruptionPoints)
            .FirstOrDefault()?.Id;
    }

    // ── Provocation Territory Selection ───────────────────────────────────────

    /// <summary>
    /// Ranks presence territories for Provocation activation.
    /// Called by RootAbility.SelectProvocationTerritories when ProvocationTerritoryLimit > 0.
    /// </summary>
    public IEnumerable<string>? RankProvocationTerritories(IReadOnlyList<Territory> candidates, EncounterState state)
    {
        return candidates
            .OrderByDescending(t => ProvocationTerritoryScore(t, state))
            .Select(t => t.Id);
    }

    private int ProvocationTerritoryScore(Territory t, EncounterState state)
    {
        int score = 0;
        if (Params.Targeting.ProvocationPreferMostInvaders)
            score += t.Invaders.Count(i => i.IsAlive) * 10;
        if (Params.Targeting.ProvocationPreferHeartProximity)
            score += (5 - Math.Min(5, state.Graph.Distance(t.Id, state.Graph.HeartId))) * 5;
        if (Params.Targeting.ProvocationPreferMostNatives)
            score += t.Natives.Count(n => n.IsAlive) * 8;
        return score;
    }
}
