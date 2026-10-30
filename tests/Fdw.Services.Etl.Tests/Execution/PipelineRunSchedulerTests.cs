using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Execution;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for the pipeline run scheduler (FDW-3085): runs that write different tables overlap, runs that write the same table take
/// turns, the server and tenant limits cap how many are in flight, a run that must run alone does, and a run that waits for the pipeline
/// it started hands its place back (FDW-1511). A pipeline's cap (FDW-3117) caps how many of its runs are in flight, and a pipeline that refuses
/// refuses a run past it.
/// </summary>
public sealed class PipelineRunSchedulerTests
{
    private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(150);

    private static PipelineRunIdentity Run(Guid? tenantId = null, string pipeline = "Pipeline") => new(Guid.NewGuid(), Guid.NewGuid(), pipeline, tenantId);

    private static PipelineRunClaim Writing(string[] tables, int server = 8, int tenant = 8, Guid? tenantId = null, PipelineRunCap? cap = null) =>
        PipelineRunClaim.Writing(Run(tenantId), tables, new PipelineRunLimitValues(server, tenant), cap);

    private static PipelineRunClaim Alone() => PipelineRunClaim.RunningAlone(Run(), new PipelineRunLimitValues(8, 8), null);

    private static PipelineRunCap Cap(string pipeline, int limit, bool refuses = false) => new(string.Concat("pipeline/", pipeline), limit, refuses);

    private static async Task<bool> Started(Task<PipelineRunPlace> entering) =>
        await Task.WhenAny(entering, Task.Delay(Short, TestContext.Current.CancellationToken)).ConfigureAwait(false) == entering;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task RunsThatWriteDifferentTablesRunAtTheSameTime()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        await scheduler.Enter(Writing(["store//Customers"]), TestContext.Current.CancellationToken);

        var second = scheduler.Enter(Writing(["store//Orders"]), TestContext.Current.CancellationToken);

        (await Started(second)).ShouldBeTrue();
        scheduler.Running.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task RunsThatWriteTheSameTableTakeTurnsWithoutRegardToCase()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var first = await scheduler.Enter(Writing(["store//Customers"]), TestContext.Current.CancellationToken);

        var second = scheduler.Enter(Writing(["STORE//customers"]), TestContext.Current.CancellationToken);

