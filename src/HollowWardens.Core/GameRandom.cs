namespace HollowWardens.Core;

/// <summary>
/// Seeded random wrapper for deterministic gameplay and replay support.
/// All randomness in the game flows through this class so a fixed seed
/// + fixed player action sequence always produces the same outcome.
/// </summary>
public class GameRandom
{
    private readonly Random _rng;

    public int Seed { get; }

    public GameRandom(int seed)
    {
        Seed = seed;
        _rng = new Random(seed);
    }

    public int Next(int max) => _rng.Next(max);
    public int Next(int min, int max) => _rng.Next(min, max);

    public void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static GameRandom FromSeed(int seed) => new(seed);
    public static GameRandom NewRandom() => new(Environment.TickCount);
}
