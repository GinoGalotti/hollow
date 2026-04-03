using Godot;
using HollowWardens.Core.Localization;
using HollowWardens.Core.Models;

/// <summary>
/// Shows the current and next tide action cards as mini card frames.
/// Current: 100×60 PanelContainer with colored border (red=Painful, green=Easy).
/// Next: same frame at 60% opacity.
/// </summary>
public partial class TidePreviewController : VBoxContainer
{
    private Label        _tideLabel      = null!;
    private PanelContainer _currentPanel = null!;
    private StyleBoxFlat   _currentStyle = null!;
    private Label          _currentName  = null!;
    private Label          _currentDesc  = null!;
    private PanelContainer _nextPanel    = null!;
    private StyleBoxFlat   _nextStyle    = null!;
    private Label          _nextName     = null!;
    private Label          _nextDesc     = null!;

    private static readonly Color PainfulBg  = new(0.25f, 0.05f, 0.05f);
    private static readonly Color PainfulBdr = new(0.90f, 0.20f, 0.20f);
    private static readonly Color EasyBg     = new(0.05f, 0.20f, 0.05f);
    private static readonly Color EasyBdr    = new(0.20f, 0.80f, 0.20f);
    private static readonly Color NeutralBg  = new(0.12f, 0.10f, 0.10f);
    private static readonly Color NeutralBdr = new(0.40f, 0.35f, 0.30f);

    public override void _Ready()
    {
        var cinzel = FontCache.CinzelBold;
        var imFell = FontCache.IMFell;

        var header = new Label { Text = Loc.Get("LABEL_TIDE_PREVIEW"), Modulate = new Color(0.5f, 0.8f, 1f) };
        if (cinzel != null) header.AddThemeFontOverride("font", cinzel);
        AddChild(header);

        _tideLabel = new Label();
        if (cinzel != null) _tideLabel.AddThemeFontOverride("font", cinzel);
        _tideLabel.AddThemeFontSizeOverride("font_size", 14);
        AddChild(_tideLabel);

        // Current action mini card
        (_currentPanel, _currentStyle, _currentName, _currentDesc) = BuildActionCard(cinzel, imFell);
        AddChild(_currentPanel);

        // "Next:" label
        var nextHdr = new Label { Text = Loc.Get("LABEL_NEXT"), Modulate = new Color(0.55f, 0.52f, 0.48f) };
        nextHdr.AddThemeFontSizeOverride("font_size", 10);
        AddChild(nextHdr);

        // Next action mini card (dimmed)
        (_nextPanel, _nextStyle, _nextName, _nextDesc) = BuildActionCard(cinzel, imFell);
        _nextPanel.Modulate = new Color(1f, 1f, 1f, 0.6f);
        AddChild(_nextPanel);

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.EncounterReady        += RefreshTide;
        bridge.TurnStarted           += RefreshTide;
        bridge.ActionCardRevealed    += (name, painful) => SetCurrent(name, painful);
        bridge.NextActionPreviewed   += (name, painful) => SetNext(name, painful);
        bridge.PhaseChanged          += _ => RefreshTide();
        bridge.ResolutionTurnStarted += n =>
        {
            _tideLabel.Text = Loc.Get("PHASE_RESOLUTION_N", n,
                GameBridge.Instance?.State?.Config?.ResolutionTurns ?? 0);
            SetCurrent("", false);
            SetNext("", false);
        };

        RefreshTide();
    }

    // ── Mini card frame factory ───────────────────────────────────────────────

    private static (PanelContainer panel, StyleBoxFlat style, Label name, Label desc)
        BuildActionCard(Font? cinzel, Font? imFell)
    {
        var style = new StyleBoxFlat
        {
            BgColor     = NeutralBg,
            BorderColor = NeutralBdr,
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(4);
        style.SetContentMarginAll(6);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(100, 60) };
        panel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        panel.AddChild(vbox);

        var nameLbl = new Label { AutowrapMode = TextServer.AutowrapMode.Off, Text = "—" };
        if (cinzel != null) nameLbl.AddThemeFontOverride("font", cinzel);
        nameLbl.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(nameLbl);

        var descLbl = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text         = "",
            Modulate     = new Color(0.60f, 0.58f, 0.55f),
        };
        if (imFell != null) descLbl.AddThemeFontOverride("font", imFell);
        descLbl.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(descLbl);

        return (panel, style, nameLbl, descLbl);
    }

    // ── Update helpers ────────────────────────────────────────────────────────

    private void SetCurrent(string name, bool painful)
    {
        ApplyCardStyle(_currentStyle, name, painful);
        _currentName.Text = string.IsNullOrEmpty(name) ? Loc.Get("LABEL_NO_CARD") : $"▶ {name}";
        _currentDesc.Text = string.IsNullOrEmpty(name) ? "" : painful ? "Painful Pool" : "Easy Pool";
    }

    private void SetNext(string name, bool painful)
    {
        ApplyCardStyle(_nextStyle, name, painful);
        _nextName.Text = string.IsNullOrEmpty(name) ? "—" : $"  {name}";
        _nextDesc.Text = string.IsNullOrEmpty(name) ? "" : painful ? "Painful Pool" : "Easy Pool";
    }

    private static void ApplyCardStyle(StyleBoxFlat style, string name, bool painful)
    {
        if (string.IsNullOrEmpty(name))
        {
            style.BgColor = NeutralBg; style.BorderColor = NeutralBdr;
        }
        else if (painful)
        {
            style.BgColor = PainfulBg; style.BorderColor = PainfulBdr;
        }
        else
        {
            style.BgColor = EasyBg; style.BorderColor = EasyBdr;
        }
    }

    private void RefreshTide()
    {
        var bridge = GameBridge.Instance;
        if (bridge?.State == null) return;

        _tideLabel.Text = Loc.Get("PHASE_TIDE_N", bridge.State.CurrentTide, bridge.State.Config.TideCount);

        var card = bridge.State.CurrentActionCard;
        if (card != null)
            SetCurrent(card.Name, card.Pool == ActionPool.Painful);
        else
            SetCurrent("", false);
    }
}
