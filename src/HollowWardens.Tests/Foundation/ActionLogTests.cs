namespace HollowWardens.Tests.Foundation;

using HollowWardens.Core;
using HollowWardens.Core.Models;
using Xunit;

public class ActionLogTests
{
    private static GameAction MakeAction(GameActionType type, string? cardId = null) => new()
    {
        TurnNumber = 1,
        Phase      = TurnPhase.Vigil,
        Type       = type,
        CardId     = cardId
    };

    [Fact]
    public void RecordIncrementsTimestamp()
    {
        var log = new ActionLog();

        log.Record(MakeAction(GameActionType.PlayTop, "c1"));
        log.Record(MakeAction(GameActionType.PlayTop, "c2"));
        log.Record(MakeAction(GameActionType.SkipPhase));

        Assert.Equal(0, log.Actions[0].Timestamp);
        Assert.Equal(1, log.Actions[1].Timestamp);
        Assert.Equal(2, log.Actions[2].Timestamp);
    }

    [Fact]
    public void ExportContainsSeedAndActions()
    {
        var log = new ActionLog();
        log.Record(MakeAction(GameActionType.PlayTop, "card1"));
        log.Record(MakeAction(GameActionType.PlayBottom, "card2"));

        var exported = log.Export(42);

        Assert.StartsWith("SEED:", exported);
        Assert.Contains(log.Actions[0].ToString(), exported);
        Assert.Contains(log.Actions[1].ToString(), exported);
    }

    [Fact]
    public void ParseSeedExtractsCorrectly()
    {
        var log = new ActionLog();
        log.Record(MakeAction(GameActionType.Rest));

        var exported = log.Export(12345);
        int parsed   = ActionLog.ParseSeed(exported);

        Assert.Equal(12345, parsed);
    }

    [Fact]
    public void TruncateRemovesActionsAfterIndex()
    {
        var log = new ActionLog();
        for (int i = 0; i < 5; i++)
            log.Record(MakeAction(GameActionType.PlayTop, $"c{i}"));

        log.TruncateTo(2);

        // indices 0, 1, 2 remain → count = 3
        Assert.Equal(3, log.Count);
        Assert.Equal(0, log.Actions[0].Timestamp);
        Assert.Equal(2, log.Actions[2].Timestamp);
    }

    [Fact]
    public void EmptyLogExportsJustSeed()
    {
        var log      = new ActionLog();
        var exported = log.Export(7);

        Assert.Equal("SEED:7|", exported);
    }
}
