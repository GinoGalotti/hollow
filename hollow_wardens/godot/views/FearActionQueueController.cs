using Godot;

/// <summary>
/// Shows queued fear action "cards" as face-down rectangles.
/// Flips them (simple tween scale-X) when FearActionRevealed fires.
/// </summary>
public partial class FearActionQueueController : VBoxContainer
{
    private Label       _titleLabel   = null!;
    private HBoxContainer _cardRow   = null!;
    private Label       _revealLabel  = null!;

    private readonly System.Collections.Generic.Queue<string> _revealed = new();

    public override void _Ready()
    {
        AddChild(new Label { Text = "── Fear Queue ──", Modulate = new Color(1, 0.5f, 0) });

        _cardRow     = new HBoxContainer();
        _revealLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 60)
        };

        AddChild(_cardRow);
        AddChild(new HSeparator());
        AddChild(new Label { Text = "Revealed:", Modulate = Colors.LightGray });
        AddChild(_revealLabel);

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.FearActionQueued   += OnCardQueued;
        bridge.FearActionRevealed += OnCardRevealed;
        bridge.TurnStarted        += ClearRevealed;

        UpdateCardRow(0);
    }

    private void OnCardQueued()
    {
        int count = GameBridge.Instance?.State.FearActions?.QueuedCount ?? 0;
        UpdateCardRow(count);
    }

    private void OnCardRevealed(string description)
    {
        _revealed.Enqueue(description);
        if (_revealed.Count > 3) _revealed.Dequeue();

        // Animate last queued card flipping (scale X tween)
        if (_cardRow.GetChildCount() > 0)
        {
            var card = _cardRow.GetChild<Control>(_cardRow.GetChildCount() - 1);
            var tween = CreateTween();
            tween.TweenProperty(card, "scale:x", 0.0f, 0.1f);
            tween.TweenCallback(Callable.From(() =>
            {
                card.Modulate = new Color(0.8f, 0.6f, 0.2f);
                card.GetChild<Label>(0).Text = "!";
            }));
            tween.TweenProperty(card, "scale:x", 1.0f, 0.1f);
        }

        int count = GameBridge.Instance?.State.FearActions?.QueuedCount ?? 0;
        UpdateCardRow(count);
        _revealLabel.Text = string.Join("\n", _revealed);
    }

    private void ClearRevealed()
    {
        _revealed.Clear();
        _revealLabel.Text = "";
        UpdateCardRow(0);
    }

    private void UpdateCardRow(int count)
    {
        foreach (Node child in _cardRow.GetChildren()) child.QueueFree();

        for (int i = 0; i < count; i++)
        {
            var panel = new PanelContainer
            {
                CustomMinimumSize = new Vector2(30, 42),
                Modulate = new Color(0.4f, 0.2f, 0.6f)
            };
            var lbl = new Label { Text = "?" };
            panel.AddChild(lbl);
            _cardRow.AddChild(panel);
        }

        if (count == 0)
        {
            var empty = new Label { Text = "(none)", Modulate = Colors.DarkGray };
            _cardRow.AddChild(empty);
        }
    }
}
