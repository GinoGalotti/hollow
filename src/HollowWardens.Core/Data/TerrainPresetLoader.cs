namespace HollowWardens.Core.Data;

using System.Text.Json;
using System.Text.Json.Serialization;
using HollowWardens.Core.Models;

/// <summary>
/// Loads terrain preset definitions from data/terrain_presets.json and applies them to territories.
/// </summary>
public static class TerrainPresetLoader
{
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Applies a named terrain preset (and optional per-territory overrides) to a list of territories.
    /// Silently skips territory IDs that don't exist in the territory list.
    /// Returns false if the preset file or the named preset is not found.
    /// </summary>
    public static bool Apply(
        IEnumerable<Territory> territories,
        string presetId,
        string terrainPresetsJsonPath,
        Dictionary<string, string>? overrides = null)
    {
        if (!File.Exists(terrainPresetsJsonPath)) return false;

        var json = File.ReadAllText(terrainPresetsJsonPath);
        var file = JsonSerializer.Deserialize<TerrainPresetsFile>(json, _opts);
        var preset = file?.Presets?.FirstOrDefault(p => p.Id == presetId);
        if (preset == null) return false;

        var territoryMap = territories.ToDictionary(t => t.Id);

        // Apply preset
        foreach (var (territoryId, terrainName) in preset.Territories)
        {
            if (territoryMap.TryGetValue(territoryId, out var t))
                t.Terrain = ParseTerrain(terrainName);
        }

        // Apply per-territory overrides
        if (overrides != null)
        {
            foreach (var (territoryId, terrainName) in overrides)
            {
                if (territoryMap.TryGetValue(territoryId, out var t))
                    t.Terrain = ParseTerrain(terrainName);
            }
        }

        return true;
    }

    private static TerrainType ParseTerrain(string name) => name.ToLowerInvariant() switch
    {
        "plains"   => TerrainType.Plains,
        "forest"   => TerrainType.Forest,
        "mountain" => TerrainType.Mountain,
        "wetland"  => TerrainType.Wetland,
        "sacred"   => TerrainType.Sacred,
        "scorched" => TerrainType.Scorched,
        "blighted" => TerrainType.Blighted,
        "ruins"    => TerrainType.Ruins,
        "fertile"  => TerrainType.Fertile,
        _          => TerrainType.Plains
    };

    private class TerrainPresetsFile
    {
        public List<TerrainPresetJson>? Presets { get; set; }
    }

    private class TerrainPresetJson
    {
        public string Id { get; set; } = string.Empty;
        public Dictionary<string, string> Territories { get; set; } = new();
    }
}
