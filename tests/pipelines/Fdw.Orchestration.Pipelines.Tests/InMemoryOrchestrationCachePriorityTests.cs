using Fdw.Orchestration.Abstractions.Caching;
using Fdw.Orchestration.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace Fdw.Orchestration.Pipelines.Tests;

/// <summary>
/// FDW-4402: each cache priority states the memory cache's own priority it means, and an entry that names the NotFound placeholder
/// (what CachePriorities.ByName answers for a word it does not hold) is refused with an ArgumentException, because
/// IOrchestrationCache.Set returns a plain Task and has no result to carry a coded failure.
/// </summary>
public sealed class InMemoryOrchestrationCachePriorityTests
{
    [Fact]
    public void EveryPriorityStatesTheMemoryPriorityItMeans()
    {
        CachePriorities.Low.MemoryPriority.ShouldBe(CacheItemPriority.Low);
        CachePriorities.Normal.MemoryPriority.ShouldBe(CacheItemPriority.Normal);
        CachePriorities.High.MemoryPriority.ShouldBe(CacheItemPriority.High);
        CachePriorities.NeverRemove.MemoryPriority.ShouldBe(CacheItemPriority.NeverRemove);
    }

    [Fact]
    public void AnEntryThatNamesTheNotFoundPriorityIsRefused()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var sut = new InMemoryOrchestrationCache(memory);

        Should.Throw<ArgumentException>(() => sut.Set("key", "value", new CacheEntryOptions { Priority = CachePriorities.ByName("NoSuchPriority") }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AnEntryThatNamesAKnownPriorityIsStored()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var sut = new InMemoryOrchestrationCache(memory);

        await sut.Set("key", "value", new CacheEntryOptions { Priority = CachePriorities.High }, TestContext.Current.CancellationToken);

        (await sut.Get<string>("key", TestContext.Current.CancellationToken)).ShouldBe("value");
    }
}
