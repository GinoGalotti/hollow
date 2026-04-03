using Godot;
using HollowWardens.Core.Localization;

/// <summary>
/// Four-screen overlay: warden → mode → encounter (Single/Practice) or realm (Full Run).
/// Also handles inter-encounter chain continuation when ChainAdvanceReady fires.
/// </summary>
public partial class WardenSelectController : CanvasLayer
{
    // ── Encounter definitions ─────────────────────────────────────────────────

    private static readonly (string id, string nameKey, string subKey)[] Encounters =
    {
        ("pale_march_standard", "ENCOUNTER_STANDARD_NAME", "ENCOUNTER_STANDARD_SUB"),
        ("pale_march_scouts",   "ENCOUNTER_SCOUTS_NAME",   "ENCOUNTER_SCOUTS_SUB"),
        ("pale_march_siege",    "ENCOUNTER_SIEGE_NAME",    "ENCOUNTER_SIEGE_SUB"),
        ("pale_march_elite",    "ENCOUNTER_ELITE_NAME",    "ENCOUNTER_ELITE_SUB"),
        ("pale_march_frontier", "ENCOUNTER_FRONTIER_NAME", "ENCOUNTER_FRONTIER_SUB"),
    };

    // Default slot selections (standard, scouts, elite)
    private readonly string[] _chainSlots = { "pale_march_standard", "pale_march_scouts", "pale_march_elite" };

    // ── Screens ───────────────────────────────────────────────────────────────

    private Control? _wardenScreen;
    private Control? _modeScreen;
    private Control? _encounterScreen;
    private Control? _passiveScreen;
    private Control? _chainContinueScreen;
    private string   _selectedWarden = "root";

    // Fonts cached in _Ready for reuse across screens
    private Font? _cinzel;
    private Font? _imFell;

    public override void _Ready()
    {
        Layer = 2;  // Above game UI CanvasLayer (layer 1)

        var game  = GetNode<Node>("/root/Game");
        var board = game?.GetNodeOrNull<Node2D>("Board");
        var ui    = game?.GetNodeOrNull<CanvasLayer>("UI");
        if (board != null) board.Visible = false;
        if (ui    != null) ui.Visible    = false;

        _cinzel = FontCache.CinzelBold;
        _imFell = FontCache.IMFell;

        var overlay = new Control();
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        _wardenScreen        = BuildWardenScreen(overlay);
        _modeScreen          = BuildModeScreen(overlay);
        _encounterScreen     = BuildEncounterScreen(overlay);
        _passiveScreen       = BuildPassiveScreen(overlay);
        _chainContinueScreen = BuildChainContinueScreen(overlay);

        _modeScreen.Visible          = false;
        _encounterScreen.Visible     = false;
        _passiveScreen.Visible       = false;
        _chainContinueScreen.Visible = false;

        // Subscribe to chain advance signal — fired when an encounter ends mid-chain
        var bridge = GetNodeOrNull<GameBridge>("/root/GameBridge");
        if (bridge != null)
            bridge.ChainAdvanceReady += OnChainAdvanceReady;
    }

    // ── Screen 1: Warden selection ────────────────────────────────────────────

    private Control BuildWardenScreen(Control parent)
    {
        var container = new Control();
        container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(container);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        container.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(400, 300);
        panel.AddChild(vbox);

        var title = new Label { Text = Loc.Get("MENU_TITLE"), HorizontalAlignment = HorizontalAlignment.Center };
        if (_cinzel != null) title.AddThemeFontOverride("font", _cinzel);
        title.AddThemeFontSizeOverride("font_size", 20);
        title.Modulate = new Color(0.9f, 0.85f, 0.7f);
        vbox.AddChild(title);

        var subtitle = new Label { Text = Loc.Get("WARDEN_SELECT_TITLE"), HorizontalAlignment = HorizontalAlignment.Center };
        if (_imFell != null) subtitle.AddThemeFontOverride("font", _imFell);
        subtitle.AddThemeFontSizeOverride("font_size", 13);
        subtitle.Modulate = Colors.LightGray;
        vbox.AddChild(subtitle);
        vbox.AddChild(new HSeparator());

        AddWardenButton(vbox, "root",
            Loc.Get("WARDEN_ROOT_NAME"),
            Loc.Get("WARDEN_ROOT_DESC"),
            Loc.Get("WARDEN_ROOT_FLAVOR"));

        AddWardenButton(vbox, "ember",
            Loc.Get("WARDEN_EMBER_NAME"),
            Loc.Get("WARDEN_EMBER_DESC"),
            Loc.Get("WARDEN_EMBER_FLAVOR"));

        return container;
    }

