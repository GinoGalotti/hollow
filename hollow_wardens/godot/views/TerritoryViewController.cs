using Godot;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Models;

/// <summary>
/// Node2D that draws one territory as a colored rectangle with text stats,
/// plus rows of unit squares (invaders + natives) below the box.
/// Handles targeting-mode click selection, counter-attack invader clicks,
/// and card-play floating text feedback.
/// </summary>
public partial class TerritoryViewController : Node2D
{
    private static readonly Rect2 TerritoryRect = new(-62, -42, 124, 84);

    // Unit square layout constants
    private const int SqSize  = 22;
    private const int SqGap   = 4;
    private const int SqStep  = SqSize + SqGap;
    private const int MaxPerRow = 5;
    private const int InvaderRowY = 50;
    private const int NativeRowY  = 78;

    private Territory? _territory;

    // Floating text feedback
    private string _feedbackText  = "";
    private float  _feedbackAlpha = 0f;
    private float  _feedbackY     = -50f;
    private Color  _feedbackColor = new(1f, 1f, 0.4f);

    // Cached invader rects for click hit-testing (populated in _Draw)
    private readonly List<(Rect2 rect, string invaderId)> _invaderRects = new();

    public override void _Ready()
    {
        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        _territory = bridge.State.GetTerritory(Name);

        bridge.CorruptionChanged         += (id, _, _) => { if (id == Name) QueueRedraw(); };
        bridge.InvaderArrived            += (_, id, _) => { if (id == Name) QueueRedraw(); };
        bridge.InvaderDefeated           += _           => QueueRedraw();
        bridge.InvaderAdvanced           += (_, f, t)   => { if (f == Name || t == Name) QueueRedraw(); };
        bridge.TargetingModeChanged      += _           => QueueRedraw();
        bridge.CounterAttackPendingGodot += (tid, _)   => { if (tid == Name || Name == CounterAttackTerritory) QueueRedraw(); };
        bridge.CardPlayFeedback          += OnCardPlayFeedback;

        SetProcess(false);
    }

    private string? CounterAttackTerritory => GameBridge.Instance?.CounterAttackTerritory;

    // ── Floating text ─────────────────────────────────────────────────────────

    private void OnCardPlayFeedback(string message, string targetTerritoryId, int category)
    {
        bool showHere = targetTerritoryId == Name
                     || (targetTerritoryId.Length == 0 && Name == "I1");
        if (!showHere) return;

        _feedbackText  = message;
        _feedbackAlpha = 1f;
        _feedbackY     = -50f;
        _feedbackColor = category switch
        {
            1 => new Color(0.5f, 0.7f, 1.0f),
            2 => Colors.White,
            3 => new Color(1f, 0.8f, 0.2f),
            _ => new Color(1f, 1f, 0.4f),
        };
        SetProcess(true);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _feedbackAlpha -= (float)(delta / 1.5);
        _feedbackY     -= (float)(delta * 18.0);
        QueueRedraw();

        if (_feedbackAlpha <= 0f)
        {
            _feedbackAlpha = 0f;
            SetProcess(false);
        }
    }

    // ── Click handling ─────────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mb) return;

        var bridge   = GameBridge.Instance;
        if (bridge == null) return;

        var localPos = ToLocal(mb.GlobalPosition);

