using HollowWardens.Core.Telemetry;
using HollowWardens.Sim;
using Microsoft.Data.Sqlite;
using Xunit;

namespace HollowWardens.Tests;

public class SQLiteSinkTests : IDisposable
{
    private readonly string _dbPath;

    public SQLiteSinkTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"hw_test_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { /* best-effort cleanup */ }
    }

    private long QueryScalar(string sql)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return (long)(cmd.ExecuteScalar() ?? 0L);
    }

    [Fact]
    public void CreateDb_CreatesAllTables()
    {
        using var sink = new SQLiteSink(_dbPath);
        sink.Flush();

        var tables = new[] { "runs", "encounters", "decisions", "tide_snapshots", "events" };
        foreach (var table in tables)
        {
            var count = QueryScalar(
                $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{table}'");
            Assert.Equal(1, count);
        }
    }

    [Fact]
    public void WriteRun_InsertsRecord()
    {
        using var sink = new SQLiteSink(_dbPath);
        var r = new RunRecord { RunId = "run-001", Warden = "root", Result = "complete" };
        sink.WriteRun(r);

        var count = QueryScalar("SELECT COUNT(*) FROM runs WHERE run_id='run-001'");
        Assert.Equal(1, count);
    }

    [Fact]
    public void WriteEncounter_InsertsRecord()
    {
        using var sink = new SQLiteSink(_dbPath);
        var r = new EncounterRecord { RunId = "run-002", EncounterId = "enc-scouts", Result = "win" };
        sink.WriteEncounter(r);

        var count = QueryScalar($"SELECT COUNT(*) FROM encounters WHERE run_id='run-002'");
        Assert.Equal(1, count);
    }

    [Fact]
    public void WriteDecision_InsertsRecord()
    {
        using var sink = new SQLiteSink(_dbPath);
        var r = new DecisionRecord { RunId = "run-003", Type = "card_play", Chosen = "root-ward" };
        sink.WriteDecision(r);

        var count = QueryScalar("SELECT COUNT(*) FROM decisions WHERE run_id='run-003'");
        Assert.Equal(1, count);
    }

    [Fact]
    public void WriteTideSnapshot_InsertsRecord()
    {
        using var sink = new SQLiteSink(_dbPath);
        var r = new TideSnapshot { RunId = "run-004", EncounterIndex = 0, Tide = 1, Weave = 18 };
        sink.WriteTideSnapshot(r);

        var count = QueryScalar("SELECT COUNT(*) FROM tide_snapshots WHERE run_id='run-004'");
        Assert.Equal(1, count);
    }

    [Fact]
    public void WriteEvent_InsertsRecord()
    {
        using var sink = new SQLiteSink(_dbPath);
        var r = new EventRecord { RunId = "run-005", EventId = "evt-crossroads", OptionChosen = 1 };
        sink.WriteEvent(r);

        var count = QueryScalar("SELECT COUNT(*) FROM events WHERE run_id='run-005'");
        Assert.Equal(1, count);
    }

    [Fact]
    public void MultipleWrites_AllPersist()
    {
        using var sink = new SQLiteSink(_dbPath);
        for (int i = 0; i < 10; i++)
        {
            sink.WriteDecision(new DecisionRecord { RunId = "run-batch", Type = "rest", Chosen = "rest" });
        }

        var count = QueryScalar("SELECT COUNT(*) FROM decisions WHERE run_id='run-batch'");
        Assert.Equal(10, count);
    }

    [Fact]
    public void RunRecord_HasVersionFields()
    {
        using var sink = new SQLiteSink(_dbPath);
        var r = new RunRecord { RunId = "run-ver", BalanceHash = "abc123456789" };
        sink.WriteRun(r);

        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT game_version, balance_hash FROM runs WHERE run_id='run-ver'";
        using var reader = cmd.ExecuteReader();
        Assert.True(reader.Read());
        Assert.False(string.IsNullOrEmpty(reader.GetString(0))); // game_version
        Assert.Equal("abc123456789", reader.GetString(1));        // balance_hash
    }

    [Fact]
    public void NullSink_DoesNotThrow()
    {
        var sink = new NullSink();
        sink.WriteRun(new RunRecord());
        sink.WriteEncounter(new EncounterRecord());
        sink.WriteDecision(new DecisionRecord());
        sink.WriteTideSnapshot(new TideSnapshot());
        sink.WriteEvent(new EventRecord());
        sink.Flush();
        sink.Dispose();
    }
}
