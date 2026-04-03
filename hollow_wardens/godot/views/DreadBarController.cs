using Godot;
using HollowWardens.Core.Localization;
using System;

/// <summary>
/// Displays Weave (20-segment bar), Dread (4 skull icons), and Fear progress (thin bar).
/// Weave segments pulse red when ≤5 remain.
/// </summary>
public partial class DreadBarController : VBoxContainer
{
    private readonly ColorRect[]  _weaveSegments    = new ColorRect[20];
    private readonly TextureRect[] _skullRects       = new TextureRect[4];
    private HBoxContainer _segmentRow = null!;
    private ColorRect     _fearFill   = null!;
    private Label         _weaveLabel = null!;
    private Label         _fearLabel  = null!;

    private bool   _needsPulse;
    private double _pulseTimer;
    private float  _displayedWeave = -1f;

    public override void _Ready()
    {
        var cinzel    = FontCache.CinzelBold;
        var skullIcon = LoadIcon("res://godot/assets/art/kenney_board-game-icons/PNG/Default (64px)/skull.png");

        // ── WEAVE section ─────────────────────────────────────────────────────
        var weaveHeader = new Label { Text = "WEAVE", Modulate = new Color(0.4f, 0.8f, 1.0f) };
        if (cinzel != null) weaveHeader.AddThemeFontOverride("font", cinzel);
        weaveHeader.AddThemeFontSizeOverride("font_size", 13);
        AddChild(weaveHeader);

        // 20 segments + "X/20" label in one row
        var weaveRow = new HBoxContainer();
        weaveRow.AddThemeConstantOverride("separation", 4);

        _segmentRow = new HBoxContainer();
        _segmentRow.AddThemeConstantOverride("separation", 1);
        for (int i = 0; i < 20; i++)
        {
            _weaveSegments[i] = new ColorRect
            {
                Color               = new Color(0.2f, 0.6f, 0.9f),
                CustomMinimumSize   = new Vector2(10, 14),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            };
            _segmentRow.AddChild(_weaveSegments[i]);
        }
        weaveRow.AddChild(_segmentRow);

        _weaveLabel = new Label
        {
            Text              = "20/20",
            VerticalAlignment = VerticalAlignment.Center,
            Modulate          = new Color(0.4f, 0.8f, 1.0f),
        };
        weaveRow.AddChild(_weaveLabel);
        AddChild(weaveRow);

        AddChild(new HSeparator());

        // ── DREAD section ─────────────────────────────────────────────────────
        var dreadRow = new HBoxContainer();
        dreadRow.AddThemeConstantOverride("separation", 6);

        var dreadLbl = new Label { Text = "Dread", VerticalAlignment = VerticalAlignment.Center };
        if (cinzel != null) dreadLbl.AddThemeFontOverride("font", cinzel);
        dreadLbl.AddThemeFontSizeOverride("font_size", 12);
        dreadRow.AddChild(dreadLbl);

        for (int i = 0; i < 4; i++)
        {
            _skullRects[i] = new TextureRect
            {
                Texture           = skullIcon,
                CustomMinimumSize = new Vector2(16, 16),
                StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode        = TextureRect.ExpandModeEnum.FitWidthProportional,
                Modulate          = new Color(0.3f, 0.3f, 0.3f), // unlit default
            };
            dreadRow.AddChild(_skullRects[i]);
        }
        AddChild(dreadRow);

        // ── FEAR section ──────────────────────────────────────────────────────
        _fearLabel = new Label { Text = "Fear: 0 (next: 15)" };
        _fearLabel.AddThemeFontSizeOverride("font_size", 11);
        AddChild(_fearLabel);

        // Thin fear progress bar (160×6)
        var fearOuter = new Control
        {
            CustomMinimumSize   = new Vector2(160, 6),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            ClipChildren        = ClipChildrenMode.Only,
        };
        var fearBg = new ColorRect { Color = new Color(0.10f, 0.06f, 0.14f) };
        fearBg.AnchorRight  = 1f;
        fearBg.AnchorBottom = 1f;
        fearOuter.AddChild(fearBg);

        _fearFill = new ColorRect { Color = new Color(0.5f, 0.2f, 0.7f) };
        _fearFill.AnchorTop    = 0f;
        _fearFill.AnchorBottom = 1f;
        _fearFill.OffsetRight  = 0f;
        fearOuter.AddChild(_fearFill);
        AddChild(fearOuter);

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.EncounterReady += Refresh;
        bridge.DreadAdvanced  += _ => Refresh();
        bridge.FearGenerated  += _ => Refresh();
        bridge.WeaveChanged   += _ => Refresh();

        SetProcess(false);
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (!_needsPulse)
        {
            _segmentRow.SelfModulate = Colors.White;
            SetProcess(false);
            return;
        }
        _pulseTimer += delta;
        float alpha = 0.6f + 0.4f * (float)((Math.Sin(_pulseTimer * Math.PI * 2.5) + 1.0) / 2.0);
        _segmentRow.SelfModulate = new Color(1f, 1f, 1f, alpha);
    }

