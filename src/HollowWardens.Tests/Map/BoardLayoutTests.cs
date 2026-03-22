namespace HollowWardens.Tests.Map;

using HollowWardens.Core.Map;
using Xunit;

/// <summary>
/// Tests for the 4 board layouts: standard (3-2-1), wide (4-3-2-1),
/// narrow (2-1-1), and twin_peaks (3-2-2-1).
/// </summary>
public class BoardLayoutTests
{
    // ── Territory Counts ──────────────────────────────────────────────────────

    [Fact]
    public void StandardLayout_Has6Territories() =>
        Assert.Equal(6, TerritoryGraph.Standard.AllTerritoryIds.Length);

    [Fact]
    public void WideLayout_Has10Territories() =>
        Assert.Equal(10, TerritoryGraph.Wide.AllTerritoryIds.Length);

    [Fact]
    public void NarrowLayout_Has4Territories() =>
        Assert.Equal(4, TerritoryGraph.Narrow.AllTerritoryIds.Length);

    [Fact]
    public void TwinPeaksLayout_Has8Territories() =>
        Assert.Equal(8, TerritoryGraph.TwinPeaks.AllTerritoryIds.Length);

    // ── Arrival Rows ──────────────────────────────────────────────────────────

    [Fact]
    public void WideLayout_ArrivalRowHas4() =>
        Assert.Equal(4, TerritoryGraph.Wide.ArrivalIds.Length);

    [Fact]
    public void NarrowLayout_ArrivalRowHas2() =>
        Assert.Equal(2, TerritoryGraph.Narrow.ArrivalIds.Length);

    // ── Layout-Specific Properties ────────────────────────────────────────────

    [Fact]
    public void TwinPeaks_MiddleRowNotAdjacent() =>
        Assert.False(TerritoryGraph.TwinPeaks.IsAdjacent("M1", "M2"));

    [Fact]
    public void WideLayout_DistanceA1ToI1Is3() =>
        Assert.Equal(3, TerritoryGraph.Wide.Distance("A1", "I1"));

    [Fact]
    public void NarrowLayout_DistanceA1ToI1Is2() =>
        Assert.Equal(2, TerritoryGraph.Narrow.Distance("A1", "I1"));

    // ── Common Properties ─────────────────────────────────────────────────────

    [Fact]
    public void AllLayouts_HaveExactlyOneHeart()
    {
        Assert.Equal("I1", TerritoryGraph.Standard.HeartId);
        Assert.Equal("I1", TerritoryGraph.Wide.HeartId);
        Assert.Equal("I1", TerritoryGraph.Narrow.HeartId);
        Assert.Equal("I1", TerritoryGraph.TwinPeaks.HeartId);
    }

    [Fact]
    public void Create_UnknownLayout_DefaultsToStandard()
    {
        var graph = TerritoryGraph.Create("unknown_layout_xyz");
        Assert.Equal(6, graph.AllTerritoryIds.Length);
    }
}
