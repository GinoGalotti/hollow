namespace HollowWardens.Core.Data;

using System.Text.Json;
using System.Text.Json.Serialization;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Models;

public static class WardenLoader
{
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public static WardenData Load(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"Warden data file not found: {jsonPath}", jsonPath);

        var json = File.ReadAllText(jsonPath);
        var raw  = JsonSerializer.Deserialize<WardenJson>(json, _opts)
            ?? throw new InvalidDataException($"Failed to parse warden file: {jsonPath}");

        return Map(raw);
    }

    public static List<Card>        LoadCards(string jsonPath)   => Load(jsonPath).Cards;
    public static List<PassiveData> LoadPassives(string jsonPath) => Load(jsonPath).Passives;

    private static WardenData Map(WardenJson raw) => new()
    {
        WardenId         = raw.WardenId,
        Version          = raw.Version,
        Name             = raw.Name,
        Archetype        = raw.Archetype,
        Flavor           = raw.Flavor,
        ElementAffinity  = new ElementAffinity
        {
            Primary   = raw.ElementAffinity.Primary,
            Secondary = raw.ElementAffinity.Secondary,
            Tertiary  = raw.ElementAffinity.Tertiary,
        },
        HandLimit        = raw.HandLimit,
        StartingPresence = new StartingPresence
        {
            Territory = raw.StartingPresence.Territory,
            Count     = raw.StartingPresence.Count,
        },
        Passives         = raw.Passives.Select(MapPassive).ToList(),
        ResolutionStyle  = raw.ResolutionStyle,
        Cards            = raw.Cards.Select(c => MapCard(c, raw.WardenId)).ToList(),
    };

    private static PassiveData MapPassive(PassiveJson p) => new()
    {
        Id          = p.Id,
        Name        = p.Name,
        Icon        = p.Icon,
        Flavor      = p.Flavor,
        Description = p.Description,
        Trigger     = p.Trigger,
        Mechanic    = p.Mechanic,
        Params      = p.Params,
        IsPool      = p.Pool,
        Upgrade     = p.Upgrade == null ? null : new PassiveUpgradeData
        {
            Id             = p.Upgrade.Id,
            DescriptionKey = p.Upgrade.DescriptionKey,
            Effects        = p.Upgrade.Effects.Select(e => new PassiveUpgradeEffect
            {
                Type  = e.Type,
                Key   = e.Key,
                Value = e.Value
            }).ToList()
        }
    };

    private static Card MapCard(CardJson c, string wardenId) => new()
    {
        Id              = c.Id,
        Name            = c.Name,
        WardenId        = wardenId,
        Rarity          = ParseRarity(c.Rarity),
        IsStarting      = c.Starting,
        Elements        = c.Elements.Select(ParseElement).ToArray(),
        TopTiming       = ParseCardTiming(c.TopTiming),
        TopEffect       = MapEffect(c.Top),
        BottomEffect    = MapEffect(c.Bottom),
        BottomSecondary = c.Bottom.Secondary != null ? MapEffect(c.Bottom.Secondary) : null,
        UpgradeSlots    = c.Upgrades.Select(MapUpgradeSlot).ToList()
    };

    private static CardUpgradeSlot MapUpgradeSlot(UpgradeSlotJson u) => new()
    {
        Id             = u.Id,
        Slot           = u.Slot,
        Field          = u.Field,
        Action         = u.Action,
        Element        = u.Element,
        From           = u.From,
        To             = u.To,
        Cost           = u.Cost,
        DescriptionKey = u.DescriptionKey
    };

    private static EffectData MapEffect(EffectJson e) => new()
    {
        Type  = Enum.Parse<EffectType>(e.Type, ignoreCase: true),
        Value = e.Value,
        Range = e.Range
    };

    private static Element    ParseElement(string s) => Enum.Parse<Element>(s, ignoreCase: true);
    private static CardTiming ParseCardTiming(string? s) => s?.ToLowerInvariant() switch
    {
        "fast"  => CardTiming.Fast,
        "slow"  => CardTiming.Slow,
        _       => CardTiming.Slow  // default to Slow if missing
    };
    private static CardRarity ParseRarity(string s)  => s.ToLowerInvariant() switch
    {
        "dormant"  => CardRarity.Dormant,
        "awakened" => CardRarity.Awakened,
        "ancient"  => CardRarity.Ancient,
        _ => throw new ArgumentException($"Unknown rarity: {s}")
    };

    // ─── JSON model types ─────────────────────────────────────────────────────

    private class WardenJson
    {
        [JsonPropertyName("warden_id")]
        public string WardenId { get; set; } = string.Empty;
        public string Version   { get; set; } = string.Empty;
        public string Name      { get; set; } = string.Empty;
        public string Archetype { get; set; } = string.Empty;
        public string Flavor    { get; set; } = string.Empty;

        [JsonPropertyName("element_affinity")]
        public ElementAffinityJson ElementAffinity { get; set; } = new();

        [JsonPropertyName("hand_limit")]
        public int HandLimit { get; set; } = 5;

        [JsonPropertyName("starting_presence")]
        public StartingPresenceJson StartingPresence { get; set; } = new();

        public List<PassiveJson> Passives { get; set; } = new();

        [JsonPropertyName("resolution_style")]
        public string ResolutionStyle { get; set; } = string.Empty;

        public List<CardJson> Cards { get; set; } = new();
    }

    private class ElementAffinityJson
    {
        public string Primary   { get; set; } = string.Empty;
        public string Secondary { get; set; } = string.Empty;
        public string Tertiary  { get; set; } = string.Empty;
    }

    private class StartingPresenceJson
    {
        public string Territory { get; set; } = "I1";
        public int    Count     { get; set; } = 1;
    }

    private class PassiveJson
    {
        public string Id          { get; set; } = string.Empty;
        public string Name        { get; set; } = string.Empty;
        public string Icon        { get; set; } = string.Empty;
        public string Flavor      { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Trigger     { get; set; } = string.Empty;
        public string Mechanic    { get; set; } = string.Empty;
        public Dictionary<string, object>? Params { get; set; }
        public PassiveUpgradeJson? Upgrade { get; set; }
        public bool Pool { get; set; }
    }

    private class PassiveUpgradeJson
    {
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("description_key")]
        public string DescriptionKey { get; set; } = string.Empty;

        public List<PassiveUpgradeEffectJson> Effects { get; set; } = new();
    }

    private class PassiveUpgradeEffectJson
    {
        public string Type { get; set; } = string.Empty;
        public string? Key { get; set; }
        public int Value { get; set; }
    }

    private class CardJson
    {
        public string   Id       { get; set; } = string.Empty;
        public string   Name     { get; set; } = string.Empty;
        public string   Rarity   { get; set; } = string.Empty;
        public bool     Starting { get; set; }
        public string[] Elements { get; set; } = Array.Empty<string>();

        [JsonPropertyName("top_timing")]
        public string?  TopTiming { get; set; }

        public EffectJson Top    { get; set; } = new();
        public BottomJson Bottom { get; set; } = new();

        [JsonPropertyName("upgrades")]
        public List<UpgradeSlotJson> Upgrades { get; set; } = new();
    }

    private class UpgradeSlotJson
    {
        public string Id { get; set; } = "";
        public string Slot { get; set; } = "";
        public string? Field { get; set; }
        public string? Action { get; set; }
        public string? Element { get; set; }
        public int From { get; set; }
        public int To { get; set; }
        public int Cost { get; set; } = 1;

        [JsonPropertyName("description_key")]
        public string DescriptionKey { get; set; } = "";
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
