namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;

public class ElementSystem : IElementSystem
{
    private readonly Dictionary<Element, int> _pool = new();
    private readonly HashSet<(Element, int)> _firedThisTurn = new();
    private readonly List<(Element Element, int Tier)> _banked = new();
    private readonly BalanceConfig? _config;

    public ElementSystem(BalanceConfig? config = null)
    {
        _config = config;
        foreach (Element e in Enum.GetValues<Element>())
            _pool[e] = 0;
    }

    public int Get(Element element) => _pool[element];

    public void AddElements(Element[] elements, int multiplier = 1)
    {
        foreach (var element in elements)
        {
            _pool[element] += multiplier;
            GameEvents.ElementChanged?.Invoke(element, _pool[element]);
        }
        CheckThresholds();
    }

    public void Decay()
    {
        foreach (Element e in Enum.GetValues<Element>())
            DecayElement(e, GetDecayAmount(e));
        ClearBanked();
        GameEvents.ElementsDecayed?.Invoke();
    }

    /// <summary>
    /// Apply extra decay to ALL elements (used for Root's rest penalty).
    /// Does not clear banked effects or fire ElementsDecayed — call Decay() for the regular turn decay.
    /// </summary>
    public void ApplyExtraDecay(int amount)
    {
        if (amount <= 0) return;
        foreach (Element e in Enum.GetValues<Element>())
            DecayElement(e, amount);
    }

    private void DecayElement(Element e, int amount)
    {
        if (amount <= 0) return;
        var newVal = Math.Max(0, _pool[e] - amount);
        if (newVal != _pool[e])
        {
            _pool[e] = newVal;
            GameEvents.ElementChanged?.Invoke(e, newVal);
        }
    }

    /// <summary>Returns the per-turn decay for the given element based on its current tier.</summary>
    private int GetDecayAmount(Element e)
    {
        if (_config == null) return 1;
        if (!_config.TierScaledDecay) return _config.ElementDecayPerTurn;

        int val = _pool[e];
        if (val >= _config.GetThreshold(e, 3)) return _config.ElementDecayAtT3;
        if (val >= _config.GetThreshold(e, 2)) return _config.ElementDecayAtT2;
        if (val >= _config.GetThreshold(e, 1)) return _config.ElementDecayAtT1;
        return _config.ElementDecayBelowT1;
    }

    public void OnNewTurn()
    {
        _firedThisTurn.Clear();
    }

    /// <summary>Called at the start of a Rest turn — resets fired tracking then checks
    /// thresholds against the existing carryover pool (no new elements are added).</summary>
    public void OnRestTurn()
    {
        _firedThisTurn.Clear();
        CheckThresholds();
    }

    public IReadOnlyList<(Element Element, int Tier)> GetBankedEffects()
        => _banked.AsReadOnly();

    public void ResolveBanked(Element element, int tier)
        => _banked.Remove((element, tier));

    public void ClearBanked()
        => _banked.Clear();

    private void CheckThresholds()
    {
        foreach (Element e in Enum.GetValues<Element>())
        {
            int val = _pool[e];
            TryFireTier(e, 1, _config?.GetThreshold(e, 1) ?? 4, val);
            TryFireTier(e, 2, _config?.GetThreshold(e, 2) ?? 7, val);
            TryFireTier(e, 3, _config?.GetThreshold(e, 3) ?? 11, val);
        }
    }

    private void TryFireTier(Element element, int tier, int threshold, int value)
    {
        if (value >= threshold && _firedThisTurn.Add((element, tier)))
        {
            _banked.Add((element, tier));
            GameEvents.ThresholdTriggered?.Invoke(element, tier);
        }
    }
}
