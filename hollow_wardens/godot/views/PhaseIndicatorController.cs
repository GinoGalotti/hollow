using Godot;
using HollowWardens.Core.Localization;
using HollowWardens.Core.Models;

/// <summary>
/// Shows current phase, tide, weave, and deck counts in a status panel.
/// </summary>
public partial class PhaseIndicatorController : VBoxContainer
{
    private Label _phaseLabel = null!;
    private Label _deckLabel  = null!;
    private Label _hintLabel  = null!;

    public override void _Ready()
    {
        var header = new Label { Text = Loc.Get("LABEL_STATUS"), Modulate = Colors.Yellow };
        AddChild(header);

        _phaseLabel = new Label();
        AddChild(_phaseLabel);

        _deckLabel = new Label { Modulate = Colors.LightGray };
        _hintLabel = new Label
        {
            Text         = Loc.Get("INPUT_HINT_PHASE"),
            Modulate     = new Color(0.6f, 0.6f, 0.6f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };

        AddChild(new HSeparator());
        AddChild(_deckLabel);
        AddChild(new HSeparator());
        AddChild(_hintLabel);

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.EncounterReady         += RefreshPhase;
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
        if (bridge?.State == null) return;

        bool rest = bridge.IsRestTurn;
        bool res  = bridge.IsInResolution;

        string phaseName;
        Color  color;

        if (res)
        {
            phaseName = Loc.Get("PHASE_RESOLUTION_N", bridge.State.CurrentTide, bridge.State.Config.TideCount);
            color = new Color(0.6f, 0.9f, 0.6f);
        }
        else if (rest)
        {
            phaseName = Loc.Get("INPUT_HINT_REST");
            color = new Color(0.8f, 0.5f, 0.2f);
        }
        else
        {
            phaseName = bridge.CurrentPhase switch
            {
                TurnPhase.Vigil => Loc.Get("PHASE_VIGIL_N", bridge.State.CurrentTide, bridge.State.Config.TideCount),
                TurnPhase.Tide  => Loc.Get("PHASE_TIDE"),
                TurnPhase.Dusk  => Loc.Get("PHASE_DUSK"),
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
    }

    private void OnDeckCounts(int draw, int discard, int dissolved, int dormant)
    {
        _deckLabel.Text = Loc.Get("DECK_COUNTS", draw, discard, dissolved, dormant);
    }

    private void OnEncounterEnded(int result)
    {
        _phaseLabel.Text     = Loc.Get("ENCOUNTER_OVER", (EncounterResult)result);
        _phaseLabel.Modulate = result == (int)EncounterResult.Breach ? Colors.Red : Colors.LightGreen;
        _hintLabel.Text      = "";
    }
}
