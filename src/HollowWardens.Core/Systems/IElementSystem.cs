namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Models;

public interface IElementSystem
{
    int Get(Element element);
    void AddElements(Element[] elements, int multiplier = 1);
    void Decay();
    /// <summary>Apply extra decay to all elements (e.g., Root's rest penalty).</summary>
    void ApplyExtraDecay(int amount);
    void OnNewTurn();
    void OnRestTurn();
    IReadOnlyList<(Element Element, int Tier)> GetBankedEffects();
    void ResolveBanked(Element element, int tier);
    void ClearBanked();
}
