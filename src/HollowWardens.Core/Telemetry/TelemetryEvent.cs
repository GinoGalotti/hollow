namespace HollowWardens.Core.Telemetry;

/// <summary>Base class for all telemetry records.</summary>
public abstract class TelemetryRecord
{
    public string GameVersion { get; set; } = Core.GameVersion.Full;
    public string BalanceHash { get; set; } = "";
    public int SchemaVersion { get; set; } = 1;
    public string Source { get; set; } = "player"; // "player", "bot", "telemetry_bot"
}

public class RunRecord : TelemetryRecord
{
    public string RunId { get; set; } = Guid.NewGuid().ToString();
    public string PlayerId { get; set; } = "local";
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    public string Warden { get; set; } = "";
    public string Mode { get; set; } = "single"; // "full_run", "single", "chain_sim"
    public string? Realm { get; set; }
    public int Seed { get; set; }
    public string Result { get; set; } = ""; // "complete", "failed_e1", etc.
    public int EncountersCompleted { get; set; }
    public int FinalMaxWeave { get; set; }
    public int FinalWeave { get; set; }
    public int CardsDrafted { get; set; }
    public int CardsUpgraded { get; set; }
    public int CardsRemoved { get; set; }
    public int PassivesUpgraded { get; set; }
    public int PassivesUnlocked { get; set; }
    public int TokensEarned { get; set; }
    public int TokensSpent { get; set; }
    public double DurationSeconds { get; set; }
    public string? PathJson { get; set; } // JSON array of visited node IDs
}

public class EncounterRecord : TelemetryRecord
{
    public string EncounterUid { get; set; } = Guid.NewGuid().ToString();
    public string RunId { get; set; } = "";
    public int EncounterIndex { get; set; }
    public string EncounterId { get; set; } = "";
    public string BoardLayout { get; set; } = "standard";
    public string Result { get; set; } = "";
    public string? RewardTier { get; set; }
    public int TidesCompleted { get; set; }
    public int FinalWeave { get; set; }
    public int MaxWeaveAtStart { get; set; }
    public int InvadersKilled { get; set; }
    public int NativesKilled { get; set; }
    public int HeartDamageEvents { get; set; }
    public int PeakCorruption { get; set; }
    public int TotalCorruptionAtEnd { get; set; }
    public int TotalPresenceAtEnd { get; set; }
    public int TotalFearGenerated { get; set; }
    public int Sacrifices { get; set; }
    public string? PassivesUnlockedJson { get; set; }
    public double DurationSeconds { get; set; }
    public string? ExportString { get; set; }
}

public class DecisionRecord : TelemetryRecord
{
    public string RunId { get; set; } = "";
    public int EncounterIndex { get; set; }
    public int Tide { get; set; }
    public string Phase { get; set; } = "";
    public int TurnNumber { get; set; }
    public long TimestampMs { get; set; } // ms since encounter start
    public string Type { get; set; } = ""; // see Decision Types
    // What was available
    public string? OptionsJson { get; set; }
    // What was chosen
    public string Chosen { get; set; } = "";
    public string? ChosenDetail { get; set; }
    // Context
    public int Weave { get; set; }
    public int MaxWeave { get; set; }
    public string? HandJson { get; set; }
    public string? BoardJson { get; set; }
    // Card play specifics
    public string? CardId { get; set; }
    public string? CardHalf { get; set; } // "top", "bottom"
    public string? TargetTerritory { get; set; }
    public string? ElementsBefore { get; set; }
    public string? ElementsAfter { get; set; }
}

public class TideSnapshot : TelemetryRecord
{
    public string RunId { get; set; } = "";
    public int EncounterIndex { get; set; }
    public int Tide { get; set; }
    public int Weave { get; set; }
    public int MaxWeave { get; set; }
    public int AliveInvaders { get; set; }
    public int TotalPresence { get; set; }
    public int TotalCorruption { get; set; }
    public int FearGenerated { get; set; }
    public int InvadersKilled { get; set; }
    public int InvadersArrived { get; set; }
    public int CardsInHand { get; set; }
    public int CardsInDeck { get; set; }
    public int CardsDissolved { get; set; }
}

public class EventRecord : TelemetryRecord
{
    public string EventUid { get; set; } = Guid.NewGuid().ToString();
    public string RunId { get; set; } = "";
    public int AfterEncounterIndex { get; set; }
    public string EventId { get; set; } = "";
    public string EventType { get; set; } = "";
    public int OptionChosen { get; set; }
    public string? EffectsJson { get; set; }
    public int WeaveBefore { get; set; }
    public int WeaveAfter { get; set; }
    public int TokensBefore { get; set; }
    public int TokensAfter { get; set; }
}
