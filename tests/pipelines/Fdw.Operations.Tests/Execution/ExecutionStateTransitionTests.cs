using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

namespace Fdw.Operations.Tests.Execution;

/// <summary>
/// A run that waits for approval has a state of its own (FDW-1740): a run is held in it from Scheduled, and approval returns it to Scheduled
/// while denial cancels it. No other move out of it is valid, so a waiting run cannot start without a decision.
/// </summary>
public sealed class ExecutionStateTransitionTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AwaitingApprovalIsAStateARunCanBeHeldIn()
    {
        var state = ExecutionStateTypes.AwaitingApproval;

        ReferenceEquals(state, ExecutionStateTypes.NotFound).ShouldBeFalse();
        state.Name.ShouldBe("AwaitingApproval");
        state.IsTerminal.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ARunIsHeldFromScheduled()
    {
        ExecutionStateTypes.Scheduled.CanTransitionTo("AwaitingApproval").ShouldBeTrue();
    }

    [Theory]
    [InlineData("Scheduled")]
    [InlineData("Cancelled")]
    public void AWaitingRunCanBeApprovedBackToScheduledOrDenied(string target)
    {
        ExecutionStateTypes.AwaitingApproval.CanTransitionTo(target).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Triggered")]
    [InlineData("Initialized")]
    [InlineData("Running")]
    [InlineData("Completed")]
    [InlineData("Failed")]
    [InlineData("Paused")]
    public void AWaitingRunCannotStartWithoutADecision(string target)
    {
        ExecutionStateTypes.AwaitingApproval.CanTransitionTo(target).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Initialized")]
    [InlineData("Running")]
    [InlineData("Completed")]
    public void OnlyAScheduledRunCanBeHeldForApproval(string from)
    {
        ExecutionStateTypes.ByName(from).CanTransitionTo("AwaitingApproval").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARunRefusedBeforeItIsTriggeredCanFailFromScheduled()
    {
        ExecutionStateTypes.Scheduled.CanTransitionTo("Failed").ShouldBeTrue();
        ExecutionStateTypes.Scheduled.CanTransitionTo("Triggered").ShouldBeTrue();
        ExecutionStateTypes.Scheduled.CanTransitionTo("Running").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ADeniedRunCannotBeRevived()
    {
        ExecutionStateTypes.Cancelled.IsTerminal.ShouldBeTrue();
        ExecutionStateTypes.Cancelled.CanTransitionTo("Scheduled").ShouldBeFalse();
    }

    [Theory]
    [InlineData("Running")]
    [InlineData("Completed")]
    [InlineData("Failed")]
    [InlineData("Cancelled")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void APausedRunCanResumeBeCancelledOrEndByItself(string target)
    {
        // Why Completed and Failed: a pause holds a run at its next batch, but the work before that batch goes on; a run paused during
        // it can end there. If the row could not follow, it would stay Paused for good after the run was gone (FDW-2950).
        ExecutionStateTypes.Paused.CanTransitionTo(target).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Scheduled")]
    [InlineData("Initialized")]
    [InlineData("AwaitingApproval")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void APausedRunCannotGoBackToBeforeItStarted(string target)
    {
        ExecutionStateTypes.Paused.CanTransitionTo(target).ShouldBeFalse();
    }
}
