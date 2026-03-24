using Godot;
using HollowWardens.Core.Debug;
using HollowWardens.Core.Localization;
using HollowWardens.Core.Models;

/// <summary>
/// Developer console overlay. Toggle with backtick (`) key.
/// LineEdit input at bottom, RichTextLabel scrollback above.
/// Dispatches parsed commands to GameBridge methods.
/// </summary>
public partial class DevConsole : CanvasLayer
{
    private RichTextLabel? _output;
    private LineEdit?      _input;
    private bool           _consoleVisible;

    public override void _Ready()
    {
        Layer          = 100; // topmost
        Visible        = false;
        _consoleVisible = false;

        BuildUI();
    }

    private void BuildUI()
    {
        var bg = new ColorRect();
        bg.Color = new Color(0f, 0f, 0f, 0.82f);
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        bg.CustomMinimumSize = new Vector2(0, 300);
        AddChild(bg);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        vbox.OffsetBottom = 300;
        AddChild(vbox);

        _output = new RichTextLabel();
        _output.BbcodeEnabled    = true;
        _output.ScrollActive     = true;
        _output.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _output.CustomMinimumSize = new Vector2(0, 260);
        vbox.AddChild(_output);

        _input = new LineEdit();
        _input.PlaceholderText = Loc.Get("DEV_CONSOLE_PLACEHOLDER");
        _input.CustomMinimumSize = new Vector2(0, 28);
        _input.TextSubmitted += OnCommandSubmitted;
        vbox.AddChild(_input);

        Print(Loc.Get("DEV_CONSOLE_WELCOME"));
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Quoteleft) // backtick
            {
                _consoleVisible = !_consoleVisible;
                Visible         = _consoleVisible;
                if (_consoleVisible)
                    _input?.GrabFocus();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void OnCommandSubmitted(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _input!.Clear();

        Print($"[color=#aaaaaa]> {text}[/color]");

        var cmd = CommandParser.Parse(text);
        if (!cmd.IsValid)
        {
            Print($"[color=#ff6666]{cmd.Error}[/color]");
            return;
        }

        DispatchCommand(cmd);
    }

    private void DispatchCommand(ParsedCommand cmd)
    {
        var bridge = GetNodeOrNull<GameBridge>("/root/GameBridge");

        switch (cmd.Name)
        {
            case "help":
                if (cmd.Args.Length > 0 && CommandParser.HelpText.TryGetValue(cmd.Args[0], out var helpLine))
                    Print(helpLine);
                else
                    foreach (var (_, line) in CommandParser.HelpText)
                        Print(line);
                break;

            case "add_presence":
            {
                int count = cmd.Args.Length >= 2 && int.TryParse(cmd.Args[1], out int n) ? n : 1;
                if (bridge?.State != null && cmd.Args.Length >= 1)
                {
                    var territory = bridge.State.GetTerritory(cmd.Args[0]);
                    if (territory != null) { territory.PresenceCount += count; Print($"+{count} presence on {cmd.Args[0]}"); }
                    else Print($"Unknown territory: {cmd.Args[0]}");
                }
                break;
            }

            case "kill_all":
                if (bridge?.State != null)
                {
                    foreach (var t in bridge.State.Territories)
                        t.Invaders.Clear();
                    Print("All invaders removed");
                }
                break;

            case "export":
                Print(bridge?.ExportEncounterState() ?? "No active encounter");
                break;

            case "run_info":
                Print($"Mode={GameBridge.SelectedMode}  Realm={GameBridge.SelectedRealmId}  Warden={GameBridge.SelectedWardenId}");
                if (bridge?.State != null)
                    Print($"Weave={bridge.State.Weave?.CurrentWeave}/{bridge.State.Weave?.MaxWeave}  Tide={bridge.State.CurrentTide}");
                break;

            case "set_weave":
            case "set_max_weave":
            case "set_corruption":
            case "set_element":
            case "set_dread":
            case "spawn":
            case "add_card":
            case "upgrade_card":
            case "unlock_passive":
            case "upgrade_passive":
            case "give_tokens":
            case "trigger_event":
            case "skip_tide":
            case "end_encounter":
            case "restart":
            case "encounter":
                Print($"[color=#ffcc44]{cmd.Name}: coming soon — command parsed but not yet wired[/color]");
                break;

            default:
                Print($"[color=#ff6666]Unknown command: /{cmd.Name}. Type /help for a list.[/color]");
                break;
        }
    }

    private void Print(string line)
    {
        _output?.AppendText(line + "\n");
    }
}
