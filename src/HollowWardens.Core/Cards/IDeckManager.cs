namespace HollowWardens.Core.Cards;

using HollowWardens.Core.Models;

public interface IDeckManager
{
    int DrawPileCount { get; }
    int DiscardCount { get; }
    int DissolvedCount { get; }
    int DormantCount { get; }
    IReadOnlyList<Card> Hand { get; }
    void RefillHand();
    void PlayTop(Card card);
    void PlayBottom(Card card, EncounterTier tier);
    void Rest();
    bool NeedsRest { get; }  // deck empty or near-empty
    void AwakenAllDormant();
}
