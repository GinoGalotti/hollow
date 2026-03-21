using Godot;
using System.Collections.Generic;
using HollowWardens.Core.Models;

/// <summary>
/// Displays a single Card model: name, elements, top/bottom effect, dormant state.
/// Parent (HandDisplayController) calls Setup() after instantiation.
/// </summary>
public partial class CardViewController : PanelContainer
{
    private Card _card = null!;
    private Label         _nameLabel = null!;
    private HBoxContainer _elemRow   = null!;  // element icon row
    private Label         _topLabel  = null!;
    private Label         _botLabel  = null!;
    private Button        _playBtn   = null!;

    // Element icon textures loaded in _Ready()
    private readonly Dictionary<Element, Texture2D?> _elemIcons = new();

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(150, 200);

        // TODO: visual upgrade — card_empty.png as panel background (StyleBoxTexture)

        var cinzel = GD.Load<Font>("res://godot/assets/fonts/Cinzel-Bold.ttf");
        var imFell = GD.Load<Font>("res://godot/assets/fonts/IMFellEnglish-Regular.ttf");

        const string IconBase = "res://godot/assets/art/kenney_board-game-icons/PNG/Default (64px)/";
        _elemIcons[Element.Root]   = GD.Load<Texture2D>(IconBase + "resource_wood.png");
        _elemIcons[Element.Mist]   = GD.Load<Texture2D>(IconBase + "flask_half.png");
        _elemIcons[Element.Shadow] = GD.Load<Texture2D>(IconBase + "skull.png");
        _elemIcons[Element.Ash]    = GD.Load<Texture2D>(IconBase + "fire.png");
        _elemIcons[Element.Gale]   = GD.Load<Texture2D>(IconBase + "arrow_right.png");
        _elemIcons[Element.Void]   = GD.Load<Texture2D>(IconBase + "hexagon_outline.png");

        var vbox = new VBoxContainer();
        AddChild(vbox);

        _nameLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        if (cinzel != null) _nameLabel.AddThemeFontOverride("font", cinzel);
        _nameLabel.AddThemeFontSizeOverride("font_size", 13);

        _elemRow = new HBoxContainer();

        _topLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _botLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart,
                                Modulate = new Color(0.8f, 0.8f, 1.0f) };
        if (imFell != null) _topLabel.AddThemeFontOverride("font", imFell);
        if (imFell != null) _botLabel.AddThemeFontOverride("font", imFell);
        _topLabel.AddThemeFontSizeOverride("font_size", 11);
        _botLabel.AddThemeFontSizeOverride("font_size", 11);

        _playBtn = new Button();

        vbox.AddChild(_nameLabel);
        vbox.AddChild(_elemRow);
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "TOP:" });
        vbox.AddChild(_topLabel);
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "BOT:" });
        vbox.AddChild(_botLabel);
        vbox.AddChild(_playBtn);

        _playBtn.Pressed += OnPlayPressed;

        var bridge = GameBridge.Instance;
        if (bridge != null)
        {
            bridge.PhaseChanged         += _ => Refresh();
            bridge.TargetingModeChanged += _ => Refresh();
        }
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

        // Rebuild element icon row
        foreach (var child in _elemRow.GetChildren()) child.QueueFree();
        if (_card.Elements.Length > 0)
        {
            foreach (var elem in _card.Elements)
            {
                if (_elemIcons.TryGetValue(elem, out var icon) && icon != null)
                    _elemRow.AddChild(new TextureRect
                    {
                        Texture           = icon,
                        CustomMinimumSize = new Vector2(14, 14),
                        StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered,
                        ExpandMode        = TextureRect.ExpandModeEnum.FitWidthProportional,
                    });
                else
                    _elemRow.AddChild(new Label { Text = elem.ToString()[..2] });
            }
        }
        else
        {
            _elemRow.AddChild(new Label { Text = "—" });
        }

        _topLabel.Text = FormatEffect(_card.TopEffect);
        _botLabel.Text = FormatEffect(_card.BottomEffect);

        // Rarity tint × dormant state
        Color rarityTint = _card.Rarity switch
        {
            CardRarity.Awakened => new Color(0.70f, 0.80f, 1.00f),
            CardRarity.Ancient  => new Color(1.00f, 0.85f, 0.50f),
            _                   => Colors.White
        };
        bool dormant = _card.IsDormant;
        Modulate = dormant ? rarityTint * new Color(0.5f, 0.5f, 0.5f, 1f) : rarityTint;

        var bridge     = GameBridge.Instance;
        var phase      = bridge?.CurrentPhase;
        bool inRes     = bridge?.IsInResolution ?? false;
        bool targeting = bridge?.IsWaitingForTarget ?? false;

        _playBtn.Visible  = true;
        _playBtn.Modulate = Colors.White;

        if (targeting)
        {
            _playBtn.Text     = "—";
            _playBtn.Disabled = true;
        }
        else if (dormant)
        {
            _playBtn.Text     = "Dormant";
            _playBtn.Disabled = true;
        }
        else if (phase == TurnPhase.Vigil || inRes)
        {
            bool canPlay      = bridge?.CanPlayTop() ?? true;
            _playBtn.Text     = inRes ? "Play (Top)" : "Play Top";
            _playBtn.Disabled = !canPlay;
            if (!canPlay) _playBtn.Modulate = new Color(1f, 1f, 1f, 0.4f);
        }
        else if (phase == TurnPhase.Dusk)
        {
            bool canPlay      = bridge?.CanPlayBottom() ?? true;
            _playBtn.Text     = "Play Bottom";
            _playBtn.Disabled = !canPlay;
            if (!canPlay) _playBtn.Modulate = new Color(1f, 1f, 1f, 0.4f);
        }
        else
        {
            // Tide, Rest — hide play button entirely
            _playBtn.Visible = false;
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
