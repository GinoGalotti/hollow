using Godot;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Data;
using HollowWardens.Core.Localization;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;

/// <summary>
/// Upgrade screen accessible from rest stops or when spending tokens.
/// Two tabs: "Upgrade a Card" and "Upgrade a Passive".
/// Token cost displayed; Confirm deducts tokens.
/// </summary>
public partial class UpgradeScreenController : CanvasLayer
{
    [Signal] public delegate void CardUpgradeChosenEventHandler(string cardId, string upgradeId);
    [Signal] public delegate void PassiveUpgradeChosenEventHandler(string upgradeId);
    [Signal] public delegate void UpgradeDismissedEventHandler();

    private Font? _cinzel;
    private Font? _imFell;

    private RunState?    _runState;
    private List<Card>   _deckCards   = new();
    private WardenData?  _wardenData;

    public override void _Ready()
    {
        Layer   = 10;
        Visible = false;
        _cinzel = GD.Load<Font>("res://godot/assets/fonts/Cinzel-Bold.ttf");
        _imFell = GD.Load<Font>("res://godot/assets/fonts/IMFellEnglish-Regular.ttf");
    }

    /// <summary>
    /// Populate and show the upgrade screen.
    /// </summary>
    public void Show(RunState runState, List<Card> deckCards, WardenData wardenData)
    {
        _runState   = runState;
        _deckCards  = deckCards;
        _wardenData = wardenData;

        foreach (var child in GetChildren())
            child.QueueFree();

        BuildUI();
        Visible = true;
    }