    private void AddWardenButton(VBoxContainer vbox, string wardenId,
        string name, string desc, string flavor)
    {
        var btn = new Button { Text = $"{name}\n{desc}\n\"{flavor}\"" };
        btn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        if (_imFell != null) btn.AddThemeFontOverride("font", _imFell);
        btn.AddThemeFontSizeOverride("font_size", 11);
        btn.Pressed += () => ShowModeScreen(wardenId);
        vbox.AddChild(btn);
    }

    // ── Screen 2: Mode selection ──────────────────────────────────────────────

    private Control BuildModeScreen(Control parent)
    {
        var container = new Control();
        container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(container);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        container.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(440, 320);
        panel.AddChild(vbox);

        var title = new Label { Text = Loc.Get("MODE_SELECT_TITLE"), HorizontalAlignment = HorizontalAlignment.Center };
        if (_cinzel != null) title.AddThemeFontOverride("font", _cinzel);
        title.AddThemeFontSizeOverride("font_size", 18);
        title.Modulate = new Color(0.9f, 0.85f, 0.7f);
        vbox.AddChild(title);
        vbox.AddChild(new HSeparator());

        AddModeButton(vbox, "full_run",   Loc.Get("MODE_FULL_RUN"),   Loc.Get("MODE_FULL_RUN_DESC"),   new Color(0.4f, 1f, 0.6f));
        AddModeButton(vbox, "single",     Loc.Get("MODE_SINGLE"),     Loc.Get("MODE_SINGLE_DESC"),     Colors.White);

        vbox.AddChild(new HSeparator());

        var backBtn = new Button { Text = Loc.Get("BTN_BACK") };
        if (_imFell != null) backBtn.AddThemeFontOverride("font", _imFell);
        backBtn.AddThemeFontSizeOverride("font_size", 10);
        backBtn.Modulate = Colors.LightGray;
        backBtn.Pressed += () =>
        {
            if (_modeScreen   != null) _modeScreen.Visible   = false;
            if (_wardenScreen != null) _wardenScreen.Visible = true;
        };
        vbox.AddChild(backBtn);

        return container;
    }

    private void AddModeButton(VBoxContainer vbox, string mode, string label, string desc, Color tint)
    {
        var btn = new Button { Text = $"{label}\n{desc}" };
        btn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        if (_imFell != null) btn.AddThemeFontOverride("font", _imFell);
        btn.AddThemeFontSizeOverride("font_size", 12);
        btn.Modulate = tint;
        btn.Pressed += () => OnModeSelected(mode);
        vbox.AddChild(btn);
    }

    private void OnModeSelected(string mode)
    {
        GameBridge.SelectedMode = mode;
        if (mode == "full_run")
        {
            // Only one realm exists — skip realm selection, start immediately.
            StartFullRun(_selectedWarden, "realm_1");
        }
        else
        {
            ShowEncounterScreen();
        }
    }

    // ── Screen 3a: Encounter selection (Single + Chain tabs) ──────────────────

