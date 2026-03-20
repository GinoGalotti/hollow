namespace HollowWardens.Tests;

using Xunit;

/// <summary>
/// Tests in this collection do not run in parallel with any other test collection.
/// Used for tests that subscribe to static GameEvents delegates, which can be cleared
/// by other test classes' Dispose() methods running on parallel threads.
/// </summary>
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollection { }
