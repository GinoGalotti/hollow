using Godot;
using HollowWardens.Core.Localization;
using HollowWardens.Core.Models;

/// <summary>
/// Overlay shown after a Rest turn. Displays the two dissolved cards and
/// offers reroll buttons (cost: 2 weave each). Dismissed with Continue.
/// </summary>
public partial class RestScreenController : CanvasLayer
{
    [Signal] public delegate void RerollRequestedEventHandler(string cardId);
    [Signal] public delegate void RestContinuedEventHandler();

    private Font? _cinzel;
    private Font? _imFell;

    public override void _Ready()
    {
        Layer   = 8;
        Visible = false;
        _cinzel = FontCache.CinzelBold;
        _imFell = FontCache.IMFell;
    }

    /// <summary>
    /// Show the rest screen. dissolvedCards is the list of cards dissolved
    /// during this rest (may be empty if bottom-discard had < 2 cards).
    /// currentWeave is used to enable/disable reroll buttons.
    /// </summary>
    public void Show(IReadOnlyList<Card> dissolvedCards, int currentWeave)
    {
        foreach (var child in GetChildren())
            child.QueueFree();

        BuildUI(dissolvedCards, currentWeave);
        Visible = true;
    }

    private void BuildUI(IReadOnlyList<Card> dissolvedCards, int currentWeave)
    {
        var overlay = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.65f)
        };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        overlay.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(420, 220);
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        // ── Title ──────────────────────────────────────────────────────────
        var title = new Label
        {
            Text = Loc.Get("REST_SCREEN_TITLE"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        ApplyFont(title, _cinzel, 18, new Color(0.9f, 0.85f, 0.7f));
        vbox.AddChild(title);

        vbox.AddChild(new HSeparator());

        // ── Dissolved cards ────────────────────────────────────────────────
        var header = new Label { Text = Loc.Get("REST_DISSOLVED_HEADER") };
        ApplyFont(header, _imFell, 12, Colors.LightGray);
        vbox.AddChild(header);

        if (dissolvedCards.Count == 0)
        {
            var noneLabel = new Label { Text = Loc.Get("REST_NO_DISSOLVED") };
            ApplyFont(noneLabel, _imFell, 11, Colors.DarkGray);
            vbox.AddChild(noneLabel);
        }
        else
        {
            foreach (var card in dissolvedCards)
            {
                var cardId = card.Id;
                var row    = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 12);

                var nameLabel = new Label
                {
                    Text               = card.Name,
                    CustomMinimumSize  = new Vector2(200, 0),
                    VerticalAlignment  = VerticalAlignment.Center
                };
                ApplyFont(nameLabel, _imFell, 12, new Color(0.95f, 0.6f, 0.5f));
                row.AddChild(nameLabel);

                bool canAfford = currentWeave >= 2;
                var rerollBtn  = new Button
                {
                    Text     = Loc.Get("REST_REROLL_BTN"),
                    Disabled = !canAfford
                };
                ApplyFont(rerollBtn, _imFell, 11, canAfford ? new Color(0.4f, 1f, 0.7f) : Colors.Gray);
                rerollBtn.Pressed += () =>
                {
                    EmitSignal(SignalName.RerollRequested, cardId);
                    // Disable all reroll buttons after one is pressed (one per rest)
                    DisableAllRerollButtons();
                };
                row.AddChild(rerollBtn);

                vbox.AddChild(row);
            }
        }

        vbox.AddChild(new HSeparator());

        // ── Continue ───────────────────────────────────────────────────────
        var continueBtn = new Button { Text = Loc.Get("REST_CONTINUE") };
        ApplyFont(continueBtn, _cinzel, 14, new Color(0.4f, 1f, 0.6f));
        continueBtn.Pressed += () =>
        {
            Visible = false;
            EmitSignal(SignalName.RestContinued);
        };
        vbox.AddChild(continueBtn);
    }

    private void DisableAllRerollButtons()
    {
        foreach (var node in GetChildren())
            DisableRerollsInNode(node);
    }

    private static void DisableRerollsInNode(Node node)
    {
        if (node is Button btn && btn.Text == Loc.Get("REST_REROLL_BTN"))
            btn.Disabled = true;
        foreach (var child in node.GetChildren())
            DisableRerollsInNode(child);
    }

    private void ApplyFont(Control ctrl, Font? font, int size, Color color)
    {
        if (font != null) ctrl.AddThemeFontOverride("font", font);
        ctrl.AddThemeFontSizeOverride("font_size", size);
        ctrl.Modulate = color;
    }
}
