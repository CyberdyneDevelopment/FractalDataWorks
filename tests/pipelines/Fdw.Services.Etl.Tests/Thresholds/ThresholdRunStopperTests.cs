using System;
using System.Threading;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Thresholds;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for stopping a run for a threshold (FDW-3865): the run this host holds is cancelled and remembers why, only the first reason counts, a person's cancel or a
/// time limit that came first is not overwritten, and a run the host does not hold is a failure.
/// </summary>
public sealed class ThresholdRunStopperTests
{
    private static (PipelineRunControl Control, PipelineRunHandle Run, CancellationTokenSource Stopping) Held()
    {
        var control = new PipelineRunControl();
        var stopping = new CancellationTokenSource();
        return (control, control.Register(Guid.NewGuid(), stopping.Token), stopping);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AHeldRunIsCancelledAndKeepsTheThresholdsReason()
    {
        var (control, run, stopping) = Held();
        using (stopping)
        using (run)
        {
            var result = await new ThresholdRunStopper(control).Stop(run.ExecutionId, "rejected is 60 on load/clean");

            result.IsSuccess.ShouldBeTrue();
            run.Token.IsCancellationRequested.ShouldBeTrue();
            run.IsCancelled.ShouldBeTrue();
            run.IsThresholdStopped.ShouldBeTrue();
            run.ThresholdReason.ShouldBe("rejected is 60 on load/clean");
            run.IsTimedOut.ShouldBeFalse();
            stopping.IsCancellationRequested.ShouldBeFalse();
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task OnlyTheFirstThresholdReasonCounts()
    {
        var (control, run, stopping) = Held();
        using (stopping)
        using (run)
        {
            var stopper = new ThresholdRunStopper(control);
            await stopper.Stop(run.ExecutionId, "first");
            var second = await stopper.Stop(run.ExecutionId, "second");

            second.IsSuccess.ShouldBeTrue();
            run.ThresholdReason.ShouldBe("first");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunAPersonAlreadyCancelledIsNotRelabelledAsAThresholdStop()
    {
        var (control, run, stopping) = Held();
        using (stopping)
        using (run)
        {
            control.Cancel(run.ExecutionId).IsSuccess.ShouldBeTrue();

            var result = await new ThresholdRunStopper(control).Stop(run.ExecutionId, "late");

            result.IsSuccess.ShouldBeTrue();
            run.IsThresholdStopped.ShouldBeFalse();
            run.ThresholdReason.ShouldBeNull();
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunTheHostDoesNotHoldIsAFailure()
    {
        var result = await new ThresholdRunStopper(new PipelineRunControl()).Stop(Guid.NewGuid(), "reason");

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-40994");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ABlankReasonIsRefusedNotStored()
    {
        var (control, run, stopping) = Held();
        using (stopping)
        using (run)
        {
            Should.Throw<ArgumentException>(() => new ThresholdRunStopper(control).Stop(run.ExecutionId, " "));

            run.IsCancelled.ShouldBeFalse();
        }
    }
}
