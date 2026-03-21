using Godot;

/// <summary>
/// Overlay shown when a fear action is ready for player resolution.
/// Untargeted: shows description + "Confirm (Space)" button.
/// Targeted: shows description + "Targeting…" text; player clicks a territory.
/// Hides automatically after the fear action is resolved.
/// </summary>
public partial class FearConfirmController : Control
{
    public static FearConfirmController? Instance { get; private set; }

    private Label  _descLabel     = null!;
    private Label  _hintLabel     = null!;
    private Button _confirmButton = null!;

    public override void _Ready()
    {
        Instance = this;
        Visible  = false;

        SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        CustomMinimumSize = new Vector2(340, 120);

        var bg = new PanelContainer();
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var vbox = new VBoxContainer();
        bg.AddChild(vbox);

        vbox.AddChild(new Label
        {
            Text     = "⚡ Fear Action",
            Modulate = new Color(0.45f, 1f, 0.45f)
        });

        _descLabel = new Label
        {
            AutowrapMode      = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(300, 40)
        };
        vbox.AddChild(_descLabel);

        _hintLabel = new Label { Modulate = new Color(0.7f, 0.7f, 0.7f) };
        vbox.AddChild(_hintLabel);

        _confirmButton = new Button { Text = "Confirm (Space)" };
        _confirmButton.Pressed += () => GameBridge.Instance?.ConfirmFearAction();
        vbox.AddChild(_confirmButton);

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.FearActionPending         += OnFearActionPending;
        bridge.ThresholdResolved         += (_, __, ___) => { }; // hook for future
        // Hide when fear action resolves (TargetingModeChanged to false after targeted resolve)
        bridge.TargetingModeChanged      += active => { if (!active && Visible && _confirmButton.Disabled) Hide(); };
        bridge.TurnStarted               += () => Visible = false; // cleanup on turn start
    }

    public override void _ExitTree()
    {
        Instance = null;
    }

    private void OnFearActionPending(string description, bool needsTarget)
    {
        _descLabel.Text    = description;
        _confirmButton.Visible  = !needsTarget;
        _confirmButton.Disabled = needsTarget;

        if (needsTarget)
        {
            _hintLabel.Text = "Click a territory to apply";
            _hintLabel.Modulate = new Color(1f, 0.85f, 0.3f);
        }
        else
        {
            _hintLabel.Text = "Press Space or click Confirm";
            _hintLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
        }

        Visible = true;
    }
}
