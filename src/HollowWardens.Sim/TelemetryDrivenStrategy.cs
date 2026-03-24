using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Telemetry;

namespace HollowWardens.Sim;

/// <summary>
/// A bot strategy that makes decisions based on a PlayerProfile derived from telemetry.
/// Weights card choices and targeting by observed player behavior rather than hardcoded priorities.
/// Falls back to BotStrategy heuristics when profile data is absent or unhelpful.
/// </summary>
public class TelemetryDrivenStrategy : IPlayerStrategy
{
    private readonly PlayerProfile _profile;
    private readonly Random _rng;
    private readonly BotStrategy _fallback;
    private int _topsPlayedThisTurn;

    public TelemetryDrivenStrategy(PlayerProfile profile, Random rng)
    {
        _profile  = profile;
        _rng      = rng;
        _fallback = new BotStrategy();
    }

    public Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state)
    {
        var playable = hand.Where(c => !c.IsDormant).ToList();
        if (playable.Count == 0) { _topsPlayedThisTurn = 0; return null; }

        // Respect play limit (2 tops per Vigil)
        if (_topsPlayedThisTurn >= 2) { _topsPlayedThisTurn = 0; return null; }

        // Sometimes voluntary rest even with playable cards
        double voluntaryRestRate = _profile.RestTiming.VoluntaryRestPct;
        if (voluntaryRestRate > 0 && _rng.NextDouble() < voluntaryRestRate)
        {
            _topsPlayedThisTurn = 0;
            return null;
        }

        var card = PickCardByDistribution(playable);
        if (card != null) _topsPlayedThisTurn++;
        return card;
    }

    public Card? ChooseBottomPlay(IReadOnlyList<Card> hand, EncounterState state)
    {
        var playable = hand.Where(c => !c.IsDormant).ToList();
        if (playable.Count == 0) return null;

        // Use profile distribution, or fall back to BotStrategy
        return PickCardByDistribution(playable) ?? _fallback.ChooseBottomPlay(hand, state);
    }

    public Dictionary<Invader, int>? AssignCounterDamage(Territory territory, int damagePool, EncounterState state)
        => null; // auto-assign

    public string? ChooseRestGrowthTarget(EncounterState state)
        => _fallback.ChooseRestGrowthTarget(state);

    public string? ChooseTarget(EffectData effect, EncounterState state)
    {
        string effectKey = effect.Type.ToString();

        // Check profile targeting preference
        if (_profile.TargetingPreference.TryGetValue(effectKey, out string? preferredTerritory))
        {
            var territory = state.GetTerritory(preferredTerritory);
            if (territory != null) return preferredTerritory;

            // Pattern-based fallback from preference strings
            if (preferredTerritory == "most_invaded")
                return state.Territories
                    .Where(t => t.Invaders.Any(i => i.IsAlive))
                    .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
                    .FirstOrDefault()?.Id;

            if (preferredTerritory == "highest_corruption")
                return state.Territories
                    .OrderByDescending(t => t.CorruptionPoints)
                    .FirstOrDefault()?.Id;
        }

        // Fall through to BotStrategy
        return _fallback.ChooseTarget(effect, state);
    }

    /// <summary>Picks a card from the offer list weighted by the profile's card play distribution.</summary>
    public Card ChooseDraft(List<Card> offered)
    {
        if (offered.Count == 0) throw new ArgumentException("No cards offered");

        var weights = offered.Select(c =>
            _profile.DraftPreferences.TryGetValue(c.Id, out double w) ? w : 0.01).ToArray();

        return PickWeighted(offered, weights) ?? offered[0];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Card? PickCardByDistribution(List<Card> cards)
    {
        if (cards.Count == 0) return null;
        if (_profile.CardPlayDistribution.Count == 0)
            return cards[_rng.Next(cards.Count)]; // no profile data: random

        var weights = cards.Select(c =>
            _profile.CardPlayDistribution.TryGetValue(c.Id, out double w) ? w : 0.01).ToArray();

        return PickWeighted(cards, weights);
    }

    private T? PickWeighted<T>(List<T> items, double[] weights)
    {
        double total = weights.Sum();
        if (total <= 0) return items.Count > 0 ? items[0] : default;

        double roll = _rng.NextDouble() * total;
        double cumulative = 0;
        for (int i = 0; i < items.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative) return items[i];
        }
        return items[^1];
    }
}
