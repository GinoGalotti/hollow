namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Events;
using HollowWardens.Core.Models;

public class ElementSystem : IElementSystem
{
    private readonly Dictionary<Element, int> _pool = new();
    private readonly HashSet<(Element, int)> _firedThisTurn = new();
    private readonly List<(Element Element, int Tier)> _banked = new();

    public ElementSystem()
    {
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
        {
            var newVal = Math.Max(0, _pool[e] - 1);
            if (newVal != _pool[e])
            {
                _pool[e] = newVal;
                GameEvents.ElementChanged?.Invoke(e, newVal);
            }
        }
        ClearBanked();
        GameEvents.ElementsDecayed?.Invoke();
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
            TryFireTier(e, 1, 4, val);
            TryFireTier(e, 2, 7, val);
            TryFireTier(e, 3, 11, val);
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
