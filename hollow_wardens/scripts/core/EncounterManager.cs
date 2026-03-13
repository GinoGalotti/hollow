using Godot;
using System;
using System.Linq;

public partial class EncounterManager : Node
{
    public enum EncounterState { Idle, Running, Resolution, Ended }

    [Signal] public delegate void EncounterStartedEventHandler(EncounterData data);
    [Signal] public delegate void EncounterEndedEventHandler(int rewardTier);
    [Signal] public delegate void TideStepCompletedEventHandler(int step);
    [Signal] public delegate void ResolutionPhaseStartedEventHandler(int turnsRemaining);
    [Signal] public delegate void BreachOccurredEventHandler();

    public EncounterState State { get; private set; } = EncounterState.Idle;
    public EncounterData? CurrentEncounter { get; private set; }
    public int CurrentTideStep { get; private set; } = 0;
    public int ResolutionTurnsUsed { get; private set; } = 0;

    public Random? Rng { get; set; }

    private readonly TerritoryGraph _graph = new();
    private readonly TurnManager _turnManager = new();
    private readonly TideExecutor _tideExecutor = new();

    public void StartEncounter(EncounterData data)
    {
        if (State != EncounterState.Idle)
            throw new InvalidOperationException("EncounterManager must be Idle to start.");
        CurrentEncounter = data;
        CurrentTideStep = 0;
        ResolutionTurnsUsed = 0;
        InitializeTerritories(data);
        _tideExecutor.EncounterData = data;
        _tideExecutor.Graph = _graph;
        _turnManager.IsEclipse = data.IsEclipse;
        State = EncounterState.Running;
        EmitSignal(SignalName.EncounterStarted, data);
    }

    private void InitializeTerritories(EncounterData data)
    {
        GameState.Instance!.Territories.Clear();
        GameState.Instance.ActiveInvaders.Clear();
        foreach (var id in _graph.AllIds)
        {
            var territory = new TerritoryState
            {
                Id = id,
                IsEntryPoint = id == "E1" || id == "E2" || id == "E3",
                IsSacredSite = id == "SS"
            };
            if (data.StartingCorruption.ContainsKey(id))
                territory.Corruption = (int)data.StartingCorruption[id];
            GameState.Instance.Territories[id] = territory;
        }
    }

    public void RunTideStep()
    {
        if (State != EncounterState.Running)
            throw new InvalidOperationException("EncounterManager must be Running.");
        CurrentTideStep++;
        GameState.Instance!.TideStep = CurrentTideStep;
        _tideExecutor.SpawnPhase(CurrentTideStep);
        _tideExecutor.AdvancePhase();
        _tideExecutor.RavagePhase();
        _tideExecutor.EscalatePhase(CurrentTideStep);
        EmitSignal(SignalName.TideStepCompleted, CurrentTideStep);
        if (CheckForResolution())
            BeginResolutionPhase();
    }

    public bool CheckForResolution() =>
        CurrentEncounter != null && CurrentTideStep >= CurrentEncounter.TideSteps;

    private void BeginResolutionPhase()
    {
        State = EncounterState.Resolution;
        _turnManager.EnterResolution();
        var territories = GameState.Instance?.Territories.Values.ToList() ?? new();
        GameState.Instance?.CurrentWarden?.OnResolutionStart(territories);
        EmitSignal(SignalName.ResolutionPhaseStarted, CurrentEncounter!.ResolutionTurns - ResolutionTurnsUsed);
    }

    public void RunResolutionTurn()
    {
        if (State != EncounterState.Resolution)
            throw new InvalidOperationException("EncounterManager must be in Resolution.");
        if (!HasActiveInvaders())
        {
            EndEncounter();
            return;
        }
        ResolutionTurnsUsed++;
        if (CheckForBreach())
        {
            GameState.Instance!.BreachCount++;
            EmitSignal(SignalName.BreachOccurred);
            EndEncounter();
            return;
        }
        EmitSignal(SignalName.ResolutionPhaseStarted, CurrentEncounter!.ResolutionTurns - ResolutionTurnsUsed);
    }

    private bool CheckForBreach() =>
        CurrentEncounter != null &&
        ResolutionTurnsUsed >= CurrentEncounter.ResolutionTurns &&
        HasActiveInvaders();

    public int EvaluateRewardTier()
    {
        if (!HasActiveInvaders() && ResolutionTurnsUsed == 0) return 0; // Clean
        if (!HasActiveInvaders()) return 1;                             // Weathered
        return 2;                                                        // Breach
    }

    private bool HasActiveInvaders() =>
        GameState.Instance?.ActiveInvaders.Any(u => !u.IsDefeated) ?? false;

    private void EndEncounter()
    {
        var warden = GameState.Instance?.CurrentWarden;
        if (warden != null)
        {
            var tier = CurrentEncounter?.Tier ?? EncounterData.EncounterTier.Standard;
            var rng = Rng ?? new Random();
            foreach (var card in warden.DissolvedThisEncounter.ToList())
            {
                if (tier == EncounterData.EncounterTier.Elite)
                {
                    if (rng.NextDouble() < 0.5)
                    {
                        warden.PermanentlyRemoved.Add(card);
                        EventBus.Instance?.EmitSignal(EventBus.SignalName.CardPermanentlyRemoved, card);
                    }
                    else
                    {
                        warden.Discard.Add(card);
                    }
                }
                else if (tier == EncounterData.EncounterTier.Standard)
                {
                    warden.Discard.Add(card);
                }
                // Boss: DissolvedThisEncounter is empty — CardEngine routes Boss dissolves to PermanentlyRemoved directly
            }
            warden.DissolvedThisEncounter.Clear();
        }
        State = EncounterState.Ended;
        EmitSignal(SignalName.EncounterEnded, EvaluateRewardTier());
    }
}
