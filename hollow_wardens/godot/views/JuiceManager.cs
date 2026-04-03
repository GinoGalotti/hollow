using Godot;
using HollowWardens.Core.Models;

/// <summary>
/// Screen-level juice effects: shake on weave damage, phase color washes, dread darken.
/// Subscribed to GameBridge signals; does NOT modify any view controller.
/// Registered as a child of Game in Game.tscn.
/// </summary>
public partial class JuiceManager : Node
{
    private Control? _rootLayout;
    private int      _prevWeave = -1;

    public override void _Ready()
    {
        _rootLayout = GetNodeOrNull<Control>("/root/Game/UI/RootLayout");

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.EncounterReady += () =>
        {
            _prevWeave = GameBridge.Instance?.State?.Weave?.CurrentWeave ?? -1;
        };
        bridge.WeaveChanged  += OnWeaveChanged;
        bridge.PhaseChanged  += OnPhaseChanged;
        bridge.DreadAdvanced += OnDreadAdvanced;
    }

    // ── Screen shake on weave damage ──────────────────────────────────────────

    private void OnWeaveChanged(int newWeave)
    {
        bool decreased = _prevWeave >= 0 && newWeave < _prevWeave;
        _prevWeave = newWeave;

        if (!decreased || _rootLayout == null) return;

        var tween = CreateTween();
        for (int i = 0; i < 3; i++)
        {
            float ox = GD.Randf() * 4f - 2f;
            float oy = GD.Randf() * 4f - 2f;
            tween.TweenProperty(_rootLayout, "position", new Vector2(ox, oy), 0.05);
        }
        tween.TweenProperty(_rootLayout, "position", Vector2.Zero, 0.1);
    }

    // ── Phase color washes ────────────────────────────────────────────────────

    private void OnPhaseChanged(int phaseInt)
    {
        var phase = (TurnPhase)phaseInt;
        Color? wash = phase switch
        {
            TurnPhase.Vigil => new Color(0.2f, 0.4f, 0.7f, 0.15f),
            TurnPhase.Tide  => new Color(0.7f, 0.2f, 0.2f, 0.15f),
            TurnPhase.Dusk  => new Color(0.7f, 0.5f, 0.2f, 0.10f),
            _               => null,
        };
        if (wash == null || _rootLayout == null) return;
        PlayOverlay(wash.Value, 0.3f, 0.3f, 0.3f);
    }

    // ── Dread advance darken ──────────────────────────────────────────────────

    private void OnDreadAdvanced(int _)
    {
        if (_rootLayout == null) return;
        PlayOverlay(new Color(0f, 0f, 0f, 0.3f), 0.3f, 0.5f, 0.5f);
    }

    // ── Shared overlay helper ─────────────────────────────────────────────────

    private void PlayOverlay(Color color, float fadeIn, float hold, float fadeOut)
    {
        var overlay = new ColorRect
        {
            MouseFilter  = Control.MouseFilterEnum.Ignore,
            Color        = new Color(color.R, color.G, color.B, 0f),
        };
        overlay.AnchorRight  = 1f;
        overlay.AnchorBottom = 1f;
        _rootLayout!.AddChild(overlay);

        var tween = overlay.CreateTween();
        tween.TweenProperty(overlay, "color", color, fadeIn);
        tween.TweenInterval(hold);
        tween.TweenProperty(overlay, "color", new Color(color.R, color.G, color.B, 0f), fadeOut);
        tween.TweenCallback(Callable.From(overlay.QueueFree));
    }
}
