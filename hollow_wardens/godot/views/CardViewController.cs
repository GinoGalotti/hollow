using Godot;
using System.Collections.Generic;
using HollowWardens.Core.Localization;
using HollowWardens.Core.Models;

/// <summary>
/// Displays a single Card model: name, elements, top/bottom effect, dormant state.
/// Parent (HandDisplayController) calls Setup() after instantiation.
/// </summary>
public partial class CardViewController : PanelContainer
{
    private Card _card = null!;
    private Label         _nameLabel     = null!;
    private HBoxContainer _elemRow       = null!;  // element icon row
    private Label         _timingBadge   = null!;
    private Label         _topLabel      = null!;
    private Label         _botLabel      = null!;
    private Button        _playBtn       = null!;
    private Button        _selectTopBtn  = null!;
    private Button        _selectBotBtn  = null!;

    // Element icon textures loaded in _Ready()
    private readonly Dictionary<Element, Texture2D?> _elemIcons = new();

    // Stored so they can be disconnected in _ExitTree()
    private GameBridge.PhaseChangedEventHandler?              _onPhaseChanged;
    private GameBridge.TargetingModeChangedEventHandler?      _onTargetingModeChanged;
    private GameBridge.PairingSelectionChangedEventHandler?   _onPairingSelectionChanged;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(160, 210);

        MouseFilter = MouseFilterEnum.Pass;

        var cinzel = FontCache.CinzelBold;
        var imFell = FontCache.IMFell;

        // Use same icon filenames and loading approach as ElementTrackerController
        const string IconBase = "res://godot/assets/art/kenney_board-game-icons/PNG/Default (64px)/";
        _elemIcons[Element.Root]   = LoadIcon(IconBase + "resource_wood.png");
        _elemIcons[Element.Mist]   = LoadIcon(IconBase + "flask_half.png");
        _elemIcons[Element.Shadow] = LoadIcon(IconBase + "skull.png");
        _elemIcons[Element.Ash]    = LoadIcon(IconBase + "fire.png");
        _elemIcons[Element.Gale]   = LoadIcon(IconBase + "arrow_right.png");
        _elemIcons[Element.Void]   = LoadIcon(IconBase + "hexagon_outline.png");

        var vbox = new VBoxContainer();
        AddChild(vbox);

        _nameLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        if (cinzel != null) _nameLabel.AddThemeFontOverride("font", cinzel);
        _nameLabel.AddThemeFontSizeOverride("font_size", 13);

        _elemRow = new HBoxContainer();

