using HollowWardens.Core;
using HollowWardens.Core.Telemetry;
using Xunit;

namespace HollowWardens.Tests;

public class TelemetryModelTests
{
    [Fact]
    public void RunRecord_HasGameVersion()
    {
        var record = new RunRecord();
        Assert.Equal(GameVersion.Full, record.GameVersion);
    }

    [Fact]
    public void RunRecord_HasSchemaVersion()
    {
        var record = new RunRecord();
        Assert.Equal(1, record.SchemaVersion);
    }

    [Fact]
    public void EncounterRecord_GeneratesUniqueUid()
    {
        var a = new EncounterRecord();
        var b = new EncounterRecord();
        Assert.NotEqual(a.EncounterUid, b.EncounterUid);
    }

    [Fact]
    public void DecisionRecord_DefaultSource_IsPlayer()
    {
        var record = new DecisionRecord();
        Assert.Equal("player", record.Source);
    }
}
