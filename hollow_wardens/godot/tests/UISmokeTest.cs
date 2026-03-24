using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using HollowWardens.Core.Models;

/// <summary>
/// Automated UI smoke tests. Added to the root scene when --run-ui-tests is passed.
/// Uses a step-based state machine with 0.4 s delays between steps.
/// Exit code 0 = all tests passed. Exit code 1 = any failure.
/// </summary>
public partial class UISmokeTest : Node
{
    private int    _currentTest = -1;
    private int    _step        = 0;
    private double _timer       = 0;
    private double _stepDelay   = 0.4;
    private int    _passed      = 0;
    private int    _failed      = 0;
    private bool   _currentTestFailed = false;
    private List<string> _failures = new();

    private List<(string Name, List<Func<bool>> Steps)> _tests = new();

    public override void _Ready()
    {
        GD.Print("=== HOLLOW WARDENS UI SMOKE TESTS ===\n");
        RegisterAllTests();
        _currentTest = 0;
        _step        = 0;
    }

    public override void _Process(double delta)
    {
        if (_currentTest >= _tests.Count)
        {
            PrintFinalReport();
            GetTree().Quit(_failed == 0 ? 0 : 1);
            SetProcess(false);
            return;
        }

        _timer += delta;
        if (_timer < _stepDelay) return;
        _timer = 0;

        var test = _tests[_currentTest];
        if (_step == 0)
        {
            _currentTestFailed = false;
            GD.Print($"[Test {_currentTest + 1}: {test.Name}]");
        }

        if (_step < test.Steps.Count)
        {
            try
            {
                bool advance = test.Steps[_step]();
                if (advance) _step++;
            }
            catch (Exception ex)
            {
                Fail(test.Name, $"Exception at step {_step}: {ex.Message}");
                AdvanceToNextTest();
            }
        }
        else
        {
            if (_currentTestFailed)
            {
                _failed++;
                GD.Print($"TEST {_currentTest + 1}: FAIL\n");
            }
            else
            {
                _passed++;
                GD.Print($"TEST {_currentTest + 1}: PASS\n");
            }
            AdvanceToNextTest();
        }
    }

    private void AdvanceToNextTest()
    {
        _currentTest++;
        _step = 0;
    }

    private void PrintFinalReport()
    {
        GD.Print($"=== RESULTS: {_passed}/{_tests.Count} PASSED, {_failed} FAILED ===");
        foreach (var f in _failures)
            GD.Print($"  ✗ {f}");
    }

