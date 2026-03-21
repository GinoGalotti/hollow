namespace HollowWardens.Core.Data;

using System.Text.Json;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Models;

[Obsolete("Use WardenLoader.LoadCards() instead. CardLoader will be removed in a future version.")]
public static class CardLoader
{
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public static List<Card> Load(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var doc  = JsonSerializer.Deserialize<CardFileJson>(json, _opts)
            ?? throw new InvalidDataException($"Failed to parse card file: {jsonPath}");
        return doc.Cards.Select(MapCard).ToList();
    }

    private static Card MapCard(CardJson c) => new()
    {
        Id              = c.Id,
        Name            = c.Name,
        WardenId        = "root",
        Rarity          = ParseRarity(c.Rarity),
        IsStarting      = c.Starting,
        Elements        = c.Elements.Select(ParseElement).ToArray(),
        TopEffect       = MapEffect(c.Top),
        BottomEffect    = MapEffect(c.Bottom),
        BottomSecondary = c.Bottom.Secondary != null ? MapEffect(c.Bottom.Secondary) : null
    };

    private static EffectData MapEffect(EffectJson e) => new()
    {
        Type  = Enum.Parse<EffectType>(e.Type, ignoreCase: true),
        Value = e.Value,
        Range = e.Range
    };

    private static Element     ParseElement(string s) => Enum.Parse<Element>(s, ignoreCase: true);

    private static CardRarity  ParseRarity(string s) => s.ToLowerInvariant() switch
    {
        "dormant"  => CardRarity.Dormant,
        "awakened" => CardRarity.Awakened,
        "ancient"  => CardRarity.Ancient,
        _ => throw new ArgumentException($"Unknown rarity: {s}")
    };

    // ─── JSON model types ─────────────────────────────────────────────────────

    private class CardFileJson
    {
        public List<CardJson> Cards { get; set; } = new();
    }

    private class CardJson
    {
        public string   Id       { get; set; } = string.Empty;
        public string   Name     { get; set; } = string.Empty;
        public string   Rarity   { get; set; } = string.Empty;
        public bool     Starting { get; set; }
        public string[] Elements { get; set; } = Array.Empty<string>();
        public EffectJson Top    { get; set; } = new();
        public BottomJson Bottom { get; set; } = new();
    }

    private class EffectJson
    {
        public string Type  { get; set; } = string.Empty;
        public int    Value { get; set; }
        public int    Range { get; set; }
    }

    private class BottomJson : EffectJson
    {
        public EffectJson? Secondary { get; set; }
    }
}
