namespace HollowWardens.Core.Wardens;

using HollowWardens.Core.Models;

/// <summary>
/// Tracks which passives are active for the current encounter.
/// Base passives (3 per warden) are always active.
/// Pool passives start locked and are unlocked via end-of-encounter rewards
/// (or ForceUnlock for sim/testing).
/// </summary>
public class PassiveGating
{
    private readonly HashSet<string> _activePassives = new();
    private readonly HashSet<string> _basePassives   = new(); // always-active; never hidden
    private readonly HashSet<string> _lockedPassives = new();
    private readonly HashSet<string> _upgradedPassives = new();
    private readonly string _wardenId;

    // Pool passives available this run (null = all pool passives available — default for sim/tests)
    private HashSet<string>? _runAvailablePool;

    /// <summary>Fires when a passive is unlocked. Args: (passiveId, passiveName)</summary>
    public event Action<string, string>? PassiveUnlocked;

    public PassiveGating(string wardenId)
    {
        _wardenId = wardenId;
        if (wardenId == "root")        InitializeRoot();
        else if (wardenId == "ember")  InitializeEmber();
    }

    private void InitializeRoot()
    {
        // Always active — base passives
        _activePassives.Add("network_fear");
        _activePassives.Add("dormancy");
        _activePassives.Add("assimilation");
        _basePassives.Add("network_fear");
        _basePassives.Add("dormancy");
        _basePassives.Add("assimilation");

        // Pool passives: rest_growth, presence_provocation, network_slow
        // These start locked — unlocked via end-of-encounter rewards (or ForceUnlock for sim)
    }

    private void InitializeEmber()
    {
        // Always active — base passives
        _activePassives.Add("ash_trail");
        _activePassives.Add("flame_out");
        _activePassives.Add("scorched_earth");
        _basePassives.Add("ash_trail");
        _basePassives.Add("flame_out");
        _basePassives.Add("scorched_earth");

        // Pool passives: ember_fury, heat_wave, controlled_burn, phoenix_spark
        // These start locked — unlocked via end-of-encounter rewards (or ForceUnlock for sim)
    }

    public bool IsActive(string passiveId) => _activePassives.Contains(passiveId);

    /// <summary>
    /// Marks the given pool passive IDs as available for this run.
    /// Call once before the encounter starts; omit to make all pool passives available (sim/test default).
    /// </summary>
    public void SetRunPassives(IEnumerable<string> selectedPoolPassiveIds)
        => _runAvailablePool = new HashSet<string>(selectedPoolPassiveIds);

    /// <summary>
    /// Returns true if this passive should be visible in the current encounter.
    /// Base passives: always true. Pool passives: only if selected for this run.
    /// </summary>
    public bool IsRunAvailable(string passiveId)
    {
        if (_lockedPassives.Contains(passiveId)) return false;
        if (_basePassives.Contains(passiveId))   return true;
        if (_runAvailablePool == null)            return true;  // no selection set = all pool available
        return _runAvailablePool.Contains(passiveId);
    }

    /// <summary>
    /// Marks a passive as upgraded. Returns false if already upgraded.
    /// The upgrade ID is the passive upgrade's ID (e.g. "network_fear_u1").
    /// </summary>
    public bool UpgradePassive(string upgradeId)
    {
        if (_upgradedPassives.Contains(upgradeId)) return false;
        _upgradedPassives.Add(upgradeId);
        return true;
    }

    /// <summary>Returns true if the given upgrade ID has been applied.</summary>
    public bool IsUpgraded(string upgradeId) => _upgradedPassives.Contains(upgradeId);

    public IReadOnlySet<string> UpgradedPassives => _upgradedPassives;

    public IReadOnlySet<string> ActivePassives => _activePassives;

    /// <summary>Force a passive to be active (for sim profiles / testing / reward-based unlock).</summary>
    public void ForceUnlock(string passiveId)
    {
        if (_activePassives.Contains(passiveId)) return; // already active
        if (_lockedPassives.Contains(passiveId)) return; // force-locked, cannot unlock
        _activePassives.Add(passiveId);
        PassiveUnlocked?.Invoke(passiveId, passiveId);
    }

    /// <summary>Force a passive to be permanently inactive (survives unlock attempts).</summary>
    public void ForceLock(string passiveId)
    {
        _activePassives.Remove(passiveId);
        _lockedPassives.Add(passiveId);
    }

    /// <summary>Reset for new encounter (back to base passives only, preserves run selection).</summary>
    public void Reset()
    {
        _activePassives.Clear();
        _basePassives.Clear();
        _upgradedPassives.Clear();
        if (_wardenId == "root")       InitializeRoot();
        else if (_wardenId == "ember") InitializeEmber();
    }
}
