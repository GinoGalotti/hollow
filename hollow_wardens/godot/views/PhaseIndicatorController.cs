using Godot;
using HollowWardens.Core.Localization;
using HollowWardens.Core.Models;

/// <summary>
/// Horizontal phase strip shown in TopStrip.
/// Displays VIGIL | TIDE | DUSK with the active section highlighted and others dimmed.
/// Tide count (e.g. "Tide 3/6") appears in the active label.
/// Deck counts are pushed to the DeckCounts label in BottomDock via node path.
/// </summary>
public partial class PhaseIndicatorController : HBoxContainer
{
    private static readonly Color InactiveColor = new(0.45f, 0.42f, 0.38f);
    private static readonly Color VigilColor    = new(0.5f,  0.8f,  1.0f);
    private static readonly Color TideColor     = new(1.0f,  0.4f,  0.4f);
    private static readonly Color DuskColor     = new(1.0f,  0.7f,  0.3f);
    private static readonly Color RestColor     = new(0.8f,  0.5f,  0.2f);
    private static readonly Color ResolveColor  = new(0.6f,  0.9f,  0.6f);

    private Label  _vigilLabel      = null!;
    private Label  _tideLabel       = null!;
    private Label  _duskLabel       = null!;
    private Label  _hintLabel       = null!;
    private Button _confirmPairBtn  = null!;
    private Label? _deckLabel;   // Label in BottomDock, found by path

    public override void _Ready()
    {
        var cinzel = FontCache.CinzelBold;

        // Left margin
        AddChild(new Control { CustomMinimumSize = new Vector2(12, 0) });

        _vigilLabel = MakePhaseLabel("VIGIL", cinzel);
        AddChild(_vigilLabel);

        AddChild(new VSeparator());

        _tideLabel = MakePhaseLabel("TIDE", cinzel);
        AddChild(_tideLabel);

        AddChild(new VSeparator());

        _duskLabel = MakePhaseLabel("DUSK", cinzel);
        AddChild(_duskLabel);

        // Spacer pushes right-side controls to the far right
        AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Confirm Pair button — shown only in pairing selection mode
        _confirmPairBtn = new Button
        {
            Text    = Loc.Get("BTN_CONFIRM_PAIR"),
            Visible = false,
        };
        if (cinzel != null) _confirmPairBtn.AddThemeFontOverride("font", cinzel);
        _confirmPairBtn.AddThemeFontSizeOverride("font_size", 13);
        _confirmPairBtn.Pressed += () => GameBridge.Instance?.ConfirmPair();
        AddChild(_confirmPairBtn);

        AddChild(new Control { CustomMinimumSize = new Vector2(8, 0) });

        _hintLabel = new Label
        {
            Text     = Loc.Get("INPUT_HINT_PHASE"),
            Modulate = new Color(0.55f, 0.52f, 0.48f),
        };
        AddChild(_hintLabel);

        AddChild(new Control { CustomMinimumSize = new Vector2(12, 0) });

        // Locate the DeckCounts label that lives in BottomDock/HandArea
        _deckLabel = GetNodeOrNull<Label>("/root/Game/UI/RootLayout/BottomDock/HandArea/DeckCounts");

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.EncounterReady          += RefreshPhase;
        bridge.PhaseChanged            += _ => RefreshPhase();
        bridge.TurnStarted             += RefreshPhase;
        bridge.RestStarted             += RefreshPhase;
        bridge.DeckCountsChanged       += OnDeckCounts;
        bridge.ResolutionTurnStarted   += _ => RefreshPhase();
        bridge.EncounterEnded          += OnEncounterEnded;
        bridge.PairingSelectionChanged += (_, _) => RefreshPairButton();

        RefreshPhase();
    }

    private static Label MakePhaseLabel(string text, Font? font)
    {
        var label = new Label
        {
            Text                  = text,
            Modulate              = InactiveColor,
            HorizontalAlignment   = HorizontalAlignment.Center,
            CustomMinimumSize     = new Vector2(120, 0),
        };
        if (font != null) label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", 16);
        return label;
    }

