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
}
