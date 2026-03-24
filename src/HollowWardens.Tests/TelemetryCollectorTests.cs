using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Telemetry;
using Xunit;

namespace HollowWardens.Tests;

/// <summary>Capture sink that stores all written records in memory for assertions.</summary>
internal class CaptureSink : ITelemetrySink
{
    public List<RunRecord> Runs { get; } = new();
    public List<EncounterRecord> Encounters { get; } = new();
    public List<DecisionRecord> Decisions { get; } = new();
    public List<TideSnapshot> TideSnapshots { get; } = new();
    public List<EventRecord> Events { get; } = new();

    public void WriteRun(RunRecord record) => Runs.Add(record);
    public void WriteEncounter(EncounterRecord record) => Encounters.Add(record);
    public void WriteDecision(DecisionRecord record) => Decisions.Add(record);
    public void WriteTideSnapshot(TideSnapshot record) => TideSnapshots.Add(record);
    public void WriteEvent(EventRecord record) => Events.Add(record);
    public void Flush() { }
    public void Dispose() { }
}

public class TelemetryCollectorTests
{
    private static EncounterState MakeState(int weave = 18, int maxWeave = 20) =>
        new EncounterState
        {
            Config = new EncounterConfig { Id = "enc-test", BoardLayout = "standard" },
            Balance = new BalanceConfig { MaxWeave = maxWeave },
            // Weave, Deck, Elements left null — collector handles gracefully
        };

    [Fact]
    public void StartRun_SetsRunId()
    {
        var sink = new CaptureSink();
        var collector = new TelemetryCollector(sink, "abc123", source: "bot");

        collector.StartRun("root", "chain_sim", null, 42);

        // EndRun to get the RunId out
        collector.EndRun(null, "complete");

        Assert.Single(sink.Runs);
        Assert.False(string.IsNullOrEmpty(sink.Runs[0].RunId));
    }

    [Fact]
    public void EndRun_WritesRunRecord()
    {
        var sink = new CaptureSink();
        var collector = new TelemetryCollector(sink, "hash123", source: "bot");

        collector.StartRun("ember", "chain_sim", "realm_1", 99);
        collector.EndRun(null, "failed_e2");

        Assert.Single(sink.Runs);
        var r = sink.Runs[0];
        Assert.Equal("failed_e2", r.Result);
        Assert.Equal("bot", r.Source);
        Assert.Equal("hash123", r.BalanceHash);
        Assert.Equal("ember", r.Warden);
    }

    [Fact]
    public void RecordDecision_CapturesContext()
    {
        var sink = new CaptureSink();
        var collector = new TelemetryCollector(sink, "hash456");
        var state = MakeState();

        collector.StartRun("root", "single", null, 1);
        collector.SetTide(2);
        collector.SetPhase("vigil");
        collector.SetTurn(3);
        collector.RecordDecision("card_play", "root-ward", null, null, state, cardId: "root-ward", cardHalf: "top");

        Assert.Single(sink.Decisions);
        var d = sink.Decisions[0];
        Assert.Equal("card_play", d.Type);
        Assert.Equal("root-ward", d.Chosen);
        Assert.Equal("root-ward", d.CardId);
        Assert.Equal("top", d.CardHalf);
        Assert.Equal(2, d.Tide);
        Assert.Equal("vigil", d.Phase);
        Assert.Equal(3, d.TurnNumber);
        Assert.Equal(20, d.MaxWeave); // from BalanceConfig
        Assert.NotNull(d.HandJson);
        Assert.NotNull(d.BoardJson);
    }

    [Fact]
    public void RecordTideSnapshot_CapturesBoardState()
    {
        var sink = new CaptureSink();
        var collector = new TelemetryCollector(sink, "hash789");

        var state = new EncounterState
        {
            Config = new EncounterConfig { Id = "enc-scouts" },
            Balance = new BalanceConfig { MaxWeave = 20 },
            Territories = new List<Territory>
            {
                new Territory { Id = "t1", CorruptionPoints = 3, PresenceCount = 2,
                    Invaders = new List<Invader> { new Invader { Hp = 1 } } },
                new Territory { Id = "t2", CorruptionPoints = 1, PresenceCount = 1 },
            }
        };

        collector.StartRun("root", "single", null, 1);
        collector.StartEncounter("enc-scouts", "standard", 20);
        collector.SetTide(1);
        collector.RecordTideSnapshot(state, arrived: 2, killed: 1);

        Assert.Single(sink.TideSnapshots);
        var snap = sink.TideSnapshots[0];
        Assert.Equal(1, snap.AliveInvaders);
        Assert.Equal(3, snap.TotalPresence);
        Assert.Equal(4, snap.TotalCorruption);
        Assert.Equal(2, snap.InvadersArrived);
        Assert.Equal(1, snap.InvadersKilled);
    }

    [Fact]
    public void RecordEvent_CapturesBeforeAfter()
    {
        var sink = new CaptureSink();
        var collector = new TelemetryCollector(sink, "hashEVT");

        collector.StartRun("root", "full_run", "realm_1", 5);
        collector.StartEncounter("enc-a", "standard", 20);
        collector.EndEncounter(MakeState(), "win", "tier1");
        // Now at encounterIndex 1 → AfterEncounterIndex = 0
        collector.RecordEvent("evt-crossroads", "narrative", 2, null,
                              weaveBefore: 15, weaveAfter: 18,
                              tokensBefore: 2, tokensAfter: 1);

        Assert.Single(sink.Events);
        var e = sink.Events[0];
        Assert.Equal("evt-crossroads", e.EventId);
        Assert.Equal(2, e.OptionChosen);
        Assert.Equal(15, e.WeaveBefore);
        Assert.Equal(18, e.WeaveAfter);
        Assert.Equal(2, e.TokensBefore);
        Assert.Equal(1, e.TokensAfter);
        Assert.Equal(0, e.AfterEncounterIndex);
    }

    [Fact]
    public void OnInvaderKilled_IncrementsStat()
    {
        var sink = new CaptureSink();
        var collector = new TelemetryCollector(sink, "hashKILL");
        var state = MakeState();

        collector.StartRun("root", "single", null, 1);
        collector.StartEncounter("enc-a", "standard", 20);
        collector.OnInvaderKilled();
        collector.OnInvaderKilled();
        collector.OnInvaderKilled();
        collector.EndEncounter(state, "win", null);

        Assert.Single(sink.Encounters);
        Assert.Equal(3, sink.Encounters[0].InvadersKilled);
    }

    [Fact]
    public void MultipleEncounters_IndexIncrements()
    {
        var sink = new CaptureSink();
        var collector = new TelemetryCollector(sink, "hashIDX");
        var state = MakeState();

        collector.StartRun("root", "chain_sim", null, 1);
        collector.StartEncounter("enc-a", "standard", 20);
        collector.EndEncounter(state, "win", null);
        collector.StartEncounter("enc-b", "standard", 20);
        collector.EndEncounter(state, "win", null);
        collector.StartEncounter("enc-c", "standard", 20);
        collector.EndEncounter(state, "win", null);

        Assert.Equal(3, sink.Encounters.Count);
        Assert.Equal(0, sink.Encounters[0].EncounterIndex);
        Assert.Equal(1, sink.Encounters[1].EncounterIndex);
        Assert.Equal(2, sink.Encounters[2].EncounterIndex);
    }
}
