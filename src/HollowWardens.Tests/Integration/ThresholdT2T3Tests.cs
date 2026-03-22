namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders.PaleMarch;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Tier 2 and Tier 3 threshold effects for all 6 elements.
/// All tests use AutoResolve to bypass the pending queue.
/// HP reference: Marcher=4, Ironclad=5, Outrider=3, Pioneer=2.
/// </summary>
public class ThresholdT2T3Tests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    private static (EncounterState state, PaleMarchFaction faction) BuildState(int weave = 20)
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, _, _, _, faction) = IntegrationHelpers.Build(
            IntegrationHelpers.MakeCards(5), new RootAbility(), config, weave: weave);
        state.GetTerritory("I1")!.PresenceCount = 1;
        return (state, faction);
    }

    private static Invader MakeMarcher(PaleMarchFaction f, string tid)  => f.CreateUnit(UnitType.Marcher,  tid);
    private static Invader MakeOutrider(PaleMarchFaction f, string tid) => f.CreateUnit(UnitType.Outrider, tid);
    private static Invader MakeIronclad(PaleMarchFaction f, string tid) => f.CreateUnit(UnitType.Ironclad, tid);

    // ── ROOT T2 ───────────────────────────────────────────────────────────────

    [Fact]
    public void RootTier2_ReducesCorruptionByThree_InHighestCorruptTerritory()
    {
        var (state, _) = BuildState();
        state.GetTerritory("I1")!.CorruptionPoints = 10;

        new ThresholdResolver().AutoResolve(Element.Root, 2, state);

        Assert.Equal(7, state.GetTerritory("I1")!.CorruptionPoints);
    }

    [Fact]
    public void RootTier2_PicksTerritoryWithPresenceAndHighestCorruption()
    {
        var (state, _) = BuildState();
        // I1 has presence (1) + 5 corruption; M1 no presence + 10 corruption
        state.GetTerritory("I1")!.CorruptionPoints = 5;
        state.GetTerritory("M1")!.CorruptionPoints = 10;

        new ThresholdResolver().AutoResolve(Element.Root, 2, state);

        // Only I1 has presence → reduced; M1 untouched
        Assert.Equal(2, state.GetTerritory("I1")!.CorruptionPoints);
        Assert.Equal(10, state.GetTerritory("M1")!.CorruptionPoints);
    }

    // ── ROOT T3 ───────────────────────────────────────────────────────────────

    [Fact]
    public void RootTier3_PlacesTwoPresenceTokens()
    {
        var (state, _) = BuildState();
        int before = state.Territories.Sum(t => t.PresenceCount);

        new ThresholdResolver().AutoResolve(Element.Root, 3, state);

        Assert.Equal(before + 2, state.Territories.Sum(t => t.PresenceCount));
    }

    [Fact]
    public void RootTier3_ReducesCorruptionByTwoInEachPresenceTerritory()
    {
        var (state, _) = BuildState();
        state.GetTerritory("I1")!.CorruptionPoints = 6;
        state.GetTerritory("M1")!.CorruptionPoints = 4;
        state.GetTerritory("M1")!.PresenceCount = 1;

        new ThresholdResolver().AutoResolve(Element.Root, 3, state);

        // Both territories with presence lose 2 corruption
        Assert.True(state.GetTerritory("I1")!.CorruptionPoints <= 4);
        Assert.True(state.GetTerritory("M1")!.CorruptionPoints <= 2);
    }

    // ── MIST T2 ───────────────────────────────────────────────────────────────

    [Fact]
    public void MistTier2_ReturnsOneCardFromDiscardToHand()
    {
        var (state, _) = BuildState();
        state.Deck!.RefillHand();
        state.Deck.PlayTop(state.Deck.Hand[0]); // put 1 card in discard

        int handBefore    = state.Deck.Hand.Count;
        int discardBefore = state.Deck.DiscardCount;

        new ThresholdResolver().AutoResolve(Element.Mist, 2, state);

        Assert.Equal(handBefore + 1,    state.Deck.Hand.Count);
        Assert.Equal(discardBefore - 1, state.Deck.DiscardCount);
    }

    [Fact]
    public void MistTier2_DoesNothing_WhenDiscardIsEmpty()
    {
        var (state, _) = BuildState();
        state.Deck!.RefillHand();
        int handBefore = state.Deck.Hand.Count;

        new ThresholdResolver().AutoResolve(Element.Mist, 2, state);

        Assert.Equal(handBefore, state.Deck.Hand.Count);
    }

    // ── MIST T3 ───────────────────────────────────────────────────────────────

    [Fact]
    public void MistTier3_RestoresThreeWeave()
    {
        var (state, _) = BuildState(weave: 10);

        new ThresholdResolver().AutoResolve(Element.Mist, 3, state);

        Assert.Equal(13, state.Weave!.CurrentWeave);
    }

    [Fact]
    public void MistTier3_ReturnsAllDiscardedCardsToHand()
    {
        var (state, _) = BuildState();
        state.Deck!.RefillHand();
        state.Deck.PlayTop(state.Deck.Hand[0]);
        state.Deck.PlayTop(state.Deck.Hand[0]); // 2 in discard

        new ThresholdResolver().AutoResolve(Element.Mist, 3, state);

        Assert.Equal(0, state.Deck.DiscardCount);
    }

    // ── SHADOW T2 ─────────────────────────────────────────────────────────────

    [Fact]
    public void ShadowTier2_ElevatesNextQueuedFearActionByOneDreadLevel()
    {
        var (state, _) = BuildState();
        // Dread = level 1; fear pools: fa_d1 (level 1) and fa_d2 (level 2)
        new ThresholdResolver().AutoResolve(Element.Shadow, 2, state);

        state.FearActions!.OnFearSpent(5); // queue 1 action — should draw from level 2
        var queued = state.FearActions.DrainQueue();

        Assert.Single(queued);
        Assert.Equal(2, queued[0].DreadLevel);
    }

    [Fact]
    public void ShadowTier2_ElevationConsumedAfterOneAction_NotSecond()
    {
        var (state, _) = BuildState();
        new ThresholdResolver().AutoResolve(Element.Shadow, 2, state);

        state.FearActions!.OnFearSpent(10); // queue 2 actions
        var queued = state.FearActions.DrainQueue();

        Assert.Equal(2, queued.Count);
        Assert.Equal(2, queued[0].DreadLevel); // first elevated
        Assert.Equal(1, queued[1].DreadLevel); // second normal
    }

    // ── SHADOW T3 ─────────────────────────────────────────────────────────────

    [Fact]
    public void ShadowTier3_GeneratesFiveFear()
    {
        var (state, _) = BuildState();
        int before = state.Dread!.TotalFearGenerated;

        new ThresholdResolver().AutoResolve(Element.Shadow, 3, state);

        Assert.Equal(before + 5, state.Dread.TotalFearGenerated);
    }

    // ── ASH T2 ────────────────────────────────────────────────────────────────

    [Fact]
    public void AshTier2_DealsTwoDamageToAllInvadersInMostInvadedTerritory()
    {
        var (state, faction) = BuildState();
        var territory = state.GetTerritory("M1")!;
        var i1 = MakeMarcher(faction, "M1"); // MaxHp=4
        var i2 = MakeMarcher(faction, "M1");
        territory.Invaders.Add(i1);
        territory.Invaders.Add(i2);

        new ThresholdResolver().AutoResolve(Element.Ash, 2, state);

        Assert.Equal(2, i1.Hp); // 4-2=2
        Assert.Equal(2, i2.Hp);
    }

    [Fact]
    public void AshTier2_AddsOneCorruptionToTargetTerritory()
    {
        var (state, faction) = BuildState();
        var territory = state.GetTerritory("M1")!;
        territory.Invaders.Add(MakeMarcher(faction, "M1"));

        new ThresholdResolver().AutoResolve(Element.Ash, 2, state);

        Assert.Equal(1, territory.CorruptionPoints);
    }

    // ── ASH T3 ────────────────────────────────────────────────────────────────

    [Fact]
    public void AshTier3_DealsThreeDamageToAllBoardInvaders()
    {
        var (state, faction) = BuildState();
        var m1inv = MakeMarcher(faction, "M1");  // wounded to 3 → 3 damage kills
        m1inv.Hp = 3;
        var a1inv = MakeOutrider(faction, "A1"); // MaxHp=3 → 3 damage kills
        state.GetTerritory("M1")!.Invaders.Add(m1inv);
        state.GetTerritory("A1")!.Invaders.Add(a1inv);

        new ThresholdResolver().AutoResolve(Element.Ash, 3, state);

        Assert.False(m1inv.IsAlive);
        Assert.False(a1inv.IsAlive);
    }

    [Fact]
    public void AshTier3_NoCorruptionAdded_CorruptionRiderRemoved()
    {
        // D31 fix: Ash T3 no longer adds +1 corruption to each territory.
        var (state, faction) = BuildState();
        var m1 = state.GetTerritory("M1")!;
        var a1 = state.GetTerritory("A1")!;
        m1.Invaders.Add(MakeMarcher(faction, "M1"));
        a1.Invaders.Add(MakeMarcher(faction, "A1"));

        new ThresholdResolver().AutoResolve(Element.Ash, 3, state);

        Assert.Equal(0, m1.CorruptionPoints);
        Assert.Equal(0, a1.CorruptionPoints);
    }

    [Fact]
    public void AshTier3_IroncladSurvivesThreeDamage()
    {
        var (state, faction) = BuildState();
        var inv = MakeIronclad(faction, "M1"); // MaxHp=5
        state.GetTerritory("M1")!.Invaders.Add(inv);

        new ThresholdResolver().AutoResolve(Element.Ash, 3, state);

        Assert.True(inv.IsAlive);
        Assert.Equal(2, inv.Hp); // 5-3=2
    }

    // ── GALE T2 ───────────────────────────────────────────────────────────────

    [Fact]
    public void GaleTier2_PushesAllInvadersOutOfClosestTerritory()
    {
        var (state, faction) = BuildState();
        var i1 = state.GetTerritory("I1")!;
        i1.Invaders.Add(MakeMarcher(faction, "I1"));
        i1.Invaders.Add(MakeMarcher(faction, "I1"));

        new ThresholdResolver().AutoResolve(Element.Gale, 2, state);

        Assert.Equal(0, i1.Invaders.Count(inv => inv.IsAlive));
        // Both pushed to M-row
        int mRowCount = state.GetTerritory("M1")!.Invaders.Count
                      + state.GetTerritory("M2")!.Invaders.Count;
        Assert.Equal(2, mRowCount);
    }

    [Fact]
    public void GaleTier2_LeavesOtherTerritoryUntouched()
    {
        var (state, faction) = BuildState();
        state.GetTerritory("I1")!.Invaders.Add(MakeMarcher(faction, "I1")); // closest → pushed
        var m1inv = MakeMarcher(faction, "M1");
        state.GetTerritory("M1")!.Invaders.Add(m1inv);                      // not closest → untouched

        new ThresholdResolver().AutoResolve(Element.Gale, 2, state);

        // M1 invader not pushed (only I1 was targeted as closest)
        Assert.Equal("M1", m1inv.TerritoryId);
    }

    // ── GALE T3 ───────────────────────────────────────────────────────────────

    [Fact]
    public void GaleTier3_PushesAllInvadersOnBoard()
    {
        var (state, faction) = BuildState();
        var i1 = state.GetTerritory("I1")!;
        var m1 = state.GetTerritory("M1")!;
        var i1inv = MakeMarcher(faction, "I1");
        var m1inv = MakeMarcher(faction, "M1");
        i1.Invaders.Add(i1inv);
        m1.Invaders.Add(m1inv);

        new ThresholdResolver().AutoResolve(Element.Gale, 3, state);

        // Each invader is displaced from its starting territory
        Assert.NotEqual("I1", i1inv.TerritoryId);
        Assert.NotEqual("M1", m1inv.TerritoryId);
    }

    [Fact]
    public void GaleTier3_ARowInvadersCanPushToAdjacentARow()
    {
        // A1 neighbors: A2 (dist 2) and M1 (dist 1). Push prefers higher distance → A2.
        var (state, faction) = BuildState();
        var a1 = state.GetTerritory("A1")!;
        var inv = MakeMarcher(faction, "A1");
        a1.Invaders.Add(inv);

        new ThresholdResolver().AutoResolve(Element.Gale, 3, state);

        // A1 is pushed to A2 (same distance from I1, valid push target)
        Assert.Equal("A2", inv.TerritoryId);
    }

    // ── VOID T2 ───────────────────────────────────────────────────────────────

    [Fact]
    public void VoidTier2_AllInvadersOnBoardTakeOneDamage()
    {
        var (state, faction) = BuildState();
        var m1inv = MakeMarcher(faction, "M1");   // MaxHp=4
        var m2inv = MakeIronclad(faction, "M2");  // MaxHp=5
        state.GetTerritory("M1")!.Invaders.Add(m1inv);
        state.GetTerritory("M2")!.Invaders.Add(m2inv);

        new ThresholdResolver().AutoResolve(Element.Void, 2, state);

        Assert.Equal(3, m1inv.Hp); // 4-1=3
        Assert.Equal(4, m2inv.Hp); // 5-1=4
    }

    [Fact]
    public void VoidTier2_KillsOutrider_WithOneDamage()
    {
        var (state, faction) = BuildState();
        var inv = MakeOutrider(faction, "M1"); // MaxHp=2, but wounded to 1
        inv.Hp = 1;
        state.GetTerritory("M1")!.Invaders.Add(inv);

        new ThresholdResolver().AutoResolve(Element.Void, 2, state);

        Assert.False(inv.IsAlive);
    }

    // ── VOID T3 ───────────────────────────────────────────────────────────────

    [Fact]
    public void VoidTier3_AllInvadersTakeTwoDamage()
    {
        var (state, faction) = BuildState();
        var m1inv = MakeMarcher(faction, "M1");   // MaxHp=4 → 4-2=2
        var m2inv = MakeIronclad(faction, "M2");  // MaxHp=5 → 5-2=3
        state.GetTerritory("M1")!.Invaders.Add(m1inv);
        state.GetTerritory("M2")!.Invaders.Add(m2inv);

        new ThresholdResolver().AutoResolve(Element.Void, 3, state);

        Assert.Equal(2, m1inv.Hp);
        Assert.Equal(3, m2inv.Hp);
    }

    [Fact]
    public void VoidTier3_KillsOutrider_WithTwoDamage()
    {
        var (state, faction) = BuildState();
        var inv = MakeOutrider(faction, "M1"); // wounded to 2 → dies to 2 damage
        inv.Hp = 2;
        state.GetTerritory("M1")!.Invaders.Add(inv);

        new ThresholdResolver().AutoResolve(Element.Void, 3, state);

        Assert.False(inv.IsAlive);
    }
}
