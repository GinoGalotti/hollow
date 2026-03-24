namespace HollowWardens.Core.Events;

using System.Text.Json.Serialization;
using HollowWardens.Core.Effects;

/// <summary>
/// A roguelike run event loaded from data/events/*.json.
/// Type field is for UI presentation only — all types share the same options/effects structure.
/// </summary>
public class EventData
{
    [JsonPropertyName("id")]                public string Id { get; set; } = "";
    [JsonPropertyName("type")]              public string Type { get; set; } = "choice";
    [JsonPropertyName("name_key")]          public string NameKey { get; set; } = "";
    [JsonPropertyName("description_key")]   public string DescriptionKey { get; set; } = "";
    [JsonPropertyName("warden_filter")]     public string? WardenFilter { get; set; }
    [JsonPropertyName("tags")]              public List<string> Tags { get; set; } = new();
    [JsonPropertyName("rarity")]            public string Rarity { get; set; } = "common";
    [JsonPropertyName("options")]           public List<EventOption> Options { get; set; } = new();
}

public class EventOption
{
    [JsonPropertyName("label_key")]         public string LabelKey { get; set; } = "";
    [JsonPropertyName("description_key")]   public string DescriptionKey { get; set; } = "";
    [JsonPropertyName("element_threshold")] public int? ElementThreshold { get; set; }
    [JsonPropertyName("element_type")]      public string? ElementType { get; set; }
    [JsonPropertyName("effects")]           public List<RunEffect> Effects { get; set; } = new();
}
