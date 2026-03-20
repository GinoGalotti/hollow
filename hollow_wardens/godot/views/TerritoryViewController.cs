using Godot;
using HollowWardens.Core.Models;

/// <summary>
/// Node2D that draws one territory as a colored rectangle with text stats.
/// The node's Name must match the territory ID (A1, A2, A3, M1, M2, I1).
/// </summary>
public partial class TerritoryViewController : Node2D
{
    private Territory? _territory;

    public override void _Ready()
    {
        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        _territory = bridge.State.GetTerritory(Name);

        bridge.CorruptionChanged += (id, _, _) => { if (id == Name) QueueRedraw(); };
        bridge.InvaderArrived    += (_, id, _) => { if (id == Name) QueueRedraw(); };
        bridge.InvaderDefeated   += _         => QueueRedraw();
        bridge.InvaderAdvanced   += (_, f, t) => { if (f == Name || t == Name) QueueRedraw(); };
    }

    public override void _Draw()
    {
        var t = _territory;
        if (t == null) return;

        var bg = CorruptionColor(t.CorruptionLevel);
        DrawRect(new Rect2(-62, -42, 124, 84), bg);
        DrawRect(new Rect2(-62, -42, 124, 84), Colors.White, false, 2f);

        var font = ThemeDB.FallbackFont;
        const int fs = 12;

        DrawString(font, new Vector2(-58, -26), t.Id,
            HorizontalAlignment.Left, 120, fs + 3, Colors.White);
        DrawString(font, new Vector2(-58, -10), $"Presence: {t.PresenceCount}",
            HorizontalAlignment.Left, 120, fs, Colors.Cyan);
        DrawString(font, new Vector2(-58, 4), $"Invaders: {t.Invaders.Count(i => i.IsAlive)}",
            HorizontalAlignment.Left, 120, fs, new Color(1, 0.5f, 0.5f));
        DrawString(font, new Vector2(-58, 18), $"Natives:  {t.Natives.Count(n => n.IsAlive)}",
            HorizontalAlignment.Left, 120, fs, new Color(0.5f, 1, 0.5f));
        DrawString(font, new Vector2(-58, 32), $"Corrupt:  {t.CorruptionPoints} (L{t.CorruptionLevel})",
            HorizontalAlignment.Left, 120, fs, new Color(1, 0.8f, 0.2f));
    }

    private static Color CorruptionColor(int level) => level switch
    {
        0 => new Color(0.15f, 0.40f, 0.15f),
        1 => new Color(0.55f, 0.50f, 0.10f),
        2 => new Color(0.55f, 0.25f, 0.05f),
        3 => new Color(0.50f, 0.00f, 0.05f),
        _ => new Color(0.25f, 0.25f, 0.25f),
    };
}
