using System;
using System.Threading.Tasks;
using Fdw.Services.Etl.Execution;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for the scheduler's list of the runs in flight and the runs waiting (FDW-3097): the times, the order, and the reason each waiting run
/// cannot start now, which the scheduler's own rules give.
/// </summary>
public sealed class PipelineRunQueueSnapshotTests
{
    private sealed class SteppedTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 29, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Step(TimeSpan by) => _now += by;
    }

    private static PipelineRunClaim Claim(PipelineRunIdentity run, string[] tables, int server = 8, int tenant = 8, PipelineRunCap? cap = null) =>
        PipelineRunClaim.Writing(run, tables, new PipelineRunLimitValues(server, tenant), cap);

    private static PipelineRunIdentity Run(string pipeline, Guid? tenantId = null) => new(Guid.NewGuid(), Guid.NewGuid(), pipeline, tenantId);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunInFlightHasItsTimesAndNoReasonAndAWaitingRunHasItsReasonAndNoStart()
    {
        var time = new SteppedTime();
        var scheduler = new PipelineRunScheduler(time);
        var first = Run("Orders");
        await scheduler.Enter(Claim(first, ["store//Sales"]), TestContext.Current.CancellationToken);
        time.Step(TimeSpan.FromMinutes(5));
        var second = Run("Refunds");
        scheduler.Queue(Claim(second, ["STORE//sales"]));

        var snapshot = scheduler.Snapshot();

        var running = snapshot.Running.ShouldHaveSingleItem();
        running.Run.ShouldBe(first);
        running.QueuedAt.ShouldBe(new DateTimeOffset(2026, 10, 29, 9, 0, 0, TimeSpan.Zero));
        running.StartedAt.ShouldBe(running.QueuedAt);
        running.Waiting.ShouldBeNull();
        var waiting = snapshot.Waiting.ShouldHaveSingleItem();
        waiting.Run.ShouldBe(second);
        waiting.QueuedAt.ShouldBe(new DateTimeOffset(2026, 10, 29, 9, 5, 0, TimeSpan.Zero));
        waiting.StartedAt.ShouldBeNull();
        var block = waiting.Waiting.ShouldNotBeNull();
        block.Reason.Name.ShouldBe("SharedTable");
        block.BlockedByExecutionId.ShouldBe(first.ExecutionId);
        block.Table.ShouldBe("store//Sales");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task WaitingRunsAreListedInTheOrderTheyArrived()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        await scheduler.Enter(Claim(Run("A"), ["t"]), TestContext.Current.CancellationToken);
        var b = Run("B");
        var c = Run("C");
        scheduler.Queue(Claim(b, ["t"]));
        scheduler.Queue(Claim(c, ["t"]));

        var waiting = scheduler.Snapshot().Waiting;

        waiting.Count.ShouldBe(2);
        waiting[0].Run.ShouldBe(b);
        waiting[1].Run.ShouldBe(c);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheServerLimitIsTheReasonAndNamesTheLimit()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        await scheduler.Enter(Claim(Run("A"), ["a"], server: 1), TestContext.Current.CancellationToken);
        scheduler.Queue(Claim(Run("B"), ["b"], server: 1));

        var block = scheduler.Snapshot().Waiting.ShouldHaveSingleItem().Waiting.ShouldNotBeNull();

        block.Reason.Name.ShouldBe("ServerLimit");
        block.Limit.ShouldBe(1);
        block.BlockedByExecutionId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheTenantLimitIsTheReasonForARunOfTheSameTenantOnly()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var tenant = Guid.NewGuid();
        await scheduler.Enter(Claim(Run("A", tenant), ["a"], tenant: 1), TestContext.Current.CancellationToken);
        scheduler.Queue(Claim(Run("B", tenant), ["b"], tenant: 1));
        scheduler.Queue(Claim(Run("C", Guid.NewGuid()), ["c"], tenant: 1));

        var snapshot = scheduler.Snapshot();

        var block = snapshot.Waiting.ShouldHaveSingleItem().Waiting.ShouldNotBeNull();
        block.Reason.Name.ShouldBe("TenantLimit");
        block.Limit.ShouldBe(1);
        snapshot.Running.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ThePipelineLimitIsTheReasonForAFurtherRunOfThatPipeline()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var cap = new PipelineRunCap("pipeline/orders", 1, false);
        await scheduler.Enter(Claim(Run("Orders"), [], cap: cap), TestContext.Current.CancellationToken);
        scheduler.Queue(Claim(Run("Orders"), [], cap: cap));

        var block = scheduler.Snapshot().Waiting.ShouldHaveSingleItem().Waiting.ShouldNotBeNull();

        block.Reason.Name.ShouldBe("PipelineLimit");
        block.Limit.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatMustRunAloneBlocksTheOthersAndWaitsForTheHost()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var alone = Run("Rebuild");
        await scheduler.Enter(PipelineRunClaim.RunningAlone(alone, new PipelineRunLimitValues(8, 8), null), TestContext.Current.CancellationToken);
        scheduler.Queue(Claim(Run("Orders"), ["t"]));
        var needsHost = Run("Reindex");
        scheduler.Queue(PipelineRunClaim.RunningAlone(needsHost, new PipelineRunLimitValues(8, 8), null));

        var waiting = scheduler.Snapshot().Waiting;

        waiting[0].Waiting.ShouldNotBeNull().Reason.Name.ShouldBe("RunningAlone");
        waiting[0].Waiting.ShouldNotBeNull().BlockedByExecutionId.ShouldBe(alone.ExecutionId);
        waiting[1].Run.ShouldBe(needsHost);
        waiting[1].Waiting.ShouldNotBeNull().Reason.Name.ShouldBe("NeedsHostAlone");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatOnlyWaitsForAnEarlierWaitingRunIsBehindIt()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        await scheduler.Enter(Claim(Run("A"), ["x"]), TestContext.Current.CancellationToken);
        var b = Run("B");
        scheduler.Queue(Claim(b, ["x", "y"]));
        scheduler.Queue(Claim(Run("C"), ["y"]));

        var waiting = scheduler.Snapshot().Waiting;

        waiting[0].Waiting.ShouldNotBeNull().Reason.Name.ShouldBe("SharedTable");
        var behind = waiting[1].Waiting.ShouldNotBeNull();
        behind.Reason.Name.ShouldBe("BehindEarlierRun");
        behind.BlockedByExecutionId.ShouldBe(b.ExecutionId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASnapshotIsACopyAndTheListEmptiesWhenTheRunsEnd()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var first = await scheduler.Enter(Claim(Run("A"), ["t"]), TestContext.Current.CancellationToken);
        var second = scheduler.Queue(Claim(Run("B"), ["t"]));
        var before = scheduler.Snapshot();

        scheduler.Release(first);
        await scheduler.Wait(second, TestContext.Current.CancellationToken);
        scheduler.Release(second);

        before.Running.Count.ShouldBe(1);
        before.Waiting.Count.ShouldBe(1);
        var after = scheduler.Snapshot();
        after.Running.ShouldBeEmpty();
        after.Waiting.ShouldBeEmpty();
    }
}
