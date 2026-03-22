using Godot;
using System.Collections.Generic;

/// <summary>
/// Displays the warden's passive abilities in the bottom-left corner.
/// Reads State.WardenData.Passives (from WardenLoader) and renders each
/// passive as an icon + name row with the description shown inline below.
/// Tooltip contains flavor text only. Static display — built once on EncounterReady.
/// </summary>
public partial class PassivePanelController : VBoxContainer
{
    // Keyed by passive Id for future per-row state updates
    private readonly Dictionary<string, VBoxContainer> _passiveRows = new();
    private bool _built = false;

    public override void _Ready()
    {
        var bridge = GameBridge.Instance;
        if (bridge != null)
            bridge.EncounterReady += Build;

        Build();
    }

    private void Build()
    {
        if (_built) return;

        var bridge   = GameBridge.Instance;
        var passives = bridge?.State?.WardenData?.Passives;

        if (passives == null || passives.Count == 0)
        {
            Visible = false;
            return;
        }

        _built = true;
        Visible = true;

        var cinzel = GD.Load<Font>("res://godot/assets/fonts/Cinzel-Bold.ttf");
        var imFell = GD.Load<Font>("res://godot/assets/fonts/IMFellEnglish-Regular.ttf");

        // Header — warden name
        var wardenName = bridge?.State?.WardenData?.Name ?? "Warden";
        var header = new Label { Text = wardenName, Modulate = new Color(0.9f, 0.85f, 0.7f) };
        if (cinzel != null) header.AddThemeFontOverride("font", cinzel);
        header.AddThemeFontSizeOverride("font_size", 11);
        AddChild(header);

        var gating = bridge?.State?.PassiveGating;

        // One entry per passive: VBoxContainer(HBoxContainer(icon+name) + description label)
        foreach (var passive in passives)
        {
            var entry = new VBoxContainer
            {
                TooltipText = passive.Flavor  // hover shows lore/flavor text
            };

            bool isLocked = gating != null && !gating.IsActive(passive.Id);
            if (isLocked)
                entry.Modulate = new Color(1, 1, 1, 0.5f);

            // Icon + name row
            var nameRow = new HBoxContainer();

            var icon = LoadPassiveIcon(passive.Icon);
            if (icon != null)
            {
                nameRow.AddChild(new TextureRect
                {
                    Texture           = icon,
                    CustomMinimumSize = new Vector2(20, 20),
                    ExpandMode        = TextureRect.ExpandModeEnum.FitWidthProportional,
                    StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered,
                });
            }

            var nameLabel = new Label
            {
                Text              = passive.Name,
                Modulate          = new Color(0.9f, 0.88f, 0.8f),
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode      = TextServer.AutowrapMode.Off
            };
            if (imFell != null) nameLabel.AddThemeFontOverride("font", imFell);
            nameLabel.AddThemeFontSizeOverride("font_size", 11);
            nameRow.AddChild(nameLabel);
            entry.AddChild(nameRow);

            // Inline description
            var descLabel = new Label
            {
                Text         = passive.Description,
                Modulate     = new Color(0.75f, 0.75f, 0.72f, 0.9f),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            if (imFell != null) descLabel.AddThemeFontOverride("font", imFell);
            descLabel.AddThemeFontSizeOverride("font_size", 9);
            entry.AddChild(descLabel);

            // Unlock condition label (shown only for locked passives)
            if (isLocked)
            {
                var unlockCondition = GetUnlockCondition(passive.Id);
                if (unlockCondition.Length > 0)
                {
                    var unlockLabel = new Label
                    {
                        Text         = unlockCondition,
                        Modulate     = new Color(0.6f, 0.6f, 0.55f, 0.8f),
                        AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    };
                    if (imFell != null) unlockLabel.AddThemeFontOverride("font", imFell);
                    unlockLabel.AddThemeFontSizeOverride("font_size", 8);
                    entry.AddChild(unlockLabel);
                }
            }

            _passiveRows[passive.Id] = entry;
            AddChild(entry);
        }

        // Subscribe to passive unlock signal to un-dim when unlocked
        if (bridge != null)
            bridge.PassiveUnlocked += OnPassiveUnlocked;
    }

    private void OnPassiveUnlocked(string passiveId)
    {
        if (_passiveRows.TryGetValue(passiveId, out var entry))
            entry.Modulate = Colors.White;
    }

    private static string GetUnlockCondition(string passiveId) => passiveId switch
    {
        "rest_growth"          => "Unlocks at: Root T1 (4 Root)",
        "presence_provocation" => "Unlocks at: Root T2 (7 Root)",
        "network_slow"         => "Unlocks at: Shadow T1 (4 Shadow)",
        _                      => ""
    };

    private static Texture2D? LoadPassiveIcon(string iconName)
    {
        var path = $"res://godot/assets/art/kenney_board-game-icons/PNG/Default (64px)/{iconName}.png";
        if (ResourceLoader.Exists(path))
            return GD.Load<Texture2D>(path);
        return GD.Load<Texture2D>("res://godot/assets/art/kenney_board-game-icons/PNG/Default (64px)/hexagon_question.png");
    }
}
