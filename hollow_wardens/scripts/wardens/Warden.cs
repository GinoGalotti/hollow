using Godot;
using System.Collections.Generic;

public partial class Warden : Node
{
    [Export] public WardenData? WardenData { get; set; }

    public Hand? Hand { get; protected set; }
    public Deck? Deck { get; protected set; }
    public List<CardData> Discard { get; } = new();
    public List<CardData> DissolvedThisEncounter { get; } = new();
    public List<CardData> PermanentlyRemoved { get; } = new();

    // Override in subclasses for Warden-specific Dissolution behavior
    public virtual void OnDissolve(CardData card) { }

    // Called at start of Resolution phase — override for Warden-specific resolution style
    public virtual void OnResolutionStart(List<TerritoryState> territories) { }

    // Override to apply passive presence bonuses (e.g. Root's network Fear)
    public virtual Dictionary<string, int> GetPresenceBonus(TerritoryState territory)
        => new();

    public void RecoverDiscard()
    {
        // Return all discarded cards to hand
        foreach (var card in Discard)
            Hand?.AddCard(card);
        Discard.Clear();
    }
}
