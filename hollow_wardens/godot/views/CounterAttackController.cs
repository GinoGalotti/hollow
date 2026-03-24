using Godot;
using System.Collections.Generic;
using HollowWardens.Core.Localization;

/// <summary>
/// Interactive overlay for player-assigned native counter-attack damage.
/// Shows territory, pool, invader list; player clicks invaders to assign damage.
/// Confirm applies assignments. Skip assigns 0. Reset clears current assignments.
/// Subscribes to the new CounterAttackPendingGodot signal from GameBridge.
/// </summary>
public partial class CounterAttackController : PanelContainer
{
    public static CounterAttackController? Instance { get; private set; }
    public IReadOnlyDictionary<string, int> Assignments => _assignments;

    private Label        _headerLabel   = null!;
    private Label        _poolLabel     = null!;
    private VBoxContainer _invaderBox   = null!;
    private Label        _assignedLabel = null!;
    private Button       _confirmBtn    = null!;
    private Button       _skipBtn       = null!;
    private Button       _resetBtn      = null!;

    private readonly Dictionary<string, int> _assignments = new();
    private int _poolRemaining;
    private int _totalPool;

    public override void _Ready()
    {
        Instance = this;
        Visible = false;
        CustomMinimumSize = new Vector2(380, 200);
        SetAnchorsAndOffsetsPreset(LayoutPreset.Center);

        var vbox = new VBoxContainer();
        AddChild(vbox);

        _headerLabel = new Label { Modulate = new Color(1f, 0.7f, 0.2f) };
        vbox.AddChild(_headerLabel);

        _poolLabel = new Label();
        vbox.AddChild(_poolLabel);

        _invaderBox = new VBoxContainer();
        vbox.AddChild(_invaderBox);

        _assignedLabel = new Label();
        vbox.AddChild(_assignedLabel);

        var btnRow = new HBoxContainer();
        vbox.AddChild(btnRow);

        _confirmBtn = new Button { Text = Loc.Get("BTN_CONFIRM") };
        _skipBtn    = new Button { Text = Loc.Get("BTN_SKIP_DMG") };
        _resetBtn   = new Button { Text = Loc.Get("BTN_RESET") };
        _confirmBtn.Pressed += OnConfirm;
        _skipBtn.Pressed    += OnSkip;
        _resetBtn.Pressed   += OnReset;
        btnRow.AddChild(_confirmBtn);
        btnRow.AddChild(_skipBtn);
        btnRow.AddChild(_resetBtn);

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.CounterAttackPendingGodot += OnCounterAttackPending;
    }

    public override void _ExitTree() => Instance = null;

    private void OnCounterAttackPending(string territoryId, int pool)
    {
        _assignments.Clear();
        _totalPool     = pool;
        _poolRemaining = pool;

        _headerLabel.Text = Loc.Get("COUNTER_ATTACK_TITLE", territoryId);
        RefreshPoolLabel(pool);
        BuildInvaderButtons(territoryId, pool);
        Visible = true;
    }

    private void RefreshPoolLabel(int total)
    {
        int assigned = total - _poolRemaining;
        _poolLabel.Text    = Loc.Get("COUNTER_ATTACK_POOL", total, assigned, _poolRemaining);
        _assignedLabel.Text = _assignments.Count > 0
            ? "Assignments: " + string.Join(", ", _assignments.Select(kv => $"{kv.Key[..Math.Min(6, kv.Key.Length)]}={kv.Value}"))
            : Loc.Get("CA_NO_DAMAGE");
    }