    private Control BuildEncounterScreen(Control parent)
    {
        var container = new Control();
        container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(container);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        container.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(500, 420);
        panel.AddChild(vbox);

        var title = new Label { Text = Loc.Get("ENCOUNTER_SELECT_TITLE"), HorizontalAlignment = HorizontalAlignment.Center };
        if (_cinzel != null) title.AddThemeFontOverride("font", _cinzel);
        title.AddThemeFontSizeOverride("font_size", 18);
        title.Modulate = new Color(0.9f, 0.85f, 0.7f);
        vbox.AddChild(title);
        vbox.AddChild(new HSeparator());

        // Mode toggle: [Single] [Chain]
        var modeRow = new HBoxContainer();
        modeRow.Alignment = BoxContainer.AlignmentMode.Center;

        var singleBtn = new Button { Text = Loc.Get("ENCOUNTER_SELECT_MODE_SINGLE") };
        var chainBtn  = new Button { Text = Loc.Get("ENCOUNTER_SELECT_MODE_CHAIN") };
        if (_cinzel != null) { singleBtn.AddThemeFontOverride("font", _cinzel); chainBtn.AddThemeFontOverride("font", _cinzel); }
        singleBtn.AddThemeFontSizeOverride("font_size", 12);
        chainBtn.AddThemeFontSizeOverride("font_size", 12);
        modeRow.AddChild(singleBtn);
        modeRow.AddChild(chainBtn);
        vbox.AddChild(modeRow);
        vbox.AddChild(new HSeparator());

        // Single panel
        var singlePanel = BuildSinglePanel();
        vbox.AddChild(singlePanel);

        // Chain panel (hidden by default)
        var chainPanel = BuildChainPanel();
        chainPanel.Visible = false;
        vbox.AddChild(chainPanel);

        // Mode toggle wiring
        singleBtn.Pressed += () => { singlePanel.Visible = true;  chainPanel.Visible = false; };
        chainBtn.Pressed  += () => { singlePanel.Visible = false; chainPanel.Visible = true;  };

        // Back button
        var backBtn = new Button { Text = Loc.Get("BTN_BACK") };
        if (_imFell != null) backBtn.AddThemeFontOverride("font", _imFell);
        backBtn.AddThemeFontSizeOverride("font_size", 10);
        backBtn.Modulate = Colors.LightGray;
        backBtn.Pressed += () =>
        {
            if (_encounterScreen != null) _encounterScreen.Visible = false;
            if (_modeScreen      != null) _modeScreen.Visible      = true;
        };
        vbox.AddChild(backBtn);

        return container;
    }

    private VBoxContainer BuildSinglePanel()
    {
        var panel = new VBoxContainer();
        foreach (var (id, nameKey, subKey) in Encounters)
        {
            var encId = id;
            var btn   = new Button { Text = $"{Loc.Get(nameKey)}\n{Loc.Get(subKey)}" };
            btn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            if (_imFell != null) btn.AddThemeFontOverride("font", _imFell);
            btn.AddThemeFontSizeOverride("font_size", 11);
            btn.Pressed += () => StartSingleEncounter(_selectedWarden, encId);
            panel.AddChild(btn);
        }
        return panel;
    }

