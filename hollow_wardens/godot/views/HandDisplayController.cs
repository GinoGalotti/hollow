using Godot;
using HollowWardens.Core.Models;

/// <summary>
/// Horizontal strip of CardViewControllers representing the player's hand.
/// Subscribes to GameBridge.HandChanged and rebuilds card views on each update.
/// Width is constrained at runtime so the Tide Preview panel always has 300 px.
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

        // Constrain right edge to leave 300 px for TidePreview
        Callable.From(UpdateWidth).CallDeferred();
        GetViewport().SizeChanged += UpdateWidth;

        Rebuild();
    }

    private void UpdateWidth()
    {
        float maxRight = GetViewportRect().Size.X - 300f;
        if (maxRight > OffsetLeft)
            OffsetRight = maxRight;
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
