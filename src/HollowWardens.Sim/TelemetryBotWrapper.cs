using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Telemetry;
using HollowWardens.Core.Turn;

namespace HollowWardens.Sim;

/// <summary>
/// Decorator around any IPlayerStrategy that records each decision to TelemetryCollector.
/// Does not change any decisions — purely observational.
/// </summary>
public class TelemetryBotWrapper : IPlayerStrategy
{
    private readonly IPlayerStrategy _inner;
    private readonly TelemetryCollector _telemetry;

    public TelemetryBotWrapper(IPlayerStrategy inner, TelemetryCollector telemetry)
    {
        _inner = inner;
        _telemetry = telemetry;
    }

    public Card? ChooseTopPlay(IReadOnlyList<Card> hand, EncounterState state)
    {
        var card = _inner.ChooseTopPlay(hand, state);
        string reason = GetReason(_inner);
        string optionsJson = SerializeCardIds(hand.Where(c => !c.IsDormant));

        if (card != null)
            _telemetry.RecordDecision("card_play", card.Id, reason, optionsJson, state,
                cardId: card.Id, cardHalf: "top");
        else
            _telemetry.RecordDecision("rest", "rest", reason, optionsJson, state);

        return card;
    }

    public Card? ChooseBottomPlay(IReadOnlyList<Card> hand, EncounterState state)
    {
        var card = _inner.ChooseBottomPlay(hand, state);
        string reason = GetReason(_inner);
        string optionsJson = SerializeCardIds(hand.Where(c => !c.IsDormant));

        if (card != null)
            _telemetry.RecordDecision("card_play", card.Id, reason, optionsJson, state,
                cardId: card.Id, cardHalf: "bottom");
        else
            _telemetry.RecordDecision("rest", "rest", reason, optionsJson, state);

        return card;
    }

    public Dictionary<Invader, int>? AssignCounterDamage(Territory territory, int damagePool, EncounterState state)
        => _inner.AssignCounterDamage(territory, damagePool, state);

    public string? ChooseRestGrowthTarget(EncounterState state)
        => _inner.ChooseRestGrowthTarget(state);

    public string? ChooseTarget(EffectData effect, EncounterState state)
    {
        var target = _inner.ChooseTarget(effect, state);
        if (target != null)
        {
            string options = SerializeStringList(state.Territories.Select(t => t.Id));
            _telemetry.RecordDecision("targeting", target, effect.Type.ToString(), options, state,
                targetTerritory: target);
        }
        return target;
    }

    // ── Pairing system delegation ────────────────────────────────────────────

    public bool UsesPairingSystem => _inner.UsesPairingSystem;

    public CardPair? ChoosePair(IReadOnlyList<Card> hand, EncounterState state)
        => _inner.ChoosePair(hand, state);

    public bool ShouldRest(IReadOnlyList<Card> hand, EncounterState state)
        => _inner.ShouldRest(hand, state);

    public bool ShouldReroll(Card dissolved, EncounterState state)
        => _inner.ShouldReroll(dissolved, state);

    private static string GetReason(IPlayerStrategy strategy) => strategy switch
    {
        BotStrategy bs         => bs.LastDecisionReason,
        EmberBotStrategy es    => es.LastDecisionReason,
        RootTallStrategy rs    => rs.LastDecisionReason,
        _ => ""
    };

    private static string SerializeCardIds(IEnumerable<Card> cards)
        => System.Text.Json.JsonSerializer.Serialize(cards.Select(c => c.Id).ToList());

    private static string SerializeStringList(IEnumerable<string> items)
        => System.Text.Json.JsonSerializer.Serialize(items.ToList());
}
