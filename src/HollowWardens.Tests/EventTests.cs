namespace HollowWardens.Tests;

using HollowWardens.Core.Events;
using HollowWardens.Core.Run;
using Xunit;

public class EventTests
{
    private static readonly Random _rng = new(42);

    private static List<EventData> LoadAll()
    {
        return EventLoader.LoadAll();
    }

    private static RunState MakeRun() => new()
    {
        CurrentWeave = 15,
        MaxWeave = 20,
        UpgradeTokens = 2,
        DeckCardIds = new() { "card_a", "card_b", "card_c" },
        PermanentlyRemovedCardIds = new(),
        CorruptionCarryover = new() { ["A1"] = 0, ["M1"] = 0 },
        PermanentlyUnlockedPassives = new(),
        AppliedPassiveUpgradeIds = new(),
    };

    [Fact]
    public void EventLoader_LoadsAllEvents()
    {
        var events = LoadAll();
        Assert.Equal(12, events.Count);
    }

    [Fact]
    public void EventLoader_FiltersByTags()
    {
        var all = LoadAll();
        var stage1 = EventLoader.Filter(all, requiredTags: new() { "stage_1" });

        // stage_1 events: whispering_grove, root_gate, sheltered_grove, pale_advance, wandering_trader
        Assert.True(stage1.Count > 0);
        Assert.All(stage1, e => Assert.Contains("stage_1", e.Tags, StringComparer.OrdinalIgnoreCase));

        // stage_3 only events should NOT be in stage_1 filter
        var stage3Only = all.Where(e => !e.Tags.Contains("stage_1", StringComparer.OrdinalIgnoreCase));
        foreach (var e in stage3Only)
            Assert.DoesNotContain(e, stage1);
    }

    [Fact]
    public void EventLoader_FiltersByWarden()
    {
        var all = LoadAll();
        var rootEvents = EventLoader.Filter(all, wardenId: "root");

        // Ember-specific events should be excluded
        Assert.All(rootEvents, e =>
            Assert.True(e.WardenFilter == null || e.WardenFilter.Equals("root", StringComparison.OrdinalIgnoreCase)));

        // All ember-specific events (ash_crucible, phoenix_rebirth) must not appear
        var emberOnly = all.Where(e => e.WardenFilter?.Equals("ember", StringComparison.OrdinalIgnoreCase) == true);
        foreach (var e in emberOnly)
            Assert.DoesNotContain(e, rootEvents);
    }

    [Fact]
    public void EventRunner_AppliesChoiceEffects()
    {
        var all = LoadAll();
        var grove = all.First(e => e.Id == "whispering_grove");
        var run = MakeRun();

        // Option B: cleanse carryover + dissolve 1 card
        run.CorruptionCarryover["A1"] = 5;
        int deckBefore = run.DeckCardIds.Count;

        EventRunner.ResolveOption(run, grove, optionIndex: 1, _rng);

        Assert.Empty(run.CorruptionCarryover);
        Assert.Equal(deckBefore - 1, run.DeckCardIds.Count);
    }

    [Fact]
    public void EventRunner_AppliesAllEffectsInOrder()
    {
        var all = LoadAll();
        var spring = all.First(e => e.Id == "ancient_spring");
        var run = MakeRun();
        int tokensBefore = run.UpgradeTokens;

        // Option C: add 1 token
        EventRunner.ResolveOption(run, spring, optionIndex: 2, _rng);

        Assert.Equal(tokensBefore + 1, run.UpgradeTokens);
    }

    [Fact]
    public void SacrificeEvent_MeetsThreshold_GrantsReward()
    {
        var all = LoadAll();
        var gate = all.First(e => e.Id == "root_gate");
        var run = MakeRun();

        // elementCount=6 meets threshold of 6 → passive should be unlocked
        bool resolved = EventRunner.ResolveOption(run, gate, optionIndex: 0, _rng, elementCount: 6);

        Assert.True(resolved);
        Assert.Contains("rest_growth", run.PermanentlyUnlockedPassives);
    }

    [Fact]
    public void SacrificeEvent_MissesThreshold_NoReward()
    {
        var all = LoadAll();
        var gate = all.First(e => e.Id == "root_gate");
        var run = MakeRun();

        // elementCount=3 does NOT meet threshold of 6 → no passive unlocked
        bool resolved = EventRunner.ResolveOption(run, gate, optionIndex: 0, _rng, elementCount: 3);

        Assert.False(resolved);
        Assert.DoesNotContain("rest_growth", run.PermanentlyUnlockedPassives);
    }

    [Fact]
    public void CorruptionEvent_SpendToken_ReducesDamage()
    {
        var all = LoadAll();
        var advance = all.First(e => e.Id == "pale_advance");
        var run = MakeRun();
        run.UpgradeTokens = 2;
        run.CorruptionCarryover["A1"] = 0;
        run.CorruptionCarryover["M1"] = 0;

        // Option A: 2 territories gain corruption (no token spent)
        var runA = run.Clone();
        EventRunner.ResolveOption(runA, advance, optionIndex: 0, _rng);

        // Option B: spend 1 token, only 1 territory gains corruption
        var runB = run.Clone();
        EventRunner.ResolveOption(runB, advance, optionIndex: 1, _rng);

        int totalA = runA.CorruptionCarryover.Values.Sum();
        int totalB = runB.CorruptionCarryover.Values.Sum();

        Assert.Equal(2, totalA);
        Assert.Equal(1, totalB);
        Assert.Equal(1, runB.UpgradeTokens); // token spent
    }
}
