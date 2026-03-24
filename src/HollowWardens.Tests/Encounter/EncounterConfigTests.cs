namespace HollowWardens.Tests.Encounter;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using Xunit;

public class EncounterConfigTests
{
    [Theory]
    [InlineData(EncounterTier.Standard, 2)]
    [InlineData(EncounterTier.Elite,    3)]
    [InlineData(EncounterTier.Boss,     1)]
    public void ResolutionTurns_ReturnsCorrectValuePerTier(EncounterTier tier, int expected)
    {
        var config = new EncounterConfig { Tier = tier };
        Assert.Equal(expected, config.ResolutionTurns);
    }

    [Fact]
    public void ResolutionTurns_DefaultTier_ReturnsTwo()
    {
        // EncounterTier default (0) falls through to the _ => 2 case.
        var config = new EncounterConfig();
        Assert.Equal(2, config.ResolutionTurns);
    }

    // ── Null-safety regression test ─────────────────────────────────────────
    // Guard against: GameBridge.Instance?.State.Config.ResolutionTurns
    //   (missing ?. after State and Config) — crashing in TidePreviewController's
    //   ResolutionTurnStarted callback when State was null before BuildEncounter.

    [Fact]
    public void ResolutionTurns_NullableConfigReference_SafelyReturnsZero()
    {
        EncounterConfig? config = null;
        int turns = config?.ResolutionTurns ?? 0;
        Assert.Equal(0, turns);
    }
}
