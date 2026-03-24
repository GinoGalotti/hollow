namespace HollowWardens.Tests;

using System.Text.Json;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders.PaleMarch;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Turn;
using HollowWardens.Sim;
using Xunit;

public class BalanceConfigTests
{
    // ── 1. Defaults match current hardcoded values ────────────────────────────

    [Fact]
    public void DefaultConfig_MatchesCurrentHardcodedValues()
    {
        var cfg = new BalanceConfig();

        Assert.Equal(3,           cfg.MaxPresencePerTerritory);
        Assert.Equal(1,           cfg.AmplificationPerPresence);
        Assert.Equal(int.MaxValue, cfg.AmplificationCap);
        Assert.Equal(3,           cfg.NetworkFearCap);
        Assert.Equal(1,           cfg.SacrificePresenceCost);
        Assert.Equal(3,           cfg.SacrificeCorruptionCleanse);
        Assert.Equal(0,           cfg.InvaderHpBonus);
        Assert.Equal(2,           cfg.BaseRavageCorruption);
        Assert.Equal(1.0f,        cfg.CorruptionRateMultiplier);
        Assert.Equal(20,          cfg.MaxWeave);
        Assert.Equal(20,          cfg.StartingWeave);
        Assert.Equal(4,           cfg.ElementTier1Threshold);
        Assert.Equal(7,           cfg.ElementTier2Threshold);
        Assert.Equal(11,          cfg.ElementTier3Threshold);
        Assert.Equal(1,           cfg.ElementDecayPerTurn);
        Assert.Equal(1,           cfg.TopElementMultiplier);
        Assert.Equal(2,           cfg.BottomElementMultiplier);
        Assert.Equal(5,           cfg.FearPerAction);
        Assert.Equal(15,          cfg.DreadThreshold1);
        Assert.Equal(30,          cfg.DreadThreshold2);
        Assert.Equal(45,          cfg.DreadThreshold3);
        Assert.Equal(2,           cfg.DefaultNativeHp);
        Assert.Equal(3,           cfg.DefaultNativeDamage);
        Assert.Equal(2,           cfg.VigilPlayLimit);
        Assert.Equal(1,           cfg.DuskPlayLimit);
    }

    // ── 2. Clone is independent ───────────────────────────────────────────────

    [Fact]
    public void Config_Clone_IsIndependent()
    {
        var original = new BalanceConfig();
        var clone    = original.Clone();

        clone.MaxPresencePerTerritory = 99;
        clone.NetworkFearCap          = 99;

        Assert.Equal(3, original.MaxPresencePerTerritory);
        Assert.Equal(3, original.NetworkFearCap);
    }

    // ── 3. AmplificationHelper respects AmplificationPerPresence ─────────────

    [Fact]
    public void AmplificationHelper_RespectsConfig_PerPresence()
    {
        var territory = new Territory { Id = "M1", Row = TerritoryRow.Middle, PresenceCount = 1 };
        var state     = new EncounterState();
        state.Territories.Add(territory);
        state.Balance.AmplificationPerPresence = 2; // 1 presence = +2

        int result = AmplificationHelper.GetAmplifiedValue(5, state, "M1");

        Assert.Equal(7, result); // 5 + (1 × 2)
    }

    // ── 4. AmplificationHelper respects AmplificationCap ─────────────────────

    [Fact]
    public void AmplificationHelper_RespectsConfig_Cap()
    {
        var territory = new Territory { Id = "M1", Row = TerritoryRow.Middle, PresenceCount = 5 };
        var state     = new EncounterState();
        state.Territories.Add(territory);
        state.Balance.AmplificationCap = 2; // cap bonus at 2

        int result = AmplificationHelper.GetAmplifiedValue(5, state, "M1");

        Assert.Equal(7, result); // 5 + min(5, 2) = 5 + 2
    }

    // ── 5. SacrificePresence respects config cleanse amount ───────────────────

    [Fact]
    public void SacrificePresence_RespectsConfig_CleanseAmount()
    {
        var territory = new Territory
            { Id = "M1", Row = TerritoryRow.Middle, CorruptionPoints = 10, PresenceCount = 2 };
        var state = new EncounterState();
        state.Territories.Add(territory);
        state.Presence   = new PresenceSystem(() => state.Territories);
        state.Corruption = new CorruptionSystem();
        state.Balance.SacrificeCorruptionCleanse = 5;

        var actions = new TurnActions(state, new EffectResolver());
        actions.SacrificePresence("M1");

        Assert.Equal(5, territory.CorruptionPoints); // 10 - 5
    }

    // ── 6. InvaderHpBonus applied on creation ─────────────────────────────────

