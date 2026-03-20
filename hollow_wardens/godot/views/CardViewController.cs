using Godot;
using HollowWardens.Core.Models;

/// <summary>
/// Displays a single Card model: name, elements, top/bottom effect, dormant state.
/// Parent (HandDisplayController) calls Setup() after instantiation.
/// </summary>
public partial class CardViewController : PanelContainer
{
    private Card _card = null!;
    private Label _nameLabel = null!;
    private Label _elemLabel = null!;
    private Label _topLabel  = null!;
    private Label _botLabel  = null!;
    private Button _playBtn  = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(150, 200);

        var vbox = new VBoxContainer();
        AddChild(vbox);

        _nameLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _elemLabel = new Label { Modulate = new Color(0.7f, 0.9f, 0.7f) };
        _topLabel  = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _botLabel  = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart,
                                 Modulate = new Color(0.8f, 0.8f, 1.0f) };
        _playBtn   = new Button();

        vbox.AddChild(_nameLabel);
        vbox.AddChild(_elemLabel);
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "TOP:" });
        vbox.AddChild(_topLabel);
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "BOT:" });
        vbox.AddChild(_botLabel);
        vbox.AddChild(_playBtn);

        _playBtn.Pressed += OnPlayPressed;
    }

    public void Setup(Card card)
    {
        _card = card;
        Refresh();
    }

    public void Refresh()
    {
        if (_card == null || _nameLabel == null) return;

        _nameLabel.Text = _card.Name;
        _elemLabel.Text = _card.Elements.Length > 0
            ? string.Join(" ", System.Array.ConvertAll(_card.Elements, e => e.ToString()[..2]))
            : "—";
        _topLabel.Text = FormatEffect(_card.TopEffect);
        _botLabel.Text = FormatEffect(_card.BottomEffect);

        bool dormant = _card.IsDormant;
        Modulate = dormant ? new Color(0.5f, 0.5f, 0.5f) : Colors.White;

        var bridge = GameBridge.Instance;
        var phase  = bridge?.CurrentPhase;
        bool inRes = bridge?.IsInResolution ?? false;

        if (dormant)
        {
            _playBtn.Text     = "Dormant";
            _playBtn.Disabled = true;
        }
        else if (phase == TurnPhase.Vigil || inRes)
        {
            _playBtn.Text     = inRes ? "Play (Top)" : "Play Top";
            _playBtn.Disabled = false;
        }
        else if (phase == TurnPhase.Dusk)
        {
            _playBtn.Text     = "Play Bottom";
            _playBtn.Disabled = false;
        }
        else
        {
            _playBtn.Text     = "—";
            _playBtn.Disabled = true;
        }
    }

    private void OnPlayPressed()
    {
        var bridge = GameBridge.Instance;
        if (bridge == null || _card == null) return;

        bool inRes = bridge.IsInResolution;
        var  phase = bridge.CurrentPhase;

        if (phase == TurnPhase.Vigil || inRes)
            bridge.PlayTop(_card);
        else if (phase == TurnPhase.Dusk)
            bridge.PlayBottom(_card);
    }

    private static string FormatEffect(HollowWardens.Core.Effects.EffectData e)
        => $"{e.Type} ×{e.Value}" + (e.Range > 0 ? $" r{e.Range}" : "");
}
