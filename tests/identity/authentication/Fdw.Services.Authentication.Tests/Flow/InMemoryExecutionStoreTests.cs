using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Execution;
using Fdw.Services.Authentication.Execution;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// The store is what stands between a stranger and a half-finished login, so these are attacks too.
/// </summary>
public sealed class InMemoryExecutionStoreTests
{
    private static bool IsGuid(string value) => Guid.TryParse(value, out _);

    private static ExecutionRecord Record(TimeSpan? lifetime = null) => new()
    {
        Id = Guid.NewGuid(),
        FlowName = "test-flow",
        Context = new AuthenticationContext(),
        CurrentStepIndex = 1,
        ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(5)),
    };

    [Fact]
    public async Task ConsumingTwiceFailsTheSecondTime()
    {
        var store = new InMemoryExecutionStore();
        var token = (await store.Suspend(Record(), TestContext.Current.CancellationToken)).Value!;

        var first = await store.TryConsume(token, TestContext.Current.CancellationToken);
        var second = await store.TryConsume(token, TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ConcurrentConsumersYieldExactlyOneWinner()
    {
        var store = new InMemoryExecutionStore();
        var token = (await store.Suspend(Record(), TestContext.Current.CancellationToken)).Value!;

        // a check-then-act implementation lets more than one of these through
        var attempts = await Task.WhenAll(Enumerable.Range(0, 64)
            .Select(_ => store.TryConsume(token, TestContext.Current.CancellationToken)));

        attempts.Count(a => a.IsSuccess).ShouldBe(1);
    }

    [Fact]
    public async Task ExpiredRecordFailsAndIsIndistinguishableFromAMissingOne()
    {
        var store = new InMemoryExecutionStore();
        var token = (await store.Suspend(Record(TimeSpan.FromMilliseconds(-1)), TestContext.Current.CancellationToken)).Value!;

        var expired = await store.TryConsume(token, TestContext.Current.CancellationToken);
        var neverExisted = await store.TryConsume("not-a-real-token", TestContext.Current.CancellationToken);

        expired.IsSuccess.ShouldBeFalse();
        neverExisted.IsSuccess.ShouldBeFalse();
        expired.CurrentMessage.ShouldBe(neverExisted.CurrentMessage);
    }

    [Fact]
    public async Task TokensAreUnpredictableAndDistinct()
    {
        var store = new InMemoryExecutionStore();

        var tokens = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(async _ => (await store.Suspend(Record(), TestContext.Current.CancellationToken)).Value!));

        tokens.Distinct(StringComparer.Ordinal).Count().ShouldBe(100);
        tokens.ShouldAllBe(t => t.Length >= 40);
        tokens.ShouldAllBe(t => !IsGuid(t));
    }
}
