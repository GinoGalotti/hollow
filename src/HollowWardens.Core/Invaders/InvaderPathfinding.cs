namespace HollowWardens.Core.Invaders;

using HollowWardens.Core.Map;
using HollowWardens.Core.Models;

public static class InvaderPathfinding
{
    private const string SacredHeart = "I1";

    /// <summary>
    /// Returns the territory ID the invader should move to, or null if the invader
    /// cannot or should not move this turn.
    /// </summary>
    public static string? GetNextMove(Invader invader, BoardState board)
    {
        if (invader.TerritoryId == SacredHeart)
            return null;

        // Ironclad only moves on alternate Advance turns
        if (invader.UnitType == UnitType.Ironclad && !invader.AlternateMoveTurn)
            return null;

        var candidates = TerritoryGraph.Standard.GetNeighbors(invader.TerritoryId)
            .Where(id => IsCloserToHeart(invader.TerritoryId, id))
            .Select(id => board.Get(id))
            .ToList();

        if (candidates.Count == 0)
            return null;

        // Prefer territories with Presence or Natives
        var preferred = candidates
            .Where(t => t.HasPresence || t.Natives.Any(n => n.IsAlive))
            .ToList();

        return (preferred.Count > 0 ? preferred : candidates)
            .OrderByDescending(MovePriority)
            .ThenBy(t => t.Id)   // stable tiebreak
            .First().Id;
    }

    /// <summary>Toggles the AlternateMoveTurn flag on an Ironclad after each Advance step.</summary>
    public static void ToggleIroncladMove(Invader invader)
    {
        if (invader.UnitType == UnitType.Ironclad)
            invader.AlternateMoveTurn = !invader.AlternateMoveTurn;
    }

    private static bool IsCloserToHeart(string fromId, string toId)
    {
        int distFrom = TerritoryGraph.Standard.Distance(fromId, SacredHeart);
        int distTo   = TerritoryGraph.Standard.Distance(toId,   SacredHeart);
        return distTo < distFrom;
    }

    private static int MovePriority(Territory t)
    {
        int score = 0;
        if (t.HasPresence) score += 2;
        if (t.Natives.Any(n => n.IsAlive)) score += 1;
        return score;
    }
}
