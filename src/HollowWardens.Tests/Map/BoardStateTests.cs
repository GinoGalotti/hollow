namespace HollowWardens.Tests.Map;

using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using Xunit;

public class BoardStateTests
{
    [Fact]
    public void CreatePyramidHas6Territories()
    {
        var board = BoardState.CreatePyramid();
        Assert.Equal(6, board.Territories.Count);
    }

    [Fact]
    public void ArrivalRowHas3Territories()
    {
        var board = BoardState.CreatePyramid();
        Assert.Equal(3, board.GetByRow(TerritoryRow.Arrival).Count());
    }

    [Fact]
    public void MiddleRowHas2Territories()
    {
        var board = BoardState.CreatePyramid();
        Assert.Equal(2, board.GetByRow(TerritoryRow.Middle).Count());
    }

    [Fact]
    public void InnerRowHas1Territory()
    {
        var board = BoardState.CreatePyramid();
        Assert.Single(board.GetByRow(TerritoryRow.Inner));
    }

    [Fact]
    public void DistanceA1ToI1Is2()
    {
        // A1 → M1 → I1
        var dist = TerritoryGraph.Standard.Distance("A1", "I1");
        Assert.Equal(2, dist);
    }

    [Fact]
    public void RangeQueryReturnsCorrectTerritories()
    {
        var board = BoardState.CreatePyramid();
        // From M1, range 1: M1 itself (dist 0) + A1, A2, M2, I1 (all direct neighbors, dist 1)
        var inRange = board.GetInRange("M1", 1).Select(t => t.Id).OrderBy(x => x).ToList();
        var expected = new[] { "A1", "A2", "I1", "M1", "M2" }.OrderBy(x => x).ToList();
        Assert.Equal(expected, inRange);
    }
}
