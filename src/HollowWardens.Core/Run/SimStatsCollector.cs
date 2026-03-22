namespace HollowWardens.Core.Run;

using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Encounter;

/// <summary>
/// Wires to GameEvents to collect SimStats during an encounter.
/// </summary>
public class SimStatsCollector
{
    private readonly SimStats _stats;
    private readonly EncounterState _state;

    private Action<Invader>? _onInvaderDefeated;
    private Action<Native, Territory>? _onNativeDefeated;
    private Action<int>? _onFearGenerated;
    private Action<Territory>? _onHeartDamage;
    private Action<Territory>? _onDesecrated;
    private Action<Territory, int>? _onSacrifice;
    private Action<Territory, int, int>? _onCorruptionChanged;
    private Action<int>? _onTideCompleted;
    private Action<Invader, Territory>? _onInvaderArrived;

    // Per-tide running counters — reset each time a TideSnapshot is recorded
    private int _tideFear;
    private int _tideKills;
    private int _tideArrivals;

    public SimStatsCollector(SimStats stats, EncounterState state)
    {
        _stats = stats;
        _state = state;
    }

    public void WireEvents()
    {
        _onInvaderDefeated = _ => { _stats.InvadersKilled++; _tideKills++; };
        _onNativeDefeated = (_, __) => _stats.NativesKilled++;
        _onFearGenerated = a => { _stats.TotalFearGenerated += a; _tideFear += a; };
        _onHeartDamage = _ => _stats.HeartDamageEvents++;
        _onDesecrated = _ => _stats.DesecrationEvents++;
        _onSacrifice = (_, __) => _stats.SacrificeCount++;
        _onCorruptionChanged = (t, pts, _) =>
        {
            if (pts > _stats.PeakCorruption) _stats.PeakCorruption = pts;
        };
        _onTideCompleted = tide => RecordTideSnapshot(tide);
        _onInvaderArrived = (_, __) => _tideArrivals++;

        GameEvents.InvaderDefeated += _onInvaderDefeated;
        GameEvents.NativeDefeated += _onNativeDefeated;
        GameEvents.FearGenerated += _onFearGenerated;
        GameEvents.HeartDamageDealt += _onHeartDamage;
        GameEvents.TerritoryDesecrated += _onDesecrated;
        GameEvents.PresenceSacrificed += _onSacrifice;
        GameEvents.CorruptionChanged += _onCorruptionChanged;
        GameEvents.TideCompleted += _onTideCompleted;
        GameEvents.InvaderArrived += _onInvaderArrived;
    }

    /// <summary>Call at the end of each Tide to record a snapshot. Resets per-tide counters.</summary>
    public void RecordTideSnapshot(int tideNumber)
    {
        _stats.TideSnapshots.Add(new SimStats.TideSnapshot
        {
            Tide = tideNumber,
            Weave = _state.Weave?.CurrentWeave ?? 0,
            TotalInvadersAlive = _state.Territories.Sum(t => t.Invaders.Count(i => i.IsAlive)),
            TotalPresence = _state.Territories.Sum(t => t.PresenceCount),
            TotalCorruption = _state.Territories.Sum(t => t.CorruptionPoints),
            MaxCorruptionLevel = _state.Territories.Max(t => t.CorruptionLevel),
            FearGeneratedThisTide = _tideFear,
            InvadersKilledThisTide = _tideKills,
            InvadersArrivedThisTide = _tideArrivals,
        });
        _tideFear = 0;
        _tideKills = 0;
        _tideArrivals = 0;
    }

    public void Finalize(EncounterResult result)
    {
        _stats.Result = result;
        _stats.FinalWeave = _state.Weave?.CurrentWeave ?? 0;
        _stats.TidesCompleted = _state.CurrentTide;
        _stats.FinalCarryover = _state.ExtractCarryover();
    }

    public void UnwireEvents()
    {
        GameEvents.InvaderDefeated -= _onInvaderDefeated;
        GameEvents.NativeDefeated -= _onNativeDefeated;
        GameEvents.FearGenerated -= _onFearGenerated;
        GameEvents.HeartDamageDealt -= _onHeartDamage;
        GameEvents.TerritoryDesecrated -= _onDesecrated;
        GameEvents.PresenceSacrificed -= _onSacrifice;
        GameEvents.CorruptionChanged -= _onCorruptionChanged;
        GameEvents.TideCompleted -= _onTideCompleted;
        GameEvents.InvaderArrived -= _onInvaderArrived;
    }
}
