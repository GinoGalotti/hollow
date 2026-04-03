using Godot;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Localization;
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

    // Background color for smooth corruption lerp
    private Color _bgColor       = new(0.15f, 0.40f, 0.15f);
    private Color _bgColorTarget = new(0.15f, 0.40f, 0.15f);
    private bool  _isLerpingBg;

    // Fonts — loaded in _Ready()
    private Font? _cinzelFont;
    private Font? _imFellFont;

    // Terrain icons — loaded in _Ready()
    private readonly Dictionary<TerrainType, Texture2D?> _terrainIcons = new();

    public override void _Ready()
    {
        // Upper rows must render on top of lower rows so unit squares below the tile
        // are not hidden behind adjacent territory boxes in the next row.
        ZIndex = Name.ToString() switch
        {
            "I1"         => 2,
            "M1" or "M2" => 1,
            _            => 0,  // A1, A2, A3
        };

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        if (bridge.State != null)
            _territory = bridge.State.GetTerritory(Name);

        bridge.EncounterReady            += OnEncounterReady;
        bridge.CorruptionChanged         += (id, _, _) =>
        {
            if (id != Name) return;
            _territory = GameBridge.Instance?.State?.GetTerritory(Name);
            if (_territory != null)
            {
                _bgColorTarget = CorruptionColor(_territory.CorruptionLevel);
                _isLerpingBg   = true;
                SetProcess(true);
            }
            QueueRedraw();
        };
        bridge.InvaderArrived            += (_, id, _) => { if (id == Name) QueueRedraw(); };
        bridge.InvaderDefeated           += _           => QueueRedraw();
        bridge.InvaderAdvanced           += (_, f, t)   => { if (f == Name || t == Name) QueueRedraw(); };
        bridge.TargetingModeChanged      += _           => QueueRedraw();
        bridge.CounterAttackPendingGodot += (tid, _)   => { if (tid == Name || Name == CounterAttackTerritory) QueueRedraw(); };
        bridge.CardPlayFeedback          += OnCardPlayFeedback;
        bridge.TerrainChanged            += (id, _) => { if (id == Name) QueueRedraw(); };

        _cinzelFont = FontCache.CinzelBold;
        _imFellFont = FontCache.IMFell;

        // Load terrain icons from Kenney board-game-icons pack
        const string iconBase = "res://godot/assets/art/kenney_board-game-icons/PNG/Default (64px)/";
        var iconMap = new Dictionary<TerrainType, string>
        {
            { TerrainType.Forest,   "resource_wood.png"    },
            { TerrainType.Mountain, "hexagon_outline.png"  },
            { TerrainType.Wetland,  "flask_half.png"       },
            { TerrainType.Sacred,   "award.png"            },
            { TerrainType.Scorched, "fire.png"             },
            { TerrainType.Blighted, "skull.png"            },
            { TerrainType.Ruins,    "hexagon_question.png" },
            { TerrainType.Fertile,  "resource_wheat.png"   },
        };
        foreach (var kvp in iconMap)
        {
            string path = iconBase + kvp.Value;
            _terrainIcons[kvp.Key] = ResourceLoader.Exists(path)
                ? ResourceLoader.Load<Texture2D>(path)
                : null;
        }

        SetProcess(false);
    }

    private void OnEncounterReady()
    {
        _territory = GameBridge.Instance?.State?.GetTerritory(Name);
        if (_territory != null)
            _bgColor = _bgColorTarget = CorruptionColor(_territory.CorruptionLevel);
        QueueRedraw();
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
        bool needsProcess = false;

        if (_feedbackAlpha > 0f)
        {
            _feedbackAlpha -= (float)(delta / 1.5);
            _feedbackY     -= (float)(delta * 18.0);
            if (_feedbackAlpha <= 0f) _feedbackAlpha = 0f;
            else needsProcess = true;
        }

        if (_isLerpingBg)
        {
            _bgColor = _bgColor.Lerp(_bgColorTarget, (float)(delta * 2.0));
            float dist = Math.Abs(_bgColor.R - _bgColorTarget.R)
                       + Math.Abs(_bgColor.G - _bgColorTarget.G)
                       + Math.Abs(_bgColor.B - _bgColorTarget.B);
            if (dist < 0.005f)
            {
                _bgColor     = _bgColorTarget;
                _isLerpingBg = false;
            }
            else needsProcess = true;
        }

        QueueRedraw();
        if (!needsProcess) SetProcess(false);
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

        // Territory background (smooth-lerps to target corruption color)
        DrawRect(TerritoryRect, _bgColor);

        // Targeting / counter-attack overlay on territory box
        var bridge = GameBridge.Instance;
        if (bridge?.IsWaitingForTarget == true && bridge.PendingEffect != null)
        {
            var valid = TargetValidator.GetValidTargets(bridge.State, bridge.PendingEffect.Range);
            if (valid.Contains(Name.ToString()))
            {
                DrawRect(TerritoryRect, new Color(1f, 1f, 0f, 0.15f));
                DrawRect(TerritoryRect, Colors.Yellow, filled: false, width: 4f);
            }
            else
                DrawRect(TerritoryRect, new Color(0, 0, 0, 0.55f));
        }
        else
        {
            DrawRect(TerritoryRect, Colors.White, filled: false, width: 2f);
        }

        // Corruption progress bar (bottom 8px of tile) — only shown when corruption exists
        if (t.CorruptionPoints > 0 || t.CorruptionLevel > 0)
        {
            float barWidth = 124f;
            float barX     = -62f;
            float barY     = 34f;
            DrawRect(new Rect2(barX, barY, barWidth, 8f), new Color(0.08f, 0.06f, 0.05f));
            float fill = Math.Clamp(t.CorruptionPoints / 4f, 0f, 1f);
            var fillColor = t.CorruptionLevel switch
            {
                0 => new Color(0.9f, 0.7f, 0.1f),
                1 => new Color(0.9f, 0.4f, 0.1f),
                _ => new Color(0.8f, 0.1f, 0.1f),
            };
            DrawRect(new Rect2(barX, barY, barWidth * fill, 8f), fillColor);
        }

        // Territory text
        var titleFont = _cinzelFont ?? ThemeDB.FallbackFont;
        var font      = _imFellFont ?? ThemeDB.FallbackFont;
        const int fs  = 12;

        DrawString(titleFont, new Vector2(-58, -26), t.Id,
            HorizontalAlignment.Left, 120, 14, Colors.White);
        DrawStatRow(-10f, $"{t.PresenceCount} pres", font, fs, Colors.Cyan);
        DrawStatRow(  4f, $"{t.Invaders.Count(i => i.IsAlive)} inv", font, fs, new Color(1, 0.5f, 0.5f));
        DrawStatRow( 18f, BuildNativeText(t),         font, fs, new Color(0.5f, 1, 0.5f));

        // Terrain type label
        if (t.Terrain != TerrainType.Plains)
        {
            string terrainKey   = "TERRAIN_" + t.Terrain.ToString().ToUpperInvariant();
            string terrainLabel = Loc.Has(terrainKey) ? Loc.Get(terrainKey) : t.Terrain.ToString();
            DrawStatRow(30f, $"[{terrainLabel}]", font, fs, TerrainColor(t.Terrain));
        }

        // Terrain icon in top-right corner
        if (t.Terrain != TerrainType.Plains && _terrainIcons.TryGetValue(t.Terrain, out var ticon) && ticon != null)
        {
            DrawTextureRect(ticon, new Rect2(40f, -41f, 20f, 20f), false);
        }

        // Unit squares below territory box
        bool isCounterTarget = bridge?.IsWaitingForCounterAttack == true
                            && bridge.CounterAttackTerritory == Name;
        var assignments = isCounterTarget
            ? CounterAttackController.Instance?.Assignments
            : null;

        DrawInvaderSquares(t, isCounterTarget, assignments);
        DrawNativeSquares(t);

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

    private void DrawInvaderSquares(Territory t, bool isCounterTarget,
        IReadOnlyDictionary<string, int>? assignments)
    {
        var alive = t.Invaders.Where(i => i.IsAlive).ToList();
        if (alive.Count == 0) return;

        var font = _imFellFont ?? ThemeDB.FallbackFont;
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

            // Unit type initial letter
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

    private void DrawNativeSquares(Territory t)
    {
        var font  = _imFellFont ?? ThemeDB.FallbackFont;
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

    private void DrawStatRow(float textY, string text, Font font, int fontSize, Color color)
    {
        DrawString(font, new Vector2(-58, textY), text,
            HorizontalAlignment.Left, 120, fontSize, color);
    }

    private static Color TerrainColor(TerrainType terrain) => terrain switch
    {
        TerrainType.Forest   => new Color(0.20f, 0.55f, 0.20f),
        TerrainType.Mountain => new Color(0.60f, 0.60f, 0.70f),
        TerrainType.Wetland  => new Color(0.20f, 0.55f, 0.65f),
        TerrainType.Sacred   => new Color(0.90f, 0.85f, 0.40f),
        TerrainType.Scorched => new Color(0.75f, 0.35f, 0.10f),
        TerrainType.Blighted => new Color(0.40f, 0.10f, 0.45f),
        TerrainType.Ruins    => new Color(0.55f, 0.50f, 0.45f),
        TerrainType.Fertile  => new Color(0.30f, 0.70f, 0.30f),
        _                    => Colors.White
    };
}
