namespace HollowWardens.Tests;

using HollowWardens.Core.Data;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using Xunit;

public class EncounterVarietyTests
{
    // ── 1. CreatePaleMarchScouts_HasCorrectTideCount ──────────────────────────

    [Fact]
    public void CreatePaleMarchScouts_HasCorrectTideCount()
    {
        var config = EncounterLoader.CreatePaleMarchScouts();
        Assert.Equal(6, config.TideCount);
    }

    // ── 2. CreatePaleMarchScouts_WavesAreOutriderHeavy ───────────────────────

    [Fact]
    public void CreatePaleMarchScouts_WavesAreOutriderHeavy()
    {
        var config = EncounterLoader.CreatePaleMarchScouts();
        var wave1  = config.Waves.First(w => w.TurnNumber == 1);
        bool hasOutriders = wave1.Options.Any(opt =>
            opt.Units.Values.Any(units => units.Contains(UnitType.Outrider)));
        Assert.True(hasOutriders, "Wave 1 should contain Outriders");
    }

    // ── 3. CreatePaleMarchSiege_Has8Tides ────────────────────────────────────

    [Fact]
    public void CreatePaleMarchSiege_Has8Tides()
    {
        var config = EncounterLoader.CreatePaleMarchSiege();
        Assert.Equal(8, config.TideCount);
    }

    // ── 4. CreatePaleMarchSiege_HasIronclads ────────────────────────────────

    [Fact]
    public void CreatePaleMarchSiege_HasIronclads()
    {
        var config = EncounterLoader.CreatePaleMarchSiege();
        bool anyIroncladWave = config.Waves.Any(w =>
            w.Options.Any(opt =>
                opt.Units.Values.Any(units => units.Contains(UnitType.Ironclad))));
        Assert.True(anyIroncladWave, "Siege waves should contain Ironclads");
    }

    // ── 5. CreatePaleMarchElite_HasStartingCorruption ────────────────────────

    [Fact]
    public void CreatePaleMarchElite_HasStartingCorruption()
    {
        var config = EncounterLoader.CreatePaleMarchElite();
        Assert.NotNull(config.StartingCorruption);
        Assert.True(config.StartingCorruption!.ContainsKey("A1"),
            "A1 should have starting corruption");
        Assert.Equal(3, config.StartingCorruption["A1"]);
    }

    // ── 6. CreatePaleMarchElite_IsEliteTier ──────────────────────────────────

    [Fact]
    public void CreatePaleMarchElite_IsEliteTier()
    {
        var config = EncounterLoader.CreatePaleMarchElite();
        Assert.Equal(EncounterTier.Elite, config.Tier);
    }

    // ── 7. EncounterLoader_Create_ReturnsCorrectType ──────────────────────────

    [Fact]
    public void EncounterLoader_Create_ReturnsCorrectType()
    {
        var standard = EncounterLoader.Create("pale_march_standard");
        var scouts   = EncounterLoader.Create("pale_march_scouts");
        var siege    = EncounterLoader.Create("pale_march_siege");
        var elite    = EncounterLoader.Create("pale_march_elite");
        var frontier = EncounterLoader.Create("pale_march_frontier");

        Assert.Equal(6, standard.TideCount);
        Assert.Equal(6, scouts.TideCount);
        Assert.Equal(8, siege.TideCount);
        Assert.Equal(6, elite.TideCount);
        Assert.Equal(7, frontier.TideCount);
    }

    // ── 8. EncounterLoader_Create_UnknownId_Throws ───────────────────────────

    [Fact]
    public void EncounterLoader_Create_UnknownId_Throws()
    {
        Assert.Throws<ArgumentException>(() => EncounterLoader.Create("unknown_encounter"));
    }

    // ── 9. CreatePaleMarchStandard ────────────────────────────────────────────

    [Fact]
    public void CreatePaleMarchStandard_HasCorrectTideCount()
    {
        var config = EncounterLoader.CreatePaleMarchStandard();
        Assert.Equal(6, config.TideCount);
    }

    [Fact]
    public void CreatePaleMarchStandard_UsesPyramidBoard()
    {
        var config = EncounterLoader.CreatePaleMarchStandard();
        // No BoardLayout override = defaults to standard (pyramid)
        Assert.True(string.IsNullOrEmpty(config.BoardLayout) || config.BoardLayout == "standard");
    }

    // ── 10. CreatePaleMarchFrontier ───────────────────────────────────────────

    [Fact]
    public void CreatePaleMarchFrontier_Has7Tides()
    {
        var config = EncounterLoader.CreatePaleMarchFrontier();
        Assert.Equal(7, config.TideCount);
    }

    [Fact]
    public void CreatePaleMarchFrontier_UsesWideBoard()
    {
        var config = EncounterLoader.CreatePaleMarchFrontier();
        Assert.Equal("wide", config.BoardLayout);
    }

    [Fact]
    public void CreatePaleMarchFrontier_HasFourArrivalPoints()
    {
        var config = EncounterLoader.CreatePaleMarchFrontier();
        bool hasA4 = config.Waves.Any(w => w.ArrivalPoints.Contains("A4"));
        Assert.True(hasA4, "Frontier waves should use A4 (wide board)");
    }

    // ── 11. B2 — AddB2Marchers applied to 4 encounters ───────────────────────
    // Each of these would fail without the AddB2Marchers() call because
    // several wave options in each encounter have no A1 entry in their
    // original design (e.g. scouts wave-1 option-2 uses only A2+A3).

    [Fact]
    public void B2Applied_Standard_EveryWaveOptionHasA1Marcher()
    {
        var config = EncounterLoader.CreatePaleMarchStandard();
        Assert.All(config.Waves.SelectMany(w => w.Options), opt =>
            Assert.True(
                opt.Units.ContainsKey("A1") && opt.Units["A1"].Contains(UnitType.Marcher),
                "B2: every wave option should have a Marcher at A1"));
    }

    [Fact]
    public void B2Applied_Scouts_EveryWaveOptionHasA1Marcher()
    {
        var config = EncounterLoader.CreatePaleMarchScouts();
        Assert.All(config.Waves.SelectMany(w => w.Options), opt =>
            Assert.True(
                opt.Units.ContainsKey("A1") && opt.Units["A1"].Contains(UnitType.Marcher),
                "B2: every wave option should have a Marcher at A1"));
    }

    [Fact]
    public void B2Applied_Siege_EveryWaveOptionHasA1Marcher()
    {
        var config = EncounterLoader.CreatePaleMarchSiege();
        Assert.All(config.Waves.SelectMany(w => w.Options), opt =>
            Assert.True(
                opt.Units.ContainsKey("A1") && opt.Units["A1"].Contains(UnitType.Marcher),
                "B2: every wave option should have a Marcher at A1"));
    }

    [Fact]
    public void B2Applied_Elite_EveryWaveOptionHasA1Marcher()
    {
        var config = EncounterLoader.CreatePaleMarchElite();
        Assert.All(config.Waves.SelectMany(w => w.Options), opt =>
            Assert.True(
                opt.Units.ContainsKey("A1") && opt.Units["A1"].Contains(UnitType.Marcher),
                "B2: every wave option should have a Marcher at A1"));
    }
}
