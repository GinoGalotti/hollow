namespace HollowWardens.Core.Cards;

using HollowWardens.Core.Models;

public interface IDeckManager
{
    int DrawPileCount { get; }
    int DiscardCount { get; }
    int DissolvedCount { get; }
    int DormantCount { get; }
    IReadOnlyList<Card> Hand { get; }
    IReadOnlyList<Card> DissolvedCards { get; }

    /// <summary>Permanently removes the card with the given ID from all piles.</summary>
    void PermanentlyRemove(string cardId);
    void RefillHand();
    void PlayTop(Card card);
    void PlayBottom(Card card, EncounterTier tier);
    void Rest();
    bool NeedsRest { get; }  // deck empty or near-empty
    void AwakenAllDormant();
    /// <summary>
    /// Moves up to <paramref name="maxCount"/> non-dormant cards from discard back to hand.
    /// Returns the number of cards actually moved.
    /// </summary>
    int ReturnDiscardToHand(int maxCount);

    /// <summary>
    /// Moves up to <paramref name="maxCount"/> non-dormant cards from discard back into the draw pile
    /// at random positions. Returns the number of cards actually moved.
    /// </summary>
    int ReturnDiscardToDraw(int maxCount);
}
