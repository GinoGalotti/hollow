using Godot;
using HollowWardens.Core.Localization;

/// <summary>
/// Two-screen overlay: first selects warden, then selects encounter.
/// Clicking a warden reveals the encounter buttons; clicking an encounter starts the game.
/// Inherits CanvasLayer so it renders on top of the game UI layer.
/// </summary>
public partial class WardenSelectController : CanvasLayer
{
    private Control? _wardenScreen;
    private Control? _encounterScreen;
    private string   _selectedWarden = "root";

    public override void _Ready()
    {
        Layer = 2;  // Above game UI CanvasLayer (layer 1)

        // Ensure the game board is hidden until selection is complete
        var game  = GetNode<Node>("/root/Game");
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

        _wardenScreen   = BuildWardenScreen(overlay, cinzel, imFell);
        _encounterScreen = BuildEncounterScreen(overlay, cinzel, imFell);
        _encounterScreen.Visible = false;
    }

    // ── Warden selection screen ───────────────────────────────────────────────

    private Control BuildWardenScreen(Control parent, Font? cinzel, Font? imFell)
    {
        var container = new Control();
        container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(container);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        container.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(400, 300);
        panel.AddChild(vbox);

        var title = new Label { Text = Loc.Get("WARDEN_SELECT_TITLE"), HorizontalAlignment = HorizontalAlignment.Center };
        if (cinzel != null) title.AddThemeFontOverride("font", cinzel);
        title.AddThemeFontSizeOverride("font_size", 18);
        title.Modulate = new Color(0.9f, 0.85f, 0.7f);
        vbox.AddChild(title);
        vbox.AddChild(new HSeparator());

        AddWardenButton(vbox, imFell, "root",
            Loc.Get("WARDEN_ROOT_NAME"),
            Loc.Get("WARDEN_ROOT_DESC"),
            Loc.Get("WARDEN_ROOT_FLAVOR"));

        AddWardenButton(vbox, imFell, "ember",
            Loc.Get("WARDEN_EMBER_NAME"),
            Loc.Get("WARDEN_EMBER_DESC"),
            Loc.Get("WARDEN_EMBER_FLAVOR"));

        return container;
    }

    private void AddWardenButton(VBoxContainer vbox, Font? imFell,
        string wardenId, string name, string desc, string flavor)
    {
        var btn = new Button { Text = $"{name}\n{desc}\n\"{flavor}\"" };
        btn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        if (imFell != null) btn.AddThemeFontOverride("font", imFell);
        btn.AddThemeFontSizeOverride("font_size", 11);
        btn.Pressed += () => ShowEncounterScreen(wardenId);
        vbox.AddChild(btn);
    }

    // ── Encounter selection screen ────────────────────────────────────────────

    private Control BuildEncounterScreen(Control parent, Font? cinzel, Font? imFell)
    {
        var container = new Control();
        container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(container);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        container.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(440, 360);
        panel.AddChild(vbox);

        var title = new Label { Text = Loc.Get("ENCOUNTER_SELECT_TITLE"), HorizontalAlignment = HorizontalAlignment.Center };
        if (cinzel != null) title.AddThemeFontOverride("font", cinzel);
        title.AddThemeFontSizeOverride("font_size", 18);
        title.Modulate = new Color(0.9f, 0.85f, 0.7f);
        vbox.AddChild(title);
        vbox.AddChild(new HSeparator());

        (string id, string nameKey, string subKey)[] encounters =
        {
            ("pale_march_standard", "ENCOUNTER_STANDARD_NAME", "ENCOUNTER_STANDARD_SUB"),
            ("pale_march_scouts",   "ENCOUNTER_SCOUTS_NAME",   "ENCOUNTER_SCOUTS_SUB"),
            ("pale_march_siege",    "ENCOUNTER_SIEGE_NAME",    "ENCOUNTER_SIEGE_SUB"),
            ("pale_march_elite",    "ENCOUNTER_ELITE_NAME",    "ENCOUNTER_ELITE_SUB"),
            ("pale_march_frontier", "ENCOUNTER_FRONTIER_NAME", "ENCOUNTER_FRONTIER_SUB"),
        };

        foreach (var (id, nameKey, subKey) in encounters)
        {
            var encId   = id;
            var btn     = new Button { Text = $"{Loc.Get(nameKey)}\n{Loc.Get(subKey)}" };
            btn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            if (imFell != null) btn.AddThemeFontOverride("font", imFell);
            btn.AddThemeFontSizeOverride("font_size", 11);
            btn.Pressed += () => StartEncounter(_selectedWarden, encId);
            vbox.AddChild(btn);
        }

        // Back button
        var backBtn = new Button { Text = "← Back" };
        if (imFell != null) backBtn.AddThemeFontOverride("font", imFell);
        backBtn.AddThemeFontSizeOverride("font_size", 10);
        backBtn.Modulate = Colors.LightGray;
        backBtn.Pressed += () =>
        {
            if (_encounterScreen != null) _encounterScreen.Visible = false;
            if (_wardenScreen    != null) _wardenScreen.Visible    = true;
        };
        vbox.AddChild(backBtn);

        return container;
    }

    private void ShowEncounterScreen(string wardenId)
    {
        _selectedWarden = wardenId;
        if (_wardenScreen    != null) _wardenScreen.Visible    = false;
        if (_encounterScreen != null) _encounterScreen.Visible = true;
    }

    private void StartEncounter(string wardenId, string encounterId)
    {
        GameBridge.SelectedEncounterId = encounterId;

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
