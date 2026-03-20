// Disable parallelism for all tests in this assembly — GameEvents uses shared static
// delegate fields, so parallel test classes cause flaky failures via ClearAll() races.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace HollowWardens.Tests.Effects;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;
using Xunit;

public class EffectTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    // ── PlacePresence ─────────────────────────────────────────────────────────

    [Fact]
    public void PlacePresenceAddsToTerritory()
    {
        var territories = new List<Territory> { new() { Id = "A1" } };
        var state = new EncounterState
        {
            Territories = territories,
            Presence    = new PresenceSystem(() => territories),
        };
        var target = new TargetInfo { TerritoryId = "A1" };
        var effect = new PlacePresenceEffect(new EffectData { Type = EffectType.PlacePresence, Value = 1 });

        effect.Resolve(state, target);

        Assert.Equal(1, state.GetTerritory("A1")!.PresenceCount);
    }

    // ── ReduceCorruption ──────────────────────────────────────────────────────

    [Fact]
    public void ReduceCorruptionRemovesPoints()
    {
        var territory = new Territory { Id = "A1", CorruptionPoints = 5 };
        var state = new EncounterState
        {
            Territories = new List<Territory> { territory },
            Corruption  = new CorruptionSystem(),
        };
        var target = new TargetInfo { TerritoryId = "A1" };
        var effect = new ReduceCorruptionEffect(new EffectData { Type = EffectType.ReduceCorruption, Value = 3 });

        effect.Resolve(state, target);

        Assert.Equal(2, territory.CorruptionPoints);
    }

    // ── DamageInvaders ────────────────────────────────────────────────────────

    [Fact]
    public void DamageInvadersHitsAllInTerritory()
    {
        var inv1      = new Invader { Id = "i1", Hp = 3, MaxHp = 3, TerritoryId = "A1" };
        var inv2      = new Invader { Id = "i2", Hp = 3, MaxHp = 3, TerritoryId = "A1" };
        var territory = new Territory { Id = "A1" };
        territory.Invaders.Add(inv1);
        territory.Invaders.Add(inv2);
        var state  = new EncounterState { Territories = new List<Territory> { territory } };
        var target = new TargetInfo { TerritoryId = "A1" };
        var effect = new DamageInvadersEffect(new EffectData { Type = EffectType.DamageInvaders, Value = 2 });

        effect.Resolve(state, target);

        Assert.Equal(1, inv1.Hp);
        Assert.Equal(1, inv2.Hp);
    }

    // ── GenerateFear ──────────────────────────────────────────────────────────

    [Fact]
    public void GenerateFearFiresEvent()
    {
        int captured = 0;
        GameEvents.FearGenerated += amount => captured = amount;

        var state  = new EncounterState { Territories = new List<Territory>() };
        var target = new TargetInfo { TerritoryId = "" };
        var effect = new GenerateFearEffect(new EffectData { Type = EffectType.GenerateFear, Value = 3 });

        effect.Resolve(state, target);

        Assert.Equal(3, captured);
    }
}
