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

    /// <summary>Max weave after decay applied at the end of the encounter.</summary>
    public int MaxWeave { get; set; } = 20;

    /// <summary>
    /// How much to reduce MaxWeave based on how much weave was missing at encounter end.
    /// 0 missing → 0 loss. 1-3 missing → 1 loss. 4-7 missing → 2 loss. 8+ missing → 3 loss.
    /// </summary>
    public static int CalculateMaxWeaveLoss(int maxWeave, int currentWeave)
    {
        int missing = maxWeave - currentWeave;
        return missing switch
        {
            0      => 0,
            <= 3   => 1,
            <= 7   => 2,
            _      => 3
        };
    }

    /// <summary>Card IDs permanently removed from the run deck.</summary>
    public List<string> PermanentlyRemovedCards { get; set; } = new();

    /// <summary>Dread level at the end of the encounter.</summary>
    public int DreadLevel { get; set; } = 1;

    /// <summary>Total fear generated across the encounter.</summary>
    public int TotalFearGenerated { get; set; } = 0;

    /// <summary>Passive IDs unlocked during the encounter.</summary>
    public List<string> UnlockedPassives { get; set; } = new();
}
