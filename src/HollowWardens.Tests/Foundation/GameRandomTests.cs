namespace HollowWardens.Tests.Foundation;

using HollowWardens.Core;
using Xunit;

public class GameRandomTests
{
    [Fact]
    public void SameSeedProducesSameSequence()
    {
        var r1 = GameRandom.FromSeed(12345);
        var r2 = GameRandom.FromSeed(12345);

        var seq1 = Enumerable.Range(0, 10).Select(_ => r1.Next(100)).ToList();
        var seq2 = Enumerable.Range(0, 10).Select(_ => r2.Next(100)).ToList();

        Assert.Equal(seq1, seq2);
    }

    [Fact]
    public void DifferentSeedProducesDifferentSequence()
    {
        var r1 = GameRandom.FromSeed(1);
        var r2 = GameRandom.FromSeed(2);

        var seq1 = Enumerable.Range(0, 10).Select(_ => r1.Next(100)).ToList();
        var seq2 = Enumerable.Range(0, 10).Select(_ => r2.Next(100)).ToList();

        Assert.False(seq1.SequenceEqual(seq2), "Different seeds should produce different sequences");
    }

    [Fact]
    public void ShuffleSameSeedProducesSameOrder()
    {
        var list1 = Enumerable.Range(1, 10).ToList();
        var list2 = Enumerable.Range(1, 10).ToList();

        GameRandom.FromSeed(99).Shuffle(list1);
        GameRandom.FromSeed(99).Shuffle(list2);

        Assert.Equal(list1, list2);
    }

    [Fact]
    public void ShuffleDifferentSeedProducesDifferentOrder()
    {
        var list1 = Enumerable.Range(1, 10).ToList();
        var list2 = Enumerable.Range(1, 10).ToList();

        GameRandom.FromSeed(1).Shuffle(list1);
        GameRandom.FromSeed(2).Shuffle(list2);

        Assert.False(list1.SequenceEqual(list2), "Different seeds should produce different shuffle order");
    }
}