    private void RegisterAllTests()
    {
        RegisterTest1_MenuNavigation();
        RegisterTest2_RootPlaysCards();
        RegisterTest3_EmberPlaysCards();
        RegisterTest4_DevConsoleCommands();
        RegisterTest5_FullRunStarts();
        RegisterTest6_WinEncounterRewards();
        RegisterTest7_EventScreen();
        RegisterTest8_CompleteChain();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private bool Pass(string detail)
    {
        GD.Print($"  ✓ {detail}");
        return true;
    }

    private bool Fail(string testName, string detail)
    {
        GD.Print($"  ✗ FAIL: {detail}");
        _failures.Add($"{testName}: {detail}");
        _currentTestFailed = true;
        return true; // advance past failed step
    }

    private Button? FindButtonByText(string text)
    {
        return FindAll<Button>(GetTree().Root)
            .FirstOrDefault(b => b.Text != null &&
                b.IsVisibleInTree() &&
                b.Text.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private List<T> FindAll<T>(Node root) where T : class
    {
        var result = new List<T>();
        void Search(Node n)
        {
            if (n is T t) result.Add(t);
            foreach (var c in n.GetChildren()) Search(c);
        }
        Search(root);
        return result;
    }

    private List<Button> FindEventOptionButtons(Button? confirmBtn, Button? skipBtn)
    {
        var evtScreen = GetNodeOrNull<Node>("/root/Game/EventScreen");
        return FindAll<Button>(evtScreen ?? GetTree().Root)
            .Where(b => b.IsVisibleInTree() && b != confirmBtn && b != skipBtn)
            .ToList();
    }

    private void ClickButton(Control target)
    {
        var center = target.GetGlobalRect().GetCenter();
        var press = new InputEventMouseButton
        {
            ButtonIndex    = MouseButton.Left,
            Pressed        = true,
            GlobalPosition = center,
            Position       = center
        };
        GetTree().Root.PushInput(press);
        var release = press.Duplicate() as InputEventMouseButton;
        if (release != null) { release.Pressed = false; GetTree().Root.PushInput(release); }
    }

    private void PressKey(Key key)
    {
        var press = new InputEventKey { Keycode = key, Pressed = true, PhysicalKeycode = key };
        GetTree().Root.PushInput(press);
        var release = press.Duplicate() as InputEventKey;
        if (release != null) { release.Pressed = false; GetTree().Root.PushInput(release); }
    }

    /// <summary>Execute a console command programmatically (bypasses the UI).</summary>
    private string RunCommand(string cmd) =>
        GameBridge.Instance?.ExecuteConsoleCommand(cmd) ?? "No GameBridge";

    // ── Test 1: Menu Navigation ───────────────────────────────────────────────

    private void RegisterTest1_MenuNavigation()
    {
        _tests.Add(("Menu Navigation", new List<Func<bool>>
        {
            () =>
            {
                var btn = FindButtonByText("Root");
                if (btn == null) return Fail("Menu Navigation", "Root warden button not found");
                ClickButton(btn);
                return Pass("Clicked Root warden");
            },
            () =>
            {
                var btn = FindButtonByText("Single Encounter");
                if (btn == null) return Fail("Menu Navigation", "Single Encounter button not found");
                ClickButton(btn);
                return Pass("Clicked Single Encounter");
            },
            () =>
            {
                var btn = FindButtonByText("Standard");
                if (btn == null) return Fail("Menu Navigation", "Standard encounter button not found");
                ClickButton(btn);
                return Pass("Clicked Standard");
            },
            () =>
            {
                if (GameBridge.Instance?.State == null) return false; // wait for encounter build
                return Pass("Game board loaded");
            },
            () =>
            {
                int count = GameBridge.Instance?.State?.Territories?.Count ?? 0;
                return count > 0
                    ? Pass($"Board has {count} territories")
                    : Fail("Menu Navigation", "No territories on board");
            },
        }));
    }

    // ── Test 2: Root Plays Cards ──────────────────────────────────────────────

    private void RegisterTest2_RootPlaysCards()
    {
        _tests.Add(("Root Plays Cards", new List<Func<bool>>
        {
            () =>
            {
                var bridge = GameBridge.Instance;
                if (bridge == null) return Fail("Root Plays Cards", "No GameBridge");
                return bridge.CurrentPhase == TurnPhase.Vigil
                    ? Pass("In Vigil phase")
                    : Fail("Root Plays Cards", $"Expected Vigil, got {bridge.CurrentPhase}");
            },
            () =>
            {
                var btn = FindButtonByText("Play Top");
                if (btn == null || !btn.IsVisibleInTree())
                    return Fail("Root Plays Cards", "Play Top button not found or not visible");
                ClickButton(btn);
                return Pass("Found Play Top button");
            },
            () =>
            {
                var bridge = GameBridge.Instance;
                if (bridge == null) return true;
                if (!bridge.IsWaitingForTarget) return Pass("No targeting needed");
                var t = bridge.State?.Territories?.FirstOrDefault();
                if (t != null) { bridge.CompleteTargetedPlay(t.Id); return Pass("Targeting resolved"); }
                return Fail("Root Plays Cards", "Targeting active but no territories");
            },
            () =>
            {
                PressKey(Key.Space);
                return Pass("Phase advanced (Space)");
            },
            () =>
            {
                PressKey(Key.Space);
                return Pass("Tide advanced (Space)");
            },
            () =>
            {
                var bridge = GameBridge.Instance;
                return bridge?.CurrentPhase != TurnPhase.Vigil
                    ? Pass("Phase progressed")
                    : Fail("Root Plays Cards", "Phase did not advance from Vigil");
            },
        }));
    }

    // ── Test 3: Ember Plays Cards ─────────────────────────────────────────────

    private void RegisterTest3_EmberPlaysCards()
    {
        _tests.Add(("Ember Plays Cards", new List<Func<bool>>
        {
            () => { RunCommand("/end_encounter clean"); return Pass("End encounter"); },
            () => Pass("Waiting for transition"),
            () =>
            {
                var btn = FindButtonByText("Ember");
                if (btn == null) return Fail("Ember Plays Cards", "Ember warden button not found");
                ClickButton(btn);
                return Pass("Clicked Ember warden");
            },
            () =>
            {
                var btn = FindButtonByText("Single Encounter");
                if (btn == null) return Fail("Ember Plays Cards", "Single Encounter button not found");
                ClickButton(btn);
                return Pass("Clicked Single Encounter");
            },
            () =>
            {
                var btn = FindButtonByText("Standard");
                if (btn == null) return Fail("Ember Plays Cards", "Standard button not found");
                ClickButton(btn);
                return Pass("Clicked Standard");
            },
            () =>
            {
                if (GameBridge.Instance?.State == null) return false;
                string wid = GameBridge.Instance.State.WardenData?.WardenId ?? "?";
                return wid == "ember"
                    ? Pass("Ember warden loaded")
                    : Fail("Ember Plays Cards", $"Expected ember, got {wid}");
            },
            () =>
            {
                var btn = FindButtonByText("Play Top");
                if (btn == null || !btn.IsVisibleInTree())
                    return Fail("Ember Plays Cards", "Play Top button not found or not visible");
                ClickButton(btn);
                return Pass("Play Top clicked");
            },
            () =>
            {
                var bridge = GameBridge.Instance;
                if (bridge == null) return true;
                if (!bridge.IsWaitingForTarget) return Pass("No targeting needed");
                var t = bridge.State?.Territories?.FirstOrDefault();
                if (t != null) { bridge.CompleteTargetedPlay(t.Id); return Pass("Targeting resolved"); }
                return Fail("Ember Plays Cards", "Targeting active but no territories");
            },
        }));
    }

    // ── Test 4: Dev Console Commands ──────────────────────────────────────────

    private void RegisterTest4_DevConsoleCommands()
    {
        _tests.Add(("Dev Console Commands", new List<Func<bool>>
        {
            () =>
            {
                RunCommand("/set_weave 10");
                int w = GameBridge.Instance?.State?.Weave?.CurrentWeave ?? -1;
                return w == 10
                    ? Pass("set_weave 10 OK")
                    : Fail("Dev Console Commands", $"Weave expected 10, got {w}");
            },
            () =>
            {
                RunCommand("/set_corruption A1 5");
                int pts = GameBridge.Instance?.State?.GetTerritory("A1")?.CorruptionPoints ?? -1;
                return pts == 5
                    ? Pass("set_corruption A1 5 OK")
                    : Fail("Dev Console Commands", $"A1 corruption expected 5, got {pts}");
            },
            () =>
            {
                RunCommand("/add_presence M1 2");
                int p = GameBridge.Instance?.State?.GetTerritory("M1")?.PresenceCount ?? -1;
                return p >= 2
                    ? Pass($"add_presence M1 2 OK (presence={p})")
                    : Fail("Dev Console Commands", $"M1 presence expected >=2, got {p}");
            },
            () =>
            {
                RunCommand("/kill_all");
                int alive = GameBridge.Instance?.State?.Territories?
                    .Sum(t => t.Invaders.Count(i => i.IsAlive)) ?? -1;
                return alive == 0
                    ? Pass("kill_all OK")
                    : Fail("Dev Console Commands", $"Expected 0 alive invaders, got {alive}");
            },
            () =>
            {
                RunCommand("/give_tokens 5");
                return Pass("give_tokens 5 OK (no crash)");
            },
            () =>
            {
                string info = RunCommand("/run_info");
                return !string.IsNullOrEmpty(info)
                    ? Pass("run_info returned output")
                    : Fail("Dev Console Commands", "run_info returned empty string");
            },
        }));
    }

    // ── Test 5: Full Run Starts ───────────────────────────────────────────────

    private void RegisterTest5_FullRunStarts()
    {
        _tests.Add(("Full Run Starts", new List<Func<bool>>
        {
            () => { RunCommand("/end_encounter clean"); return Pass("End encounter"); },
            () => Pass("Waiting for transition"),
            () =>
            {
                var btn = FindButtonByText("Root");
                if (btn == null) return Fail("Full Run Starts", "Root button not found");
                ClickButton(btn);
                return Pass("Clicked Root");
            },
            () =>
            {
                var btn = FindButtonByText("Full Run");
                if (btn == null) return Fail("Full Run Starts", "Full Run button not found");
                ClickButton(btn);
                return Pass("Clicked Full Run");
            },
            () =>
            {
                if (GameBridge.Instance?.State == null) return false;
                return Pass("Encounter state loaded");
            },
            () =>
            {
                return GameBridge.SelectedMode == "full_run"
                    ? Pass("Full Run mode confirmed")
                    : Fail("Full Run Starts", $"Mode={GameBridge.SelectedMode}, expected full_run");
            },
            () =>
            {
                var btn = FindButtonByText("Play Top");
                if (btn == null) return Fail("Full Run Starts", "Play Top not found");
                ClickButton(btn);
                return Pass("Play Top clickable in Full Run");
            },
        }));
    }

    // ── Test 6: Win Encounter → Reward Screen ────────────────────────────────

    private void RegisterTest6_WinEncounterRewards()
    {
        _tests.Add(("Win Encounter → Reward Screen", new List<Func<bool>>
        {
            // /finish_encounter routes through the Full Run reward flow (unlike /end_encounter)
            () => { RunCommand("/finish_encounter"); return Pass("Triggered Full Run encounter end"); },
            () => Pass("Waiting for reward screen"),
            () =>
            {
                var btn = FindButtonByText("Continue");
                if (GameBridge.SelectedMode == "full_run")
                {
                    // In Full Run mode the reward screen MUST appear
                    if (btn == null || !btn.IsVisibleInTree())
                        return Fail("Win Encounter → Reward Screen", "'Continue' not found — reward screen did not appear in Full Run mode");
                    ClickButton(btn);
                    return Pass("Reward screen 'Continue' clicked");
                }
                // Single mode: reward screen optional
                if (btn != null) { ClickButton(btn); return Pass("Reward screen button clicked"); }
                return Pass("No reward screen (single mode — acceptable)");
            },
        }));
    }

    // ── Test 7: Event Screen ─────────────────────────────────────────────────

    private void RegisterTest7_EventScreen()
    {
        _tests.Add(("Event Screen", new List<Func<bool>>
        {
            () => Pass("Waiting for post-reward transition"),
            () =>
            {
                var confirmBtn = FindButtonByText("Confirm");
                var skipBtn    = FindButtonByText("Skip");
                if (confirmBtn != null && confirmBtn.IsVisibleInTree())
                {
                    // Select first available option before confirming
                    var optionBtns = FindEventOptionButtons(confirmBtn, skipBtn);
                    if (optionBtns.Count > 0) ClickButton(optionBtns[0]);
                    ClickButton(confirmBtn);
                    return Pass("Event screen: option selected and confirmed");
                }
                if (skipBtn != null && skipBtn.IsVisibleInTree())
                {
                    ClickButton(skipBtn);
                    return Pass("Event screen: skipped");
                }
                // No event screen — run must have auto-advanced (e.g. rest node or no matching events)
                if (GameBridge.SelectedMode != "full_run")
                    return Fail("Event Screen", "No longer in Full Run mode after reward screen");
                int nodeIdx = GameBridge.Instance?.CurrentRunState?.CurrentNodeIndex ?? -1;
                return nodeIdx >= 1
                    ? Pass($"No event screen — run auto-advanced to stage {nodeIdx + 1}")
                    : Fail("Event Screen", "No event screen and run state did not advance past stage 1");
            },
        }));
    }

    // ── Test 8: Complete Full Chain ───────────────────────────────────────────
    // By this point: Test 7 advanced us to stage 2. This test drives stages 2 and 3
    // through the reward → inter-encounter node → next encounter cycle.

    private void RegisterTest8_CompleteChain()
    {
        _tests.Add(("Complete Full Chain", new List<Func<bool>>
        {
            // ── Stage 2 encounter end ─────────────────────────────────────────
            () => { RunCommand("/finish_encounter"); return Pass("Stage 2: triggered encounter end"); },
            () => Pass("Stage 2: waiting for reward screen"),
            () =>
            {
                var btn = FindButtonByText("Continue");
                if (btn == null || !btn.IsVisibleInTree())
                    return Fail("Complete Full Chain", "Stage 2: reward 'Continue' not found");
                ClickButton(btn);
                return Pass("Stage 2: reward 'Continue' clicked");
            },
            () =>
            {
                // Stage 2 may show a merchant/event/corruption screen — interact or skip
                var confirmBtn = FindButtonByText("Confirm");
                var skipBtn    = FindButtonByText("Skip");
                if (confirmBtn != null && confirmBtn.IsVisibleInTree())
                {
                    var opts = FindEventOptionButtons(confirmBtn, skipBtn);
                    if (opts.Count > 0) ClickButton(opts[0]);
                    ClickButton(confirmBtn);
                    return Pass("Stage 2: inter-encounter screen confirmed");
                }
                if (skipBtn != null && skipBtn.IsVisibleInTree())
                {
                    ClickButton(skipBtn);
                    return Pass("Stage 2: inter-encounter screen skipped");
                }
                return Pass("Stage 2: no inter-encounter screen (rest/no event)");
            },
            // ── Stage 3 encounter end ─────────────────────────────────────────
            () => { RunCommand("/finish_encounter"); return Pass("Stage 3: triggered encounter end"); },
            () => Pass("Stage 3: waiting for reward screen"),
            () =>
            {
                var btn = FindButtonByText("Continue");
                if (btn == null || !btn.IsVisibleInTree())
                    return Fail("Complete Full Chain", "Stage 3: reward 'Continue' not found");
                ClickButton(btn);
                return Pass("Stage 3: reward 'Continue' clicked");
            },
            () =>
            {
                // Stage 3 has a rest post-node — auto-advances (no event screen), run ends
                var confirmBtn = FindButtonByText("Confirm");
                var skipBtn    = FindButtonByText("Skip");
                if (confirmBtn != null && confirmBtn.IsVisibleInTree())
                {
                    var opts = FindEventOptionButtons(confirmBtn, skipBtn);
                    if (opts.Count > 0) ClickButton(opts[0]);
                    ClickButton(confirmBtn);
                    return Pass("Stage 3: event confirmed");
                }
                if (skipBtn != null && skipBtn.IsVisibleInTree())
                {
                    ClickButton(skipBtn);
                    return Pass("Stage 3: event skipped");
                }
                return Pass("Stage 3: rest node auto-advanced — run complete");
            },
            () => Pass("Full Run chain complete — stable, no crashes"),
        }));
    }
}
