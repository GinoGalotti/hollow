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

    // ── Dual-discard pairing system ───────────────────────────────────────────
    int TopDiscardCount { get; }
    int BottomDiscardCount { get; }
    IReadOnlyList<Card> TopDiscardCards { get; }
    IReadOnlyList<Card> BottomDiscardCards { get; }

    /// <summary>New pairing system: play a card as top — goes to top-discard (safe).</summary>
    void PlayAsTop(Card card);

    /// <summary>New pairing system: play a card as bottom — goes to bottom-discard (at-risk).</summary>
    void PlayAsBottom(Card card);

    /// <summary>Soak damage: discard from hand to top-discard (safe).</summary>
    void SoakDamage(Card card);

    /// <summary>Phase 1: recover tops to hand, dissolve 2 random bottoms. Call CompleteRestWithPairing after rerolls.</summary>
    RestResult BeginRestWithPairing();

    /// <summary>Reroll: save a dissolved card by swapping with a random survivor. Costs 2 weave (enforced by caller).</summary>
    void RerollDissolve(Card cardToSave);

    /// <summary>Phase 2: return surviving bottom-discard cards to hand.</summary>
    void CompleteRestWithPairing();

    // ── Legacy single-discard system (preserved for existing tests/sim) ───────
    /// <summary>True if the card is in hand and not dormant.</summary>
    bool IsPlayable(Card card);

    /// <summary>Permanently removes the card with the given ID from all piles.</summary>
    void PermanentlyRemove(string cardId);
    void RefillHand();
    /// <summary>Moves ALL cards from draw pile to hand (pairing mode — all cards start in hand, no draw pile concept).</summary>
    void DealAllToHand();
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
