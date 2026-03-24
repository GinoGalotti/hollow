namespace HollowWardens.Core.Run;

public class RunState
{
    // Identity
    public string WardenId { get; set; } = "root";
    public string RealmId { get; set; } = "realm_1";
    public int Seed { get; set; }

    // Progression
    public int CurrentNodeIndex { get; set; } = 0;
    public List<string> VisitedNodeIds { get; set; } = new();
    public List<string> CompletedEncounterIds { get; set; } = new();
    public List<string> EncounterResults { get; set; } = new();

    // Health
    public int MaxWeave { get; set; } = 20;
    public int CurrentWeave { get; set; } = 20;

    // Dread (persists across encounters)
    public int DreadLevel { get; set; } = 1;
    public int TotalFearGenerated { get; set; } = 0;

    // Economy
    public int UpgradeTokens { get; set; } = 0;

    // Deck
    public List<string> DeckCardIds { get; set; } = new();
    public List<string> PermanentlyRemovedCardIds { get; set; } = new();
    public List<string> AppliedCardUpgradeIds { get; set; } = new();

    // Passives
    public List<string> AppliedPassiveUpgradeIds { get; set; } = new();
    public List<string> PermanentlyUnlockedPassives { get; set; } = new();

    // Board carryover
    public Dictionary<string, int> CorruptionCarryover { get; set; } = new();

    public RunState Clone()
    {
        return new RunState
        {
            WardenId                    = WardenId,
            RealmId                     = RealmId,
            Seed                        = Seed,
            CurrentNodeIndex            = CurrentNodeIndex,
            VisitedNodeIds              = new List<string>(VisitedNodeIds),
            CompletedEncounterIds       = new List<string>(CompletedEncounterIds),
            EncounterResults            = new List<string>(EncounterResults),
            MaxWeave                    = MaxWeave,
            CurrentWeave                = CurrentWeave,
            DreadLevel                  = DreadLevel,
            TotalFearGenerated          = TotalFearGenerated,
            UpgradeTokens               = UpgradeTokens,
            DeckCardIds                 = new List<string>(DeckCardIds),
            PermanentlyRemovedCardIds   = new List<string>(PermanentlyRemovedCardIds),
            AppliedCardUpgradeIds       = new List<string>(AppliedCardUpgradeIds),
            AppliedPassiveUpgradeIds    = new List<string>(AppliedPassiveUpgradeIds),
            PermanentlyUnlockedPassives = new List<string>(PermanentlyUnlockedPassives),
            CorruptionCarryover         = new Dictionary<string, int>(CorruptionCarryover),
        };
    }
}
