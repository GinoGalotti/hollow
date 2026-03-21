using Godot;

/// <summary>
/// Translates Godot input actions to GameBridge calls.
/// Input actions used:
///   game_end_phase  (Space) — end Vigil or Dusk
///   game_rest       (R)     — execute rest when on a rest turn
///   game_cancel     (Esc/X) — cancel current card selection
///   P               — print action log to console (debug)
/// </summary>
public partial class InputHandler : Node
{
    public override void _UnhandledInput(InputEvent @event)
    {
        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        if (@event.IsActionPressed("game_cancel"))
        {
            bridge.CancelTargeting();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("game_end_phase"))
        {
            bridge.EndCurrentPhase();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("game_rest"))
        {
            bridge.TriggerRest();
            GetViewport().SetInputAsHandled();
        }
        else if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.P })
        {
            var seed = bridge.State?.Random?.Seed ?? 0;
            GD.Print(bridge.State?.ActionLog.Export(seed) ?? $"SEED:{seed}|");
            GetViewport().SetInputAsHandled();
        }
        else if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.D })
        {
            DebugLogController.Instance?.Toggle();
            GetViewport().SetInputAsHandled();
        }
    }
}
