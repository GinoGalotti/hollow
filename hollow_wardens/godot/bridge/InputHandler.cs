using Godot;

/// <summary>
/// Translates Godot input actions to GameBridge calls.
/// Input actions used:
///   game_end_phase  (Space) — end Vigil or Dusk
///   game_rest       (R)     — execute rest when on a rest turn
///   game_cancel     (Esc/X) — cancel current card selection
/// </summary>
public partial class InputHandler : Node
{
    public override void _UnhandledInput(InputEvent @event)
    {
        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        if (@event.IsActionPressed("game_end_phase"))
        {
            bridge.EndCurrentPhase();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("game_rest"))
        {
            bridge.TriggerRest();
            GetViewport().SetInputAsHandled();
        }
    }
}
