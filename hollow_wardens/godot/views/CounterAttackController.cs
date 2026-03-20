using Godot;

/// <summary>
/// Overlay shown when CounterAttackReady fires (auto-assign is used).
/// Displays territory and damage pool, then auto-hides after a short delay.
/// </summary>
public partial class CounterAttackController : PanelContainer
{
    private Label _infoLabel = null!;

    public override void _Ready()
    {
        Visible = false;
        CustomMinimumSize = new Vector2(360, 120);

        // Center the overlay
        SetAnchorsAndOffsetsPreset(LayoutPreset.Center);

        var vbox = new VBoxContainer();
        AddChild(vbox);

        vbox.AddChild(new Label
        {
            Text     = "── Counter-Attack (Auto) ──",
            Modulate = new Color(1f, 0.7f, 0.2f)
        });

        _infoLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(340, 60)
        };
        vbox.AddChild(_infoLabel);

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.CounterAttackReady += OnCounterAttackReady;
    }

    private void OnCounterAttackReady(string territoryId, int pool)
    {
        _infoLabel.Text = $"Territory {territoryId}:\n  Native damage pool = {pool}  (auto-assigned)";
        Visible = true;

        // Auto-hide after 2 seconds
        var timer = GetTree().CreateTimer(2.0);
        timer.Timeout += () => Visible = false;
    }
}