    private void BuildUI()
    {
        var overlay = new Control();
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        overlay.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(500, 420);
        panel.AddChild(vbox);

        // ── Title ──────────────────────────────────────────────────────────
        var title = new Label
        {
            Text = Loc.Get("UPGRADE_SCREEN_TITLE"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        ApplyFont(title, _cinzel, 18, new Color(0.9f, 0.85f, 0.7f));
        vbox.AddChild(title);

        // ── Token balance ──────────────────────────────────────────────────
        var tokenLabel = new Label
        {
            Text = Loc.Get("UPGRADE_TOKENS_AVAILABLE", _runState?.UpgradeTokens ?? 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        ApplyFont(tokenLabel, _imFell, 12, new Color(1f, 0.9f, 0.4f));
        vbox.AddChild(tokenLabel);

        vbox.AddChild(new HSeparator());

        // ── Tab row ────────────────────────────────────────────────────────
        var tabRow = new HBoxContainer();
        tabRow.Alignment = BoxContainer.AlignmentMode.Center;
        var cardTabBtn    = new Button { Text = Loc.Get("UPGRADE_CARD_TAB") };
        var passiveTabBtn = new Button { Text = Loc.Get("UPGRADE_PASSIVE_TAB") };
        ApplyFont(cardTabBtn,    _cinzel, 12, Colors.White);
        ApplyFont(passiveTabBtn, _cinzel, 12, new Color(1f, 1f, 1f, 0.6f));
        tabRow.AddChild(cardTabBtn);
        tabRow.AddChild(passiveTabBtn);
        vbox.AddChild(tabRow);
        vbox.AddChild(new HSeparator());

        // ── Tab panels ─────────────────────────────────────────────────────
        var cardPanel    = BuildCardUpgradePanel();
        var passivePanel = BuildPassiveUpgradePanel();
        passivePanel.Visible = false;
        vbox.AddChild(cardPanel);
        vbox.AddChild(passivePanel);

        cardTabBtn.Pressed += () =>
        {
            cardPanel.Visible    = true;
            passivePanel.Visible = false;
            cardTabBtn.Modulate    = Colors.White;
            passiveTabBtn.Modulate = new Color(1f, 1f, 1f, 0.6f);
        };
        passiveTabBtn.Pressed += () =>
        {
            cardPanel.Visible    = false;
            passivePanel.Visible = true;
            cardTabBtn.Modulate    = new Color(1f, 1f, 1f, 0.6f);
            passiveTabBtn.Modulate = Colors.White;
        };

        vbox.AddChild(new HSeparator());

        // ── Dismiss ────────────────────────────────────────────────────────
        var dismissBtn = new Button { Text = Loc.Get("BTN_CLOSE") };
        ApplyFont(dismissBtn, _imFell, 10, Colors.LightGray);
        dismissBtn.Pressed += () =>
        {
            Visible = false;
            EmitSignal(SignalName.UpgradeDismissed);
        };
        vbox.AddChild(dismissBtn);
    }

    private VBoxContainer BuildCardUpgradePanel()
    {
        var panel = new VBoxContainer();

        if (_deckCards.Count == 0)
        {
            var empty = new Label { Text = Loc.Get("UPGRADE_NO_CARDS") };
            ApplyFont(empty, _imFell, 11, Colors.LightGray);
            panel.AddChild(empty);
            return panel;
        }

        foreach (var card in _deckCards)
        {
            if (card.UpgradeSlots.Count == 0) continue;
            var cardRef = card;

            foreach (var upgrade in card.UpgradeSlots)
            {
                bool alreadyApplied = _runState?.AppliedCardUpgradeIds.Contains(upgrade.Id) ?? false;
                if (alreadyApplied) continue;

                string descText = string.IsNullOrEmpty(upgrade.DescriptionKey)
                    ? upgrade.Id
                    : Loc.Get(upgrade.DescriptionKey);

                var upgradeId = upgrade.Id;
                int cost      = upgrade.Cost;
                var btn = new Button { Text = $"{card.Name}: {descText}\n{Loc.Get("UPGRADE_COST", cost)}" };
                btn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                ApplyFont(btn, _imFell, 10, Colors.White);

                bool canAfford = (_runState?.UpgradeTokens ?? 0) >= cost;
                if (!canAfford) btn.Modulate = new Color(1f, 1f, 1f, 0.4f);

                btn.Pressed += () =>
                {
                    if (!canAfford) return;
                    Visible = false;
                    EmitSignal(SignalName.CardUpgradeChosen, cardRef.Id, upgradeId);
                };
                panel.AddChild(btn);
            }
        }

        return panel;
    }

    private VBoxContainer BuildPassiveUpgradePanel()
    {
        var panel = new VBoxContainer();

        if (_wardenData?.Passives == null || _wardenData.Passives.Count == 0)
        {
            var empty = new Label { Text = Loc.Get("UPGRADE_NO_PASSIVES") };
            ApplyFont(empty, _imFell, 11, Colors.LightGray);
            panel.AddChild(empty);
            return panel;
        }

        foreach (var passive in _wardenData.Passives)
        {
            if (passive.Upgrade == null) continue;

            bool alreadyApplied = _runState?.AppliedPassiveUpgradeIds.Contains(passive.Upgrade.Id) ?? false;
            if (alreadyApplied) continue;

            string descText = string.IsNullOrEmpty(passive.Upgrade.DescriptionKey)
                ? passive.Upgrade.Id
                : Loc.Get(passive.Upgrade.DescriptionKey);

            var upgradeId = passive.Upgrade.Id;
            var btn = new Button { Text = $"{passive.Name}: {descText}\n{Loc.Get("UPGRADE_COST", 1)}" };
            btn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            ApplyFont(btn, _imFell, 10, Colors.White);

            bool canAfford = (_runState?.UpgradeTokens ?? 0) >= 1;
            if (!canAfford) btn.Modulate = new Color(1f, 1f, 1f, 0.4f);

            btn.Pressed += () =>
            {
                if (!canAfford) return;
                Visible = false;
                EmitSignal(SignalName.PassiveUpgradeChosen, upgradeId);
            };
            panel.AddChild(btn);
        }

        return panel;
    }

    private void ApplyFont(Control ctrl, Font? font, int size, Color color)
    {
        if (font != null) ctrl.AddThemeFontOverride("font", font);
        ctrl.AddThemeFontSizeOverride("font_size", size);
        ctrl.Modulate = color;
    }
}
