using Godot;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Localization;
using HollowWardens.Core.Models;

/// <summary>
/// Shows all 6 element counters with graphical fill bars and threshold markers at 4, 7, 11.
/// Each row: icon | name | value | fill bar (120×12 with notch lines) | T1/T2/T3 buttons.
/// T buttons pulse gold when a threshold is pending.
/// </summary>
public partial class ElementTrackerController : VBoxContainer
{
    private readonly Label[]         _valLabels   = new Label[6];
    private readonly ColorRect[]     _fillRects   = new ColorRect[6];
    private readonly ColorRect[,]    _fillNotches = new ColorRect[6, 3];
    private readonly Button[,]       _tierButtons = new Button[6, 3];
    private readonly bool[,]         _tierReached = new bool[6, 3];
    private readonly bool[,]         _tierPending = new bool[6, 3];
    private readonly HBoxContainer[] _rows        = new HBoxContainer[6];
    private bool   _pulseState;
    private double _pulseTimer;

    private static readonly Color[] ElemColors =
    {
        new(0.2f, 0.7f, 0.2f),  // Root
        new(0.4f, 0.6f, 0.8f),  // Mist
        new(0.5f, 0.3f, 0.6f),  // Shadow
        new(0.8f, 0.3f, 0.1f),  // Ash
        new(0.6f, 0.8f, 0.3f),  // Gale
        new(0.3f, 0.3f, 0.4f),  // Void
    };

    private static readonly string[] IconFiles =
    {
        "resource_wood.png", "flask_half.png", "skull.png",
        "fire.png", "arrow_right.png", "hexagon_outline.png",
    };

