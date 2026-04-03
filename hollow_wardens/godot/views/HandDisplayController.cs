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

        bridge.EncounterReady         += Rebuild;
        bridge.HandChanged            += Rebuild;
        bridge.PhaseChanged           += _ => RefreshButtons();
        bridge.ResolutionTurnStarted  += _ => RefreshButtons();
        bridge.TargetingModeChanged   += _ => RefreshButtons();

        // Prevent cards from visually overflowing
        ClipChildren = ClipChildrenMode.Only;

        Rebuild();
    }

    private void Rebuild()
    {
        // During pairing selection the hand doesn't change — PairingSelectionChanged
        // already triggers Refresh() on each card. Skip the full rebuild to avoid
        // destroying and recreating cards on every TOP/BOT button click.
        if (GameBridge.Instance?.IsPairingSelection == true)
        {
            RefreshButtons();
            return;
        }

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

        // Compress cards horizontally when hand is large
        int count   = hand.Count;
        int overlap = count switch
        {
            <= 5 => 0,
            <= 8 => -15,
            _    => -25,
        };
        AddThemeConstantOverride("separation", overlap);
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
