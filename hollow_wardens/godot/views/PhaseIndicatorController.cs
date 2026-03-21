using Godot;
using HollowWardens.Core.Models;

/// <summary>
/// Shows current phase, tide, weave, and deck counts in a status panel.
/// </summary>
public partial class PhaseIndicatorController : VBoxContainer
{
    private Label      _phaseLabel = null!;
    private Label      _deckLabel  = null!;
    private Label      _hintLabel  = null!;
    private TextureRect _phaseIcon  = null!;

    // Phase icons indexed to TurnPhase order (Vigil, Tide, Dusk, Rest, Resolution)
    private Texture2D?[] _phaseIcons = Array.Empty<Texture2D?>();

    public override void _Ready()
    {
        const string IconBase = "res://godot/assets/art/kenney_board-game-icons/PNG/Default (64px)/";
        _phaseIcons = new Texture2D?[]
        {
            GD.Load<Texture2D>(IconBase + "book_open.png"),   // Vigil
            GD.Load<Texture2D>(IconBase + "pawn_right.png"),  // Tide
            GD.Load<Texture2D>(IconBase + "cards_flip.png"),  // Dusk
            GD.Load<Texture2D>(IconBase + "campfire.png"),    // Rest
            GD.Load<Texture2D>(IconBase + "award.png"),       // Resolution
        };

        var cinzel = GD.Load<Font>("res://godot/assets/fonts/Cinzel-Bold.ttf");

        var header = new Label { Text = "── Status ──", Modulate = Colors.Yellow };
        if (cinzel != null) header.AddThemeFontOverride("font", cinzel);
        AddChild(header);

        // Phase row: icon + label
        var phaseRow = new HBoxContainer();
        _phaseIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(18, 18),
            StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode        = TextureRect.ExpandModeEnum.FitWidthProportional,
        };
        phaseRow.AddChild(_phaseIcon);
        _phaseLabel = new Label();
        if (cinzel != null) _phaseLabel.AddThemeFontOverride("font", cinzel);
        phaseRow.AddChild(_phaseLabel);
        AddChild(phaseRow);
        _deckLabel  = new Label { Modulate = Colors.LightGray };
        _hintLabel  = new Label
        {
            Text     = "[Space] End Phase\n[R] Rest",
            Modulate = new Color(0.6f, 0.6f, 0.6f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };

        AddChild(new HSeparator());
        AddChild(_deckLabel);
        AddChild(new HSeparator());
        AddChild(_hintLabel);

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.PhaseChanged           += _ => RefreshPhase();
        bridge.TurnStarted            += RefreshPhase;
        bridge.RestStarted            += RefreshPhase;
        bridge.DeckCountsChanged      += OnDeckCounts;
        bridge.ResolutionTurnStarted  += _ => RefreshPhase();
        bridge.EncounterEnded         += OnEncounterEnded;

        RefreshPhase();
    }

    private void RefreshPhase()
    {
        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bool rest = bridge.IsRestTurn;
        bool res  = bridge.IsInResolution;

        string phaseName;
        Color  color;

        if (res)
        {
            phaseName = $"Resolution {bridge.State.CurrentTide}/{bridge.State.Config.TideCount}";
            color = new Color(0.6f, 0.9f, 0.6f);
        }
        else if (rest)
        {
            phaseName = "REST  [press R or Space]";
            color = new Color(0.8f, 0.5f, 0.2f);
        }
        else
        {
            phaseName = bridge.CurrentPhase switch
            {
                TurnPhase.Vigil => $"VIGIL  (Tide {bridge.State.CurrentTide}/{bridge.State.Config.TideCount})",
                TurnPhase.Tide  => "TIDE",
                TurnPhase.Dusk  => "DUSK",
                _               => bridge.CurrentPhase.ToString()
            };
            color = bridge.CurrentPhase switch
            {
                TurnPhase.Vigil => new Color(0.5f, 0.8f, 1.0f),
                TurnPhase.Dusk  => new Color(0.9f, 0.7f, 0.4f),
                TurnPhase.Tide  => new Color(1.0f, 0.4f, 0.4f),
                _               => Colors.White
            };
        }

        _phaseLabel.Text     = phaseName;
        _phaseLabel.Modulate = color;

        // Update phase icon
        int iconIdx = res ? 4 : rest ? 3 : bridge.CurrentPhase switch
        {
            TurnPhase.Vigil => 0,
            TurnPhase.Tide  => 1,
            TurnPhase.Dusk  => 2,
            _               => 0
        };
        if (_phaseIcons.Length > iconIdx)
            _phaseIcon.Texture = _phaseIcons[iconIdx];
    }

    private void OnDeckCounts(int draw, int discard, int dissolved, int dormant)
    {
        _deckLabel.Text = $"Draw: {draw}  Disc: {discard}\nDissolved: {dissolved}  Dormant: {dormant}";
    }

    private void OnEncounterEnded(int result)
    {
        _phaseLabel.Text     = $"Encounter Over: {(EncounterResult)result}";
        _phaseLabel.Modulate = result == (int)EncounterResult.Breach ? Colors.Red : Colors.LightGreen;
        _hintLabel.Text      = "";
    }
}
