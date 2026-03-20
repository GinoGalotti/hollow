namespace HollowWardens.Core.Cards;

using HollowWardens.Core.Models;

/// <summary>
/// Manages the player's hand. Provides play validation and dormancy checks.
/// </summary>
public class HandManager
{
    private readonly List<Card> _cards = new();

    public IReadOnlyList<Card> Cards => _cards;
    public int Count => _cards.Count;
    public int DormantCount => _cards.Count(c => c.IsDormant);

    public IReadOnlyList<Card> PlayableCards => _cards.Where(c => !c.IsDormant).ToList();

    public bool Contains(Card card) => _cards.Contains(card);

    /// <summary>True if the card is in hand and not dormant.</summary>
    public bool IsPlayable(Card card) => Contains(card) && !card.IsDormant;

    internal void Add(Card card) => _cards.Add(card);
    internal bool Remove(Card card) => _cards.Remove(card);
}
