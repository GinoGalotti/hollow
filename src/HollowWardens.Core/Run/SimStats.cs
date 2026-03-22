namespace HollowWardens.Core.Run;

using HollowWardens.Core.Models;
using HollowWardens.Core.Encounter;

/// <summary>
/// Collects per-encounter statistics by subscribing to GameEvents during a simulation run.
/// Create a new instance per encounter, call WireEvents() before running, UnwireEvents() after.
/// </summary>
public class SimStats
{
    // Per-encounter totals
    public int TidesCompleted { get; set; }
    public int FinalWeave { get; set; }
    public int TotalFearGenerated { get; set; }
    public int InvadersKilled { get; set; }
    public int NativesKilled { get; set; }
    public int HeartDamageEvents { get; set; }
    public int PeakCorruption { get; set; }
    public int DesecrationEvents { get; set; }
    public int SacrificeCount { get; set; }
    public EncounterResult Result { get; set; }

    // Per-tide snapshots (tide number → snapshot)
    public List<TideSnapshot> TideSnapshots { get; set; } = new();

    // Carryover snapshot extracted at encounter end
    public BoardCarryover? FinalCarryover { get; set; }

    public class TideSnapshot
    {
        public int Tide { get; set; }
        public int Weave { get; set; }
        public int TotalInvadersAlive { get; set; }
        public int TotalPresence { get; set; }
        public int TotalCorruption { get; set; }
        public int MaxCorruptionLevel { get; set; }
        public int FearGeneratedThisTide { get; set; }
        public int InvadersKilledThisTide { get; set; }
        public int InvadersArrivedThisTide { get; set; }
    }
}
