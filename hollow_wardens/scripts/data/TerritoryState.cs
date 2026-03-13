using System;
using System.Collections.Generic;

// Plain C# class — not a Godot Resource. Lives in GameState.Territories dictionary.
public class TerritoryState
{
    public string Id { get; set; } = "";
    public int Corruption { get; set; } = 0;       // 0–3
    public int PresenceCount { get; set; } = 0;
    public List<InvaderUnit> InvaderUnits { get; set; } = new();
    public bool IsSacredSite { get; set; } = false;
    public bool IsEntryPoint { get; set; } = false;

    public bool IsDefended => PresenceCount > 0;

    public void Ravage()
    {
        Corruption = Math.Min(Corruption + 1, 3);
        // GameState.Instance will emit CorruptionChanged signal
    }
}
