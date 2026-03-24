using Godot;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Localization;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;

/// <summary>
/// Post-encounter reward screen shown in Full Run mode.
/// Display tier, draft choices, token rewards, optional card removal, and heal toggle.
/// </summary>
public partial class RewardScreenController : CanvasLayer
{
    [Signal] public delegate void DraftCardChosenEventHandler(string cardId);
    [Signal] public delegate void CardRemovalChosenEventHandler(string cardId);
    [Signal] public delegate void HealChosenEventHandler();
    [Signal] public delegate void RewardContinuedEventHandler();

    private Font? _cinzel;
    private Font? _imFell;

    private RewardResult?      _reward;
    private EncounterResult    _encounterResult;
    private List<Card>         _draftChoices = new();
    private RunState?          _runState;
    private string?            _chosenCardId;
    private string?            _removalCardId;
    private bool               _choseHeal;

    public override void _Ready()
    {
        Layer   = 10;
        Visible = false;
        _cinzel = GD.Load<Font>("res://godot/assets/fonts/Cinzel-Bold.ttf");
        _imFell = GD.Load<Font>("res://godot/assets/fonts/IMFellEnglish-Regular.ttf");
    }

    /// <summary>
    /// Populate and show the reward screen.
    /// </summary>
    public void Show(RewardResult reward, EncounterResult encounterResult,
        List<Card> draftChoices, RunState runState)
    {
        _reward          = reward;
        _encounterResult = encounterResult;
        _draftChoices    = draftChoices;
        _runState        = runState;
        _chosenCardId    = null;
        _removalCardId   = null;
        _choseHeal       = false;

        // Clear any prior children
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
        vbox.CustomMinimumSize = new Vector2(520, 400);
        panel.AddChild(vbox);

        // ── Title ──────────────────────────────────────────────────────────
        var title = new Label
        {
            Text = Loc.Get("REWARD_SCREEN_TITLE"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        ApplyFont(title, _cinzel, 20, new Color(0.9f, 0.85f, 0.7f));
        vbox.AddChild(title);

        // ── Result + Tier ──────────────────────────────────────────────────
        string resultKey = _encounterResult switch
        {
            EncounterResult.Clean     => "REWARD_RESULT_CLEAN",
            EncounterResult.Weathered => "REWARD_RESULT_WEATHERED",
            _                         => "REWARD_RESULT_BREACH"
        };
        var resultLabel = new Label
        {
            Text = $"{Loc.Get(resultKey)}  —  {Loc.Get("REWARD_TIER_LABEL", _reward!.RewardTier)}",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        ApplyFont(resultLabel, _imFell, 13, Colors.LightGray);
        vbox.AddChild(resultLabel);

        // ── Tokens earned ──────────────────────────────────────────────────
        if (_reward!.UpgradeTokens > 0)
        {
            var tokenLabel = new Label
            {
                Text = Loc.Get("REWARD_TOKENS_EARNED", _reward.UpgradeTokens),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            ApplyFont(tokenLabel, _imFell, 12, new Color(1f, 0.9f, 0.4f));
            vbox.AddChild(tokenLabel);
        }

        vbox.AddChild(new HSeparator());

        // ── Draft choices ──────────────────────────────────────────────────
        var draftTitle = new Label { Text = Loc.Get("REWARD_DRAFT_TITLE", _reward.DraftChoices) };
        ApplyFont(draftTitle, _cinzel, 13, new Color(0.9f, 0.85f, 0.7f));
        vbox.AddChild(draftTitle);

        var draftRow = new HBoxContainer();
        draftRow.Alignment = BoxContainer.AlignmentMode.Center;
        var draftButtons   = new List<Button>();

        foreach (var card in _draftChoices)
        {
            var cardId = card.Id;
            var btn    = new Button { Text = $"{card.Name}\n{card.TopEffect.Type} {card.TopEffect.Value}" };
            btn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            btn.CustomMinimumSize = new Vector2(120, 60);
            ApplyFont(btn, _imFell, 10, Colors.White);
            btn.Pressed += () =>
            {
                _chosenCardId = cardId;
                foreach (var b in draftButtons)
                    b.Modulate = new Color(1f, 1f, 1f, 0.5f);
                btn.Modulate = Colors.White;
            };
            draftButtons.Add(btn);
            draftRow.AddChild(btn);
        }
        vbox.AddChild(draftRow);

        // ── Tier 1: card removal ───────────────────────────────────────────
        if (_reward.CanRemoveCard && _runState != null)
        {
            vbox.AddChild(new HSeparator());
            var removeLabel = new Label { Text = Loc.Get("REWARD_REMOVE_CARD") };
            ApplyFont(removeLabel, _cinzel, 11, new Color(0.9f, 0.85f, 0.7f));
            vbox.AddChild(removeLabel);

            // For simplicity: show a text field to enter card ID (full card list in future)
            var removeRow   = new HBoxContainer();
            var removeField = new LineEdit { PlaceholderText = "card_id", CustomMinimumSize = new Vector2(150, 0) };
            var removeBtn   = new Button { Text = Loc.Get("BTN_REMOVE") };
            removeBtn.Pressed += () =>
            {
                _removalCardId = removeField.Text.Trim();
                if (_removalCardId.Length > 0)
                    EmitSignal(SignalName.CardRemovalChosen, _removalCardId);
            };
            removeRow.AddChild(removeField);
            removeRow.AddChild(removeBtn);
            vbox.AddChild(removeRow);
        }

        // ── Tier 3: heal instead of token ─────────────────────────────────
        if (_reward.CanChooseHeal)
        {
            vbox.AddChild(new HSeparator());
            var healBtn = new Button { Text = Loc.Get("REWARD_HEAL_INSTEAD") };
            ApplyFont(healBtn, _imFell, 11, new Color(0.4f, 1f, 0.6f));
            healBtn.Pressed += () =>
            {
                _choseHeal = true;
                healBtn.Modulate = Colors.White;
                EmitSignal(SignalName.HealChosen);
            };
            vbox.AddChild(healBtn);
        }

        vbox.AddChild(new HSeparator());

        // ── Continue ───────────────────────────────────────────────────────
        var continueBtn = new Button { Text = Loc.Get("BTN_CONTINUE") };
        ApplyFont(continueBtn, _cinzel, 14, new Color(0.4f, 1f, 0.6f));
        continueBtn.Pressed += () =>
        {
            if (_chosenCardId != null)
                EmitSignal(SignalName.DraftCardChosen, _chosenCardId);
            Visible = false;
            EmitSignal(SignalName.RewardContinued);
        };
        vbox.AddChild(continueBtn);
    }

    private void ApplyFont(Control ctrl, Font? font, int size, Color color)
    {
        if (font != null) ctrl.AddThemeFontOverride("font", font);
        ctrl.AddThemeFontSizeOverride("font_size", size);
        ctrl.Modulate = color;
    }
}
