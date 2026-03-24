namespace HollowWardens.Core.Cards;

using HollowWardens.Core;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;

/// <summary>
/// Manages the card deck for a single encounter: draw pile, hand, discard, dissolved, and dormant state.
/// </summary>
public class DeckManager : IDeckManager
{
    private readonly IWardenAbility _warden;
    private readonly List<Card> _drawPile;
    private readonly List<Card> _discardPile = new();
    private readonly List<Card> _dissolved = new();
    private readonly List<Card> _permanentlyRemoved = new();
    private readonly HandManager _handManager;
    private readonly GameRandom _rng;
    private readonly int _handLimit;

    /// <param name="shuffle">Set false for deterministic tests.</param>
    public DeckManager(
        IWardenAbility warden,
        IEnumerable<Card> deck,
        GameRandom? rng = null,
        int handLimit = 5,
        bool shuffle = true)
    {
        _warden = warden;
        _drawPile = deck.ToList();
        _handManager = new HandManager();
        _rng = rng ?? GameRandom.NewRandom();
        _handLimit = handLimit;
        if (shuffle) Shuffle(_drawPile);
    }

    public int DrawPileCount => _drawPile.Count;
    public int DiscardCount => _discardPile.Count;
    public int DissolvedCount => _dissolved.Count;

    public int DormantCount =>
        _drawPile.Count(c => c.IsDormant) +
        _discardPile.Count(c => c.IsDormant) +
        _handManager.DormantCount;

    public IReadOnlyList<Card> Hand => _handManager.Cards;

    /// <summary>True when the draw pile is empty but discards can be recycled via Rest.</summary>
    public bool NeedsRest => _drawPile.Count == 0 && _discardPile.Count > 0;

    public IReadOnlyList<Card> DissolvedCards => _dissolved;
    public IReadOnlyList<Card> PermanentlyRemovedCards => _permanentlyRemoved;

    /// <summary>Permanently removes the card with the given ID from all piles.</summary>
    public void PermanentlyRemove(string cardId)
    {
        var card = _drawPile.FirstOrDefault(c => c.Id == cardId)
                ?? _discardPile.FirstOrDefault(c => c.Id == cardId)
                ?? _dissolved.FirstOrDefault(c => c.Id == cardId)
                ?? _handManager.Cards.FirstOrDefault(c => c.Id == cardId);
        if (card == null) return;

        _drawPile.Remove(card);
        _discardPile.Remove(card);
        _dissolved.Remove(card);
        _handManager.Remove(card);
        _permanentlyRemoved.Add(card);
    }

    /// <summary>True if the card is in hand and not dormant.</summary>
    public bool IsPlayable(Card card) => _handManager.IsPlayable(card);

    public void RefillHand()
    {
        int drawn = 0;
        while (_handManager.Count < _handLimit && _drawPile.Count > 0)
        {
            var card = _drawPile[^1];
            _drawPile.RemoveAt(_drawPile.Count - 1);
            _handManager.Add(card);
            drawn++;
        }
        if (drawn > 0)
            GameEvents.DeckRefilled?.Invoke(drawn);
    }

    public void PlayTop(Card card)
    {
        if (!_handManager.Contains(card))
            throw new InvalidOperationException($"Card '{card.Id}' is not in hand.");
        if (card.IsDormant)
            throw new InvalidOperationException($"Card '{card.Id}' is dormant and cannot be played as top.");

        _handManager.Remove(card);
        _discardPile.Add(card);
        GameEvents.CardPlayed?.Invoke(card, TurnPhase.Vigil);
    }

    public void PlayBottom(Card card, EncounterTier tier)
    {
        if (!_handManager.Remove(card))
            throw new InvalidOperationException($"Card '{card.Id}' is not in hand.");

        GameEvents.CardPlayed?.Invoke(card, TurnPhase.Dusk);

        var result = _warden.OnBottomPlayed(card, tier);
        ApplyBottomResult(card, result);
    }

