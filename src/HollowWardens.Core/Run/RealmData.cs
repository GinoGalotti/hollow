namespace HollowWardens.Core.Run;

using System.Text.Json.Serialization;

public class RealmData
{
    [JsonPropertyName("id")]       public string Id { get; set; } = "";
    [JsonPropertyName("name_key")] public string NameKey { get; set; } = "";
    [JsonPropertyName("stages")]   public List<StageData> Stages { get; set; } = new();
    [JsonPropertyName("paths")]    public List<PathEdge> Paths { get; set; } = new();
}

public class StageData
{
    [JsonPropertyName("stage")]                    public int Stage { get; set; }
    [JsonPropertyName("encounter_id")]             public string? EncounterId { get; set; }
    [JsonPropertyName("encounter_options")]        public List<EncounterOption>? EncounterOptions { get; set; }
    [JsonPropertyName("post_encounter_nodes")]     public List<MapNode> PostEncounterNodes { get; set; } = new();
    [JsonPropertyName("is_optional")]              public bool IsOptional { get; set; } = false;
    [JsonPropertyName("requires_tier1_on_previous")] public bool RequiresTier1OnPrevious { get; set; } = false;
}

public class MapNode
{
    [JsonPropertyName("id")]           public string Id { get; set; } = "";
    [JsonPropertyName("type")]         public string Type { get; set; } = "";
    [JsonPropertyName("tags")]         public List<string> Tags { get; set; } = new();
    [JsonPropertyName("column")]       public int Column { get; set; }
    [JsonPropertyName("encounter_id")] public string? EncounterId { get; set; }
}

public class EncounterOption
{
    [JsonPropertyName("column")]       public int Column { get; set; }
    [JsonPropertyName("encounter_id")] public string EncounterId { get; set; } = "";
}

public class PathEdge
{
    [JsonPropertyName("from_stage")]  public int FromStage { get; set; }
    [JsonPropertyName("from_column")] public int FromColumn { get; set; }
    [JsonPropertyName("to_stage")]    public int ToStage { get; set; }
    [JsonPropertyName("to_column")]   public int ToColumn { get; set; }
}
