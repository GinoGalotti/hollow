namespace HollowWardens.Tests;

using System.Collections.Generic;
using System.IO;
using HollowWardens.Core.Localization;
using Xunit;

public class LocalizationTests
{
    [Fact]
    public void Get_ExistingKey_ReturnsValue()
    {
        Loc.LoadFromDict(new Dictionary<string, string> { ["KEY"] = "Value" });
        Assert.Equal("Value", Loc.Get("KEY"));
    }

    [Fact]
    public void Get_MissingKey_ReturnsKeyItself()
    {
        Loc.LoadFromDict(new Dictionary<string, string>());
        Assert.Equal("MISSING", Loc.Get("MISSING"));
    }

    [Fact]
    public void Get_WithFormatArgs_FormatsCorrectly()
    {
        Loc.LoadFromDict(new Dictionary<string, string> { ["TIDE"] = "Tide {0}/{1}" });
        Assert.Equal("Tide 3/6", Loc.Get("TIDE", 3, 6));
    }

    [Fact]
    public void Get_WithBadFormatArgs_ReturnsTemplate()
    {
        Loc.LoadFromDict(new Dictionary<string, string> { ["BAD"] = "{0} {1} {2}" });
        // Passing only 1 arg for a 3-placeholder template — should not throw
        var result = Loc.Get("BAD", "only-one");
        Assert.Equal("{0} {1} {2}", result);
    }

    [Fact]
    public void LoadFromCsv_ParsesCorrectly()
    {
        var csv  = "KEY,en\nHELLO,Hello World\nBYE,Goodbye";
        var path = Path.Combine(Path.GetTempPath(), "hw_loc_test.csv");
        File.WriteAllText(path, csv);
        try
        {
            Loc.Load(path, "en");
            Assert.Equal(2, Loc.Count);
            Assert.Equal("Hello World", Loc.Get("HELLO"));
            Assert.Equal("Goodbye", Loc.Get("BYE"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LoadFromCsv_HandlesQuotedCommas()
    {
        var csv  = "KEY,en\nGREET,\"hello, world\"";
        var path = Path.Combine(Path.GetTempPath(), "hw_loc_quoted.csv");
        File.WriteAllText(path, csv);
        try
        {
            Loc.Load(path, "en");
            Assert.Equal("hello, world", Loc.Get("GREET"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LoadFromCsv_MissingFile_NoException()
    {
        var ex = Record.Exception(() => Loc.Load("/nonexistent/path/to/strings.csv", "en"));
        Assert.Null(ex);
    }

    [Fact]
    public void Clear_RemovesAllStrings()
    {
        Loc.LoadFromDict(new Dictionary<string, string> { ["KEY"] = "Value" });
        Loc.Clear();
        Assert.Equal("KEY", Loc.Get("KEY")); // returns key itself when missing
        Assert.Equal(0, Loc.Count);
    }

    [Fact]
    public void Has_ReturnsTrueForLoaded()
    {
        Loc.LoadFromDict(new Dictionary<string, string> { ["EXISTS"] = "yes" });
        Assert.True(Loc.Has("EXISTS"));
    }

    [Fact]
    public void Has_ReturnsFalseForMissing()
    {
        Loc.LoadFromDict(new Dictionary<string, string>());
        Assert.False(Loc.Has("NOPE"));
    }

    [Fact]
    public void Load_EscapeNewline_IsRealNewline()
    {
        var csv  = "KEY,en\nMULTI,\"line1\\nline2\"";
        var path = Path.Combine(Path.GetTempPath(), "hw_loc_nl_test.csv");
        File.WriteAllText(path, csv);
        try
        {
            Loc.Load(path, "en");
            var value = Loc.Get("MULTI");
            Assert.Contains('\n', value);         // real newline, not backslash-n
            Assert.DoesNotContain("\\n", value);  // no literal \n remaining
        }
        finally { File.Delete(path); }
    }

    // ── New keys added in Phase 6h ────────────────────────────────────────────

    [Theory]
    [InlineData("PHASE_VIGIL_N")]
    [InlineData("DECK_COUNTS")]
    [InlineData("BTN_BACK")]
    [InlineData("BTN_PLAY_TOP_RES")]
    [InlineData("BTN_SKIP_DMG")]
    [InlineData("LABEL_REVEALED")]
    [InlineData("LABEL_NEXT")]
    [InlineData("LABEL_NO_CARD")]
    [InlineData("LABEL_NONE")]
    [InlineData("CA_NO_DAMAGE")]
    [InlineData("CA_DMG_N")]
    [InlineData("FEAR_CONFIRM_BTN")]
    [InlineData("ENCOUNTER_SELECT_MODE_SINGLE")]
    [InlineData("ENCOUNTER_SELECT_MODE_CHAIN")]
    [InlineData("CHAIN_SLOT_E1")]
    [InlineData("CHAIN_SLOT_E2")]
    [InlineData("CHAIN_SLOT_CAPSTONE")]
    [InlineData("BTN_START_CHAIN")]
    [InlineData("CHAIN_RESULT_TITLE")]
    [InlineData("CHAIN_CONTINUE")]
    [InlineData("CHAIN_CARRYOVER")]
    public void AllNewKeys_PresentInCsvFile(string key)
    {
        var csvPath = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..",
            "data", "localization", "strings.csv"));

        Loc.Load(csvPath, "en");
        Assert.True(Loc.Has(key), $"Missing key: {key}");
    }

    [Fact]
    public void PhaseVigilN_FormatsCorrectly()
    {
        Loc.LoadFromDict(new Dictionary<string, string> { ["PHASE_VIGIL_N"] = "VIGIL  (Tide {0}/{1})" });
        Assert.Equal("VIGIL  (Tide 2/6)", Loc.Get("PHASE_VIGIL_N", 2, 6));
    }

    [Fact]
    public void DeckCounts_FormatsCorrectly()
    {
        Loc.LoadFromDict(new Dictionary<string, string> { ["DECK_COUNTS"] = "Draw: {0}  Disc: {1}\nDissolved: {2}  Dormant: {3}" });
        var result = Loc.Get("DECK_COUNTS", 5, 3, 1, 2);
        Assert.Equal("Draw: 5  Disc: 3\nDissolved: 1  Dormant: 2", result);
    }

    [Fact]
    public void CaDmgN_FormatsZeroAndNonZero()
    {
        Loc.LoadFromDict(new Dictionary<string, string> { ["CA_DMG_N"] = "{0} dmg" });
        Assert.Equal("0 dmg", Loc.Get("CA_DMG_N", 0));
        Assert.Equal("3 dmg", Loc.Get("CA_DMG_N", 3));
    }
}
