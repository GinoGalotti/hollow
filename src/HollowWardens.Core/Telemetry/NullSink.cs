namespace HollowWardens.Core.Telemetry;

/// <summary>No-op sink used when telemetry is disabled.</summary>
public class NullSink : ITelemetrySink
{
    public void WriteRun(RunRecord record) { }
    public void WriteEncounter(EncounterRecord record) { }
    public void WriteDecision(DecisionRecord record) { }
    public void WriteTideSnapshot(TideSnapshot record) { }
    public void WriteEvent(EventRecord record) { }
    public void Flush() { }
    public void Dispose() { }
}
