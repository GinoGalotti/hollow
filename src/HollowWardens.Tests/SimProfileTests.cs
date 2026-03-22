namespace HollowWardens.Tests;

using System.Text.Json;
using HollowWardens.Core.Data;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using HollowWardens.Sim;
using Xunit;

/// <summary>
/// Tests for SimProfile loading and SimProfileApplier.
/// </summary>
public class SimProfileTests
{
    private static string GetRootJsonPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "wardens", "root.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not find root.json in ancestor directories");
    }

    private static WardenData LoadRoot() => WardenLoader.Load(GetRootJsonPath());

    // ── 1. LoadProfile_ParsesAllFields ───────────────────────────────────────

    [Fact]
    public void LoadProfile_ParsesAllFields()
    {
        var json = """
        {
            "name": "Test profile",
            "runs": 200,
            "seed": 99,
            "warden": "ember",
            "encounter": "pale_march_standard",
            "output": "test-output",
            "warden_overrides": {
                "hand_limit": 6,
                "force_passives": ["phoenix_spark"]
            },
            "encounter_overrides": {
                "tide_count": 8
            },
            "balance_overrides": {
                "max_weave": 18
            }
        }
        """;
        var opts    = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var profile = JsonSerializer.Deserialize<SimProfile>(json, opts);

        Assert.NotNull(profile);
        Assert.Equal("Test profile", profile.Name);
        Assert.Equal(200,              profile.Runs);
        Assert.Equal(99,               profile.Seed);
        Assert.Equal("ember",          profile.Warden);
        Assert.Equal("test-output",    profile.Output);
        Assert.Equal(6,                profile.WardenOverrides?.HandLimit);
        Assert.Contains("phoenix_spark", profile.WardenOverrides!.ForcePassives!);
        Assert.Equal(8,                profile.EncounterOverrides?.TideCount);
        Assert.NotNull(profile.BalanceOverrides);
    }

    // ── 2. ApplyWardenOverrides_AddsCards ────────────────────────────────────

    [Fact]
    public void ApplyWardenOverrides_AddsCards()
    {
        var wardenData = LoadRoot();
        Assert.False(wardenData.Cards.First(c => c.Id == "root_010").IsStarting);

        var overrides = new WardenOverrides { AddCards = new List<string> { "root_010" } };
        SimProfileApplier.ApplyWardenOverrides(wardenData, overrides);

        Assert.True(wardenData.Cards.First(c => c.Id == "root_010").IsStarting);
    }

    // ── 3. ApplyWardenOverrides_RemovesCards ─────────────────────────────────

    [Fact]
    public void ApplyWardenOverrides_RemovesCards()
    {
        var wardenData = LoadRoot();
        Assert.True(wardenData.Cards.First(c => c.Id == "root_001").IsStarting);

        var overrides = new WardenOverrides { RemoveCards = new List<string> { "root_001" } };
        SimProfileApplier.ApplyWardenOverrides(wardenData, overrides);

        Assert.False(wardenData.Cards.First(c => c.Id == "root_001").IsStarting);
    }

    // ── 4. ApplyWardenOverrides_UpgradesCardValue ─────────────────────────────

    [Fact]
    public void ApplyWardenOverrides_UpgradesCardValue()
    {
        var wardenData = LoadRoot();
        // root_001 top effect value is 2
        Assert.Equal(2, wardenData.Cards.First(c => c.Id == "root_001").TopEffect.Value);

        var overrides = new WardenOverrides
        {
            UpgradeCards = new Dictionary<string, CardUpgrade>
            {
                ["root_001"] = new CardUpgrade { Top = new EffectOverride { Value = 4 } }
            }
        };
        SimProfileApplier.ApplyWardenOverrides(wardenData, overrides);

        Assert.Equal(4, wardenData.Cards.First(c => c.Id == "root_001").TopEffect.Value);
    }

    // ── 5. ApplyEncounterOverrides_ChangesTideCount ───────────────────────────

    [Fact]
    public void ApplyEncounterOverrides_ChangesTideCount()
    {
        var config    = EncounterLoader.CreatePaleMarchStandard();
        int original  = config.TideCount;
        var overrides = new EncounterOverrides { TideCount = 12 };

        SimProfileApplier.ApplyEncounterOverrides(config, overrides);

        Assert.Equal(12, config.TideCount);
        Assert.NotEqual(original, config.TideCount);
    }

    // ── 6. ApplyBalanceOverrides_SetsConfigFields ─────────────────────────────

    [Fact]
    public void ApplyBalanceOverrides_SetsConfigFields()
    {
        var balance = new BalanceConfig();
        var json    = """{"max_weave": 15, "invader_hp_bonus": 2}""";
        var opts    = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var raw     = JsonSerializer.Deserialize<Dictionary<string, object>>(json, opts);

        SimProfileApplier.ApplyBalanceOverrides(balance, raw!);

        Assert.Equal(15, balance.MaxWeave);
        Assert.Equal(2,  balance.InvaderHpBonus);
    }

    // ── 7. ApplyPassiveOverrides_ForcesUnlock ────────────────────────────────

    [Fact]
    public void ApplyPassiveOverrides_ForcesUnlock()
    {
        var gating    = new PassiveGating("root");
        Assert.False(gating.IsActive("network_slow")); // not yet unlocked

        var overrides = new WardenOverrides { ForcePassives = new List<string> { "network_slow" } };
        SimProfileApplier.ApplyPassiveOverrides(gating, overrides);

        Assert.True(gating.IsActive("network_slow"));
    }

    // ── 8. ApplyPassiveOverrides_ForcesLock ──────────────────────────────────

    [Fact]
    public void ApplyPassiveOverrides_ForcesLock()
    {
        var gating = new PassiveGating("root");
        Assert.True(gating.IsActive("network_fear")); // always-active

        var overrides = new WardenOverrides { LockPassives = new List<string> { "network_fear" } };
        SimProfileApplier.ApplyPassiveOverrides(gating, overrides);

        Assert.False(gating.IsActive("network_fear"));
    }

    // ── 9. ApplyStartingCorruption_SetsPoints ────────────────────────────────

    [Fact]
    public void ApplyStartingCorruption_SetsPoints()
    {
        var territories = BoardState.CreatePyramid().Territories.Values.ToList();
        var state       = new EncounterState
        {
            Territories = territories,
            Corruption  = new CorruptionSystem()
        };
        var corruption = new Dictionary<string, int> { ["A1"] = 5, ["M1"] = 3 };

        SimProfileApplier.ApplyStartingCorruption(state, corruption);

        Assert.Equal(5, state.GetTerritory("A1")?.CorruptionPoints);
        Assert.Equal(3, state.GetTerritory("M1")?.CorruptionPoints);
    }

    // ── 10. CliFlags_OverrideProfileValues ───────────────────────────────────

    [Fact]
    public void CliFlags_OverrideProfileValues()
    {
        // Simulate the CLI-over-profile resolution logic
        var profile = new SimProfile { Runs = 200, Seed = 99, Warden = "ember" };

        int?    cliRuns   = 50;
        int?    cliSeed   = null;
        string? cliWarden = "root";

        int    effectiveRuns   = cliRuns   ?? profile.Runs;
        int    effectiveSeed   = cliSeed   ?? profile.Seed;
        string effectiveWarden = cliWarden ?? profile.Warden;

        Assert.Equal(50,     effectiveRuns);   // CLI overrides profile
        Assert.Equal(99,     effectiveSeed);   // profile used when CLI absent
        Assert.Equal("root", effectiveWarden); // CLI overrides profile
    }
}
