using Godot;
using System;
using System.Collections.Generic;

public partial class GameState : Node
{
    public static GameState? Instance { get; private set; }

    // Run-level state
    public int Weave { get; private set; } = 20;
    public int Fear { get; private set; } = 0;
    public int RunTurn { get; private set; } = 0;
    public int CurrentRealm { get; private set; } = 1;
    public Warden? CurrentWarden { get; set; }
    public Dictionary<string, TerritoryState> Territories { get; } = new();
    public List<InvaderUnit> ActiveInvaders { get; } = new();

    // Encounter-level state
    public EncounterData.EncounterTier EncounterTier { get; set; }
    public int TideStep { get; set; } = 0;
    public int BreachCount { get; set; } = 0;

    // Signals
    [Signal] public delegate void WeaveChangedEventHandler(int newValue, int delta);
    [Signal] public delegate void FearChangedEventHandler(int newValue, int delta);
    [Signal] public delegate void FearThresholdReachedEventHandler(int threshold);

    public override void _Ready() => Instance = this;

    public void ModifyWeave(int delta)
    {
        Weave = Math.Max(0, Weave + delta);
        EmitSignal(SignalName.WeaveChanged, Weave, delta);
    }

    public void ModifyFear(int delta)
    {
        Fear += delta;
        EmitSignal(SignalName.FearChanged, Fear, delta);
        CheckFearThresholds();
    }

    private void CheckFearThresholds()
    {
        foreach (int threshold in new[] { 5, 12, 20 })
        {
            if (Fear >= threshold)
            {
                EmitSignal(SignalName.FearThresholdReached, threshold);
                Fear -= threshold; // Reset after threshold hit
                break;
            }
        }
    }

    public void IncrementRunTurn() => RunTurn++;

    public TerritoryState GetTerritory(string id) => Territories[id];
    public bool IsRunOver() => Weave <= 0;
}
