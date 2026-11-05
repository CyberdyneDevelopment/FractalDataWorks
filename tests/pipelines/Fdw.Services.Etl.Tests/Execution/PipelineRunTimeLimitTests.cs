using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for the time limit of a pipeline run (FDW-3805): a timeout the request states stops the run when it has passed and marks it timed out, a request that states
/// none arms nothing, and a limit disposed before it fires stops nothing.
/// </summary>
public sealed class PipelineRunTimeLimitTests
{
    private static PipelineExecutionRequest Request(int? timeoutSeconds, string? schedule = "Nightly") => new()
    {
        ExecutionId = Guid.NewGuid(),
        PipelineId = Guid.NewGuid(),
        PipelineName = "monthly-sales",
        TriggerSource = "Scheduled",
        ScheduleName = schedule,
        TenantId = Guid.NewGuid(),
        RunRoles = ["DataWriter"],
        TimeoutSeconds = timeoutSeconds,
    };

    private static PipelineRunHandle Handle(PipelineRunControl control, CancellationToken stopping) => control.Register(Guid.NewGuid(), stopping);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task WhenTheTimeHasPassedTheRunIsStoppedAndMarkedTimedOut()
    {
        using var stopping = new CancellationTokenSource();
        using var run = Handle(new PipelineRunControl(), stopping.Token);
        var request = Request(30);

        using var limit = PipelineRunTimeLimit.Arm(run, request, TimeSpan.FromMilliseconds(50), NullLogger.Instance);
        await Task.WhenAny(Task.Delay(Timeout.Infinite, run.Token), Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        run.Token.IsCancellationRequested.ShouldBeTrue();
        run.IsTimedOut.ShouldBeTrue();
        run.IsCancelled.ShouldBeTrue();
        stopping.IsCancellationRequested.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARequestThatStatesNoTimeoutArmsNothing()
    {
        using var stopping = new CancellationTokenSource();
        using var run = Handle(new PipelineRunControl(), stopping.Token);

        using var limit = PipelineRunTimeLimit.Arm(run, Request(null), NullLogger.Instance);
        await Task.Delay(150, TestContext.Current.CancellationToken);

        run.Token.IsCancellationRequested.ShouldBeFalse();
        run.IsTimedOut.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALimitDisposedBeforeItFiresStopsNothing()
    {
        using var stopping = new CancellationTokenSource();
        using var run = Handle(new PipelineRunControl(), stopping.Token);
        var limit = PipelineRunTimeLimit.Arm(run, Request(30), TimeSpan.FromMilliseconds(100), NullLogger.Instance);

        limit.Dispose();
        await Task.Delay(300, TestContext.Current.CancellationToken);

        run.Token.IsCancellationRequested.ShouldBeFalse();
        run.IsTimedOut.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ALimitThatFiresAfterTheRunEndedFindsTheHandleDisposedAndDoesNothing()
    {
        using var stopping = new CancellationTokenSource();
        var run = Handle(new PipelineRunControl(), stopping.Token);
        run.Dispose();

        Should.NotThrow(() => run.TimeOut());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheReasonNamesTheTimeoutAndTheScheduleThatStatedIt()
    {
        PipelineRunTimeLimit.Reason(Request(5, "Nightly")).ShouldBe("The run exceeded the 5 second timeout of schedule 'Nightly'");
        PipelineRunTimeLimit.Reason(Request(5, null)).ShouldBe("The run exceeded the 5 second timeout of the Scheduled trigger");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheLongestTimeoutIsTheLongestDelayTheTimerAccepts()
    {
        using var timer = new CancellationTokenSource(TimeSpan.FromSeconds(PipelineExecutionRequest.MaxTimeoutSeconds));

        timer.IsCancellationRequested.ShouldBeFalse();
    }
}