    public override void _Ready()
    {
        const string IconBase = "res://godot/assets/art/kenney_board-game-icons/PNG/Default (64px)/";
        var cinzel = FontCache.CinzelBold;

        var header = new Label { Text = Loc.Get("LABEL_ELEMENTS"), Modulate = Colors.Yellow };
        if (cinzel != null) header.AddThemeFontOverride("font", cinzel);
        AddChild(header);

        for (int i = 0; i < 6; i++)
        {
            var e   = (Element)i;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 4);

            // Icon (20×20)
            var icon = LoadIcon(IconBase + IconFiles[i]);
            if (icon != null)
                row.AddChild(new TextureRect
                {
                    Texture           = icon,
                    CustomMinimumSize = new Vector2(20, 20),
                    StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered,
                    ExpandMode        = TextureRect.ExpandModeEnum.FitWidthProportional,
                });

            // Name (48px fixed)
            row.AddChild(new Label
            {
                Text              = e.ToString(),
                CustomMinimumSize = new Vector2(48, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });

            // Numeric value
            _valLabels[i] = new Label
            {
                Text                = "0",
                CustomMinimumSize   = new Vector2(18, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment   = VerticalAlignment.Center,
            };
            row.AddChild(_valLabels[i]);

            // Graphical fill bar
            var (barCtl, fill, notches) = BuildFillBar(i);
            _fillRects[i] = fill;
            for (int n = 0; n < 3; n++) _fillNotches[i, n] = notches[n];
            row.AddChild(barCtl);

            // T1/T2/T3 buttons
            int[] thresholdValues = { 4, 7, 11 };
            for (int t = 0; t < 3; t++)
            {
                int capturedTier = t + 1;
                int capturedElem = i;
                string desc    = ThresholdResolver.GetDescription(e, capturedTier);
                string tooltip = $"{desc}\nRequires: {thresholdValues[t]} {e}";
                var btn = new Button
                {
                    Text              = $"T{capturedTier}",
                    Disabled          = true,
                    CustomMinimumSize = new Vector2(30, 0),
                    Modulate          = t == 0 ? Colors.White : new Color(1f, 1f, 1f, 0.35f),
                    TooltipText       = tooltip,
                };
                btn.Pressed += () =>
                {
                    GD.Print($"[Threshold] {(Element)capturedElem} T{capturedTier} pressed");
                    GameBridge.Instance?.ResolveThreshold(capturedElem, capturedTier);
                };
                _tierButtons[i, t] = btn;
                row.AddChild(btn);
            }

            _rows[i] = row;
            AddChild(row);
        }

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.EncounterReady     += Refresh;
        bridge.ElementChanged     += OnElementChanged;
        bridge.ThresholdTriggered += OnThresholdTriggered;
        bridge.ElementsDecayed    += Refresh;
        bridge.TurnStarted        += Refresh;

        bridge.ThresholdPending  += OnThresholdPending;
        bridge.ThresholdExpired  += OnThresholdExpiredOrResolved;
        bridge.ThresholdResolved += (e, t, _) => OnThresholdExpiredOrResolved(e, t);

        SetProcess(true);
        Refresh();
    }

    public override void _Process(double delta)
    {
        _pulseTimer += delta;
        if (_pulseTimer < 0.45) return;
        _pulseTimer  = 0;
        _pulseState  = !_pulseState;

        for (int i = 0; i < 6; i++)
            for (int t = 0; t < 3; t++)
                if (_tierPending[i, t])
                    _tierButtons[i, t].Modulate = _pulseState
                        ? new Color(1f, 0.85f, 0.1f)
                        : new Color(1f, 0.96f, 0.65f);
    }

    // ── Fill bar construction ─────────────────────────────────────────────────

    private static (Control bar, ColorRect fill, ColorRect[] notches) BuildFillBar(int elemIdx)
    {
        var bar = new Control
        {
            CustomMinimumSize   = new Vector2(120, 12),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter,
            ClipChildren        = ClipChildrenMode.Only,
        };

        // Dark gray background fills the entire bar
        var bg = new ColorRect { Color = new Color(0.15f, 0.15f, 0.15f) };
        bg.AnchorRight  = 1f;
        bg.AnchorBottom = 1f;
        bar.AddChild(bg);

        // Colored fill grows from left edge
        var fill = new ColorRect { Color = ElemColors[elemIdx] };
        fill.AnchorTop    = 0f;
        fill.AnchorBottom = 1f;
        fill.OffsetRight  = 0f; // updated in UpdateRow
        bar.AddChild(fill);

        // Notch lines at 4/13, 7/13, 11/13 positions
        float[] notchX  = { 4f / 13f * 120f, 7f / 13f * 120f, 11f / 13f * 120f };
        var notches = new ColorRect[3];
        for (int n = 0; n < 3; n++)
        {
            var notch = new ColorRect { Color = new Color(1f, 1f, 1f, 0.3f) };
            notch.AnchorTop    = 0f;
            notch.AnchorBottom = 1f;
            notch.OffsetLeft   = notchX[n];
            notch.OffsetRight  = notchX[n] + 1.5f;
            bar.AddChild(notch);
            notches[n] = notch;
        }

        return (bar, fill, notches);
    }

    // ── Signal handlers ───────────────────────────────────────────────────────

    private void OnElementChanged(int element, int value) => UpdateRow((Element)element);
    private void OnThresholdTriggered(int element, int tier) => UpdateRow((Element)element);
    private void Refresh() { for (int i = 0; i < 6; i++) UpdateRow((Element)i); }

    private void OnThresholdPending(int element, int tier, string description)
    {
        if (tier < 1 || tier > 3) return;
        int t = tier - 1;
        _tierReached[element, t] = true;
        _tierPending[element, t] = true;
        _tierButtons[element, t].Disabled    = false;
        _tierButtons[element, t].TooltipText = description;
        _tierButtons[element, t].Modulate    = new Color(1f, 0.85f, 0.1f);
        UpdateRow((Element)element); // refresh notch to gold
        if (t + 1 < 3)
            _tierButtons[element, t + 1].Modulate = Colors.White;

        // Flash the row gold to draw attention
        var tween = CreateTween();
        tween.TweenProperty(_rows[element], "self_modulate", new Color(1f, 0.85f, 0.1f), 0.05f);
        tween.TweenInterval(0.1f);
        tween.TweenProperty(_rows[element], "self_modulate", Colors.White, 0.2f);
    }

    private void OnThresholdExpiredOrResolved(int element, int tier)
    {
        if (tier < 1 || tier > 3) return;
        int t = tier - 1;
        _tierPending[element, t]          = false;
        _tierButtons[element, t].Disabled = true;
        _tierButtons[element, t].Modulate = _tierReached[element, t]
            ? Colors.White
            : new Color(1f, 1f, 1f, 0.35f);
    }

    private void UpdateRow(Element element)
    {
        var bridge = GameBridge.Instance;
        if (bridge?.State == null) return;

        int val = bridge.State.Elements?.Get(element) ?? 0;
        int idx = (int)element;

        _valLabels[idx].Text        = val.ToString();
        _fillRects[idx].OffsetRight = (float)(val / 13.0) * 120f;

        // Notch color: gold if that tier has been reached, white 30% otherwise
        for (int n = 0; n < 3; n++)
        {
            _fillNotches[idx, n].Color = _tierReached[idx, n]
                ? new Color(1f, 0.85f, 0.1f, 0.9f)
                : new Color(1f, 1f, 1f, 0.3f);
        }
    }

    private static Texture2D? LoadIcon(string path)
    {
        try { return GD.Load<Texture2D>(path); }
        catch { return null; }
    }
}
