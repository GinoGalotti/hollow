namespace HollowWardens.Core.Wardens;

using HollowWardens.Core.Models;

/// <summary>
/// Tracks which passives are active for the current encounter.
/// Some start active, others unlock when element thresholds are first hit.
/// </summary>
public class PassiveGating
{
    private readonly HashSet<string> _activePassives = new();
    private readonly Dictionary<(Element element, int tier), string> _unlockConditions = new();
    private readonly string _wardenId;

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
        // Always active
        _activePassives.Add("network_fear");
        _activePassives.Add("dormancy");
        _activePassives.Add("assimilation");

        // Unlock conditions
        _unlockConditions[(Element.Root, 1)]   = "rest_growth";
        _unlockConditions[(Element.Root, 2)]   = "presence_provocation";
        _unlockConditions[(Element.Shadow, 1)] = "network_slow";
    }

    private void InitializeEmber()
    {
        // Always active
        _activePassives.Add("ash_trail");
        _activePassives.Add("flame_out");
        _activePassives.Add("scorched_earth");

        // Unlock conditions
        _unlockConditions[(Element.Ash, 1)]    = "ember_fury";
        _unlockConditions[(Element.Ash, 2)]    = "heat_wave";
        _unlockConditions[(Element.Shadow, 1)] = "controlled_burn";
        _unlockConditions[(Element.Gale, 1)]   = "phoenix_spark";
    }

    public bool IsActive(string passiveId) => _activePassives.Contains(passiveId);

    /// <summary>
    /// Called when an element threshold fires. Returns the unlocked passive ID, or null.
    /// </summary>
    public string? OnThresholdTriggered(Element element, int tier)
    {
        var key = (element, tier);
        if (!_unlockConditions.TryGetValue(key, out var passiveId)) return null;
        if (_activePassives.Contains(passiveId)) return null; // already unlocked

        _activePassives.Add(passiveId);
        _unlockConditions.Remove(key); // one-time unlock
        PassiveUnlocked?.Invoke(passiveId, passiveId); // UI can resolve name
        return passiveId;
    }

    public IReadOnlySet<string> ActivePassives => _activePassives;

    /// <summary>Force a passive to be active (for sim profiles / testing).</summary>
    public void ForceUnlock(string passiveId) => _activePassives.Add(passiveId);

    /// <summary>Force a passive to be inactive (for sim profiles / testing).</summary>
    public void ForceLock(string passiveId) => _activePassives.Remove(passiveId);

    /// <summary>Reset for new encounter (back to base passives only).</summary>
    public void Reset()
    {
        _activePassives.Clear();
        _unlockConditions.Clear();
        if (_wardenId == "root")       InitializeRoot();
        else if (_wardenId == "ember") InitializeEmber();
    }
}
