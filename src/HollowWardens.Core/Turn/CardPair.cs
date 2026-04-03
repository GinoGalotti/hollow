namespace HollowWardens.Core.Turn;

using HollowWardens.Core.Models;

/// <summary>
/// A pair of cards selected for a single turn: one played as Top (fast/slow), one as Bottom.
/// The core decision unit of the pairing system.
/// </summary>
public record CardPair(Card TopCard, Card BottomCard)
{
    /// <summary>Top card resolves BEFORE the Tide (Fast phase).</summary>
    public bool TopIsFast => TopCard.TopTiming == CardTiming.Fast;

    /// <summary>Top card resolves AFTER the Tide (Slow phase).</summary>
    public bool TopIsSlow => TopCard.TopTiming == CardTiming.Slow;
}
