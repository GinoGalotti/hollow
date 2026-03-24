using Godot;
using HollowWardens.Core.Localization;

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
        var cinzel = GD.Load<Font>("res://godot/assets/fonts/Cinzel-Bold.ttf");
        var imFell = GD.Load<Font>("res://godot/assets/fonts/IMFellEnglish-Regular.ttf");
        // TODO: visual upgrade — card_empty.png mini frame for action card preview

        var header = new Label { Text = Loc.Get("LABEL_TIDE_PREVIEW"), Modulate = new Color(0.5f, 0.8f, 1f) };
        if (cinzel != null) header.AddThemeFontOverride("font", cinzel);
        AddChild(header);

        _tideLabel    = new Label();
        if (cinzel != null) _tideLabel.AddThemeFontOverride("font", cinzel);
        _currentLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        if (imFell != null) _currentLabel.AddThemeFontOverride("font", imFell);
        _nextLabel    = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = Colors.LightGray
        };
        if (imFell != null) _nextLabel.AddThemeFontOverride("font", imFell);

        AddChild(_tideLabel);
        AddChild(_currentLabel);
        AddChild(new HSeparator());
        AddChild(new Label { Text = Loc.Get("LABEL_NEXT"), Modulate = Colors.LightGray });
        AddChild(_nextLabel);

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.EncounterReady        += RefreshTide;
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
            _tideLabel.Text    = Loc.Get("PHASE_RESOLUTION_N", n, GameBridge.Instance?.State?.Config?.ResolutionTurns ?? 0);
            _currentLabel.Text = "";
            _nextLabel.Text    = "";
        };

        RefreshTide();
    }

    private void RefreshTide()
    {
        var bridge = GameBridge.Instance;
        if (bridge?.State == null) return;

        int current = bridge.State.CurrentTide;
        int total   = bridge.State.Config.TideCount;
        _tideLabel.Text = Loc.Get("PHASE_TIDE_N", current, total);

        var card = bridge.State.CurrentActionCard;
        if (card != null)
        {
            _currentLabel.Text     = $"▶ {card.Name}";
            _currentLabel.Modulate = card.Pool == HollowWardens.Core.Models.ActionPool.Painful
                ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 1f, 0.4f);
        }
        else
        {
            _currentLabel.Text = Loc.Get("LABEL_NO_CARD");
        }
    }
}
