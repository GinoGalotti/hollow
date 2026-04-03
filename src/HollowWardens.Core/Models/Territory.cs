namespace HollowWardens.Core.Models;

public class Territory
{
    public string Id { get; set; } = string.Empty;
    public TerritoryRow Row { get; set; }
    public int CorruptionPoints { get; set; }
    public int CorruptionLevel => CorruptionPoints switch
    {
        < 3 => 0,    // Clean
        < 8 => 1,    // Tainted
        < 15 => 2,   // Defiled
        _ => 3        // Desecrated
    };
    public int PresenceCount { get; set; }
    public bool IsEntryPoint { get; set; }
    public List<Invader> Invaders { get; set; } = new();
    public List<Native> Natives { get; set; } = new();
    public List<BoardToken> Tokens { get; set; } = new();
    public bool HasPresence => PresenceCount > 0;

    // ── Terrain ───────────────────────────────────────────────────────────────
    /// <summary>Current terrain type. Defaults to Plains (no modifier).</summary>
    public TerrainType Terrain { get; set; } = TerrainType.Plains;

    /// <summary>
    /// General-purpose terrain timer. Semantics depend on terrain:
    /// - Scorched: counts clean tides (→ Plains after 3 clean tides)
    /// </summary>
    public int TerrainTimer { get; set; }
}
