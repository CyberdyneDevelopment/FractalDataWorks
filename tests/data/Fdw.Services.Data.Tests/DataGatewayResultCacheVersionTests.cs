using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A write that lands while a read runs keeps that read's result out of the cache, and an entry put over another
/// under the same key is still dropped by the next invalidation (FDW-4782).
/// </summary>
public sealed class DataGatewayResultCacheVersionTests
{
    private const string Key = "partition|_cfg|conn.Connection:f=0000abcd:System.String";
    private const string Tag = "conn.Connection";
    private const string OtherTag = "conn.Secret";

    private static DataGatewayResultCache Cache()
        => new(new MemoryCache(new MemoryCacheOptions()), NullLoggerFactory.Instance);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AReadDuringWhichItsTagIsInvalidatedIsNotKept()
    {
        var cache = Cache();
        var readFrom = cache.Now();

        cache.InvalidateByTag(Tag);

        cache.Set(Key, GenericResult<string>.Success("read before the write"), [Tag], TimeSpan.FromMinutes(5), readFrom).ShouldBeFalse();
        cache.TryGet<string>(Key, out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AnInvalidationWithNothingCachedStillStopsAReadInFlightFromKeepingItsResult()
    {
        var cache = Cache();
        var readFrom = cache.Now();

        // Nothing is cached under the tag: the no-keys path still moves the tag's clock.
        cache.InvalidateByTag(Tag);

        cache.ChangedSince(Tag, readFrom).ShouldBeTrue();
        cache.Set(Key, GenericResult<string>.Success("stale"), [Tag], TimeSpan.FromMinutes(5), readFrom).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AnInvalidationOfAnotherTagDoesNotStopTheResultBeingKept()
    {
        var cache = Cache();
        var readFrom = cache.Now();

        cache.InvalidateByTag(OtherTag);

        cache.Set(Key, GenericResult<string>.Success("current"), [Tag], TimeSpan.FromMinutes(5), readFrom).ShouldBeTrue();
        cache.TryGet<string>(Key, out var kept).ShouldBeTrue();
        kept!.Value.ShouldBe("current");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AReadThatBeganAfterTheInvalidationIsKept()
    {
        var cache = Cache();
        cache.InvalidateByTag(Tag);
        var readFrom = cache.Now();

        cache.Set(Key, GenericResult<string>.Success("current"), [Tag], TimeSpan.FromMinutes(5), readFrom).ShouldBeTrue();
        cache.TryGet<string>(Key, out _).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EmptyingTheWholeCacheStopsEveryReadInFlightFromKeepingItsResult()
    {
        var cache = Cache();
        var readFrom = cache.Now();

        cache.InvalidateAll();

        cache.Set(Key, GenericResult<string>.Success("stale"), [Tag], TimeSpan.FromMinutes(5), readFrom).ShouldBeFalse();
        cache.TryGet<string>(Key, out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AnEntryPutOverAnotherUnderTheSameKeyIsStillDroppedByTheNextInvalidation()
    {
        var cache = Cache();
        cache.Set(Key, GenericResult<string>.Success("first"), [Tag], TimeSpan.FromMinutes(5));

        // A forced refresh puts a new entry over the old one; the old entry's eviction callback runs for the replace.
        cache.Set(Key, GenericResult<string>.Success("second"), [Tag], TimeSpan.FromMinutes(5));

        // MemoryCache runs eviction callbacks on the thread pool: give the replaced entry's callback time to run.
        await Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);

        cache.InvalidateByTag(Tag);

        cache.TryGet<string>(Key, out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ABuildFromSeveralReadsIsKeptWhenATagChangedOnlyBeforeItsOwnReadBegan()
    {
        var cache = Cache();
        var firstRead = cache.Now();
        cache.InvalidateByTag(OtherTag);
        var secondRead = cache.Now();

        var readFrom = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            [Tag] = firstRead,
            [OtherTag] = secondRead,
        };

        cache.Set(Key, GenericResult<string>.Success("built"), readFrom, TimeSpan.FromMinutes(5)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ABuildFromSeveralReadsIsNotKeptWhenATagChangedAfterItsOwnReadBegan()
    {
        var cache = Cache();
        var firstRead = cache.Now();
        var secondRead = cache.Now();
        cache.InvalidateByTag(OtherTag);

        var readFrom = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            [Tag] = firstRead,
            [OtherTag] = secondRead,
        };

        cache.Set(Key, GenericResult<string>.Success("built"), readFrom, TimeSpan.FromMinutes(5)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ConcurrentSetsAndInvalidationsNeverLeaveAnEntryReadBeforeTheLastInvalidation()
    {
        // Fixed counts, not a timer: a timed loop may get no time on a busy machine, and then proves nothing. Both loops wait on one gate so
        // they run at the same time.
        const int Invalidations = 20_000;
        const int Sets = 20_000;
        var cache = Cache();
        var go = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var writer = Task.Run(async () =>
        {
            await go.Task.ConfigureAwait(false);
            for (var i = 0; i < Invalidations; i++)
                cache.InvalidateByTag(Tag);
        }, TestContext.Current.CancellationToken);

        var reader = Task.Run(async () =>
        {
            await go.Task.ConfigureAwait(false);
            for (var i = 0; i < Sets; i++)
            {
                var readFrom = cache.Now();
                cache.Set(Key, GenericResult<long>.Success(readFrom), [Tag], TimeSpan.FromMinutes(5), readFrom);
            }
        }, TestContext.Current.CancellationToken);

        go.SetResult();
        await Task.WhenAll(writer, reader);

        // Only an invalidation moves the clock, by one each.
        cache.Now().ShouldBe(Invalidations);

        // An entry still kept was read after the last invalidation; one read before it was refused or removed, whatever the interleaving.
        if (cache.TryGet<long>(Key, out var kept))
            kept!.Value.ShouldBe(Invalidations);

        // And the next invalidation leaves nothing.
        cache.InvalidateByTag(Tag);
        cache.TryGet<long>(Key, out _).ShouldBeFalse();
    }
}
