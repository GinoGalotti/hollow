namespace HollowWardens.Core.Telemetry;

public class PlayerProfile
{
    public string Name { get; set; } = "";
    public string Source { get; set; } = "telemetry";
    public int SampleSize { get; set; }
    public string? GameVersionFilter { get; set; }
    public Dictionary<string, double> CardPlayDistribution { get; set; } = new();
    public Dictionary<string, string> TargetingPreference { get; set; } = new();
    public double BottomPlayRate { get; set; }
    public RestTimingProfile RestTiming { get; set; } = new();
    public Dictionary<string, double> DraftPreferences { get; set; } = new();
    public Dictionary<string, double> UpgradePreferences { get; set; } = new();
    public double EventRiskTolerance { get; set; }
}

public class RestTimingProfile
{
    public double ForcedRestPct { get; set; }    // rest with 0 playable cards
    public double VoluntaryRestPct { get; set; }
    public double AvgCardsInHandAtRest { get; set; }
}
