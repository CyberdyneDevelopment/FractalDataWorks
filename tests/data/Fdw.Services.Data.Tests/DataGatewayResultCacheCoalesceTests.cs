using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Concurrent reads of one cache key are one read (FDW-983): the call that leads reads, the others wait for it.
/// What is asserted is how many times the read ran and what every caller was handed.
/// </summary>
public sealed class DataGatewayResultCacheCoalesceTests
{
    private const string Key = "partition|_cfg|conn.Connection:f=0000abcd:System.String";

    private static DataGatewayResultCache Cache()
        => new(new MemoryCache(new MemoryCacheOptions()), NullLoggerFactory.Instance);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ConcurrentCallsForOneKeyRunTheReadOnceAndAllGetItsResult()
    {
        var cache = Cache();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runs = 0;

        async Task<IGenericResult<string>> Read()
        {
            Interlocked.Increment(ref runs);
            await release.Task;
            return GenericResult<string>.Success("one read");
        }

        var calls = Enumerable.Range(0, 10)
            .Select(_ => cache.Coalesce(Key, Read, TestContext.Current.CancellationToken))
            .ToArray();
        release.SetResult();
        var results = await Task.WhenAll(calls);

        runs.ShouldBe(1);
        results.ShouldAllBe(result => result.IsSuccess && result.Value == "one read");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SequentialCallsEachRunTheReadBecauseNothingIsKeptOnceItIsDone()
    {
        var cache = Cache();
        var runs = 0;

        Task<IGenericResult<string>> Read()
        {
            Interlocked.Increment(ref runs);
            return Task.FromResult(GenericResult<string>.Success("read"));
        }

        await cache.Coalesce(Key, Read, TestContext.Current.CancellationToken);
        await cache.Coalesce(Key, Read, TestContext.Current.CancellationToken);

        runs.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task DifferentKeysAreDifferentReads()
    {
        var cache = Cache();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runs = 0;

        async Task<IGenericResult<string>> Read()
        {
            Interlocked.Increment(ref runs);
            await release.Task;
            return GenericResult<string>.Success("read");
        }

        var first = cache.Coalesce(Key, Read, TestContext.Current.CancellationToken);
        var second = cache.Coalesce(Key + "-other-partition", Read, TestContext.Current.CancellationToken);
        release.SetResult();
        await Task.WhenAll(first, second);

        runs.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AFailureIsSharedWithTheCallersWaitingOnItButTheReadStillRanOnce()
    {
        var cache = Cache();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runs = 0;

        async Task<IGenericResult<string>> Read()
        {
            Interlocked.Increment(ref runs);
            await release.Task;
            return GenericResult<string>.Failure(new Fdw.Messages.GenericMessage("the database refused"));
        }

        var calls = Enumerable.Range(0, 4)
            .Select(_ => cache.Coalesce(Key, Read, TestContext.Current.CancellationToken))
            .ToArray();
        release.SetResult();
        var results = await Task.WhenAll(calls);

        runs.ShouldBe(1);
        results.ShouldAllBe(result => result.IsFailure);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ACancelledLeaderDoesNotFailAWaiterThatWasNotCancelled()
    {
        var cache = Cache();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var leaderToken = new CancellationTokenSource();

        async Task<IGenericResult<string>> LeaderRead()
        {
            await release.Task;
            leaderToken.Token.ThrowIfCancellationRequested();
            return GenericResult<string>.Success("never returned");
        }

        var leader = cache.Coalesce(Key, LeaderRead, leaderToken.Token);
        var waiter = cache.Coalesce(
            Key,
            () => Task.FromResult(GenericResult<string>.Success("the waiter's own read")),
            TestContext.Current.CancellationToken);

        leaderToken.Cancel();
        release.SetResult();

        await Should.ThrowAsync<OperationCanceledException>(leader);
        var result = await waiter;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("the waiter's own read");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AWaiterThatIsCancelledStopsWaitingWithoutCancellingTheLeadersRead()
    {
        var cache = Cache();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runs = 0;
        using var waiterToken = new CancellationTokenSource();

        async Task<IGenericResult<string>> Read()
        {
            Interlocked.Increment(ref runs);
            await release.Task;
            return GenericResult<string>.Success("the leader's read");
        }

        var leader = cache.Coalesce(Key, Read, TestContext.Current.CancellationToken);
        var waiter = cache.Coalesce(Key, Read, waiterToken.Token);

        waiterToken.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(waiter);
        leader.IsCompleted.ShouldBeFalse();

        release.SetResult();
        var result = await leader;

        runs.ShouldBe(1);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("the leader's read");
    }
}