    private void BuildInvaderButtons(string territoryId, int totalPool)
    {
        foreach (var child in _invaderBox.GetChildren())
            child.QueueFree();

        var bridge = GameBridge.Instance;
        var territory = bridge?.State?.GetTerritory(territoryId);
        if (territory == null) return;

        var alive = territory.Invaders.Where(i => i.IsAlive).ToList();
        foreach (var invader in alive)
        {
            string id   = invader.Id;
            string label = $"{invader.UnitType.ToString()[..1]}  HP:{invader.Hp}/{invader.MaxHp}";
            var row = new HBoxContainer();

            var nameLabel = new Label
            {
                Text              = label,
                CustomMinimumSize = new Vector2(120, 0),
                Modulate          = HpColor((float)invader.Hp / invader.MaxHp)
            };
            row.AddChild(nameLabel);

            var assignLabel = new Label
            {
                Text = Loc.Get("CA_DMG_N", 0),
                CustomMinimumSize = new Vector2(60, 0)
            };
            row.AddChild(assignLabel);

            var plusBtn = new Button { Text = "+1" };
            plusBtn.Pressed += () =>
            {
                if (_poolRemaining <= 0) return;
                _assignments.TryGetValue(id, out int cur);
                if (cur >= invader.Hp) return; // can't over-kill
                _assignments[id] = cur + 1;
                _poolRemaining--;
                assignLabel.Text = Loc.Get("CA_DMG_N", _assignments[id]);
                RefreshPoolLabel(totalPool);
            };

            var minusBtn = new Button { Text = "-1" };
            minusBtn.Pressed += () =>
            {
                if (!_assignments.TryGetValue(id, out int cur) || cur <= 0) return;
                _assignments[id] = cur - 1;
                _poolRemaining++;
                if (_assignments[id] == 0) _assignments.Remove(id);
                assignLabel.Text = Loc.Get("CA_DMG_N", _assignments.TryGetValue(id, out int v) ? v : 0);
                RefreshPoolLabel(totalPool);
            };

            row.AddChild(plusBtn);
            row.AddChild(minusBtn);
            _invaderBox.AddChild(row);
        }
    }

    private void OnConfirm()
    {
        GameBridge.Instance?.SubmitCounterAttack(new Dictionary<string, int>(_assignments));
        Visible = false;
    }

    private void OnSkip()
    {
        GameBridge.Instance?.SkipCounterAttack();
        Visible = false;
    }

    private void OnReset()
    {
        var bridge = GameBridge.Instance;
        if (bridge == null) return;
        int total = bridge.CounterAttackPool;
        _assignments.Clear();
        _poolRemaining = total;
        // Rebuild invader buttons to reset per-invader labels
        string? tid = bridge.CounterAttackTerritory;
        if (tid != null) BuildInvaderButtons(tid, total);
        RefreshPoolLabel(total);
    }

    /// <summary>Called by TerritoryViewController when player clicks an invader square.</summary>
    public void AssignDamage(string invaderId, int delta)
    {
        var bridge = GameBridge.Instance;
        if (bridge?.IsWaitingForCounterAttack != true) return;

        _assignments.TryGetValue(invaderId, out int current);
        int newVal = current + delta;
        if (newVal < 0) return;

        if (delta > 0 && _poolRemaining <= 0) return;

        // Don't assign more damage than the invader has HP
        var territory = bridge.State?.GetTerritory(bridge.CounterAttackTerritory);
        var invader   = territory?.Invaders.FirstOrDefault(i => i.Id == invaderId && i.IsAlive);
        if (invader != null && newVal > invader.Hp) return;

        int actualDelta = newVal - current;
        if (actualDelta == 0) return;

        if (newVal == 0)
            _assignments.Remove(invaderId);
        else
            _assignments[invaderId] = newVal;

        _poolRemaining -= actualDelta;
        if (Visible) RefreshPoolLabel(_totalPool);
    }

    private static Color HpColor(float hpFraction) => hpFraction switch
    {
        >= 0.7f => new Color(1f, 0.6f, 0.4f),   // full HP — light orange
        >= 0.4f => new Color(1f, 0.35f, 0.2f),  // wounded — orange-red
        _       => new Color(0.8f, 0.15f, 0.15f) // critical — dark red
    };
}
