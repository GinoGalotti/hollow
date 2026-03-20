namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Models;

public interface IElementSystem
{
    int Get(Element element);
    void AddElements(Element[] elements, int multiplier = 1);
    void Decay();
    void OnNewTurn();
    IReadOnlyList<(Element Element, int Tier)> GetBankedEffects();
    void ResolveBanked(Element element, int tier);
    void ClearBanked();
}