    private VBoxContainer BuildChainPanel()
    {
        var panel = new VBoxContainer();

        string[] slotKeys = { "CHAIN_SLOT_E1", "CHAIN_SLOT_E2", "CHAIN_SLOT_CAPSTONE" };

        for (int slotIndex = 0; slotIndex < 3; slotIndex++)
        {
            var si = slotIndex; // capture

            var slotLabel = new Label
            {
                Text    = Loc.Get(slotKeys[slotIndex]),
                Modulate = new Color(0.9f, 0.85f, 0.7f)
            };
            if (_cinzel != null) slotLabel.AddThemeFontOverride("font", _cinzel);
            slotLabel.AddThemeFontSizeOverride("font_size", 11);
            panel.AddChild(slotLabel);

            // Tab row: one button per encounter
            var tabRow = new HBoxContainer();
            for (int ei = 0; ei < Encounters.Length; ei++)
            {
                var encIndex = ei;
                var (encId, nameKey, _) = Encounters[ei];
                var tabBtn = new Button { Text = Loc.Get(nameKey) };
                if (_imFell != null) tabBtn.AddThemeFontOverride("font", _imFell);
                tabBtn.AddThemeFontSizeOverride("font_size", 10);
                tabBtn.Modulate = encId == _chainSlots[si] ? Colors.White : new Color(1f, 1f, 1f, 0.5f);

                tabBtn.Pressed += () =>
                {
                    _chainSlots[si] = Encounters[encIndex].id;
                    // Refresh all buttons in this row
                    foreach (Node child in tabRow.GetChildren())
                    {
                        if (child is Button b)
                        {
                            int bi = tabRow.GetChildren().ToArray<Node>().ToList().IndexOf(b);
                            b.Modulate = Encounters[bi].id == _chainSlots[si]
                                ? Colors.White
                                : new Color(1f, 1f, 1f, 0.5f);
                        }
                    }
                };
                tabRow.AddChild(tabBtn);
            }
            panel.AddChild(tabRow);
        }

        panel.AddChild(new HSeparator());

        var startBtn = new Button { Text = Loc.Get("BTN_START_CHAIN") };
        if (_cinzel != null) startBtn.AddThemeFontOverride("font", _cinzel);
        startBtn.AddThemeFontSizeOverride("font_size", 13);
        startBtn.Modulate = new Color(0.4f, 1f, 0.6f);
        startBtn.Pressed += () => StartChainEncounter(_selectedWarden);
        panel.AddChild(startBtn);

        return panel;
    }

    // ── Screen 3b: Passive selection ──────────────────────────────────────────

    // Dynamic content — populated in ShowPassiveScreen; only skeleton built here
    private VBoxContainer? _passiveOptionContainer;
    private Button?        _passiveConfirmBtn;
    private Action?        _onPassiveConfirmed;
    private readonly HashSet<string> _selectedPoolIds = new();

