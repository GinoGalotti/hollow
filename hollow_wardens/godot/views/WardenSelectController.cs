using Godot;

/// <summary>
/// Warden selection overlay shown before the encounter starts.
/// Two buttons: "The Root" and "The Ember".
/// Clicking one sets GameBridge.SelectedWardenId and starts the encounter.
/// Inherits CanvasLayer so it renders on top of the game UI layer.
/// </summary>
public partial class WardenSelectController : CanvasLayer
{
    public override void _Ready()
    {
        Layer = 2;  // Above game UI CanvasLayer (layer 1)

        // Ensure the game board is hidden until a warden is selected
        var game = GetNode<Node>("/root/Game");
        var board = game?.GetNodeOrNull<Node2D>("Board");
        var ui    = game?.GetNodeOrNull<CanvasLayer>("UI");
        if (board != null) board.Visible = false;
        if (ui    != null) ui.Visible    = false;

        var cinzel = GD.Load<Font>("res://godot/assets/fonts/Cinzel-Bold.ttf");
        var imFell = GD.Load<Font>("res://godot/assets/fonts/IMFellEnglish-Regular.ttf");

        // CanvasLayer has no size; add a fullscreen Control as the UI root
        var overlay = new Control();
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        overlay.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(400, 300);
        panel.AddChild(vbox);

        // Title
        var title = new Label { Text = "Choose Your Warden", HorizontalAlignment = HorizontalAlignment.Center };
        if (cinzel != null) title.AddThemeFontOverride("font", cinzel);
        title.AddThemeFontSizeOverride("font_size", 18);
        title.Modulate = new Color(0.9f, 0.85f, 0.7f);
        vbox.AddChild(title);

        vbox.AddChild(new HSeparator());

        // Root button
        var rootBtn = new Button
        {
            Text = "The Root\nTank / Control — Root · Mist · Shadow\n\"Something ancient stirs beneath the soil.\""
        };
        rootBtn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        if (imFell != null) rootBtn.AddThemeFontOverride("font", imFell);
        rootBtn.AddThemeFontSizeOverride("font_size", 11);
        rootBtn.Pressed += () => StartEncounter("root");
        vbox.AddChild(rootBtn);

        // Ember button
        var emberBtn = new Button
        {
            Text = "The Ember\nBurst Damage / Glass Cannon — Ash · Shadow · Gale\n\"A dying fire spirit. Where it burns, destruction follows.\""
        };
        emberBtn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        if (imFell != null) emberBtn.AddThemeFontOverride("font", imFell);
        emberBtn.AddThemeFontSizeOverride("font_size", 11);
        emberBtn.Pressed += () => StartEncounter("ember");
        vbox.AddChild(emberBtn);
    }

    private void StartEncounter(string wardenId)
    {
        // Unhide game UI
        var game  = GetNode<Node>("/root/Game");
        var board = game?.GetNodeOrNull<Node2D>("Board");
        var ui    = game?.GetNodeOrNull<CanvasLayer>("UI");
        if (board != null) board.Visible = true;
        if (ui    != null) ui.Visible    = true;

        // Hide this overlay
        Visible = false;

        // Kick off the encounter
        GameBridge.Instance?.StartWithWarden(wardenId);
    }
}
