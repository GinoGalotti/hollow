namespace HollowWardens.Core.Encounter;

using HollowWardens.Core.Models;

/// <summary>
/// All tunable balance constants in one place. Systems read from this instead
/// of hardcoded values. SimProfile overrides populate this before encounter start.
/// </summary>
public class BalanceConfig
{
    // ── Presence ──────────────────────────────────────────
    public int MaxPresencePerTerritory { get; set; } = 3;
    public int AmplificationPerPresence { get; set; } = 1;
    public int AmplificationCap { get; set; } = int.MaxValue; // no cap by default

    // ── Network Fear ─────────────────────────────────────
    public int NetworkFearCap { get; set; } = 4;

    // ── Sacrifice ────────────────────────────────────────
    public int SacrificePresenceCost { get; set; } = 1;
    public int SacrificeCorruptionCleanse { get; set; } = 3;

    // ── Invaders ─────────────────────────────────────────
    public int InvaderHpBonus { get; set; } = 0; // added to all invader BaseHp
    public int BaseRavageCorruption { get; set; } = 2;
    public float CorruptionRateMultiplier { get; set; } = 1.0f;

    // ── Weave ────────────────────────────────────────────
    public int MaxWeave { get; set; } = 20;
    public int StartingWeave { get; set; } = 20;

    // ── Corruption Thresholds ────────────────────────────
    public int CorruptionLevel1Threshold { get; set; } = 3;
    public int CorruptionLevel2Threshold { get; set; } = 8;
    public int CorruptionLevel3Threshold { get; set; } = 15;

    // ── Elements ─────────────────────────────────────────
    public int ElementTier1Threshold { get; set; } = 4;
    public int ElementTier2Threshold { get; set; } = 7;
    public int ElementTier3Threshold { get; set; } = 11;
    public int ElementDecayPerTurn { get; set; } = 1;
    public int TopElementMultiplier { get; set; } = 1;
    public int BottomElementMultiplier { get; set; } = 2;

    // ── Threshold Damage (per tier, global defaults) ─────
    public int ThresholdT1Damage { get; set; } = 1;
    public int ThresholdT2Damage { get; set; } = 2;
    public int ThresholdT3Damage { get; set; } = 3;

    // ── Threshold Corruption Riders (per tier) ───────────
    public int ThresholdT2Corruption { get; set; } = 1; // T2 adds corruption
    public int ThresholdT3Corruption { get; set; } = 0; // T3 no corruption (was removed in Ember patch)

    // ── Per-Element Threshold Overrides ──────────────────
    // If an element has an override, use it. Otherwise fall back to global.
    // Key format: element name (e.g., "Ash", "Root")
    public Dictionary<string, ElementThresholdConfig> ElementOverrides { get; set; } = new();

    public class ElementThresholdConfig
    {
        public int? Tier1Threshold { get; set; }  // null = use global
        public int? Tier2Threshold { get; set; }
        public int? Tier3Threshold { get; set; }
        public int? T1Damage { get; set; }        // null = use global
        public int? T2Damage { get; set; }
        public int? T3Damage { get; set; }
        public int? T2Corruption { get; set; }
        public int? T3Corruption { get; set; }
    }

    // ── Fear / Dread ─────────────────────────────────────
    public int FearPerAction { get; set; } = 5; // fear spent to queue 1 action
    public int DreadThreshold1 { get; set; } = 15;
    public int DreadThreshold2 { get; set; } = 30;
    public int DreadThreshold3 { get; set; } = 45;

    // ── Natives ──────────────────────────────────────────
    public int DefaultNativeHp { get; set; } = 2;
    public int DefaultNativeDamage { get; set; } = 3;

    // ── Cards ────────────────────────────────────────────
    public int VigilPlayLimit { get; set; } = 2;
    public int DuskPlayLimit { get; set; } = 1;

    // ── Helpers ──────────────────────────────────────────

    public int GetThreshold(Element element, int tier)
    {
        var name = element.ToString();
        if (ElementOverrides.TryGetValue(name, out var eo))
        {
            var val = tier switch
            {
                1 => eo.Tier1Threshold,
                2 => eo.Tier2Threshold,
                3 => eo.Tier3Threshold,
                _ => null
            };
            if (val.HasValue) return val.Value;
        }
        return tier switch
        {
            1 => ElementTier1Threshold,
            2 => ElementTier2Threshold,
            3 => ElementTier3Threshold,
            _ => 0
        };
    }

    public int GetThresholdDamage(Element element, int tier)
    {
        var name = element.ToString();
        if (ElementOverrides.TryGetValue(name, out var eo))
        {
            var val = tier switch
            {
                1 => eo.T1Damage,
                2 => eo.T2Damage,
                3 => eo.T3Damage,
                _ => null
            };
            if (val.HasValue) return val.Value;
        }
        return tier switch
        {
            1 => ThresholdT1Damage,
            2 => ThresholdT2Damage,
            3 => ThresholdT3Damage,
            _ => 0
        };
    }

    public int GetThresholdCorruption(Element element, int tier)
    {
        var name = element.ToString();
        if (ElementOverrides.TryGetValue(name, out var eo))
        {
            var val = tier switch
            {
                2 => eo.T2Corruption,
                3 => eo.T3Corruption,
                _ => null
            };
            if (val.HasValue) return val.Value;
        }
        return tier switch
        {
            2 => ThresholdT2Corruption,
            3 => ThresholdT3Corruption,
            _ => 0
        };
    }

    public string GetHash()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(this,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented      = false,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes)[..12];
    }

    /// <summary>Creates a deep copy for isolation in tests/sim runs.</summary>
    public BalanceConfig Clone()
    {
        var clone = (BalanceConfig)MemberwiseClone();
        clone.ElementOverrides = new Dictionary<string, ElementThresholdConfig>(ElementOverrides);
        return clone;
    }
}
