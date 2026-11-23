using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="PipelineAtOnceRuns"/> (FDW-6141, slice 2): the runs an AtOnce runner starts are held by one host-wide object, so the host's stop reaches them and
/// waits for them to end before the host is gone.
/// </summary>
public sealed class PipelineAtOnceRunsTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    private static PipelineAtOnceRuns Sut() => new(NullLogger<PipelineAtOnceRuns>.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunStartsWithoutTheCallerWaitingForIt()
    {
        var runs = Sut();
        var end = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        runs.Run(() => end.Task);

        (await runs.Drain(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken)).ShouldBeFalse("the run has not ended");
        end.SetResult();
        (await runs.Drain(Patience, TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheHostsStopCancelsTheRunsTokenAndWaitsForTheRunsToEnd()
    {
        var runs = Sut();
        var sawStop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var end = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runs.Run(async () =>
        {
            await Task.Delay(Timeout.Infinite, runs.Stopping).ContinueWith(_ => sawStop.TrySetResult(), TaskScheduler.Default);
            await end.Task;
        });
        runs.Stopping.IsCancellationRequested.ShouldBeFalse();

        var stopping = runs.StopAsync(TestContext.Current.CancellationToken);

        await sawStop.Task.WaitAsync(Patience, TestContext.Current.CancellationToken);
        runs.Stopping.IsCancellationRequested.ShouldBeTrue();
        stopping.IsCompleted.ShouldBeFalse("the host's stop waits for the run to end");
        end.SetResult();
        await stopping.WaitAsync(Patience, TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunThatThrowsEndsWithoutTakingTheHostDown()
    {
        var runs = Sut();

        runs.Run(() => throw new InvalidOperationException("the run code failed"));

        (await runs.Drain(Patience, TestContext.Current.CancellationToken)).ShouldBeTrue();
        await runs.StopAsync(TestContext.Current.CancellationToken);
    }
}
