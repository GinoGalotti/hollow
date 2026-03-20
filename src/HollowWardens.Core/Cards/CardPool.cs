namespace HollowWardens.Core.Cards;

using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;

/// <summary>
/// Run-level card pool. Tracks which cards survive across encounters.
/// Dissolved cards return after non-Boss encounters. Permanently removed cards do not.
/// </summary>
public class CardPool
{
    private readonly List<Card> _runDeck;
    private readonly List<Card> _permanentlyRemoved = new();

    public CardPool(IEnumerable<Card> startingCards)
    {
        _runDeck = startingCards.ToList();
    }

    public IReadOnlyList<Card> RunDeck => _runDeck;
    public IReadOnlyList<Card> PermanentlyRemoved => _permanentlyRemoved;
    public int CardCount => _runDeck.Count;

    /// <summary>Creates a new DeckManager for an encounter from the current run deck.</summary>
    public DeckManager CreateEncounterDeck(IWardenAbility warden, Random? rng = null, int handLimit = 5)
        => new DeckManager(warden, _runDeck, rng, handLimit);

    /// <summary>
    /// Called after an encounter ends to sync permanent removals back to the run deck.
    /// On Boss: dissolved cards are also permanently removed.
    /// On Standard/Elite: dissolved cards stay in run deck (return next encounter).
    /// </summary>
    public void AfterEncounter(DeckManager dm, EncounterTier tier)
    {
        foreach (var card in dm.PermanentlyRemovedCards)
        {
            _runDeck.Remove(card);
            _permanentlyRemoved.Add(card);
        }

        if (tier == EncounterTier.Boss)
        {
            foreach (var card in dm.DissolvedCards)
            {
                _runDeck.Remove(card);
                _permanentlyRemoved.Add(card);
            }
        }
        // Non-Boss dissolved cards remain in _runDeck — available next encounter
    }
}
