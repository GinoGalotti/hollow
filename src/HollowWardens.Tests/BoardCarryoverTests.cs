namespace HollowWardens.Tests;

using HollowWardens.Core;
using HollowWardens.Core.Cards;
using HollowWardens.Core.Data;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using Xunit;

public class BoardCarryoverTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EncounterState BuildMinimalState(int startingWeave = 20, int dreadLevel = 1)
    {
        var territories = BoardState.CreatePyramid().Territories.Values.ToList();
        var balance     = new BalanceConfig();
        var dread       = new DreadSystem(balance);
        // Advance dread by adding enough fear to reach the target level
        if (dreadLevel >= 2) dread.OnFearGenerated(15); // → level 2
        if (dreadLevel >= 3) dread.OnFearGenerated(15); // → level 3 (30 total)
        if (dreadLevel >= 4) dread.OnFearGenerated(15); // → level 4 (45 total)

        return new EncounterState
        {
            Config      = new EncounterConfig { TideCount = 6, Tier = EncounterTier.Standard },
            Territories = territories,
            Dread       = dread,
            Weave       = new WeaveSystem(startingWeave, 20),
            Corruption  = new CorruptionSystem(),
            Balance     = balance
        };
    }

    private static Card MakeCard(string id) => new()
    {
        Id         = id,
        Name       = id,
        TopEffect  = new EffectData { Type = EffectType.GenerateFear, Value = 1 },
        BottomEffect = new EffectData { Type = EffectType.GenerateFear, Value = 1 }
    };

    // ── 1. ExtractCarryover_CleanBoard_EmptyCorruption ────────────────────────

    [Fact]
    public void ExtractCarryover_CleanBoard_EmptyCorruption()
    {
        var state    = BuildMinimalState();
        var carryover = state.ExtractCarryover();
        Assert.Empty(carryover.CorruptionCarryover);
    }

    // ── 2. ExtractCarryover_DefiledTerritory_PersistsAsL1 ───────────────────

    [Fact]
    public void ExtractCarryover_DefiledTerritory_PersistsAsL1()
    {
        var state = BuildMinimalState();
        var a1    = state.GetTerritory("A1")!;
        // L2 = Defiled requires 8 points
        state.Corruption!.AddCorruption(a1, 9);
        Assert.Equal(2, a1.CorruptionLevel);

        var carryover = state.ExtractCarryover();
        // L2 persists as 3 points (L1 threshold)
        Assert.True(carryover.CorruptionCarryover.ContainsKey("A1"));
        Assert.Equal(3, carryover.CorruptionCarryover["A1"]);
    }

    // ── 3. ExtractCarryover_DesecratedTerritory_FullPersistence ─────────────

    [Fact]
    public void ExtractCarryover_DesecratedTerritory_FullPersistence()
    {
        var state = BuildMinimalState();
        var a1    = state.GetTerritory("A1")!;
        // L3 = Desecrated requires 15 points
        state.Corruption!.AddCorruption(a1, 18);
        Assert.Equal(3, a1.CorruptionLevel);

        var carryover = state.ExtractCarryover();
        // L3 persists at full points
        Assert.True(carryover.CorruptionCarryover.ContainsKey("A1"));
        Assert.Equal(18, carryover.CorruptionCarryover["A1"]);
    }

    // ── 4. ExtractCarryover_WeavePreserved ───────────────────────────────────

    [Fact]
    public void ExtractCarryover_WeavePreserved()
    {
        var state = BuildMinimalState(startingWeave: 15);
        var carryover = state.ExtractCarryover();
        Assert.Equal(15, carryover.FinalWeave);
    }

    // ── 5. ExtractCarryover_DreadPreserved ───────────────────────────────────

    [Fact]
    public void ExtractCarryover_DreadPreserved()
    {
        var state = BuildMinimalState(dreadLevel: 2);
        var carryover = state.ExtractCarryover();
        Assert.Equal(2, carryover.DreadLevel);
    }

    // ── 6. ApplyCarryover_SetsCorruption ────────────────────────────────────

    [Fact]
    public void ApplyCarryover_SetsCorruption()
    {
        var state = BuildMinimalState();
        var carryover = new BoardCarryover
        {
            FinalWeave          = 20,
            CorruptionCarryover = new() { ["A1"] = 3 }
        };

        EncounterRunner.ApplyCarryover(state, carryover);

        var a1 = state.GetTerritory("A1")!;
        Assert.Equal(3, a1.CorruptionPoints);
    }

    // ── 7. ApplyCarryover_SetsWeave ──────────────────────────────────────────

    [Fact]
    public void ApplyCarryover_SetsWeave()
    {
        var state = BuildMinimalState();
        var carryover = new BoardCarryover { FinalWeave = 12 };

        EncounterRunner.ApplyCarryover(state, carryover);

        Assert.Equal(12, state.Weave!.CurrentWeave);
    }

    // ── 9. WeaveLoss_ZeroMissing_NoDecay ──────────────────────────────────────

    [Fact]
    public void WeaveLoss_ZeroMissing_NoDecay()
    {
        Assert.Equal(0, BoardCarryover.CalculateMaxWeaveLoss(20, 20));
    }

    // ── 10. WeaveLoss_3Missing_Loses1Max ─────────────────────────────────────

    [Fact]
    public void WeaveLoss_3Missing_Loses1Max()
    {
        Assert.Equal(1, BoardCarryover.CalculateMaxWeaveLoss(20, 17)); // missing=3
    }

    // ── 11. WeaveLoss_4Missing_Loses2Max ─────────────────────────────────────

    [Fact]
    public void WeaveLoss_4Missing_Loses2Max()
    {
        Assert.Equal(2, BoardCarryover.CalculateMaxWeaveLoss(20, 16)); // missing=4
    }

    // ── 12. WeaveLoss_8Missing_Loses3Max ─────────────────────────────────────

    [Fact]
    public void WeaveLoss_8Missing_Loses3Max()
    {
        Assert.Equal(3, BoardCarryover.CalculateMaxWeaveLoss(20, 12)); // missing=8
    }

    // ── 13. ExtractCarryover_AppliesWeaveLoss ────────────────────────────────

    [Fact]
    public void ExtractCarryover_AppliesWeaveLoss()
    {
        // currentWeave=16, maxWeave=20 → missing=4 → loss=2 → MaxWeave carryover=18
        var state = new EncounterState
        {
            Config      = new EncounterConfig { TideCount = 6, Tier = EncounterTier.Standard },
            Territories = BoardState.CreatePyramid().Territories.Values.ToList(),
            Weave       = new WeaveSystem(16, 20),
            Dread       = new DreadSystem(new BalanceConfig()),
            Corruption  = new CorruptionSystem(),
            Balance     = new BalanceConfig()
        };
        var carryover = state.ExtractCarryover();
        Assert.Equal(16, carryover.FinalWeave);
        Assert.Equal(18, carryover.MaxWeave);
    }

    // ── 14. ApplyCarryover_SetsMaxWeave ──────────────────────────────────────

    [Fact]
    public void ApplyCarryover_SetsMaxWeave()
    {
        var state = BuildMinimalState();
        var carryover = new BoardCarryover { FinalWeave = 16, MaxWeave = 18 };

        EncounterRunner.ApplyCarryover(state, carryover);

        // Max is 18; restoring more should be capped
        state.Weave!.Restore(10);
        Assert.Equal(18, state.Weave.CurrentWeave);
    }

    // ── 8. ApplyCarryover_RemovesCards ───────────────────────────────────────

    [Fact]
    public void ApplyCarryover_RemovesCards()
    {
        var state   = BuildMinimalState();
        var warden  = new RootAbility(new PresenceSystem(
            () => state.Territories, 3), state.Balance);
        var card    = MakeCard("test_card");
        state.Deck  = new DeckManager(warden, new[] { card }, shuffle: false);
        Assert.Equal(1, state.Deck.DrawPileCount);

        var carryover = new BoardCarryover
        {
            PermanentlyRemovedCards = new() { "test_card" }
        };

        EncounterRunner.ApplyCarryover(state, carryover);

        Assert.Equal(0, state.Deck.DrawPileCount);
    }
}
