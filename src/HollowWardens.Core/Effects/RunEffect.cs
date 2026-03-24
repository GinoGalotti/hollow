namespace HollowWardens.Core.Effects;

using System.Text.Json.Serialization;

/// <summary>
/// A generic effect applied to RunState between encounters.
/// Used by events, passive upgrades, and rewards.
/// </summary>
public class RunEffect
{
    [JsonPropertyName("type")]        public string Type { get; set; } = "";
    [JsonPropertyName("value")]       public int Value { get; set; }
    [JsonPropertyName("target_id")]   public string? TargetId { get; set; }
    [JsonPropertyName("rarity")]      public string? Rarity { get; set; }
    [JsonPropertyName("territories")] public List<string>? Territories { get; set; }
    [JsonPropertyName("key")]         public string? Key { get; set; }
    [JsonPropertyName("element")]     public string? Element { get; set; }
}
