namespace HollowWardens.Tests.Invaders;

using HollowWardens.Core.Invaders;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using Xunit;

public class PathfindingTests
{
    private static Invader Make(UnitType type, string territory, bool alternateTurn = true) =>
        new()
        {
            Id                = "t",
            UnitType          = type,
            Hp                = 3,
            MaxHp             = 3,
            TerritoryId       = territory,
            AlternateMoveTurn = alternateTurn
        };

    // --- Basic movement toward Sacred Heart ---

    [Fact]
    public void FromArrival_MovesToMiddle()
    {
        var board   = BoardState.CreatePyramid();
        var invader = Make(UnitType.Marcher, "A1");

        // A1 is adjacent only to A2 (same distance) and M1 (closer) — only M1 qualifies
        var next = InvaderPathfinding.GetNextMove(invader, board);

        Assert.Equal("M1", next);
    }

    [Fact]
    public void FromMiddle_MovesToInner()
    {
        var board   = BoardState.CreatePyramid();
        var invader = Make(UnitType.Marcher, "M1");

        // M1's only closer neighbour is I1
        var next = InvaderPathfinding.GetNextMove(invader, board);

        Assert.Equal("I1", next);
    }

    [Fact]
    public void AtSacredHeart_ReturnsNull()
    {
        var board   = BoardState.CreatePyramid();
        var invader = Make(UnitType.Marcher, "I1");

        Assert.Null(InvaderPathfinding.GetNextMove(invader, board));
    }

    // --- Preference rules ---

    [Fact]
    public void PrefersTerritory_WithPresence()
    {
        var board = BoardState.CreatePyramid();
        board.Get("M1").PresenceCount = 1;   // M1 has Presence, M2 does not

        var invader = Make(UnitType.Marcher, "A2");  // A2 → M1 or M2 both valid

        Assert.Equal("M1", InvaderPathfinding.GetNextMove(invader, board));
    }

    [Fact]
    public void PrefersTerritory_WithNatives()
    {
        var board = BoardState.CreatePyramid();
        board.Get("M2").Natives.Add(new Native { Hp = 2, MaxHp = 2, TerritoryId = "M2" });

        var invader = Make(UnitType.Marcher, "A2");  // A2 → M1 or M2 both valid

        Assert.Equal("M2", InvaderPathfinding.GetNextMove(invader, board));
    }

    // --- Ironclad alternating movement ---

    [Fact]
    public void Ironclad_SkipsMove_WhenNotAlternateTurn()
    {
        var board   = BoardState.CreatePyramid();
        var invader = Make(UnitType.Ironclad, "A1", alternateTurn: false);

        Assert.Null(InvaderPathfinding.GetNextMove(invader, board));
    }

    [Fact]
    public void Ironclad_Moves_WhenAlternateTurn()
    {
        var board   = BoardState.CreatePyramid();
        var invader = Make(UnitType.Ironclad, "A1", alternateTurn: true);

        Assert.NotNull(InvaderPathfinding.GetNextMove(invader, board));
    }

    [Fact]
    public void ToggleIroncladMove_FlipsFlag()
    {
        var invader = Make(UnitType.Ironclad, "A1", alternateTurn: false);

        InvaderPathfinding.ToggleIroncladMove(invader);
        Assert.True(invader.AlternateMoveTurn);

        InvaderPathfinding.ToggleIroncladMove(invader);
        Assert.False(invader.AlternateMoveTurn);
    }

    [Fact]
    public void ToggleIroncladMove_DoesNothing_ForNonIronclad()
    {
        var invader = Make(UnitType.Marcher, "A1", alternateTurn: false);

        InvaderPathfinding.ToggleIroncladMove(invader);

        Assert.False(invader.AlternateMoveTurn);  // unchanged
    }
}
