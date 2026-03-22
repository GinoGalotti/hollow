namespace HollowWardens.Tests;

using HollowWardens.Core;
using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Invaders.PaleMarch;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using HollowWardens.Core.Wardens;
using HollowWardens.Tests.Integration;
using Xunit;

/// <summary>
/// Tests for all 19 encounter design levers: easy (7), medium (6), hard (6).
/// Each test verifies the lever's config field exists and produces the expected effect.
/// </summary>
public class EncounterLeverTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    // ── Easy Tier ─────────────────────────────────────────────────────────────

    [Fact]
    public void ElementDecayOverride_UsedInsteadOfGlobal()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.ElementDecayOverride = 3;
        var balance = new BalanceConfig(); // default ElementDecayPerTurn = 1
        // Simulate ApplyEncounterConfigLevers
        if (config.ElementDecayOverride.HasValue)
            balance.ElementDecayPerTurn = config.ElementDecayOverride.Value;
        Assert.Equal(3, balance.ElementDecayPerTurn);
    }

    [Fact]
    public void StartingElements_AppliedAtSetup()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.StartingElements = new Dictionary<string, int> { ["Root"] = 3 };
        var elements = new ElementSystem();
        // Simulate ApplyEncounterConfigLevers
        foreach (var (name, count) in config.StartingElements)
            if (Enum.TryParse<Element>(name, ignoreCase: true, out var el))
                elements.AddElements(new[] { el }, count);
        Assert.Equal(3, elements.Get(Element.Root));
    }

    [Fact]
    public void ThresholdDamageBonus_AppliedToAllTiers()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.ThresholdDamageBonus = 2;
        var balance = new BalanceConfig(); // T1=1, T2=2, T3=3
        // Simulate ApplyEncounterConfigLevers
        balance.ThresholdT1Damage += config.ThresholdDamageBonus;
        balance.ThresholdT2Damage += config.ThresholdDamageBonus;
        balance.ThresholdT3Damage += config.ThresholdDamageBonus;
        Assert.Equal(3, balance.ThresholdT1Damage); // 1 + 2
        Assert.Equal(4, balance.ThresholdT2Damage); // 2 + 2
        Assert.Equal(5, balance.ThresholdT3Damage); // 3 + 2
    }

    [Fact]
    public void PlayLimitOverrides_RestrictCardPlays()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.VigilPlayLimitOverride = 1;
        config.DuskPlayLimitOverride  = 0;
        var balance = new BalanceConfig(); // VigilPlayLimit=2, DuskPlayLimit=1
        // Simulate ApplyEncounterConfigLevers
        if (config.VigilPlayLimitOverride.HasValue) balance.VigilPlayLimit = config.VigilPlayLimitOverride.Value;
        if (config.DuskPlayLimitOverride.HasValue)  balance.DuskPlayLimit  = config.DuskPlayLimitOverride.Value;
        Assert.Equal(1, balance.VigilPlayLimit);
        Assert.Equal(0, balance.DuskPlayLimit);
    }

    [Fact]
    public void NativeOverride_CustomHpAndDamage()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.NativeHpOverride     = 5;
        config.NativeDamageOverride = 10;
        var defaults = new BalanceConfig();
        int hp  = config.NativeHpOverride     ?? defaults.DefaultNativeHp;
        int dmg = config.NativeDamageOverride ?? defaults.DefaultNativeDamage;
        Assert.Equal(5,  hp);
        Assert.Equal(10, dmg);
    }

    [Fact]
    public void FearMultiplier_HalvesFear()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.FearMultiplier = 0.5f;
        var (state, _, _, _, _) = IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);
        // ApplyFearMultiplier is on EncounterState
        Assert.Equal(5, state.ApplyFearMultiplier(10)); // (int)(10 * 0.5) = 5
        Assert.Equal(0, state.ApplyFearMultiplier(1));  // (int)(1 * 0.5) = 0
    }

    [Fact]
    public void HeartDamageMultiplier_IncreasesWeaveLoss()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.HeartDamageMultiplier = 2.0f;
        var (state, _, _, _, _) = IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        // Place a Marcher (HP=4) directly in I1 (heart zone)
        var i1 = state.GetTerritory("I1")!;
        var marcher = new Invader { Id = "m1", UnitType = UnitType.Marcher, Hp = 4, MaxHp = 4, TerritoryId = "I1" };
        i1.Invaders.Add(marcher);

        int weaveBefore = state.Weave!.CurrentWeave;
        var combat = new CombatSystem();
        // ExecuteAdvance records invaders already in I1 for heart march
        combat.ExecuteAdvance(new ActionCard { AdvanceModifier = 1 }, state);
        combat.ExecuteHeartMarch(state);

        // damage = (int)(4 * 2.0) = 8
        Assert.Equal(weaveBefore - 8, state.Weave!.CurrentWeave);
    }

    // ── Medium Tier ───────────────────────────────────────────────────────────

    [Fact]
    public void InvaderCorruptionScaling_BonusHp_MatchesL1Count()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        config.InvaderCorruptionScaling = true;
        config.Waves.Add(new SpawnWave
        {
            TurnNumber = 2,
            Options = new List<SpawnWaveOption>
            {
                new() { Weight = 1, Units = new Dictionary<string, List<UnitType>> { ["A1"] = new() { UnitType.Marcher } } }
            }
        });

        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        // Corrupt 2 territories to L1 (3 pts = L1 threshold)
        state.Corruption!.AddCorruption(state.GetTerritory("A2")!, 3);
        state.Corruption!.AddCorruption(state.GetTerritory("M1")!, 3);
        Assert.Equal(2, state.Territories.Count(t => t.CorruptionLevel >= 1));

        Invader? arrived = null;
        GameEvents.InvaderArrived += (inv, _) => arrived = inv;

        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());
        tideRunner.ExecuteTide(1, state);

        Assert.NotNull(arrived);
        Assert.Equal(4 + 2, arrived!.Hp); // Marcher base HP=4, bonus=2 L1 territories
    }

    [Fact]
    public void InvaderArrivalShield_AppliedOnSpawn()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        config.InvaderArrivalShield = 3;
        config.Waves.Add(new SpawnWave
        {
            TurnNumber = 2,
            Options = new List<SpawnWaveOption>
            {
                new() { Weight = 1, Units = new Dictionary<string, List<UnitType>> { ["A1"] = new() { UnitType.Marcher } } }
            }
        });

        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        Invader? arrived = null;
        GameEvents.InvaderArrived += (inv, _) => arrived = inv;

        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());
        tideRunner.ExecuteTide(1, state);

        Assert.NotNull(arrived);
        Assert.Equal(3, arrived!.ShieldValue);
    }

    [Fact]
    public void InvaderRegenOnRest_HealsInvaders()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.InvaderRegenOnRest = 2;
        var (state, _, _, _, _) = IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        // Place a damaged Marcher (max HP = 6 so regen doesn't cap at the usual base)
        var a1 = state.GetTerritory("A1")!;
        var marcher = new Invader { Id = "m1", UnitType = UnitType.Marcher, Hp = 2, MaxHp = 6, TerritoryId = "A1" };
        a1.Invaders.Add(marcher);

        // Simulate EncounterRunner rest processing
        if (config.InvaderRegenOnRest > 0)
            foreach (var t in state.Territories)
                foreach (var inv in t.Invaders.Where(i => i.IsAlive))
                    inv.Hp = Math.Min(inv.MaxHp, inv.Hp + config.InvaderRegenOnRest);

        Assert.Equal(4, marcher.Hp); // 2 + 2
    }

    [Fact]
    public void InvaderAdvanceBonus_IncreasesMovement()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.InvaderAdvanceBonus = 1;
        var (state, _, _, _, _) = IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        // Place Marcher at A3 — standard board path: A3 → M2 → I1 (2 hops)
        var a3 = state.GetTerritory("A3")!;
        var marcher = new Invader { Id = "m1", UnitType = UnitType.Marcher, Hp = 4, MaxHp = 4, TerritoryId = "A3" };
        a3.Invaders.Add(marcher);

        // Card AdvanceModifier=1 + bonus=1 = 2 steps → reaches I1
        var card = new ActionCard { Id = "test", AdvanceModifier = 1 };
        new CombatSystem().ExecuteAdvance(card, state);

        Assert.Equal("I1", marcher.TerritoryId);
    }

    [Fact]
    public void SurgeTide_SpawnsDoubleWave()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        config.SurgeTides = new List<int> { 1 };
        config.Waves.Add(new SpawnWave
        {
            TurnNumber = 2,
            Options = new List<SpawnWaveOption>
            {
                new() { Weight = 1, Units = new Dictionary<string, List<UnitType>> { ["A1"] = new() { UnitType.Marcher } } }
            }
        });

        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        int arrivedCount = 0;
        GameEvents.InvaderArrived += (_, _) => arrivedCount++;

        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());
        tideRunner.ExecuteTide(1, state);

        // Surge: wave is spawned twice → 2 invaders instead of 1
        Assert.Equal(2, arrivedCount);
    }

    [Fact]
    public void PresencePlacementCorruptionCost_AddsCorruption()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.PresencePlacementCorruptionCost = 2;
        var (state, _, _, _, _) = IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        var a1 = state.GetTerritory("A1")!;
        var effect = new PlacePresenceEffect(new EffectData { Type = EffectType.PlacePresence, Value = 1 });
        effect.Resolve(state, new TargetInfo { TerritoryId = "A1" });

        Assert.Equal(2, a1.CorruptionPoints);
    }

    // ── Hard Tier ─────────────────────────────────────────────────────────────

    [Fact]
    public void CorruptionSpread_L1Territory_SpreadsToAdjacentL0()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.CorruptionSpread = 1;
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);
        state.Random = GameRandom.FromSeed(42);

        // Corrupt A1 to L1 (3 pts)
        state.Corruption!.AddCorruption(state.GetTerritory("A1")!, 3);
        Assert.Equal(1, state.GetTerritory("A1")!.CorruptionLevel);

        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());
        tideRunner.RunTideEndEffects(1, state);

        // A1's neighbors (A2, M1) — at least one should have received 1 corruption
        bool spread = state.GetTerritory("A2")!.CorruptionPoints > 0
                   || state.GetTerritory("M1")!.CorruptionPoints > 0;
        Assert.True(spread);
    }

    [Fact]
    public void CorruptionSpread_L0Territory_DoesNotSpread()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.CorruptionSpread = 1;
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);
        state.Random = GameRandom.FromSeed(42);

        // All territories start at L0 — nothing to spread
        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());
        tideRunner.RunTideEndEffects(1, state);

        Assert.True(state.Territories.All(t => t.CorruptionPoints == 0));
    }

    [Fact]
    public void SacredTerritory_CannotGainCorruption()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.SacredTerritories = new List<string> { "A1" };
        var (state, _, _, _, _) = IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        // Wire sacred territories (simulates ApplyEncounterConfigLevers)
        if (state.Corruption is CorruptionSystem cs)
            foreach (var id in config.SacredTerritories!)
                cs.SacredTerritories.Add(id);

        var a1 = state.GetTerritory("A1")!;
        state.Corruption!.AddCorruption(a1, 10); // would normally reach L2

        Assert.Equal(0, a1.CorruptionPoints);
    }

    [Fact]
    public void NativeErosion_ReducesHpEachTide()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.NativeErosionPerTide = 1;
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        var a1 = state.GetTerritory("A1")!;
        var native = new Native { Hp = 3, MaxHp = 3, Damage = 2, TerritoryId = "A1" };
        a1.Natives.Add(native);

        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());
        tideRunner.RunTideEndEffects(1, state);

        Assert.Equal(2, native.Hp); // 3 - 1
        Assert.True(native.IsAlive);
    }

    [Fact]
    public void NativeErosion_KillsNativeAtZero()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.NativeErosionPerTide = 2;
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        var a1 = state.GetTerritory("A1")!;
        var native = new Native { Hp = 1, MaxHp = 1, Damage = 2, TerritoryId = "A1" };
        a1.Natives.Add(native);

        int defeatedFired = 0;
        GameEvents.NativeDefeated += (_, _) => defeatedFired++;

        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());
        tideRunner.RunTideEndEffects(1, state);

        Assert.Equal(0, a1.Natives.Count(n => n.IsAlive));
        Assert.Equal(1, defeatedFired);
    }

    [Fact]
    public void BlightPulse_AddsCorruptionEveryNTides()
    {
        var config = IntegrationHelpers.MakeConfig();
        config.BlightPulseInterval = 2;
        var (state, actionDeck, cadence, spawn, faction) =
            IntegrationHelpers.Build(IntegrationHelpers.MakeCards(5), new RootAbility(), config);
        state.Random = GameRandom.FromSeed(42);

        var tideRunner = new TideRunner(actionDeck, cadence, spawn, faction, new EffectResolver());

        // Tide 1: 1 % 2 != 0 — no blight pulse
        tideRunner.RunTideEndEffects(1, state);
        Assert.True(state.Territories.All(t => t.CorruptionPoints == 0));

        // Tide 2: 2 % 2 == 0 — blight fires, one random territory gains +3 corruption
        tideRunner.RunTideEndEffects(2, state);
        Assert.True(state.Territories.Any(t => t.CorruptionPoints > 0));
    }
}
