using HollowWardens.Core.Telemetry;
using HollowWardens.Sim;
using Xunit;

namespace HollowWardens.Tests;

public class TelemetryAggregatorTests : IDisposable
{
    private readonly string _dbPath;

    public TelemetryAggregatorTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"hw_agg_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void Aggregate_EmptyDb_ReturnsDefaultProfile()
    {
        using var sink = new SQLiteSink(_dbPath);
        sink.Flush();

        var profile = TelemetryAggregator.Aggregate(_dbPath);

        Assert.Equal(0, profile.SampleSize);
        Assert.Empty(profile.CardPlayDistribution);
    }

    [Fact]
    public void Aggregate_WithDecisions_CalculatesDistribution()
    {
        using var sink = new SQLiteSink(_dbPath);
        for (int i = 0; i < 8; i++)
            sink.WriteDecision(new DecisionRecord { RunId = "r1", Type = "card_play", Chosen = "card-a", CardHalf = "top" });
        for (int i = 0; i < 2; i++)
            sink.WriteDecision(new DecisionRecord { RunId = "r1", Type = "card_play", Chosen = "card-b", CardHalf = "top" });
        sink.Flush();

        var profile = TelemetryAggregator.Aggregate(_dbPath);

        Assert.Equal(10, profile.SampleSize);
        Assert.True(profile.CardPlayDistribution.ContainsKey("card-a"));
        Assert.Equal(0.8, profile.CardPlayDistribution["card-a"], 5);
    }

    [Fact]
    public void Aggregate_VersionFilter_ExcludesOldRuns()
    {
        using var sink = new SQLiteSink(_dbPath);
        sink.WriteDecision(new DecisionRecord { RunId = "r1", Type = "card_play", Chosen = "x", GameVersion = "0.7.0+1" });
        sink.WriteDecision(new DecisionRecord { RunId = "r2", Type = "card_play", Chosen = "y", GameVersion = "0.8.0+5" });
        sink.Flush();

        var profile = TelemetryAggregator.Aggregate(_dbPath, versionFilter: "0.8");

        Assert.Equal(1, profile.SampleSize);
    }

    [Fact]
    public void Aggregate_BottomPlayRate_Calculated()
    {
        using var sink = new SQLiteSink(_dbPath);
        for (int i = 0; i < 6; i++)
            sink.WriteDecision(new DecisionRecord { Type = "card_play", Chosen = "c", CardHalf = "top" });
        for (int i = 0; i < 4; i++)
            sink.WriteDecision(new DecisionRecord { Type = "card_play", Chosen = "c", CardHalf = "bottom" });
        sink.Flush();

        var profile = TelemetryAggregator.Aggregate(_dbPath);

        Assert.Equal(0.4, profile.BottomPlayRate, 5);
    }

    [Fact]
    public void Aggregate_RestTiming_Calculated()
    {
        using var sink = new SQLiteSink(_dbPath);
        sink.WriteDecision(new DecisionRecord { Type = "card_play", Chosen = "c", CardHalf = "top" });
        sink.WriteDecision(new DecisionRecord { Type = "card_play", Chosen = "c", CardHalf = "top" });
        sink.WriteDecision(new DecisionRecord { Type = "rest", Chosen = "rest", OptionsJson = "[]" }); // forced
        sink.WriteDecision(new DecisionRecord { Type = "rest", Chosen = "rest", OptionsJson = "[\"c\"]" }); // voluntary
        sink.Flush();

        var profile = TelemetryAggregator.Aggregate(_dbPath);

        Assert.True(profile.RestTiming.ForcedRestPct > 0);
        Assert.True(profile.RestTiming.VoluntaryRestPct > 0);
    }
}
