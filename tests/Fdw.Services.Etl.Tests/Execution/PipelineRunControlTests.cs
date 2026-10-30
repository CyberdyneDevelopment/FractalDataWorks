using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Execution;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Covers <see cref="PipelineRunControl"/> and <see cref="PipelineRunPauseGate"/> (FDW-2950): a cancel reaches the run it names and
/// only that run, a request for a run the host does not hold fails, and a pause holds the steps waiting at the gate until a resume or a
/// cancel.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public sealed class PipelineRunControlTests
{
    private static string CodeOf(Fdw.Results.IGenericResult result) => result.Messages[^1].Code;

    [Fact]
    public void CancelOfARunTheHostDoesNotHoldFailsAsNotHeld()
    {
        var control = new PipelineRunControl();

        var result = control.Cancel(Guid.NewGuid());

        result.IsFailure.ShouldBeTrue();
        CodeOf(result).ShouldBe("PIPELINERUN-40994");
    }

    [Fact]
    public void CancelEndsTheRunsTokenAndMarksItCancelledByAPerson()
    {
        var control = new PipelineRunControl();
        using var stopping = new CancellationTokenSource();
        using var run = control.Register(Guid.NewGuid(), stopping.Token);

        var result = control.Cancel(run.ExecutionId);

        result.IsSuccess.ShouldBeTrue();
        run.Token.IsCancellationRequested.ShouldBeTrue();
        run.IsCancelled.ShouldBeTrue();
        stopping.IsCancellationRequested.ShouldBeFalse();
    }

    [Fact]
    public void ShutdownEndsTheRunsTokenButIsNotACancel()
    {
        var control = new PipelineRunControl();
        using var stopping = new CancellationTokenSource();
        using var run = control.Register(Guid.NewGuid(), stopping.Token);

        stopping.Cancel();

        run.Token.IsCancellationRequested.ShouldBeTrue();
        run.IsCancelled.ShouldBeFalse();
    }

    [Fact]
    public void CancelLeavesTheOtherRunsAlone()
    {
        var control = new PipelineRunControl();
        using var first = control.Register(Guid.NewGuid(), TestContext.Current.CancellationToken);
        using var second = control.Register(Guid.NewGuid(), TestContext.Current.CancellationToken);

        control.Cancel(first.ExecutionId).IsSuccess.ShouldBeTrue();

        first.Token.IsCancellationRequested.ShouldBeTrue();
        second.Token.IsCancellationRequested.ShouldBeFalse();
    }

    [Fact]
    public void ADisposedRunIsNoLongerHeld()
    {
        var control = new PipelineRunControl();
        var run = control.Register(Guid.NewGuid(), TestContext.Current.CancellationToken);
        var id = run.ExecutionId;
        run.Dispose();

        control.Cancel(id).IsFailure.ShouldBeTrue();
        control.Pause(id).IsFailure.ShouldBeTrue();
        control.Resume(id).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task APausedRunHoldsItsStepsUntilItIsResumed()
    {
        var control = new PipelineRunControl();
        using var run = control.Register(Guid.NewGuid(), TestContext.Current.CancellationToken);
        control.Pause(run.ExecutionId).IsSuccess.ShouldBeTrue();

        var waiting = run.Gate.WaitWhilePaused(TestContext.Current.CancellationToken);

        waiting.IsCompleted.ShouldBeFalse();
        control.Resume(run.ExecutionId).IsSuccess.ShouldBeTrue();
        await waiting;
    }

    [Fact]
    public async Task ARunThatIsNotPausedDoesNotHoldItsSteps()
    {
        var control = new PipelineRunControl();
        using var run = control.Register(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await run.Gate.WaitWhilePaused(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CancellingAPausedRunReleasesItsWaitingSteps()
    {
        var control = new PipelineRunControl();
        using var run = control.Register(Guid.NewGuid(), TestContext.Current.CancellationToken);
        control.Pause(run.ExecutionId).IsSuccess.ShouldBeTrue();
        var waiting = run.Gate.WaitWhilePaused(run.Token);

        control.Cancel(run.ExecutionId).IsSuccess.ShouldBeTrue();

        await Should.ThrowAsync<OperationCanceledException>(waiting);
    }

    [Fact]
    public void PausingAPausedRunAndResumingARunThatIsNotPausedFailWithAReason()
    {
        var control = new PipelineRunControl();
        using var run = control.Register(Guid.NewGuid(), TestContext.Current.CancellationToken);

        var notPaused = control.Resume(run.ExecutionId);
        control.Pause(run.ExecutionId).IsSuccess.ShouldBeTrue();
        var alreadyPaused = control.Pause(run.ExecutionId);

        CodeOf(notPaused).ShouldBe("PIPELINERUN-40996");
        CodeOf(alreadyPaused).ShouldBe("PIPELINERUN-40995");
    }
}
