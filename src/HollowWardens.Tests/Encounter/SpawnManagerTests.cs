namespace HollowWardens.Tests.Encounter;

using HollowWardens.Core;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using Xunit;

[Collection("Sequential")]
public class SpawnManagerTests : IDisposable
{
    public void Dispose()
    {
        GameEvents.WaveLocationsRevealed   = null;
        GameEvents.WaveCompositionRevealed = null;
    }

    private static SpawnWave MakeWave(int tide, params (int weight, UnitType unit)[] options)
    {
        var wave = new SpawnWave
        {
            TurnNumber    = tide,
            ArrivalPoints = new List<string> { "A1" }
        };
        foreach (var (w, u) in options)
            wave.Options.Add(new SpawnWaveOption
            {
                Weight = w,
                Units  = new Dictionary<string, List<UnitType>> { ["A1"] = new() { u } }
            });
        return wave;
    }

    [Fact]
    public void WeightedSelection_FavorsHigherWeightOption()
    {
        // 10:1 odds in favour of Marcher
        var wave    = MakeWave(1, (10, UnitType.Marcher), (1, UnitType.Ironclad));
        var manager = new SpawnManager(new[] { wave }, GameRandom.FromSeed(0));

        int marcherCount = 0;
        for (int i = 0; i < 100; i++)
        {
            var option = manager.RevealComposition(wave);
            if (option!.Units["A1"][0] == UnitType.Marcher) marcherCount++;
        }

        // With 10/11 probability, Marcher should appear well above 70% of trials
        Assert.True(marcherCount > 70, $"Marcher appeared {marcherCount}/100 times");
    }

    [Fact]
    public void PreviewWave_ReturnsWave_AndFiresLocationEvent()
    {
        SpawnWave? received = null;
        GameEvents.WaveLocationsRevealed = w => received = w;

        var wave    = MakeWave(2, (1, UnitType.Marcher));
        var manager = new SpawnManager(new[] { wave });

        var result = manager.PreviewWave(2);

        Assert.NotNull(result);
        Assert.Same(wave, received);
    }

    [Fact]
    public void PreviewWave_ReturnsNull_WhenNoWaveScheduled()
    {
        var wave    = MakeWave(3, (1, UnitType.Marcher));
        var manager = new SpawnManager(new[] { wave });

        Assert.Null(manager.PreviewWave(1));
    }

    [Fact]
    public void RevealComposition_FiresCompositionEvent()
    {
        SpawnWave? received = null;
        GameEvents.WaveCompositionRevealed = w => received = w;

        var wave    = MakeWave(1, (1, UnitType.Marcher));
        var manager = new SpawnManager(new[] { wave });

        manager.RevealComposition(wave);

        Assert.Same(wave, received);
    }

    [Fact]
    public void RevealComposition_ReturnsNull_WhenNoOptions()
    {
        var wave    = new SpawnWave { TurnNumber = 1 };  // no options
        var manager = new SpawnManager(new[] { wave });

        Assert.Null(manager.RevealComposition(wave));
    }
}
