using Godot;
using System.Collections.Generic;

// Manages the player's current hand of cards
public partial class Hand : Node
{
    private readonly List<CardData> _cards = new();

    public IReadOnlyList<CardData> Cards => _cards;
    public int Count => _cards.Count;

    public void AddCard(CardData card) => _cards.Add(card);

    public bool RemoveCard(CardData card) => _cards.Remove(card);

    public void Clear() => _cards.Clear();
}