    private void Refresh()
    {
        var bridge = GameBridge.Instance;
        if (bridge?.State == null) return;

        int level = bridge.State.Dread?.DreadLevel ?? 1;
        int total = bridge.State.Dread?.TotalFearGenerated ?? 0;
        int weave = bridge.State.Weave?.CurrentWeave ?? 0;

        // ── Weave segments ────────────────────────────────────────────────────
        bool critical = weave <= 5;
        bool low      = weave > 5 && weave <= 10;
        var fillColor = critical ? new Color(0.9f, 0.2f, 0.2f)
                      : low      ? new Color(0.9f, 0.7f, 0.1f)
                      :            new Color(0.2f, 0.6f, 0.9f);
        var emptyColor = new Color(0.15f, 0.15f, 0.15f);
        int filled = Math.Min(weave, 20);

        for (int i = 0; i < 20; i++)
            _weaveSegments[i].Color = i < filled ? fillColor : emptyColor;

        if (_displayedWeave < 0) _displayedWeave = weave;
        if (Math.Abs(_displayedWeave - weave) > 0.5f)
        {
            var wt = CreateTween();
            wt.TweenMethod(Callable.From<float>(SetWeaveCounterText), _displayedWeave, (float)weave, 0.25f);
            _displayedWeave = weave;
        }
        else
        {
            _weaveLabel.Text = $"{weave}/20";
        }
        _weaveLabel.Modulate = critical ? Colors.Red : new Color(0.4f, 0.8f, 1.0f);

        bool wasPulsing = _needsPulse;
        _needsPulse = critical;
        if (_needsPulse && !wasPulsing) SetProcess(true);
        if (!_needsPulse) _segmentRow.SelfModulate = Colors.White;

        // ── Dread skulls ──────────────────────────────────────────────────────
        var litColor = new Color(0.9f, 0.5f, 0.1f);
        var dimColor = new Color(0.3f, 0.3f, 0.3f);
        for (int i = 0; i < 4; i++)
            _skullRects[i].Modulate = i < level ? litColor : dimColor;

        // ── Fear progress ─────────────────────────────────────────────────────
        int next = NextThreshold(total);
        if (next > 0)
        {
            int prev     = next - 15;
            float frac       = Math.Clamp((float)(total - prev) / 15f, 0f, 1f);
            float targetFill = frac * 160f;
            if (Math.Abs(_fearFill.OffsetRight - targetFill) > 2f)
            {
                var ft = CreateTween();
                ft.TweenProperty(_fearFill, "offset_right", targetFill, 0.25f);
            }
            else
            {
                _fearFill.OffsetRight = targetFill;
            }
            _fearLabel.Text = $"Fear: {total} (next: {next})";
        }
        else
        {
            if (Math.Abs(_fearFill.OffsetRight - 160f) > 2f)
            {
                var ft = CreateTween();
                ft.TweenProperty(_fearFill, "offset_right", 160f, 0.25f);
            }
            else
            {
                _fearFill.OffsetRight = 160f;
            }
            _fearLabel.Text = $"Fear: {total} (max)";
        }
    }

    private void SetWeaveCounterText(float v)
        => _weaveLabel.Text = $"{(int)Math.Round(v)}/20";

    private static int NextThreshold(int fear) =>
        fear < 15 ? 15 : fear < 30 ? 30 : fear < 45 ? 45 : -1;

    private static Texture2D? LoadIcon(string path)
    {
        try { return GD.Load<Texture2D>(path); }
        catch { return null; }
    }
}
