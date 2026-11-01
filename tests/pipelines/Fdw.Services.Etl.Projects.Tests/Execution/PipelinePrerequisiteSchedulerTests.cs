using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Tests.Execution;

/// <summary>
/// Tests for <see cref="PipelinePrerequisiteScheduler"/>: a pipeline starts only after the pipelines it needs have
/// succeeded, a failed prerequisite stops everything that needs it, and an unrunnable graph fails loudly (FDW-2072).
/// </summary>
public sealed class PipelinePrerequisiteSchedulerTests
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-00000000000c");
    private static readonly Guid D = Guid.Parse("00000000-0000-0000-0000-00000000000d");
    private static readonly Guid E = Guid.Parse("00000000-0000-0000-0000-00000000000e");

    private static OrchestrationNodePipelinePrerequisiteConfiguration Needs(Guid pipeline, Guid prerequisite) => new()
    {
        PipelineId = pipeline,
        PrerequisitePipelineId = prerequisite
    };

    private static Task<Fdw.Results.IGenericResult<bool>> Schedule(
        Guid[] pipelines,
        OrchestrationNodePipelinePrerequisiteConfiguration[] prerequisites,
        int limit,
        Func<Guid, CancellationToken, Task<bool>> run) =>
        PipelinePrerequisiteScheduler.Execute(
            NullLogger.Instance, "Step", pipelines, prerequisites, limit, run, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task DependentStartsOnlyAfterEveryPrerequisiteFinished()
    {
        // Arrange: C needs A and B; all three could run at once.
        var events = new List<string>();
        async Task<bool> Run(Guid id, CancellationToken ct)
        {
            lock (events) { events.Add($"start {Name(id)}"); }
            await Task.Delay(id == C ? 1 : 40, ct);
            lock (events) { events.Add($"end {Name(id)}"); }
            return true;
        }

        // Act
        var result = await Schedule([A, B, C], [Needs(C, A), Needs(C, B)], 3, Run);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
        events.IndexOf("start C").ShouldBeGreaterThan(events.IndexOf("end A"));
        events.IndexOf("start C").ShouldBeGreaterThan(events.IndexOf("end B"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task FailedPrerequisiteStopsEveryPipelineThatNeedsIt()
    {
        // Arrange: A fails; C needs A; D needs C; E needs nothing.
        var started = new List<Guid>();
        Task<bool> Run(Guid id, CancellationToken ct)
        {
            lock (started) { started.Add(id); }
            return Task.FromResult(id != A);
        }

        // Act
        var result = await Schedule([A, C, D, E], [Needs(C, A), Needs(D, C)], 4, Run);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
        started.ShouldContain(A);
        started.ShouldContain(E);
        started.ShouldNotContain(C);
        started.ShouldNotContain(D);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task StepSucceedsWhenEveryPipelineSucceeds()
    {
        // Act
        var result = await Schedule([A, B, C], [Needs(B, A), Needs(C, B)], 2, (_, _) => Task.FromResult(true));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task PipelinesWithNoPrerequisiteBetweenThemRunTogether()
    {
        // Arrange: each run waits until all three have started, so this passes only if they overlap.
        var started = 0;
        var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Run(Guid id, CancellationToken ct)
        {
            if (Interlocked.Increment(ref started) == 3)
            {
                allStarted.TrySetResult();
            }

            await allStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
            return true;
        }

        // Act
        var result = await Schedule([A, B, C], [], 3, Run);

        // Assert
        result.Value.ShouldBeTrue();
        started.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task NeverMoreThanTheLimitRunAtOnce()
    {
        // Arrange
        var running = 0;
        var peak = 0;
        var gate = new object();
        async Task<bool> Run(Guid id, CancellationToken ct)
        {
            var now = Interlocked.Increment(ref running);
            lock (gate) { peak = Math.Max(peak, now); }
            await Task.Delay(40, ct);
            Interlocked.Decrement(ref running);
            return true;
        }

        // Act
        var result = await Schedule([A, B, C, D], [], 2, Run);

        // Assert
        result.Value.ShouldBeTrue();
        peak.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task PrerequisiteCycleFailsTheStepAndStartsNothing()
    {
        // Arrange
        var started = 0;
        Task<bool> Run(Guid id, CancellationToken ct)
        {
            Interlocked.Increment(ref started);
            return Task.FromResult(true);
        }

        // Act
        var result = await Schedule([A, B], [Needs(A, B), Needs(B, A)], 2, Run);

        // Assert
        result.IsFailure.ShouldBeTrue();
        started.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task PrerequisiteOutsideTheStepFailsTheStepAndStartsNothing()
    {
        // Arrange
        var started = 0;
        Task<bool> Run(Guid id, CancellationToken ct)
        {
            Interlocked.Increment(ref started);
            return Task.FromResult(true);
        }

        // Act
        var result = await Schedule([A, B], [Needs(B, C)], 2, Run);

        // Assert
        result.IsFailure.ShouldBeTrue();
        started.ShouldBe(0);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task LimitBelowOneFailsTheStep(int limit)
    {
        // Act
        var result = await Schedule([A], [], limit, (_, _) => Task.FromResult(true));

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ARunThatThrowsDoesNotLeaveTheSchedulerWaiting()
    {
        // Act / Assert: the exception surfaces instead of the scheduler waiting for a result that never comes.
        await Should.ThrowAsync<InvalidOperationException>(() =>
            Schedule([A, B], [Needs(B, A)], 2, (_, _) => throw new InvalidOperationException("boom")));
    }

    private static string Name(Guid id) =>
        id == A ? "A" : id == B ? "B" : id == C ? "C" : id == D ? "D" : "E";
}
