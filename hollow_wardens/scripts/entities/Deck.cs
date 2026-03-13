using Godot;
using System.Collections.Generic;

// Manages the draw pile — not the full card pool
public partial class Deck : Node
{
    private readonly List<CardData> _drawPile = new();

    public int Count => _drawPile.Count;

    public void Initialize(IEnumerable<CardData> cards)
    {
        _drawPile.Clear();
        _drawPile.AddRange(cards);
        Shuffle();
    }

    public CardData? Draw()
    {
        if (_drawPile.Count == 0) return null;
        var card = _drawPile[0];
        _drawPile.RemoveAt(0);
        return card;
    }

    public void Shuffle()
    {
        var rng = new System.Random();
        for (int i = _drawPile.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]);
        }
    }

    public void AddToBottom(CardData card) => _drawPile.Add(card);
}
