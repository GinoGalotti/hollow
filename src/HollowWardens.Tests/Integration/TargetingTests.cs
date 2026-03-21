namespace HollowWardens.Tests.Integration;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;
using Xunit;

/// <summary>
/// Pure logic tests for TargetValidator: NeedsTarget and GetValidTargets.
/// No Godot dependency — all assertions run against Core types only.
/// </summary>
public class TargetingTests : IDisposable
{
    public void Dispose() => GameEvents.ClearAll();

    // ── NeedsTarget ───────────────────────────────────────────────────────────

    [Fact]
    public void NeedsTarget_PlacePresence_WithRange1_ReturnsTrue()
    {
        var effect = new EffectData { Type = EffectType.PlacePresence, Range = 1 };
        Assert.True(TargetValidator.NeedsTarget(effect));
    }

    [Fact]
    public void NeedsTarget_GenerateFear_WithRange1_ReturnsFalse()
    {
        // GenerateFear is globally self-resolving even when Range > 0
        var effect = new EffectData { Type = EffectType.GenerateFear, Range = 1, Value = 3 };
        Assert.False(TargetValidator.NeedsTarget(effect));
    }

    [Fact]
    public void NeedsTarget_RestoreWeave_WithRange1_ReturnsFalse()
    {
        var effect = new EffectData { Type = EffectType.RestoreWeave, Range = 1, Value = 2 };
        Assert.False(TargetValidator.NeedsTarget(effect));
    }

    [Fact]
    public void NeedsTarget_AwakeDormant_WithRange1_ReturnsFalse()
    {
        var effect = new EffectData { Type = EffectType.AwakeDormant, Range = 1 };
        Assert.False(TargetValidator.NeedsTarget(effect));
    }

    [Fact]
    public void NeedsTarget_PlacePresence_WithRange0_ReturnsFalse()
    {
        // Range 0 means auto-resolve at the warden's presence — no targeting needed
        var effect = new EffectData { Type = EffectType.PlacePresence, Range = 0 };
        Assert.False(TargetValidator.NeedsTarget(effect));
    }

    // ── GetValidTargets ───────────────────────────────────────────────────────

    [Fact]
    public void GetValidTargets_PresenceOnI1_Range1_ReturnsMAndI()
    {
        // Adjacency from I1: M1 (dist 1), M2 (dist 1), I1 (dist 0) → 3 territories
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, _, _, _, _) = IntegrationHelpers.Build(
            IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        state.GetTerritory("I1")!.PresenceCount = 1;

        var targets = TargetValidator.GetValidTargets(state, range: 1);

        Assert.Equal(3, targets.Count);
        Assert.Contains("I1", targets);
        Assert.Contains("M1", targets);
        Assert.Contains("M2", targets);
    }

    [Fact]
    public void GetValidTargets_NoPresence_ReturnsEmpty()
    {
        var config = IntegrationHelpers.MakeConfig(tideCount: 1);
        var (state, _, _, _, _) = IntegrationHelpers.Build(
            IntegrationHelpers.MakeCards(5), new RootAbility(), config);

        // No presence set — all territories start at PresenceCount = 0
        var targets = TargetValidator.GetValidTargets(state, range: 2);

        Assert.Empty(targets);
    }
}
