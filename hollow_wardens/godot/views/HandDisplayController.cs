using Godot;
using HollowWardens.Core.Models;

/// <summary>
/// Horizontal strip of CardViewControllers representing the player's hand.
/// Subscribes to GameBridge.HandChanged and rebuilds card views on each update.
/// </summary>
public partial class HandDisplayController : HBoxContainer
{
    public override void _Ready()
    {
        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.HandChanged            += Rebuild;
        bridge.PhaseChanged           += _ => RefreshButtons();
        bridge.ResolutionTurnStarted  += _ => RefreshButtons();

        Rebuild();
    }

    private void Rebuild()
    {
        // Remove existing card views
        foreach (Node child in GetChildren())
            child.QueueFree();

        var hand = GameBridge.Instance?.State?.Deck?.Hand;
        if (hand == null) return;

        foreach (var card in hand)
        {
            var cv = new CardViewController();
            AddChild(cv);
            cv.Setup(card);
        }
    }

    private void RefreshButtons()
    {
        foreach (Node child in GetChildren())
        {
            if (child is CardViewController cv)
                cv.Refresh();
        }
    }
}