    [Fact]
    public void InvaderHpBonus_AppliedOnCreation()
    {
        var faction  = new PaleMarchFaction();
        faction.HpBonus = 2;

        var marcher = faction.CreateUnit(UnitType.Marcher, "A1");

        // Marcher base HP = 4; bonus = 2; expected = 6
        Assert.Equal(6, marcher.Hp);
        Assert.Equal(6, marcher.MaxHp);
    }

    // ── 7. GetThreshold defaults to global ────────────────────────────────────

    [Fact]
    public void GetThreshold_DefaultsToGlobal()
    {
        var cfg = new BalanceConfig();

        Assert.Equal(4,  cfg.GetThreshold(Element.Ash, 1));
        Assert.Equal(7,  cfg.GetThreshold(Element.Ash, 2));
        Assert.Equal(11, cfg.GetThreshold(Element.Ash, 3));
        Assert.Equal(4,  cfg.GetThreshold(Element.Root, 1));
        Assert.Equal(11, cfg.GetThreshold(Element.Root, 3));
    }

    // ── 8. GetThreshold uses element override ─────────────────────────────────

    [Fact]
    public void GetThreshold_UsesElementOverride()
    {
        var cfg = new BalanceConfig();
        cfg.ElementOverrides["Ash"] = new BalanceConfig.ElementThresholdConfig { Tier3Threshold = 14 };

        Assert.Equal(14, cfg.GetThreshold(Element.Ash, 3));
        Assert.Equal(4,  cfg.GetThreshold(Element.Ash, 1)); // unset → falls back to global
    }

    // ── 9. GetThreshold override doesn't affect other elements ────────────────

    [Fact]
    public void GetThreshold_OtherElementsUnaffected()
    {
        var cfg = new BalanceConfig();
        cfg.ElementOverrides["Ash"] = new BalanceConfig.ElementThresholdConfig { Tier3Threshold = 14 };

        Assert.Equal(11, cfg.GetThreshold(Element.Root, 3)); // Root unaffected
        Assert.Equal(11, cfg.GetThreshold(Element.Mist, 3));
    }

    // ── 10. GetThresholdDamage defaults to global ─────────────────────────────

    [Fact]
    public void GetThresholdDamage_DefaultsToGlobal()
    {
        var cfg = new BalanceConfig();

        Assert.Equal(1, cfg.GetThresholdDamage(Element.Ash, 1));
        Assert.Equal(2, cfg.GetThresholdDamage(Element.Ash, 2));
        Assert.Equal(3, cfg.GetThresholdDamage(Element.Ash, 3));
    }

    // ── 11. GetThresholdDamage uses element override ──────────────────────────

    [Fact]
    public void GetThresholdDamage_UsesElementOverride()
    {
        var cfg = new BalanceConfig();
        cfg.ElementOverrides["Ash"] = new BalanceConfig.ElementThresholdConfig { T3Damage = 2 };

        Assert.Equal(2, cfg.GetThresholdDamage(Element.Ash, 3));
        Assert.Equal(1, cfg.GetThresholdDamage(Element.Ash, 1)); // unset → falls back to global
    }

    // ── 12. Per-element threshold integrates with ElementSystem ───────────────

    [Fact]
    public void PerElementThreshold_IntegratesWithElementSystem()
    {
        var cfg = new BalanceConfig();
        cfg.ElementOverrides["Ash"] = new BalanceConfig.ElementThresholdConfig { Tier3Threshold = 14 };
        var sut = new ElementSystem(cfg);

        var firedTiers = new List<int>();
        GameEvents.ThresholdTriggered += (_, t) => firedTiers.Add(t);
        try
        {
            // pool = 12: T1 and T2 fire, T3 does NOT (override = 14, not 11)
            sut.AddElements(Enumerable.Repeat(Element.Ash, 12).ToArray());
            Assert.DoesNotContain(3, firedTiers);

            // reset threshold tracking; pool stays at 12
            sut.OnNewTurn();
            firedTiers.Clear();

            // pool = 14: T1, T2, and T3 all fire
            sut.AddElements(Enumerable.Repeat(Element.Ash, 2).ToArray());
            Assert.Contains(3, firedTiers);
        }
        finally
        {
            GameEvents.ClearAll();
        }
    }

    // ── 13. SimProfile element_overrides applied via ApplyBalanceOverrides ────

    [Fact]
    public void SimProfile_ElementOverrides_Applied()
    {
        var balance = new BalanceConfig();
        var json = """{"element_overrides": {"Ash": {"tier3_threshold": 14, "t3_damage": 2}}}""";
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var raw  = JsonSerializer.Deserialize<Dictionary<string, object>>(json, opts);

        SimProfileApplier.ApplyBalanceOverrides(balance, raw!);

        Assert.Equal(14, balance.GetThreshold(Element.Ash, 3));
        Assert.Equal(2,  balance.GetThresholdDamage(Element.Ash, 3));
        Assert.Equal(11, balance.GetThreshold(Element.Root, 3)); // unaffected
    }
}
