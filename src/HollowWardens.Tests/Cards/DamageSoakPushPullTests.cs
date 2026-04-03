namespace HollowWardens.Tests.Cards;

using HollowWardens.Core.Cards;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>Tests for Task 6: Damage Soak + Push/Pull Rework.</summary>
public class DamageSoakPushPullTests
{
    private static Card MakeCard(string id) => new Card
    {
        Id = id, Name = id, Elements = Array.Empty<Element>(),
        TopEffect = new EffectData(), BottomEffect = new EffectData()
    };

    private static DeckManager MakeDeck(params Card[] hand)
    {
        var warden = new RootAbility();
        var deck = new DeckManager(warden, hand, shuffle: false);
        deck.RefillHand();
        return deck;
    }

    private static EncounterState MakeState()
    {
        var state = new EncounterState
        {
            Corruption = new CorruptionSystem(),
            Graph = TerritoryGraph.Create("standard")
        };
        foreach (var id in state.Graph.AllTerritoryIds)
            state.Territories.Add(new Territory { Id = id });
        return state;
    }

    // ── Damage Soak ──────────────────────────────────────────────────────────

    [Fact]
    public void SoakDamage_CardGoesToTopDiscard()
    {
        var card = MakeCard("c1");
        var deck = MakeDeck(card);

        deck.SoakDamage(card);

        Assert.Equal(1, deck.TopDiscardCount);
        Assert.Empty(deck.Hand);
    }

    [Fact]
    public void SoakDamage_CardIsRecoveredOnRest()
    {
        var card = MakeCard("c1");
        var deck = MakeDeck(card);
        deck.SoakDamage(card);
        Assert.Equal(1, deck.TopDiscardCount);

        deck.BeginRestWithPairing();

        // Top-discard goes back to hand on rest
        Assert.Equal(0, deck.TopDiscardCount);
        Assert.Single(deck.Hand);
    }

    [Fact]
    public void SoakDamage_CardNotInHand_Throws()
    {
        var card = MakeCard("c1");
        var deck = MakeDeck(); // empty hand
        Assert.Throws<InvalidOperationException>(() => deck.SoakDamage(card));
    }

    // ── Push Rework ──────────────────────────────────────────────────────────

    [Fact]
    public void Push_AutoSelection_PushesToFarthestNeighbor()
    {
        var state = MakeState();
        var tM1 = state.GetTerritory("M1")!;
        var invader = new Invader { Hp = 3, MaxHp = 3, TerritoryId = "M1" };
        tM1.Invaders.Add(invader);

        var effect = new PushInvadersEffect(new EffectData { Type = EffectType.PushInvaders, Value = 1 });
        effect.Resolve(state, new TargetInfo { TerritoryId = "M1" });

        // Invader should have been pushed away from M1
        Assert.Empty(tM1.Invaders);
    }

    [Fact]
    public void Push_PlayerChoosesDestination_PerInvader()
    {
        var state = MakeState();
        var tM1 = state.GetTerritory("M1")!;

        // Add 2 invaders
        var inv1 = new Invader { Hp = 3, MaxHp = 3, TerritoryId = "M1" };
        var inv2 = new Invader { Hp = 3, MaxHp = 3, TerritoryId = "M1" };
        tM1.Invaders.Add(inv1);
        tM1.Invaders.Add(inv2);

        // Player pushes both — check that destinations are honored (A1 and A2 are neighbors of M1)
        var neighbors = state.Graph.GetNeighbors("M1").ToList();
        Assert.True(neighbors.Count >= 2, "M1 should have at least 2 neighbors");

        var dest1 = neighbors[0];
        var dest2 = neighbors.Count > 1 ? neighbors[1] : neighbors[0];

        var effect = new PushInvadersEffect(new EffectData { Type = EffectType.PushInvaders, Value = 2 });
        effect.Resolve(state, new TargetInfo
        {
            TerritoryId = "M1",
            PushDestinations = new List<string> { dest1, dest2 }
        });

        // Both invaders pushed out of M1
        Assert.Empty(tM1.Invaders);
        Assert.Equal(1, state.GetTerritory(dest1)!.Invaders.Count);
        Assert.Equal(1, state.GetTerritory(dest2)!.Invaders.Count);
    }

    [Fact]
    public void Push_CanSplitInvadersAcrossNeighbors()
    {
        var state = MakeState();
        var tA1 = state.GetTerritory("A1")!;
        for (int i = 0; i < 3; i++)
            tA1.Invaders.Add(new Invader { Hp = 3, MaxHp = 3, TerritoryId = "A1" });

        // Get valid neighbors for A1
        var neighbors = state.Graph.GetNeighbors("A1").ToList();
        Assert.NotEmpty(neighbors);

        // Split: 2 go to first neighbor, 1 goes to second (if exists)
        var dests = new List<string> { neighbors[0], neighbors[0], neighbors.Count > 1 ? neighbors[1] : neighbors[0] };
        var effect = new PushInvadersEffect(new EffectData { Type = EffectType.PushInvaders, Value = 3 });
        effect.Resolve(state, new TargetInfo { TerritoryId = "A1", PushDestinations = dests });

        Assert.Empty(tA1.Invaders);
    }

    // ── Pull ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Pull_GathersInvadersFromAdjacent()
    {
        var state = MakeState();
        var tA1 = state.GetTerritory("A1")!;
        var tM1 = state.GetTerritory("M1")!;

        // Place invader in A1 (adjacent to M1 in standard layout)
        var inv = new Invader { Hp = 3, MaxHp = 3, TerritoryId = "A1" };
        tA1.Invaders.Add(inv);

        var effect = new PullInvadersEffect(new EffectData { Type = EffectType.PullInvaders, Value = 5 });
        effect.Resolve(state, new TargetInfo { TerritoryId = "M1" });

        Assert.Empty(tA1.Invaders);
        Assert.Single(tM1.Invaders);
        Assert.Equal("M1", inv.TerritoryId);
    }

    [Fact]
    public void Pull_RespectsMaxCount()
    {
        var state = MakeState();
        var tA1 = state.GetTerritory("A1")!;
        var tM1 = state.GetTerritory("M1")!;

        // Place 4 invaders in neighbors
        for (int i = 0; i < 4; i++)
            tA1.Invaders.Add(new Invader { Hp = 3, MaxHp = 3, TerritoryId = "A1" });

        var effect = new PullInvadersEffect(new EffectData { Type = EffectType.PullInvaders, Value = 2 });
        effect.Resolve(state, new TargetInfo { TerritoryId = "M1" });

        // Only 2 should have moved
        Assert.Equal(2, tM1.Invaders.Count);
        Assert.Equal(2, tA1.Invaders.Count);
    }

    [Fact]
    public void Pull_IntoScorched_AppliesEntryDamage()
    {
        var state = MakeState();
        // Make M1 Scorched
        var tM1 = state.GetTerritory("M1")!;
        tM1.Terrain = TerrainType.Scorched;

        var tA1 = state.GetTerritory("A1")!;
        var inv = new Invader { Hp = 4, MaxHp = 4, TerritoryId = "A1" };
        tA1.Invaders.Add(inv);

        var effect = new PullInvadersEffect(new EffectData { Type = EffectType.PullInvaders, Value = 5 });
        effect.Resolve(state, new TargetInfo { TerritoryId = "M1" });

        Assert.Single(tM1.Invaders);
        Assert.Equal(2, inv.Hp); // 4 - 2 entry damage = 2
    }
}
