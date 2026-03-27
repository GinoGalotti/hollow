namespace HollowWardens.Core.Encounter;

using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Data;
using HollowWardens.Core.Models;
using HollowWardens.Core.Map;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;

public class EncounterState
{
    public EncounterConfig Config { get; set; } = new();
    public TerritoryGraph Graph { get; set; } = TerritoryGraph.Standard;
    public List<Territory> Territories { get; set; } = new();
    public IDeckManager? Deck { get; set; }
    public IElementSystem? Elements { get; set; }
    public IDreadSystem? Dread { get; set; }
    public IFearActionSystem? FearActions { get; set; }
    public IWeaveSystem? Weave { get; set; }
    public ICombatSystem? Combat { get; set; }
    public IPresenceSystem? Presence { get; set; }
    public ICorruptionSystem? Corruption { get; set; }
    public IWardenAbility? Warden { get; set; }
    public ActionCard? CurrentActionCard { get; set; }
    public int CurrentTide { get; set; }
    public TurnPhase CurrentPhase { get; set; }
    public GameRandom? Random { get; set; }
    public ActionLog ActionLog { get; set; } = new();
    public WardenData? WardenData { get; set; }
    public PassiveGating? PassiveGating { get; set; }
    public BalanceConfig Balance { get; set; } = new();

    /// <summary>
    /// Optional strategy hook for ordering Provocation candidate territories.
    /// Set by EncounterRunner before running tides. Return ordered territory IDs (first = highest
    /// priority), or null to fall back to RootAbility's default heuristic.
    /// Func signature avoids a circular dependency between Encounter and Run namespaces.
    /// </summary>
    public Func<IReadOnlyList<Territory>, EncounterState, IEnumerable<string>?>? ProvocationSelector { get; set; }

    public Territory? GetTerritory(string id) => Territories.FirstOrDefault(t => t.Id == id);

    /// <summary>Applies the encounter's FearMultiplier to a base fear amount.</summary>
    public int ApplyFearMultiplier(int baseFear) => (int)(baseFear * Config.FearMultiplier);
    public IEnumerable<Territory> TerritoriesWithInvaders() => Territories.Where(t => t.Invaders.Any(i => i.IsAlive));
    public IEnumerable<Territory> TerritoriesWithNatives() => Territories.Where(t => t.Natives.Any(n => n.IsAlive));

    /// <summary>
    /// Extracts a BoardCarryover snapshot from the end of this encounter.
    /// Corruption: L0-L1 → 0 pts, L2 → 3 pts (persists as L1 threshold), L3 → full points.
    /// </summary>
    public BoardCarryover ExtractCarryover()
    {
        var corruption = new Dictionary<string, int>();
        foreach (var t in Territories)
        {
            int pts = t.CorruptionLevel switch
            {
                0 or 1 => 0,
                2      => 3,   // L2 → persists as L1 threshold (3 pts)
                _      => t.CorruptionPoints  // L3 → full persistence
            };
            if (pts > 0)
                corruption[t.Id] = pts;
        }

        var dissolved = Deck?.DissolvedCards.Select(c => c.Id).ToList() ?? new List<string>();

        // Collect passives unlocked beyond the warden's base set
        var passives = PassiveGating?.ActivePassives.ToList() ?? new List<string>();

        int maxWeave   = Weave?.MaxWeave ?? 20;
        int finalWeave = Weave?.CurrentWeave ?? 20;
        int weaveLoss  = BoardCarryover.CalculateMaxWeaveLoss(maxWeave, finalWeave);

        return new BoardCarryover
        {
            CorruptionCarryover     = corruption,
            FinalWeave              = finalWeave,
            MaxWeave                = maxWeave - weaveLoss,
            DreadLevel              = Dread?.DreadLevel   ?? 1,
            TotalFearGenerated      = Dread?.TotalFearGenerated ?? 0,
            PermanentlyRemovedCards = dissolved,
            UnlockedPassives        = passives
        };
    }
}
