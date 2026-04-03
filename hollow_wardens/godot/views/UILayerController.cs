using Godot;

/// <summary>
/// Minimal script for the main UI CanvasLayer. Prevents a Godot 4.6 GDExtension
/// bug where a scriptless CanvasLayer whose script fails to load will consume the
/// first scripted child's script binding, leaving that child without its controller.
/// This script acts as the target for that consumption, protecting all child nodes.
/// Even if this script fails to load, DummyUIChild below absorbs the fallback binding.
/// </summary>
public partial class UILayerController : CanvasLayer { }