        // ── Counter-attack invader click ──
        if (bridge.IsWaitingForCounterAttack && bridge.CounterAttackTerritory == Name)
        {
            foreach (var (rect, invId) in _invaderRects)
            {
                if (rect.HasPoint(localPos))
                {
                    CounterAttackController.Instance?.AssignDamage(invId, 1);
                    QueueRedraw();
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }
        }

        // ── Territory targeting click ──
        if (!bridge.IsWaitingForTarget || bridge.PendingEffect == null) return;
        if (!TerritoryRect.HasPoint(localPos)) return;

        var validTargets = TargetValidator.GetValidTargets(bridge.State, bridge.PendingEffect.Range);
        if (!validTargets.Contains(Name.ToString())) return;

        bridge.CompleteTargetedPlay(Name);
        GetViewport().SetInputAsHandled();
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        _invaderRects.Clear();

        var t = _territory;
        if (t == null) return;

        // Territory background
        DrawRect(TerritoryRect, CorruptionColor(t.CorruptionLevel));

        // Targeting / counter-attack overlay on territory box
        var bridge = GameBridge.Instance;
        if (bridge?.IsWaitingForTarget == true && bridge.PendingEffect != null)
        {
            var valid = TargetValidator.GetValidTargets(bridge.State, bridge.PendingEffect.Range);
            if (valid.Contains(Name.ToString()))
                DrawRect(TerritoryRect, Colors.Yellow, filled: false, width: 3f);
            else
                DrawRect(TerritoryRect, new Color(0, 0, 0, 0.55f));
        }
        else
        {
            DrawRect(TerritoryRect, Colors.White, filled: false, width: 2f);
        }

        // Territory text
        var font = ThemeDB.FallbackFont;
        const int fs = 12;

        DrawString(font, new Vector2(-58, -26), t.Id,
            HorizontalAlignment.Left, 120, fs + 3, Colors.White);
        DrawString(font, new Vector2(-58, -10), $"Presence: {t.PresenceCount}",
            HorizontalAlignment.Left, 120, fs, Colors.Cyan);
        DrawString(font, new Vector2(-58,   4), $"Invaders: {t.Invaders.Count(i => i.IsAlive)}",
            HorizontalAlignment.Left, 120, fs, new Color(1, 0.5f, 0.5f));
        DrawString(font, new Vector2(-58,  18), BuildNativeText(t),
            HorizontalAlignment.Left, 120, fs, new Color(0.5f, 1, 0.5f));
        DrawString(font, new Vector2(-58,  32), $"Corrupt:  {t.CorruptionPoints} (L{t.CorruptionLevel})",
            HorizontalAlignment.Left, 120, fs, new Color(1, 0.8f, 0.2f));

        // Unit squares below territory box
        bool isCounterTarget = bridge?.IsWaitingForCounterAttack == true
                            && bridge.CounterAttackTerritory == Name;
        var assignments = isCounterTarget
            ? CounterAttackController.Instance?.Assignments
            : null;

        DrawInvaderSquares(t, font, isCounterTarget, assignments);
        DrawNativeSquares(t, font);

        // Counter-attack pool label
        if (isCounterTarget && bridge != null)
        {
            int assigned  = assignments?.Values.Sum() ?? 0;
            int remaining = bridge.CounterAttackPool - assigned;
            DrawString(font, new Vector2(-60, InvaderRowY - 6),
                $"Pool: {bridge.CounterAttackPool}  Rem: {remaining}",
                HorizontalAlignment.Left, 124, 10, Colors.Yellow);
        }

        // Floating feedback
        if (_feedbackAlpha > 0f)
        {
            var fc = _feedbackColor;
            DrawString(font, new Vector2(-30, _feedbackY), _feedbackText,
                HorizontalAlignment.Left, 120, fs, new Color(fc.R, fc.G, fc.B, _feedbackAlpha));
        }
    }

    private void DrawInvaderSquares(Territory t, Font font, bool isCounterTarget,
        IReadOnlyDictionary<string, int>? assignments)
    {
        var alive = t.Invaders.Where(i => i.IsAlive).ToList();
        if (alive.Count == 0) return;

        for (int idx = 0; idx < Math.Min(alive.Count, MaxPerRow * 2); idx++)
        {
            int col    = idx % MaxPerRow;
            int row    = idx / MaxPerRow;
            float x    = -60f + col * SqStep;
            float y    = InvaderRowY + row * (SqSize + 6);
            var invader = alive[idx];
            var rect   = new Rect2(x, y, SqSize, SqSize);

            _invaderRects.Add((rect, invader.Id));

            float hpFrac = invader.MaxHp > 0 ? (float)invader.Hp / invader.MaxHp : 1f;
            var bg = InvaderColor(invader.UnitType, hpFrac);
            DrawRect(rect, bg);

            if (isCounterTarget)
                DrawRect(rect, Colors.Yellow, filled: false, width: 1.5f);
            else
                DrawRect(rect, new Color(0.8f, 0.8f, 0.8f, 0.4f), filled: false, width: 1f);

            // Unit type initial
            DrawString(font, new Vector2(x + 2, y + 12),
                UnitInitial(invader.UnitType),
                HorizontalAlignment.Left, SqSize, 11, Colors.White);

            // HP text
            DrawString(font, new Vector2(x + 1, y + SqSize - 1),
                $"{invader.Hp}/{invader.MaxHp}",
                HorizontalAlignment.Left, SqSize, 8, new Color(1f, 1f, 1f, 0.85f));

            // Assigned damage overlay (red number)
            if (assignments != null && assignments.TryGetValue(invader.Id, out int dmg) && dmg > 0)
            {
                DrawString(font, new Vector2(x + SqSize - 10, y + 10),
                    $"-{dmg}", HorizontalAlignment.Left, 20, 10, Colors.Red);
            }
        }
    }

