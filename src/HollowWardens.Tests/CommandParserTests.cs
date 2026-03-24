namespace HollowWardens.Tests;

using HollowWardens.Core.Debug;
using Xunit;

public class CommandParserTests
{
    [Fact]
    public void Parse_ValidCommand_ReturnsName_Args()
    {
        var result = CommandParser.Parse("/set_weave 10");
        Assert.True(result.IsValid);
        Assert.Equal("set_weave", result.Name);
        Assert.Single(result.Args);
        Assert.Equal("10", result.Args[0]);
    }

    [Fact]
    public void Parse_EmptyInput_IsInvalid()
    {
        var result = CommandParser.Parse("   ");
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Parse_NoSlash_IsInvalid()
    {
        var result = CommandParser.Parse("set_weave 10");
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Parse_UnknownCommand_IsValid()
    {
        // Parser doesn't validate command names, just parses structure
        var result = CommandParser.Parse("/totally_unknown_cmd foo bar");
        Assert.True(result.IsValid);
        Assert.Equal("totally_unknown_cmd", result.Name);
        Assert.Equal(2, result.Args.Length);
    }

    [Fact]
    public void Parse_QuotedArgs_HandlesSpaces()
    {
        var result = CommandParser.Parse("/trigger_event \"whispering grove\" extra");
        Assert.True(result.IsValid);
        Assert.Equal("trigger_event", result.Name);
        Assert.Equal(2, result.Args.Length);
        Assert.Equal("whispering grove", result.Args[0]);
        Assert.Equal("extra", result.Args[1]);
    }
}
