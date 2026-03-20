using Godot;

/// <summary>
/// Shows the current action card name and pools, plus a preview of the next action card.
/// </summary>
public partial class TidePreviewController : VBoxContainer
{
    private Label _tideLabel    = null!;
    private Label _currentLabel = null!;
    private Label _nextLabel    = null!;

    public override void _Ready()
    {
        AddChild(new Label { Text = "── Tide Preview ──", Modulate = new Color(0.5f, 0.8f, 1f) });

        _tideLabel    = new Label();
        _currentLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _nextLabel    = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = Colors.LightGray
        };

        AddChild(_tideLabel);
        AddChild(_currentLabel);
        AddChild(new HSeparator());
        AddChild(new Label { Text = "Next:", Modulate = Colors.LightGray });
        AddChild(_nextLabel);

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.TurnStarted           += RefreshTide;
        bridge.ActionCardRevealed    += (name, painful) =>
        {
            _currentLabel.Text     = $"▶ {name}";
            _currentLabel.Modulate = painful ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 1f, 0.4f);
        };
        bridge.NextActionPreviewed   += (name, painful) =>
        {
            _nextLabel.Text     = $"  {name}";
            _nextLabel.Modulate = painful ? new Color(1f, 0.6f, 0.6f) : new Color(0.6f, 1f, 0.6f);
        };
        bridge.PhaseChanged          += _ => RefreshTide();
        bridge.ResolutionTurnStarted += n =>
        {
            _tideLabel.Text    = $"Resolution {n}/{GameBridge.Instance?.State.Config.ResolutionTurns ?? 0}";
            _currentLabel.Text = "";
            _nextLabel.Text    = "";
        };

        RefreshTide();
    }

    private void RefreshTide()
    {
        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        int current = bridge.State.CurrentTide;
        int total   = bridge.State.Config.TideCount;
        _tideLabel.Text = $"Tide {current}/{total}";

        var card = bridge.State.CurrentActionCard;
        if (card != null)
        {
            _currentLabel.Text     = $"▶ {card.Name}";
            _currentLabel.Modulate = card.Pool == HollowWardens.Core.Models.ActionPool.Painful
                ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 1f, 0.4f);
        }
        else
        {
            _currentLabel.Text = "(no card yet)";
        }
    }
}
