using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class TideExecutor : Node
{
    [Signal] public delegate void TideStepCompletedEventHandler(int step);

    public EncounterData? EncounterData { get; set; }
    public TerritoryGraph? Graph { get; set; }
    private bool _skipNextAdvance = false;

    public override void _Ready()
    {
        if (GameState.Instance != null)
            GameState.Instance.FearThresholdReached += OnFearThresholdReached;
    }

    private void OnFearThresholdReached(int threshold)
    {
        if (threshold == 5) _skipNextAdvance = true;
    }

    public async Task ExecuteTideStep(int step)
    {
        SpawnPhase(step);
        AdvancePhase();
        RavagePhase();
        EscalatePhase(step);
        EmitSignal(SignalName.TideStepCompleted, step);
        await Task.CompletedTask;
    }

    internal void SpawnPhase(int step)
    {
        if (EncounterData == null || GameState.Instance == null) return;
        foreach (var e in EncounterData.SpawnPattern)
        {
            if (e.TideStep != step) continue;
            for (int i = 0; i < e.Count; i++)
            {
                var unit = new InvaderUnit
                {
                    TerritoryId = e.TerritoryId,
                    Hp = EncounterData.Faction?.MaxHp ?? 1,
                    Data = EncounterData.Faction
                };
                if (GameState.Instance.Territories.TryGetValue(e.TerritoryId, out var territory))
                    territory.InvaderUnits.Add(unit);
                GameState.Instance.ActiveInvaders.Add(unit);
                EventBus.Instance?.EmitSignal(EventBus.SignalName.InvaderSpawned, e.TerritoryId);
            }
        }
    }

    internal void AdvancePhase()
    {
        if (Graph == null || GameState.Instance == null) return;
        if (_skipNextAdvance) { _skipNextAdvance = false; return; }

        var targetIds = GameState.Instance.Territories
            .Where(kvp => kvp.Value.PresenceCount > 0 || kvp.Value.IsSacredSite)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var unit in GameState.Instance.ActiveInvaders.Where(u => !u.IsDefeated).ToList())
        {
            var nextId = Graph.NextStepToward(unit.TerritoryId, targetIds);
            if (nextId == null) continue;
            var fromId = unit.TerritoryId;
            if (GameState.Instance.Territories.TryGetValue(fromId, out var fromTerritory))
                fromTerritory.InvaderUnits.Remove(unit);
            unit.TerritoryId = nextId;
            if (GameState.Instance.Territories.TryGetValue(nextId, out var toTerritory))
                toTerritory.InvaderUnits.Add(unit);
            EventBus.Instance?.EmitSignal(EventBus.SignalName.InvaderAdvanced, fromId, nextId);
        }
    }

    internal void RavagePhase()
    {
        if (GameState.Instance == null) return;
        foreach (var kvp in GameState.Instance.Territories)
        {
            var t = kvp.Value;
            if (t.InvaderUnits.Count == 0 || t.IsDefended) continue;
            t.Ravage();
            EventBus.Instance?.EmitSignal(EventBus.SignalName.CorruptionChanged, t.Id, t.Corruption);
            if (t.Corruption == 3)
                EventBus.Instance?.EmitSignal(EventBus.SignalName.TerritoryDesecrated, t.Id);
            GameState.Instance.ModifyWeave(-1);
            EventBus.Instance?.EmitSignal(EventBus.SignalName.InvaderRavaged, t.Id);
            if (t.IsSacredSite && t.Corruption >= 3)
                GameState.Instance.ModifyWeave(-3);
        }
    }

    internal void EscalatePhase(int step)
    {
        if (EncounterData == null) return;
        foreach (var e in EncounterData.EscalationSchedule)
        {
            if (e.TideStep == step)
                GD.Print($"[Escalate] Step {step}: {e.DescriptionKey}");
        }
    }
}