        (await Started(second)).ShouldBeFalse();
        scheduler.Release(first);
        (await Started(second)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatWritesNothingNeverWaitsForATable()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        await scheduler.Enter(Writing(["store//Customers"]), TestContext.Current.CancellationToken);

        var reader = scheduler.Enter(Writing([]), TestContext.Current.CancellationToken);

        (await Started(reader)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheServerLimitCapsHowManyRunsAreInFlight()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var first = await scheduler.Enter(Writing(["a"], server: 1), TestContext.Current.CancellationToken);

        var second = scheduler.Enter(Writing(["b"], server: 1), TestContext.Current.CancellationToken);

        (await Started(second)).ShouldBeFalse();
        scheduler.Release(first);
        (await Started(second)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheTenantLimitCapsOneTenantAndLeavesTheOthersAlone()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var tenant = Guid.NewGuid();
        await scheduler.Enter(Writing(["a"], tenant: 1, tenantId: tenant), TestContext.Current.CancellationToken);

        var sameTenant = scheduler.Enter(Writing(["b"], tenant: 1, tenantId: tenant), TestContext.Current.CancellationToken);
        var otherTenant = scheduler.Enter(Writing(["c"], tenant: 1, tenantId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        (await Started(sameTenant)).ShouldBeFalse();
        (await Started(otherTenant)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task WaitingRunsStartInTheOrderTheyArrived()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var first = await scheduler.Enter(Writing(["t"]), TestContext.Current.CancellationToken);
        var second = scheduler.Enter(Writing(["t"]), TestContext.Current.CancellationToken);
        var third = scheduler.Enter(Writing(["t"]), TestContext.Current.CancellationToken);

        scheduler.Release(first);

        (await Started(second)).ShouldBeTrue();
        (await Started(third)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALaterRunPassesAnEarlierOneThatWaitsOnATableItDoesNotWrite()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        await scheduler.Enter(Writing(["x"]), TestContext.Current.CancellationToken);
        var waitsOnX = scheduler.Enter(Writing(["x"]), TestContext.Current.CancellationToken);

        var needsOnlyY = scheduler.Enter(Writing(["y"]), TestContext.Current.CancellationToken);

        (await Started(waitsOnX)).ShouldBeFalse();
        (await Started(needsOnlyY)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALaterRunDoesNotPassAnEarlierOneThatWaitsOnATableItWrites()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        await scheduler.Enter(Writing(["x"]), TestContext.Current.CancellationToken);
        var waitsOnX = scheduler.Enter(Writing(["x", "y"]), TestContext.Current.CancellationToken);

        var needsY = scheduler.Enter(Writing(["y"]), TestContext.Current.CancellationToken);

        (await Started(waitsOnX)).ShouldBeFalse();
        (await Started(needsY)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatMustRunAloneWaitsForTheOthersAndHoldsOffLaterRuns()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var running = await scheduler.Enter(Writing(["a"]), TestContext.Current.CancellationToken);
        var alone = scheduler.Enter(Alone(), TestContext.Current.CancellationToken);
        var later = scheduler.Enter(Writing(["b"]), TestContext.Current.CancellationToken);

        (await Started(alone)).ShouldBeFalse();
        (await Started(later)).ShouldBeFalse();
        scheduler.Release(running);
        (await Started(alone)).ShouldBeTrue();
        (await Started(later)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatWaitsForThePipelineItStartedHandsItsPlaceBackAndTakesItAgain()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var parent = await scheduler.Enter(Writing(["t"]), TestContext.Current.CancellationToken);
        scheduler.Hold(parent);
        var child = scheduler.Enter(Writing(["t"]), TestContext.Current.CancellationToken);
        (await Started(child)).ShouldBeFalse();

        var handedBack = scheduler.Yield();

        (await Started(child)).ShouldBeTrue();
        scheduler.Release(await child);
        await handedBack.DisposeAsync();
        scheduler.Running.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ACallerThatHoldsNoPlaceGivesNothingBack()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        await scheduler.Enter(Writing(["t"]), TestContext.Current.CancellationToken);

        var handle = scheduler.Yield();
        var other = scheduler.Enter(Writing(["t"]), TestContext.Current.CancellationToken);
        await handle.DisposeAsync();

        (await Started(other)).ShouldBeFalse();
        scheduler.Running.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatIsCancelledWhileWaitingLeavesTheQueueAndFreesTheOnesBehindIt()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var first = await scheduler.Enter(Writing(["x"]), TestContext.Current.CancellationToken);
        using var cancel = new CancellationTokenSource();
        var cancelled = scheduler.Enter(Writing(["x", "y"]), cancel.Token);
        var behind = scheduler.Enter(Writing(["y"]), TestContext.Current.CancellationToken);
        (await Started(behind)).ShouldBeFalse();

        await cancel.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => cancelled);
        (await Started(behind)).ShouldBeTrue();
        scheduler.Release(first);
        scheduler.Waiting.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task RunsOfOnePipelineRunUpToItsLimitAndTheNextWaitsForOneToEnd()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var first = await scheduler.Enter(Writing([], cap: Cap("monthly", 2)), TestContext.Current.CancellationToken);
        var second = scheduler.Enter(Writing([], cap: Cap("monthly", 2)), TestContext.Current.CancellationToken);
        var third = scheduler.Enter(Writing([], cap: Cap("monthly", 2)), TestContext.Current.CancellationToken);

        (await Started(second)).ShouldBeTrue();
        (await Started(third)).ShouldBeFalse();
        scheduler.Release(first);
        (await Started(third)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheLimitOfOnePipelineDoesNotHoldARunOfAnother()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        await scheduler.Enter(Writing([], cap: Cap("monthly", 1)), TestContext.Current.CancellationToken);

        var other = scheduler.Enter(Writing([], cap: Cap("weekly", 1)), TestContext.Current.CancellationToken);

        (await Started(other)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunPastTheLimitOfAPipelineThatRefusesIsRefusedAndNeverQueued()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var first = await scheduler.Enter(Writing([], cap: Cap("monthly", 1, refuses: true)), TestContext.Current.CancellationToken);

        var refused = scheduler.Queue(Writing([], cap: Cap("monthly", 1, refuses: true)));

        refused.IsRefused.ShouldBeTrue();
        refused.RefusedLimit.ShouldBe(1);
        scheduler.Waiting.ShouldBe(0);
        await Should.ThrowAsync<InvalidOperationException>(() => scheduler.Wait(refused, TestContext.Current.CancellationToken));
        scheduler.Release(first);
        scheduler.Queue(Writing([], cap: Cap("monthly", 1, refuses: true))).IsRefused.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineThatRefusesCountsItsWaitingRunsAsInFlight()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        await scheduler.Enter(Writing(["t"], cap: Cap("monthly", 2, refuses: true)), TestContext.Current.CancellationToken);
        var waiting = scheduler.Queue(Writing(["t"], cap: Cap("monthly", 2, refuses: true)));
        waiting.IsRefused.ShouldBeFalse();
        scheduler.Waiting.ShouldBe(1);

        var refused = scheduler.Queue(Writing(["u"], cap: Cap("monthly", 2, refuses: true)));

        refused.IsRefused.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task RunsOfOnePipelineStartInTheOrderTheyArrivedEvenWhenALaterOneCouldStart()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var first = await scheduler.Enter(Writing(["t"], cap: Cap("monthly", 2)), TestContext.Current.CancellationToken);
        var second = scheduler.Enter(Writing(["t"], cap: Cap("monthly", 2)), TestContext.Current.CancellationToken);
        var third = scheduler.Enter(Writing(["u"], cap: Cap("monthly", 2)), TestContext.Current.CancellationToken);

        (await Started(third)).ShouldBeFalse();
        scheduler.Release(first);
        (await Started(second)).ShouldBeTrue();
        (await Started(third)).ShouldBeTrue();
    }
}