    private void DrawNativeSquares(Territory t, Font font)
    {
        var alive = t.Natives.Where(n => n.IsAlive).ToList();
        if (alive.Count == 0) return;

        // Offset native row below invader rows
        int invaderRows = alive.Count > 0
            ? (int)Math.Ceiling(t.Invaders.Count(i => i.IsAlive) / (double)MaxPerRow)
            : 0;
        float baseY = InvaderRowY + invaderRows * (SqSize + 6) + (invaderRows > 0 ? 4 : 0);
        if (t.Invaders.Count(i => i.IsAlive) == 0) baseY = NativeRowY;

        for (int idx = 0; idx < Math.Min(alive.Count, MaxPerRow); idx++)
        {
            var native = alive[idx];
            float x  = -60f + idx * SqStep;
            var rect = new Rect2(x, baseY, SqSize, SqSize);

            float hpFrac = native.MaxHp > 0 ? (float)native.Hp / native.MaxHp : 1f;
            var bg = NativeColor(hpFrac);
            DrawRect(rect, bg);
            DrawRect(rect, new Color(0.3f, 0.9f, 0.3f, 0.5f), filled: false, width: 1f);

            DrawString(font, new Vector2(x + 4, baseY + 12),
                "N", HorizontalAlignment.Left, SqSize, 11, Colors.White);
            DrawString(font, new Vector2(x + 1, baseY + SqSize - 1),
                $"{native.Hp}/{native.MaxHp}",
                HorizontalAlignment.Left, SqSize, 8, new Color(1f, 1f, 1f, 0.85f));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildNativeText(Territory t)
    {
        var alive = t.Natives.Where(n => n.IsAlive).ToList();
        if (alive.Count == 0) return "Natives:  0";
        bool anyDamaged = alive.Any(n => n.Hp < n.MaxHp);
        if (!anyDamaged) return $"Natives:  {alive.Count}";
        string hpList = string.Join(" ", alive.Select(n => $"{n.Hp}/{n.MaxHp}"));
        return $"N: {hpList}";
    }

    private static string UnitInitial(UnitType u) => u switch
    {
        UnitType.Marcher  => "M",
        UnitType.Ironclad => "I",
        UnitType.Outrider => "O",
        UnitType.Pioneer  => "P",
        _                 => "?"
    };

    private static Color InvaderColor(UnitType u, float hpFrac)
    {
        // Base hue varies by type; brightness scales with HP
        var base_ = u switch
        {
            UnitType.Marcher  => new Color(0.85f, 0.30f, 0.15f),
            UnitType.Ironclad => new Color(0.60f, 0.20f, 0.55f),
            UnitType.Outrider => new Color(0.90f, 0.55f, 0.10f),
            UnitType.Pioneer  => new Color(0.65f, 0.40f, 0.10f),
            _                 => new Color(0.6f,  0.2f,  0.2f)
        };
        float dim = 0.4f + 0.6f * hpFrac;
        return new Color(base_.R * dim, base_.G * dim, base_.B * dim);
    }

    private static Color NativeColor(float hpFrac)
    {
        float dim = 0.35f + 0.65f * hpFrac;
        return new Color(0.15f * dim, 0.70f * dim, 0.20f * dim);
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