    private void RefreshPhase()
    {
        var bridge = GameBridge.Instance;
        if (bridge?.State == null) return;

        bool rest  = bridge.IsRestTurn;
        bool res   = bridge.IsInResolution;
        var  phase = bridge.CurrentPhase;

        if (res)
        {
            _vigilLabel.Text    = "VIGIL"; _vigilLabel.Modulate = InactiveColor;
            _tideLabel.Text     = "TIDE";  _tideLabel.Modulate  = InactiveColor;
            _duskLabel.Text     = Loc.Get("PHASE_RESOLUTION_N", bridge.State.CurrentTide, bridge.State.Config.TideCount);
            _duskLabel.Modulate = ResolveColor;
            return;
        }

        if (rest)
        {
            _vigilLabel.Text    = Loc.Get("INPUT_HINT_REST"); _vigilLabel.Modulate = RestColor;
            _tideLabel.Text     = "TIDE"; _tideLabel.Modulate  = InactiveColor;
            _duskLabel.Text     = "DUSK"; _duskLabel.Modulate  = InactiveColor;
            return;
        }

        string tideCtx = Loc.Get("PHASE_TIDE_N", bridge.State.CurrentTide, bridge.State.Config.TideCount);

        bool isVigil = phase is TurnPhase.Vigil or TurnPhase.Plan or TurnPhase.Fast;
        bool isTide  = phase is TurnPhase.Tide;
        bool isDusk  = phase is TurnPhase.Dusk or TurnPhase.Slow or TurnPhase.Elements or TurnPhase.Cleanup;

        _vigilLabel.Text = phase switch
        {
            TurnPhase.Plan  => $"PLAN  ({tideCtx})",
            TurnPhase.Fast  => $"FAST  ({tideCtx})",
            TurnPhase.Vigil => Loc.Get("PHASE_VIGIL_N", bridge.State.CurrentTide, bridge.State.Config.TideCount),
            _               => "VIGIL",
        };
        _vigilLabel.Modulate = isVigil ? VigilColor : InactiveColor;

        _tideLabel.Text     = isTide ? $"TIDE  ({tideCtx})" : "TIDE";
        _tideLabel.Modulate = isTide ? TideColor : InactiveColor;

        _duskLabel.Text = phase switch
        {
            TurnPhase.Slow     => $"SLOW  ({tideCtx})",
            TurnPhase.Elements => $"ELEMENTS  ({tideCtx})",
            TurnPhase.Cleanup  => $"CLEANUP  ({tideCtx})",
            _                  => "DUSK",
        };
        _duskLabel.Modulate = isDusk ? DuskColor : InactiveColor;
    }

    private void RefreshPairButton()
    {
        var bridge = GameBridge.Instance;
        bool pairing = bridge?.IsPairingSelection ?? false;

        _confirmPairBtn.Visible  = pairing;
        _confirmPairBtn.Disabled = pairing && !(bridge?.CanConfirmPair ?? false);

        _hintLabel.Text = pairing
            ? Loc.Get("INPUT_HINT_PAIRING")
            : Loc.Get("INPUT_HINT_PHASE");
    }

    private void OnDeckCounts(int draw, int discard, int dissolved, int dormant)
    {
        if (_deckLabel != null)
            _deckLabel.Text = Loc.Get("DECK_COUNTS", draw, discard, dissolved, dormant);
    }

    private void OnEncounterEnded(int result)
    {
        var    color = result == (int)EncounterResult.Breach ? Colors.Red : Colors.LightGreen;
        string text  = Loc.Get("ENCOUNTER_OVER", (EncounterResult)result);
        _vigilLabel.Text      = text; _vigilLabel.Modulate = color;
        _tideLabel.Text       = "";
        _duskLabel.Text       = "";
        _hintLabel.Text       = "";
        _confirmPairBtn.Visible = false;
    }
}
