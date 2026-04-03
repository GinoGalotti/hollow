using Godot;
using HollowWardens.Core.Events;
using HollowWardens.Core.Localization;
using HollowWardens.Core.Run;

/// <summary>
/// Event/rest/merchant/corruption node screen shown between encounters in Full Run mode.
/// Displays event name, description, and clickable option buttons.
/// </summary>
public partial class EventScreenController : CanvasLayer
{
    [Signal] public delegate void EventResolvedEventHandler(int optionIndex);
    [Signal] public delegate void EventDismissedEventHandler();

    private Font? _cinzel;
    private Font? _imFell;

    private EventData? _currentEvent;
    private RunState?  _runState;
    private int        _selectedOption = -1;

    public override void _Ready()
    {
        Layer   = 10;
        Visible = false;
        _cinzel = FontCache.CinzelBold;
        _imFell = FontCache.IMFell;
    }

    /// <summary>
    /// Populate and show the event screen with the given event data.
    /// </summary>
    public void Show(EventData evt, RunState runState)
    {
        _currentEvent   = evt;
        _runState       = runState;
        _selectedOption = -1;

        foreach (var child in GetChildren())
            child.QueueFree();

        BuildUI();
        Visible = true;
    }

    private void BuildUI()
    {
        var overlay = new Control();
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        overlay.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(480, 320);
        panel.AddChild(vbox);

        // ── Event name ─────────────────────────────────────────────────────
        var nameLabel = new Label
        {
            Text = Loc.Get(_currentEvent!.NameKey),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        ApplyFont(nameLabel, _cinzel, 18, new Color(0.9f, 0.85f, 0.7f));
        vbox.AddChild(nameLabel);

        // ── Description ────────────────────────────────────────────────────
        var descLabel = new Label
        {
            Text = Loc.Get(_currentEvent.DescriptionKey),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        ApplyFont(descLabel, _imFell, 11, Colors.LightGray);
        vbox.AddChild(descLabel);

        vbox.AddChild(new HSeparator());

        // ── Options ────────────────────────────────────────────────────────
        var optionButtons = new List<Button>();

        for (int i = 0; i < _currentEvent.Options.Count; i++)
        {
            var option    = _currentEvent.Options[i];
            var optIdx    = i;
            string label  = Loc.Get(option.LabelKey);
            string optDesc = string.IsNullOrEmpty(option.DescriptionKey) ? "" : Loc.Get(option.DescriptionKey);

            bool locked = option.ElementThreshold.HasValue; // simplified: show threshold info
            string suffix = option.ElementThreshold.HasValue
                ? $" [{option.ElementType} ≥{option.ElementThreshold}]"
                : "";

            var btn = new Button { Text = $"{label}{suffix}\n{optDesc}" };
            btn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            ApplyFont(btn, _imFell, 11, Colors.White);

            btn.Pressed += () =>
            {
                _selectedOption = optIdx;
                foreach (var b in optionButtons)
                    b.Modulate = new Color(1f, 1f, 1f, 0.6f);
                btn.Modulate = Colors.White;
            };
            optionButtons.Add(btn);
            vbox.AddChild(btn);
        }

        vbox.AddChild(new HSeparator());

        // ── Confirm ────────────────────────────────────────────────────────
        var confirmBtn = new Button { Text = Loc.Get("BTN_CONFIRM") };
        ApplyFont(confirmBtn, _cinzel, 13, new Color(0.4f, 1f, 0.6f));
        confirmBtn.Pressed += () =>
        {
            if (_selectedOption < 0) return; // require selection
            Visible = false;
            EmitSignal(SignalName.EventResolved, _selectedOption);
        };
        vbox.AddChild(confirmBtn);

        // ── Skip ───────────────────────────────────────────────────────────
        var skipBtn = new Button { Text = Loc.Get("BTN_SKIP") };
        ApplyFont(skipBtn, _imFell, 10, Colors.LightGray);
        skipBtn.Pressed += () =>
        {
            Visible = false;
            EmitSignal(SignalName.EventDismissed);
        };
        vbox.AddChild(skipBtn);
    }

    private void ApplyFont(Control ctrl, Font? font, int size, Color color)
    {
        if (font != null) ctrl.AddThemeFontOverride("font", font);
        ctrl.AddThemeFontSizeOverride("font_size", size);
        ctrl.Modulate = color;
    }
}
