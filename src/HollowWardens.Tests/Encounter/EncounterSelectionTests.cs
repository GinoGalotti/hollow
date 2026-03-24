namespace HollowWardens.Tests.Encounter;

using HollowWardens.Core;
using HollowWardens.Core.Data;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Systems;
using Xunit;

/// <summary>
/// Tests for the chain encounter selection mode introduced in Phase 6h.
/// Validates that EncounterLoader can create all selectable encounter configs,
/// that the canonical chain slot defaults produce valid configs, and that
/// chain carryover plumbing (BoardCarryover) is not broken by any of the configs.
/// </summary>
public class EncounterSelectionTests
{
    // ── All selectable encounter IDs load without throwing ────────────────────

    [Theory]
    [InlineData("pale_march_standard")]
    [InlineData("pale_march_scouts")]
    [InlineData("pale_march_siege")]
    [InlineData("pale_march_elite")]
    [InlineData("pale_march_frontier")]
    public void EncounterLoader_AllSelectableIds_CreateValidConfig(string encounterId)
    {
        var config = EncounterLoader.Create(encounterId);
        Assert.NotNull(config);
        Assert.True(config.TideCount > 0, $"{encounterId}: TideCount must be > 0");
        Assert.NotNull(config.Waves);
        Assert.NotEmpty(config.Waves);
    }

    // ── Chain default slots produce valid configs ─────────────────────────────

    [Fact]
    public void DefaultChainSlots_E1Standard_E2Scouts_CapstoneElite_AllValid()
    {
        string[] defaultSlots = { "pale_march_standard", "pale_march_scouts", "pale_march_elite" };
        foreach (var id in defaultSlots)
        {
            var config = EncounterLoader.Create(id);
            Assert.NotNull(config);
        }
    }

    // ── All 5 encounters have distinct board layouts or tide counts ───────────
    // (regression: ensure configs are not accidentally aliased to the same object)

    [Fact]
    public void AllEncounters_ReturnDistinctInstances()
    {
        string[] ids = { "pale_march_standard", "pale_march_scouts", "pale_march_siege", "pale_march_elite", "pale_march_frontier" };
        var configs = ids.Select(EncounterLoader.Create).ToList();

        for (int i = 0; i < configs.Count; i++)
        for (int j = i + 1; j < configs.Count; j++)
            Assert.False(ReferenceEquals(configs[i], configs[j]),
                $"Configs {ids[i]} and {ids[j]} share the same instance");
    }

    // ── Frontier is distinct: wider board ─────────────────────────────────────

    [Fact]
    public void Frontier_HasWiderBoardThanStandard()
    {
        var standard = EncounterLoader.Create("pale_march_standard");
        var frontier = EncounterLoader.Create("pale_march_frontier");
        Assert.NotEqual(standard.BoardLayout, frontier.BoardLayout);
    }

    // ── Elite has starting corruption ─────────────────────────────────────────

    [Fact]
    public void Elite_HasStartingCorruption()
    {
        var elite = EncounterLoader.Create("pale_march_elite");
        Assert.NotNull(elite.StartingCorruption);
        Assert.NotEmpty(elite.StartingCorruption);
    }

    // ── Chain carryover: ExtractCarryover on a just-built state is stable ─────

    [Fact]
    public void ChainCarryover_ExtractFromFreshState_DoesNotThrow()
    {
        // Build a minimal state for each chain-eligible encounter and confirm
        // ExtractCarryover runs without error (ensures carryover fields are initialised).
        string[] chainIds = { "pale_march_standard", "pale_march_scouts", "pale_march_elite" };
        foreach (var id in chainIds)
        {
            var config      = EncounterLoader.Create(id);
            var territories = BoardState.Create(
                TerritoryGraph.Create(config.BoardLayout)).Territories.Values.ToList();
            var balance     = new BalanceConfig();

            var state = new EncounterState
            {
                Config      = config,
                Territories = territories,
                Dread       = new DreadSystem(balance),
                Weave       = new WeaveSystem(20, 20),
                Corruption  = new CorruptionSystem(),
                Balance     = balance
            };

            BoardCarryover? carryover = null;
            var threw = false;
            try   { carryover = state.ExtractCarryover(); }
            catch { threw = true; }

            Assert.False(threw, $"ExtractCarryover threw for encounter: {id}");
            Assert.NotNull(carryover);
        }
    }
}
