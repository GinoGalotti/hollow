namespace HollowWardens.Core.Encounter;

/// <summary>
/// Persistent state carried from one encounter to the next in a campaign run.
/// Extracted by EncounterState.ExtractCarryover() and applied by EncounterRunner.ApplyCarryover().
/// </summary>
public class BoardCarryover
{
    /// <summary>Corruption points per territory ID to carry into the next encounter.</summary>
    public Dictionary<string, int> CorruptionCarryover { get; set; } = new();

    /// <summary>Weave remaining at the end of the encounter.</summary>
    public int FinalWeave { get; set; } = 20;

    /// <summary>Card IDs permanently removed from the run deck.</summary>
    public List<string> PermanentlyRemovedCards { get; set; } = new();

    /// <summary>Dread level at the end of the encounter.</summary>
    public int DreadLevel { get; set; } = 1;

    /// <summary>Total fear generated across the encounter.</summary>
    public int TotalFearGenerated { get; set; } = 0;

    /// <summary>Passive IDs unlocked during the encounter.</summary>
    public List<string> UnlockedPassives { get; set; } = new();
}