    private Control BuildPassiveScreen(Control parent)
    {
        var container = new Control();
        container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(container);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        container.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(440, 320);
        panel.AddChild(vbox);

        var title = new Label { Text = "Choose 2 Abilities", HorizontalAlignment = HorizontalAlignment.Center };
        if (_cinzel != null) title.AddThemeFontOverride("font", _cinzel);
        title.AddThemeFontSizeOverride("font_size", 16);
        title.Modulate = new Color(0.9f, 0.85f, 0.7f);
        vbox.AddChild(title);

        var subtitle = new Label
        {
            Text = "Select 2 passive abilities to bring into this run.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        if (_imFell != null) subtitle.AddThemeFontOverride("font", _imFell);
        subtitle.AddThemeFontSizeOverride("font_size", 11);
        subtitle.Modulate = Colors.LightGray;
        vbox.AddChild(subtitle);
        vbox.AddChild(new HSeparator());

        // Dynamic option buttons populated by ShowPassiveScreen
        _passiveOptionContainer = new VBoxContainer();
        vbox.AddChild(_passiveOptionContainer);

        vbox.AddChild(new HSeparator());

        _passiveConfirmBtn = new Button { Text = "Confirm (0/2)", Disabled = true };
        if (_cinzel != null) _passiveConfirmBtn.AddThemeFontOverride("font", _cinzel);
        _passiveConfirmBtn.AddThemeFontSizeOverride("font_size", 13);
        _passiveConfirmBtn.Modulate = new Color(0.4f, 1f, 0.6f);
        _passiveConfirmBtn.Pressed += OnPassiveConfirmPressed;
        vbox.AddChild(_passiveConfirmBtn);

        return container;
    }

    private void ShowPassiveScreen(string wardenId, Action onConfirm)
    {
        _onPassiveConfirmed = onConfirm;
        _selectedPoolIds.Clear();

        // Clear previous options
        if (_passiveOptionContainer != null)
            foreach (var child in _passiveOptionContainer.GetChildren())
                child.QueueFree();

        // Populate with this warden's pool passives
        var poolPassives = GameBridge.GetPoolPassives(wardenId);
        foreach (var passive in poolPassives)
        {
            var btn = new Button
            {
                Text         = $"{passive.Name}\n{passive.Description}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                ToggleMode   = true,
                TooltipText  = passive.Flavor
            };
            if (_imFell != null) btn.AddThemeFontOverride("font", _imFell);
            btn.AddThemeFontSizeOverride("font_size", 11);
            var passiveId = passive.Id;
            btn.Toggled += pressed =>
            {
                if (pressed)
                {
                    if (_selectedPoolIds.Count >= 2)
                    {
                        btn.ButtonPressed = false; // reject over-selection
                        return;
                    }
                    _selectedPoolIds.Add(passiveId);
                }
                else
                {
                    _selectedPoolIds.Remove(passiveId);
                }
                UpdatePassiveConfirmBtn();
            };
            _passiveOptionContainer?.AddChild(btn);
        }

        UpdatePassiveConfirmBtn();

        if (_encounterScreen != null) _encounterScreen.Visible = false;
        if (_passiveScreen   != null) _passiveScreen.Visible   = true;
    }

    private void UpdatePassiveConfirmBtn()
    {
        if (_passiveConfirmBtn == null) return;
        int count = _selectedPoolIds.Count;
        _passiveConfirmBtn.Text     = $"Confirm ({count}/2)";
        _passiveConfirmBtn.Disabled = count != 2;
    }

    private void OnPassiveConfirmPressed()
    {
        GameBridge.SelectedPoolPassiveIds = _selectedPoolIds.ToArray();
        if (_passiveScreen != null) _passiveScreen.Visible = false;
        _onPassiveConfirmed?.Invoke();
    }

    // ── Chain continue screen ─────────────────────────────────────────────────

    private Control BuildChainContinueScreen(Control parent)
    {
        var container = new Control();
        container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(container);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        container.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(380, 200);
        panel.AddChild(vbox);

        // Title and carryover labels (populated dynamically in OnChainAdvanceReady)
        var titleLabel    = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        var carryoverLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = Colors.LightGray };
        var continueBtn   = new Button();

        if (_cinzel != null) titleLabel.AddThemeFontOverride("font", _cinzel);
        titleLabel.AddThemeFontSizeOverride("font_size", 16);
        titleLabel.Modulate = new Color(0.9f, 0.85f, 0.7f);

        if (_imFell != null) carryoverLabel.AddThemeFontOverride("font", _imFell);
        carryoverLabel.AddThemeFontSizeOverride("font_size", 11);

        if (_cinzel != null) continueBtn.AddThemeFontOverride("font", _cinzel);
        continueBtn.AddThemeFontSizeOverride("font_size", 13);
        continueBtn.Modulate = new Color(0.4f, 1f, 0.6f);

        vbox.AddChild(titleLabel);
        vbox.AddChild(carryoverLabel);
        vbox.AddChild(new HSeparator());
        vbox.AddChild(continueBtn);

        // Store labels/button so OnChainAdvanceReady can update them
        _chainContinueTitleLabel    = titleLabel;
        _chainContinueCarryoverLabel = carryoverLabel;
        _chainContinueBtn           = continueBtn;

        return container;
    }

    // Labels/button for the chain continue screen (set during BuildChainContinueScreen)
    private Label?  _chainContinueTitleLabel;
    private Label?  _chainContinueCarryoverLabel;
    private Button? _chainContinueBtn;