        _timingBadge = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode        = TextServer.AutowrapMode.Off,
        };
        _timingBadge.AddThemeFontSizeOverride("font_size", 10);
        if (cinzel != null) _timingBadge.AddThemeFontOverride("font", cinzel);

        _topLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _botLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart,
                                Modulate = new Color(0.8f, 0.8f, 1.0f) };
        if (imFell != null) _topLabel.AddThemeFontOverride("font", imFell);
        if (imFell != null) _botLabel.AddThemeFontOverride("font", imFell);
        _topLabel.AddThemeFontSizeOverride("font_size", 11);
        _botLabel.AddThemeFontSizeOverride("font_size", 11);

        _playBtn      = new Button();
        _selectTopBtn = new Button { Visible = false };
        _selectBotBtn = new Button { Visible = false };

        vbox.AddChild(_nameLabel);
        vbox.AddChild(_elemRow);
        vbox.AddChild(new HSeparator());
        vbox.AddChild(_timingBadge);
        vbox.AddChild(new Label { Text = Loc.Get("CARD_TOP") });
        vbox.AddChild(_topLabel);
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = Loc.Get("CARD_BOT") });
        vbox.AddChild(_botLabel);
        var btnRow = new HBoxContainer();
        btnRow.AddChild(_selectTopBtn);
        btnRow.AddChild(_selectBotBtn);
        vbox.AddChild(btnRow);
        vbox.AddChild(_playBtn);

        _playBtn.Pressed      += OnPlayPressed;
        _selectTopBtn.Pressed += () => GameBridge.Instance?.SelectAsTop(_card);
        _selectBotBtn.Pressed += () => GameBridge.Instance?.SelectAsBottom(_card);

        MouseEntered += () =>
        {
            var tween = CreateTween();
            tween.TweenProperty(this, "scale", new Vector2(1.05f, 1.05f), 0.1);
        };
        MouseExited += () =>
        {
            var tween = CreateTween();
            tween.TweenProperty(this, "scale", Vector2.One, 0.1);
        };

        var bridge = GameBridge.Instance;
        if (bridge != null)
        {
            _onPhaseChanged              = _ => Refresh();
            _onTargetingModeChanged      = _ => Refresh();
            _onPairingSelectionChanged   = (_, _) => Refresh();
            bridge.PhaseChanged             += _onPhaseChanged;
            bridge.TargetingModeChanged     += _onTargetingModeChanged;
            bridge.PairingSelectionChanged  += _onPairingSelectionChanged;
        }
    }

    public override void _ExitTree()
    {
        var bridge = GameBridge.Instance;
        if (bridge != null)
        {
            if (_onPhaseChanged            != null) bridge.PhaseChanged            -= _onPhaseChanged;
            if (_onTargetingModeChanged    != null) bridge.TargetingModeChanged    -= _onTargetingModeChanged;
            if (_onPairingSelectionChanged != null) bridge.PairingSelectionChanged -= _onPairingSelectionChanged;
        }
    }

    public void Setup(Card card)
    {
        _card = card;
        ApplyRarityFrame(_card.Rarity);
        Refresh();
    }

    private void ApplyRarityFrame(CardRarity rarity)
    {
        var sb = new StyleBoxFlat();
        sb.BgColor = new Color(0.12f, 0.10f, 0.09f); // dark warm bg
        sb.SetCornerRadiusAll(4);
        sb.SetBorderWidthAll(2);
        sb.BorderColor = rarity switch
        {
            CardRarity.Awakened => new Color(0.25f, 0.50f, 0.85f),
            CardRarity.Ancient  => new Color(0.80f, 0.60f, 0.15f),
            _                   => new Color(0.35f, 0.30f, 0.25f),
        };
        sb.SetContentMarginAll(6);
        AddThemeStyleboxOverride("panel", sb);
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

        // Timing badge
        bool isFast = _card.TopTiming == HollowWardens.Core.Models.CardTiming.Fast;
        _timingBadge.Text     = isFast ? Loc.Get("CARD_TIMING_FAST") : Loc.Get("CARD_TIMING_SLOW");
        _timingBadge.Modulate = isFast ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.5f, 0.6f, 1.0f);

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

        var bridge     = GameBridge.Instance;
        var phase      = bridge?.CurrentPhase;
        bool inRes     = bridge?.IsInResolution ?? false;
        bool targeting = bridge?.IsWaitingForTarget ?? false;
        bool isPairing = bridge?.IsPairingSelection ?? false;

        // ── Pairing selection mode ────────────────────────────────────────────
        if (isPairing)
        {
            bool isTop    = bridge?.PairTop?.Id    == _card.Id;
            bool isBottom = bridge?.PairBottom?.Id == _card.Id;

            // Tint: green for selected top, blue for selected bottom, normal otherwise
            Modulate = isTop    ? new Color(0.6f, 1.0f, 0.6f) * rarityTint
                     : isBottom ? new Color(0.6f, 0.6f, 1.0f) * rarityTint
                     : dormant  ? rarityTint * new Color(0.5f, 0.5f, 0.5f, 1f)
                     : rarityTint;

            _playBtn.Visible = false;
            _selectTopBtn.Visible = !dormant && !isBottom;
            _selectBotBtn.Visible = !dormant && !isTop;
            _selectTopBtn.Text    = isTop    ? Loc.Get("CARD_SELECTED_TOP") : Loc.Get("CARD_SELECT_TOP");
            _selectBotBtn.Text    = isBottom ? Loc.Get("CARD_SELECTED_BOT") : Loc.Get("CARD_SELECT_BOT");
            _selectTopBtn.Disabled = false;
            _selectBotBtn.Disabled = false;
            return;
        }

        // ── Legacy / non-pairing mode ─────────────────────────────────────────
        Modulate = dormant ? rarityTint * new Color(0.5f, 0.5f, 0.5f, 1f) : rarityTint;
        _selectTopBtn.Visible = false;
        _selectBotBtn.Visible = false;
        _playBtn.Visible  = true;
        _playBtn.Modulate = Colors.White;

        if (targeting)
        {
            _playBtn.Text     = "—";
            _playBtn.Disabled = true;
        }
        else if (dormant)
        {
            _playBtn.Text     = Loc.Get("CARD_DORMANT");
            _playBtn.Disabled = true;
        }
        else if (phase == TurnPhase.Vigil || inRes)
        {
            bool canPlay      = bridge?.CanPlayTop() ?? true;
            _playBtn.Text     = inRes ? Loc.Get("BTN_PLAY_TOP_RES") : Loc.Get("BTN_PLAY_TOP");
            _playBtn.Disabled = !canPlay;
            if (!canPlay) _playBtn.Modulate = new Color(1f, 1f, 1f, 0.4f);
        }
        else if (phase == TurnPhase.Dusk)
        {
            bool canPlay      = bridge?.CanPlayBottom() ?? true;
            _playBtn.Text     = Loc.Get("BTN_PLAY_BOTTOM");
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
    {
        int v = e.Value;
        string range = e.Range > 0 ? $" (r{e.Range})" : "";
        return e.Type switch
        {
            HollowWardens.Core.Effects.EffectType.PlacePresence      => Loc.Get("EFFECT_PLACE_PRESENCE", v) + range,
            HollowWardens.Core.Effects.EffectType.MovePresence       => Loc.Get("EFFECT_MOVE_PRESENCE", v) + range,
            HollowWardens.Core.Effects.EffectType.GenerateFear       => Loc.Get("EFFECT_GENERATE_FEAR", v),
            HollowWardens.Core.Effects.EffectType.ReduceCorruption   => Loc.Get("EFFECT_REDUCE_CORRUPTION", v),
            HollowWardens.Core.Effects.EffectType.Purify             => Loc.Get("EFFECT_PURIFY", v),
            HollowWardens.Core.Effects.EffectType.DamageInvaders     => Loc.Get("EFFECT_DAMAGE_INVADERS", v) + range,
            HollowWardens.Core.Effects.EffectType.PushInvaders       => Loc.Get("EFFECT_PUSH_INVADERS", v) + range,
            HollowWardens.Core.Effects.EffectType.RoutInvaders       => Loc.Get("EFFECT_ROUT_INVADERS", v) + range,
            HollowWardens.Core.Effects.EffectType.SlowInvaders       => Loc.Get("EFFECT_SLOW_INVADERS", v) + range,
            HollowWardens.Core.Effects.EffectType.WeakenInvaders     => Loc.Get("EFFECT_WEAKEN_INVADERS", v) + range,
            HollowWardens.Core.Effects.EffectType.ExposeInvaders     => Loc.Get("EFFECT_EXPOSE_INVADERS", v) + range,
            HollowWardens.Core.Effects.EffectType.BrittleInvaders    => Loc.Get("EFFECT_BRITTLE_INVADERS", v) + range,
            HollowWardens.Core.Effects.EffectType.RestoreWeave       => Loc.Get("EFFECT_RESTORE_WEAVE", v),
            HollowWardens.Core.Effects.EffectType.ShieldNatives      => Loc.Get("EFFECT_SHIELD_NATIVES", v) + range,
            HollowWardens.Core.Effects.EffectType.BoostNatives       => Loc.Get("EFFECT_BOOST_NATIVES", v) + range,
            HollowWardens.Core.Effects.EffectType.HealNatives        => Loc.Get("EFFECT_HEAL_NATIVES", v) + range,
            HollowWardens.Core.Effects.EffectType.DamageNatives      => Loc.Get("EFFECT_DAMAGE_NATIVES", v) + range,
            HollowWardens.Core.Effects.EffectType.SpawnNatives       => Loc.Get("EFFECT_SPAWN_NATIVES", v) + range,
            HollowWardens.Core.Effects.EffectType.MoveNatives        => Loc.Get("EFFECT_MOVE_NATIVES", v) + range,
            HollowWardens.Core.Effects.EffectType.AwakeDormant       => Loc.Get("EFFECT_AWAKE_DORMANT", v),
            HollowWardens.Core.Effects.EffectType.PullInvaders       => Loc.Get("EFFECT_PULL_INVADERS_FMT", v) + range,
            HollowWardens.Core.Effects.EffectType.CorruptionDetonate => Loc.Get("EFFECT_CORRUPTION_DETONATE_FMT", v),
            HollowWardens.Core.Effects.EffectType.AddCorruption      => Loc.Get("EFFECT_ADD_CORRUPTION_FMT", v) + range,
            HollowWardens.Core.Effects.EffectType.Conditional        => Loc.Get("EFFECT_CONDITIONAL"),
            HollowWardens.Core.Effects.EffectType.Custom             => Loc.Get("EFFECT_CUSTOM"),
            _                                                         => e.Type.ToString(),
        };
    }

    private static Texture2D? LoadIcon(string path)
    {
        try { return GD.Load<Texture2D>(path); }
        catch { return null; }
    }
}
