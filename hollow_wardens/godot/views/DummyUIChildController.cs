using Godot;

/// <summary>
/// Sacrificial first-child of the UI CanvasLayer.
/// Godot 4.6 GDExtension bug: when a CanvasLayer's assigned C# script fails to
/// bind, the engine retries binding with the first scripted child's script — which
/// fails (VBoxContainer can't be assigned to CanvasLayer) and leaves that child
/// script-less. By placing this dummy node first, all real UI controllers are
/// protected. This node intentionally renders nothing.
/// </summary>
public partial class DummyUIChildController : VBoxContainer { }
