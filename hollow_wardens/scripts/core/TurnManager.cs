using Godot;

public partial class TurnManager : Node
{
    public enum TurnPhase { Vigil, Tide, Dusk, Resolution }

    [Signal] public delegate void PhaseChangedEventHandler(int phase);
    [Signal] public delegate void TurnStartedEventHandler(int turnNumber);
    [Signal] public delegate void TurnEndedEventHandler(int turnNumber);

    public TurnPhase CurrentPhase { get; private set; }
    public int TurnNumber { get; private set; } = 0;
    public int CardsPlayedVigil { get; private set; } = 0;
    public int CardsPlayedDusk { get; private set; } = 0;
    public bool IsEclipse { get; set; } = false;
    public bool IsResolutionPhase { get; private set; } = false;

    public int VigilLimit => IsEclipse ? 1 : 2;
    public int DuskLimit  => IsEclipse ? 2 : 1;

    public void StartTurn()
    {
        TurnNumber++;
        GameState.Instance?.IncrementRunTurn();
        CardsPlayedVigil = 0;
        CardsPlayedDusk = 0;
        CurrentPhase = TurnPhase.Vigil;
        EmitSignal(SignalName.TurnStarted, TurnNumber);
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
    }

    public void EndVigil()
    {
        CurrentPhase = TurnPhase.Tide;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        GameState.Instance?.CurrentWarden?.OnTideStart();
    }

    public void EndTide()
    {
        CurrentPhase = TurnPhase.Dusk;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
    }

    public void EndDusk()
    {
        EmitSignal(SignalName.TurnEnded, TurnNumber);
    }

    public void EnterResolution()
    {
        IsResolutionPhase = true;
        CurrentPhase = TurnPhase.Resolution;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
    }

    public bool CanPlayCard(TurnPhase phase) => phase switch
    {
        TurnPhase.Vigil => CardsPlayedVigil < VigilLimit,
        TurnPhase.Dusk  => CardsPlayedDusk < DuskLimit,
        _ => false
    };

    public void RecordCardPlayed(TurnPhase phase)
    {
        if (phase == TurnPhase.Vigil) CardsPlayedVigil++;
        else if (phase == TurnPhase.Dusk) CardsPlayedDusk++;
    }

    public void PlayerRest() =>
        GameState.Instance?.CurrentWarden?.RecoverDiscard();
}
