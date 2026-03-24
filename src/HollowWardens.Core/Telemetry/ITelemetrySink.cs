namespace HollowWardens.Core.Telemetry;

public interface ITelemetrySink : IDisposable
{
    void WriteRun(RunRecord record);
    void WriteEncounter(EncounterRecord record);
    void WriteDecision(DecisionRecord record);
    void WriteTideSnapshot(TideSnapshot record);
    void WriteEvent(EventRecord record);
    void Flush();
}
