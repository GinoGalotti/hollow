namespace HollowWardens.Core.Encounter;

using HollowWardens.Core;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;

public class SpawnManager
{
    private readonly List<SpawnWave> _waves;
    private readonly GameRandom _rng;

    public SpawnManager(IEnumerable<SpawnWave> waves, GameRandom? rng = null)
    {
        _waves = waves.ToList();
        _rng = rng ?? GameRandom.NewRandom();
    }

    /// <summary>
    /// Preview the arrival location(s) for the wave arriving on <paramref name="arrivingTide"/>.
    /// Fires <see cref="GameEvents.WaveLocationsRevealed"/>. Returns null if no wave is scheduled.
    /// </summary>
    public SpawnWave? PreviewWave(int arrivingTide)
    {
        var wave = _waves.FirstOrDefault(w => w.TurnNumber == arrivingTide);
        if (wave is not null)
            GameEvents.WaveLocationsRevealed?.Invoke(wave);
        return wave;
    }

    /// <summary>
    /// Selects a composition option from the wave using weighted random and fires
    /// <see cref="GameEvents.WaveCompositionRevealed"/>. Returns null if the wave has no options.
    /// </summary>
    public SpawnWaveOption? RevealComposition(SpawnWave wave)
    {
        var selected = SelectWeighted(wave.Options);
        if (selected is not null)
            GameEvents.WaveCompositionRevealed?.Invoke(wave);
        return selected;
    }

    private SpawnWaveOption? SelectWeighted(List<SpawnWaveOption> options)
    {
        if (options.Count == 0) return null;
        int total = options.Sum(o => o.Weight);
        if (total == 0) return options[0];
        int roll = _rng.Next(total);
        int cumulative = 0;
        foreach (var option in options)
        {
            cumulative += option.Weight;
            if (roll < cumulative) return option;
        }
        return options[^1];
    }
}
