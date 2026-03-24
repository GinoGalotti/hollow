using Godot;
using HollowWardens.Core.Localization;

/// <summary>
/// Segmented dread bar with threshold markers at 15/30/45 fear.
/// Also shows current Weave value.
/// </summary>
public partial class DreadBarController : VBoxContainer
{
    private Label _dreadLabel  = null!;
    private Label _fearLabel   = null!;
    private Label _barLabel    = null!;
    private Label _weaveLabel  = null!;

    public override void _Ready()
    {
        var cinzel     = GD.Load<Font>("res://godot/assets/fonts/Cinzel-Bold.ttf");
        var skullIcon  = GD.Load<Texture2D>("res://godot/assets/art/kenney_board-game-icons/PNG/Default (64px)/skull.png");

        var header = new Label { Text = Loc.Get("LABEL_DREAD_WEAVE"), Modulate = Colors.Orange };
        if (cinzel != null) header.AddThemeFontOverride("font", cinzel);
        AddChild(header);

        // Dread row: skull icon + dread level label
        var dreadRow = new HBoxContainer();
        if (skullIcon != null)
            dreadRow.AddChild(new TextureRect
            {
                Texture           = skullIcon,
                CustomMinimumSize = new Vector2(16, 16),
                StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode        = TextureRect.ExpandModeEnum.FitWidthProportional,
            });
        _dreadLabel = new Label();
        if (cinzel != null) _dreadLabel.AddThemeFontOverride("font", cinzel);
        _dreadLabel.AddThemeFontSizeOverride("font_size", 13);
        dreadRow.AddChild(_dreadLabel);
        AddChild(dreadRow);
        _fearLabel  = new Label();
        _barLabel   = new Label { AutowrapMode = TextServer.AutowrapMode.Off };
        _weaveLabel = new Label { Modulate = new Color(0.4f, 0.8f, 1.0f) };

        AddChild(_fearLabel);
        AddChild(_barLabel);
        AddChild(new HSeparator());
        AddChild(_weaveLabel);

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.EncounterReady += Refresh;
        bridge.DreadAdvanced  += _ => Refresh();
        bridge.FearGenerated  += _ => Refresh();
        bridge.WeaveChanged   += _ => Refresh();

        Refresh();
    }

    private void Refresh()
    {
        var bridge = GameBridge.Instance;
        if (bridge?.State == null) return;

        int level = bridge.State.Dread?.DreadLevel ?? 1;
        int total = bridge.State.Dread?.TotalFearGenerated ?? 0;
        int weave = bridge.State.Weave?.CurrentWeave ?? 0;

        _dreadLabel.Text  = Loc.Get("DREAD_LEVEL", level);
        _fearLabel.Text   = Loc.Get("TOTAL_FEAR", total, NextThreshold(total));
        _weaveLabel.Text  = Loc.Get("WEAVE_DISPLAY", weave, 20) + (weave <= 0 ? " " + Loc.Get("WEAVE_BREACH") : "");

        // Bar: 45 chars, markers at 15 and 30
        int bars    = System.Math.Min(total, 45);
        char[] bar  = new char[45];
        for (int i = 0; i < 45; i++) bar[i] = i < bars ? '#' : '-';
        if (bar.Length > 15) bar[15] = bar[15] == '#' ? '|' : ':';
        if (bar.Length > 30) bar[30] = bar[30] == '#' ? '|' : ':';
        _barLabel.Text = $"[{new string(bar)}]";

        _dreadLabel.Modulate = level switch { 1 => Colors.White, 2 => Colors.Yellow, 3 => Colors.Orange, _ => Colors.Red };
        _weaveLabel.Modulate = weave <= 5 ? Colors.Red : new Color(0.4f, 0.8f, 1.0f);
    }

    private static int NextThreshold(int fear) =>
        fear < 15 ? 15 : fear < 30 ? 30 : fear < 45 ? 45 : -1;
}
