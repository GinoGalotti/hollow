using Godot;
using HollowWardens.Core.Models;

/// <summary>
/// Full-screen semi-transparent overlay showing a scrolling log of game events.
/// Toggle with the D key (handled in InputHandler).
/// Colors: yellow = player action, red = invader, green = fear, cyan = element, white = system.
/// </summary>
public partial class DebugLogController : Control
{
    public static DebugLogController? Instance { get; private set; }

    private VBoxContainer    _logBox          = null!;
    private ScrollContainer  _scroll          = null!;
    private LineEdit         _importEdit      = null!;
    private Label            _copyStatusLabel = null!;
    private int              _entryCount;
    private const int        MaxEntries = 200;

    private static readonly Color ColPlayer  = new(1f,    1f,    0.4f);  // yellow — player action
    private static readonly Color ColInvader = new(1f,    0.45f, 0.45f); // red    — invader event
    private static readonly Color ColFear    = new(0.45f, 1f,    0.45f); // green  — fear / dread
    private static readonly Color ColElement = new(0.45f, 1f,    1f);    // cyan   — element / threshold
    private static readonly Color ColSystem  = Colors.White;              // white  — system events

    public override void _Ready()
    {
        Instance = this;
        Visible  = false;

        // Full-screen anchor; individual child positioning relative to this node
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        // Semi-transparent background
        var bg = new ColorRect
        {
            Color       = new Color(0f, 0f, 0f, 0.82f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Header label (top-right corner)
        var header = new Label
        {
            Text        = "Debug Log  [D] to close",
            Modulate    = new Color(0.6f, 0.6f, 0.6f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        header.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        header.OffsetLeft  = -260;
        header.OffsetRight = -10;
        header.OffsetTop   = 6;
        AddChild(header);

        // Export / Import toolbar (top-left corner)
        var toolbar = new HBoxContainer();
        toolbar.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        toolbar.OffsetLeft   = 10;
        toolbar.OffsetTop    = 4;
        toolbar.OffsetRight  = 620;
        toolbar.OffsetBottom = 28;

        var copyBtn = new Button { Text = "Copy State" };
        copyBtn.Pressed += OnCopyState;
        toolbar.AddChild(copyBtn);

        _importEdit = new LineEdit
        {
            PlaceholderText   = "Paste saved state here…",
            CustomMinimumSize = new Vector2(220, 0),
        };
        toolbar.AddChild(_importEdit);

        var loadBtn = new Button { Text = "Load" };
        loadBtn.Pressed += OnLoadState;
        toolbar.AddChild(loadBtn);

        _copyStatusLabel = new Label { Text = "" };
        toolbar.AddChild(_copyStatusLabel);

        AddChild(toolbar);

        // Scrollable log area
        _scroll = new ScrollContainer
        {
            MouseFilter       = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(400, 200),
        };
        _scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _scroll.OffsetLeft   = 10;
        _scroll.OffsetTop    = 30;
        _scroll.OffsetRight  = -10;
        _scroll.OffsetBottom = -10;
        AddChild(_scroll);

        _logBox = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _logBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _scroll.AddChild(_logBox);

        SubscribeToEvents();
    }

    public override void _ExitTree()
    {
        Instance = null;
    }

    public void Toggle() => Visible = !Visible;

    // ── Export / Import ───────────────────────────────────────────────────────

    private void OnCopyState()
    {
        var data = GameBridge.Instance?.ExportEncounterState();
        if (data == null || data.Length == 0)
        {
            _copyStatusLabel.Text = "Nothing to export";
            return;
        }
        DisplayServer.ClipboardSet(data);
        _copyStatusLabel.Text = "Copied!";
        GetTree().CreateTimer(2.0).Timeout += () => _copyStatusLabel.Text = "";
    }

    private void OnLoadState()
    {
        var text = _importEdit.Text.Trim();
        if (text.Length == 0) return;
        GameBridge.Instance?.ImportAndReplay(text);
        _importEdit.Text      = "";
        _copyStatusLabel.Text = "State loaded";
        GetTree().CreateTimer(2.0).Timeout += () => _copyStatusLabel.Text = "";
    }

    // ── Event subscriptions ───────────────────────────────────────────────────

    private void SubscribeToEvents()
    {
        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.TurnStarted           += () => AddEntry("── Turn Started ──", ColSystem);
        bridge.TurnEnded             += () => AddEntry("── Turn Ended ──", ColSystem);
        bridge.PhaseChanged          += p => AddEntry($"Phase → {(TurnPhase)p}", ColSystem);
        bridge.ResolutionTurnStarted += n => AddEntry($"Resolution turn {n}", ColSystem);

        bridge.CardPlayFeedback += (msg, _, cat) =>
        {
            Color color = cat switch
            {
                1 => new Color(0.5f, 0.7f, 1f),    // blue-ish — Dusk
                2 => ColSystem,                      // white    — Resolution
                3 => new Color(1f, 0.8f, 0.2f),     // gold     — Threshold
                _ => ColPlayer                       // yellow   — Vigil
            };
            AddEntry($"▶ {msg}", color);
        };

        bridge.FearGenerated         += n    => AddEntry($"+{n} Fear generated", ColFear);
        bridge.FearActionQueued      += ()   => AddEntry("Fear action queued", ColFear);
        bridge.FearActionRevealed    += desc => AddEntry($"Fear action: {desc}", ColFear);
        bridge.DreadAdvanced         += lvl  => AddEntry($"Dread → level {lvl}", ColFear);

        bridge.InvaderArrived  += (id, tid, ut) => AddEntry($"Arrived: {(UnitType)ut} @ {tid}", ColInvader);
        bridge.InvaderDefeated += id             => AddEntry($"Defeated: {id}", ColInvader);
        bridge.InvaderAdvanced += (id, f, t)     => AddEntry($"Advance: {id}  {f}→{t}", ColInvader);
        bridge.CorruptionChanged += (tid, pts, lvl) => AddEntry($"Corrupt {tid}: {pts}pt L{lvl}", ColInvader);
        bridge.HeartDamageDealt += tid            => AddEntry($"Heart hit! ({tid})", ColInvader);
        bridge.CounterAttackReady += (tid, pool)  => AddEntry($"Counter-attack {tid} pool={pool}", ColSystem);

        bridge.ElementChanged     += (e, v) => AddEntry($"{(Element)e} → {v}", ColElement);
        bridge.ThresholdTriggered += (e, t) => AddEntry($"★ {(Element)e} T{t} triggered!", ColElement);
        bridge.ThresholdPending   += (e, t, d) => AddEntry($"★ {d} — awaiting player", ColElement);
        bridge.ThresholdExpired   += (e, t)    => AddEntry($"★ {(Element)e} T{t} — expired", ColElement);
        bridge.ThresholdResolved  += (e, t, d) => AddEntry($"★ {d} — resolved", ColElement);
        bridge.ElementsDecayed    += ()        => AddEntry("Elements decayed", ColSystem);

        bridge.WeaveChanged           += v    => AddEntry($"Weave → {v}", ColSystem);
        bridge.ActionCardRevealed     += (n, p) => AddEntry($"Action: {n} [{(p ? "PAINFUL" : "easy")}]", ColSystem);
    }

    // ── Log management ────────────────────────────────────────────────────────

    private void AddEntry(string text, Color color)
    {
        var label = new Label
        {
            Text        = text,
            Modulate    = color,
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _logBox.AddChild(label);
        _entryCount++;

        // Trim oldest entries over the cap
        if (_entryCount > MaxEntries)
        {
            var oldest = _logBox.GetChild(0);
            oldest.QueueFree();
            _entryCount--;
        }

        // Auto-scroll to bottom after layout is updated
        CallDeferred(MethodName.ScrollToBottom);
    }

    private void ScrollToBottom()
    {
        _scroll.ScrollVertical = int.MaxValue; // Godot clamps to actual max
    }
}
