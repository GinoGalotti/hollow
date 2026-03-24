using Godot;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Localization;
using HollowWardens.Core.Models;

/// <summary>
/// Shows all 6 element counters with threshold markers at 4, 7, 11.
/// Each element row has T1, T2, T3 buttons. T1 lights up gold when a T1 threshold
/// is pending; T2/T3 become visible (full opacity) after the previous tier has fired.
/// </summary>
public partial class ElementTrackerController : VBoxContainer
{
    private readonly Label[]   _barLabels   = new Label[6];
    private readonly Button[,] _tierButtons = new Button[6, 3]; // [element, tier-1]
    private readonly bool[,]   _tierReached = new bool[6, 3];   // tracks first-ever fire per tier
    private readonly bool[,]   _tierPending = new bool[6, 3];   // currently awaiting player resolve
    private bool   _pulseState;
    private double _pulseTimer;

    public override void _Ready()
    {
        const string IconBase = "res://godot/assets/art/kenney_board-game-icons/PNG/Default (64px)/";
        Texture2D?[] elemIcons =
        {
            GD.Load<Texture2D>(IconBase + "resource_wood.png"), // Root
            GD.Load<Texture2D>(IconBase + "flask_half.png"),    // Mist
            GD.Load<Texture2D>(IconBase + "skull.png"),         // Shadow
            GD.Load<Texture2D>(IconBase + "fire.png"),          // Ash
            GD.Load<Texture2D>(IconBase + "arrow_right.png"),   // Gale
            GD.Load<Texture2D>(IconBase + "hexagon_outline.png"),// Void
        };

        var cinzel = GD.Load<Font>("res://godot/assets/fonts/Cinzel-Bold.ttf");

        var header = new Label { Text = Loc.Get("LABEL_ELEMENTS"), Modulate = Colors.Yellow };
        if (cinzel != null) header.AddThemeFontOverride("font", cinzel);
        AddChild(header);

        for (int i = 0; i < 6; i++)
        {
            var e   = (Element)i;
            var row = new HBoxContainer();

            // Element icon
            if (elemIcons[i] != null)
                row.AddChild(new TextureRect
                {
                    Texture           = elemIcons[i],
                    CustomMinimumSize = new Vector2(16, 16),
                    StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered,
                    ExpandMode        = TextureRect.ExpandModeEnum.FitWidthProportional,
                });

            row.AddChild(new Label
            {
                Text              = e.ToString()[..3],
                CustomMinimumSize = new Vector2(26, 0)
            });

            _barLabels[i] = new Label { Text = "0  [----:----:----]" };
            row.AddChild(_barLabels[i]);

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
                        ? new Color(1f,  0.85f, 0.1f)   // gold
                        : new Color(1f,  0.96f, 0.65f);  // pale gold
    }

    private void OnElementChanged(int element, int value) => UpdateRow((Element)element);
    private void OnThresholdTriggered(int element, int tier) => UpdateRow((Element)element);
    private void Refresh() { for (int i = 0; i < 6; i++) UpdateRow((Element)i); }

    private void OnThresholdPending(int element, int tier, string description)
    {
        if (tier < 1 || tier > 3) return;
        int t = tier - 1;
        _tierReached[element, t]  = true;
        _tierPending[element, t]  = true;
        _tierButtons[element, t].Disabled    = false;
        _tierButtons[element, t].TooltipText = description;
        _tierButtons[element, t].Modulate    = new Color(1f, 0.85f, 0.1f); // gold

        // Make the next tier button fully visible now that this tier has fired
        if (t + 1 < 3)
            _tierButtons[element, t + 1].Modulate = Colors.White;
    }

    private void OnThresholdExpiredOrResolved(int element, int tier)
    {
        if (tier < 1 || tier > 3) return;
        int t = tier - 1;
        _tierPending[element, t]  = false;
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

        char[] bar = new char[13];
        for (int b = 0; b < 13; b++)
            bar[b] = b < val ? '█' : '·';

        if (bar.Length > 4)  bar[4]  = bar[4]  == '█' ? '|' : '┆';
        if (bar.Length > 7)  bar[7]  = bar[7]  == '█' ? '|' : '┆';
        if (bar.Length > 11) bar[11] = bar[11] == '█' ? '|' : '┆';

        _barLabels[idx].Text    = $"{val,2}  [{new string(bar)}]";
        _barLabels[idx].Modulate = val >= 4 ? Colors.LightGreen : Colors.White;
    }
}