    private void OnChainAdvanceReady(int nextEncounterNumber, string carryoverSummary)
    {
        if (_chainContinueTitleLabel != null)
            _chainContinueTitleLabel.Text = Loc.Get("CHAIN_RESULT_TITLE", GameBridge.ChainIndex);

        if (_chainContinueCarryoverLabel != null)
            _chainContinueCarryoverLabel.Text = carryoverSummary;

        if (_chainContinueBtn != null)
        {
            var nextName = GetEncounterDisplayName(GameBridge.ChainEncounterIds[nextEncounterNumber - 1]);
            _chainContinueBtn.Text = Loc.Get("CHAIN_CONTINUE", nextEncounterNumber) + $"\n{nextName}";
            _chainContinueBtn.Pressed -= OnContinueChainPressed; // avoid double-subscribe
            _chainContinueBtn.Pressed += OnContinueChainPressed;
        }

        // Re-enable the CanvasLayer and show the chain continue screen over the game board.
        Visible = true;
        if (_chainContinueScreen != null) _chainContinueScreen.Visible = true;
    }

    private void OnContinueChainPressed()
    {
        if (_chainContinueScreen != null) _chainContinueScreen.Visible = false;
        Visible = false; // hide overlay so game input is unblocked again
        GameBridge.Instance?.ContinueChain();
    }

    // ── Public navigation reset (called by GameBridge.ExecuteConsoleCommand) ──

    /// <summary>Resets the selector to the warden-choice screen and makes the overlay visible.</summary>
    public void ShowWardenScreen()
    {
        if (_wardenScreen        != null) _wardenScreen.Visible        = true;
        if (_modeScreen          != null) _modeScreen.Visible          = false;
        if (_encounterScreen     != null) _encounterScreen.Visible     = false;
        if (_passiveScreen       != null) _passiveScreen.Visible       = false;
        if (_chainContinueScreen != null) _chainContinueScreen.Visible = false;
        Visible = true;
    }

    // ── Navigation helpers ────────────────────────────────────────────────────

    private void ShowModeScreen(string wardenId)
    {
        _selectedWarden = wardenId;
        if (_wardenScreen != null) _wardenScreen.Visible = false;
        if (_modeScreen   != null) _modeScreen.Visible   = true;
    }

    private void ShowEncounterScreen()
    {
        if (_modeScreen      != null) _modeScreen.Visible      = false;
        if (_encounterScreen != null) _encounterScreen.Visible = true;
    }

    // ── Start helpers ─────────────────────────────────────────────────────────

    private void StartSingleEncounter(string wardenId, string encounterId)
    {
        GameBridge.ChainEncounterIds   = Array.Empty<string>();
        GameBridge.ChainIndex          = 0;
        GameBridge.SelectedEncounterId = encounterId;
        ShowPassiveScreen(wardenId, () => LaunchEncounter(wardenId));
    }

    private void StartChainEncounter(string wardenId)
    {
        GameBridge.ChainEncounterIds   = (string[])_chainSlots.Clone();
        GameBridge.ChainIndex          = 0;
        GameBridge.SelectedEncounterId = _chainSlots[0];
        ShowPassiveScreen(wardenId, () => LaunchEncounter(wardenId));
    }

    private void StartFullRun(string wardenId, string realmId)
    {
        GameBridge.SelectedMode    = "full_run";
        GameBridge.SelectedRealmId = realmId;
        // For now: start with the realm's first encounter (standard)
        GameBridge.ChainEncounterIds   = Array.Empty<string>();
        GameBridge.ChainIndex          = 0;
        GameBridge.SelectedEncounterId = "pale_march_standard";
        ShowPassiveScreen(wardenId, () => LaunchEncounter(wardenId));
    }

    private void LaunchEncounter(string wardenId)
    {
        var game  = GetNode<Node>("/root/Game");
        var board = game?.GetNodeOrNull<Node2D>("Board");
        var ui    = game?.GetNodeOrNull<CanvasLayer>("UI");
        if (board != null) board.Visible = true;
        if (ui    != null) ui.Visible    = true;

        // Hide the entire CanvasLayer so the full-screen overlay Control
        // stops intercepting mouse input while the game is running.
        Visible = false;

        GameBridge.Instance?.StartWithWarden(wardenId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetEncounterDisplayName(string encounterId)
    {
        foreach (var (id, nameKey, _) in Encounters)
            if (id == encounterId) return Loc.Get(nameKey);
        return encounterId;
    }
}