    public void Rest()
    {
        GameEvents.RestStarted?.Invoke();

        // Shuffle all discards back into draw pile
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile);
        GameEvents.DeckShuffled?.Invoke(_drawPile.Count);

        // Rest-dissolve: remove 1 random card from draw pile
        if (_drawPile.Count > 0)
        {
            int idx = _rng.Next(_drawPile.Count);
            var victim = _drawPile[idx];
            _drawPile.RemoveAt(idx);

            var result = _warden.OnRestDissolve(victim);
            ApplyRestDissolveResult(victim, result);
        }
    }

    /// <summary>
    /// Moves up to <paramref name="maxCount"/> non-dormant cards from discard back to hand.
    /// Returns the number of cards moved.
    /// </summary>
    public int ReturnDiscardToHand(int maxCount)
    {
        int moved = 0;
        for (int i = _discardPile.Count - 1; i >= 0 && moved < maxCount; i--)
        {
            var card = _discardPile[i];
            if (card.IsDormant) continue;
            _discardPile.RemoveAt(i);
            _handManager.Add(card);
            moved++;
        }
        return moved;
    }

    /// <summary>
    /// Moves up to <paramref name="maxCount"/> non-dormant cards from discard into the draw pile
    /// at random positions. Returns the number of cards moved.
    /// </summary>
    public int ReturnDiscardToDraw(int maxCount)
    {
        int moved = 0;
        for (int i = _discardPile.Count - 1; i >= 0 && moved < maxCount; i--)
        {
            var card = _discardPile[i];
            if (card.IsDormant) continue;
            _discardPile.RemoveAt(i);
            // Insert at a random position in the draw pile so restored cards aren't drawn immediately
            int insertAt = _rng.Next(_drawPile.Count + 1);
            _drawPile.Insert(insertAt, card);
            moved++;
        }
        return moved;
    }

    /// <summary>Awakens a specific dormant card, making it playable again.</summary>
    public void AwakenDormant(Card card)
    {
        if (!card.IsDormant) return;
        card.IsDormant = false;
        GameEvents.CardAwakened?.Invoke(card);
    }

    /// <summary>Awakens all dormant cards across all piles (AwakeDormant Value=0 effect).</summary>
    public void AwakenAllDormant()
    {
        var allDormant = _drawPile.Where(c => c.IsDormant)
            .Concat(_discardPile.Where(c => c.IsDormant))
            .Concat(_handManager.Cards.Where(c => c.IsDormant))
            .ToList();
        foreach (var card in allDormant)
            AwakenDormant(card);
    }

    private void ApplyBottomResult(Card card, BottomResult result)
    {
        switch (result)
        {
            case BottomResult.Dissolved:
                _dissolved.Add(card);
                GameEvents.CardDissolved?.Invoke(card);
                break;

            case BottomResult.Dormant:
                card.IsDormant = true;
                _discardPile.Add(card);
                GameEvents.CardDormant?.Invoke(card);
                break;

            case BottomResult.PermanentlyRemoved:
                _permanentlyRemoved.Add(card);
                GameEvents.CardDissolved?.Invoke(card);
                break;
        }
    }

    private void ApplyRestDissolveResult(Card card, BottomResult result)
    {
        switch (result)
        {
            case BottomResult.Dissolved:
                _dissolved.Add(card);
                GameEvents.CardRestDissolved?.Invoke(card);
                break;

            case BottomResult.Dormant:
                card.IsDormant = true;
                InsertRandom(_drawPile, card);
                GameEvents.CardDormant?.Invoke(card);
                break;

            case BottomResult.PermanentlyRemoved:
                _permanentlyRemoved.Add(card);
                GameEvents.CardRestDissolved?.Invoke(card);
                break;
        }
    }

    private void Shuffle(List<Card> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void InsertRandom(List<Card> list, Card card)
    {
        int pos = list.Count == 0 ? 0 : _rng.Next(list.Count + 1);
        list.Insert(pos, card);
    }
}
