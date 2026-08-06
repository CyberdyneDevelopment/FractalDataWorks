using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;

namespace Fdw.Orchestration.Abstractions.Tests;

public class ExecutionStatusBaseTests
{
    private sealed class TestExecutionStatus : ExecutionStatusBase
    {
        public TestExecutionStatus(
            int id,
            string name,
            bool isTerminal,
            bool isSuccess,
            bool isFailure = false,
            bool allowsRetry = false,
            bool allowsResume = false,
            bool isInProgress = false,
            bool hasWarnings = false)
            : base(id, name, isTerminal, isSuccess, isFailure, allowsRetry, allowsResume, isInProgress, hasWarnings)
        {
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsAllPropertiesCorrectly()
    {
        var status = new TestExecutionStatus(
            1, "Running",
            isTerminal: false,
            isSuccess: false,
            isFailure: false,
            allowsRetry: false,
            allowsResume: false,
            isInProgress: true,
            hasWarnings: false);

        status.Id.ShouldBe(1);
        status.Name.ShouldBe("Running");
        status.IsTerminal.ShouldBeFalse();
        status.IsSuccess.ShouldBeFalse();
        status.IsFailure.ShouldBeFalse();
        status.AllowsRetry.ShouldBeFalse();
        status.AllowsResume.ShouldBeFalse();
        status.IsInProgress.ShouldBeTrue();
        status.HasWarnings.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TerminalSuccessStatus()
    {
        var status = new TestExecutionStatus(
            2, "Succeeded",
            isTerminal: true,
            isSuccess: true);

        status.IsTerminal.ShouldBeTrue();
        status.IsSuccess.ShouldBeTrue();
        status.IsFailure.ShouldBeFalse();
        status.IsInProgress.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TerminalFailureStatusAllowsRetry()
    {
        var status = new TestExecutionStatus(
            3, "Failed",
            isTerminal: true,
            isSuccess: false,
            isFailure: true,
            allowsRetry: true);

        status.IsTerminal.ShouldBeTrue();
        status.IsSuccess.ShouldBeFalse();
        status.IsFailure.ShouldBeTrue();
        status.AllowsRetry.ShouldBeTrue();
        status.AllowsResume.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void StatusWithWarnings()
    {
        var status = new TestExecutionStatus(
            4, "CompletedWithWarnings",
            isTerminal: true,
            isSuccess: true,
            hasWarnings: true);

        status.IsSuccess.ShouldBeTrue();
        status.HasWarnings.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void PausedStatusAllowsResume()
    {
        var status = new TestExecutionStatus(
            5, "Paused",
            isTerminal: false,
            isSuccess: false,
            allowsResume: true);

        status.IsTerminal.ShouldBeFalse();
        status.AllowsResume.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultOptionalParametersAreFalse()
    {
        var status = new TestExecutionStatus(
            6, "Pending",
            isTerminal: false,
            isSuccess: false);

        status.IsFailure.ShouldBeFalse();
        status.AllowsRetry.ShouldBeFalse();
        status.AllowsResume.ShouldBeFalse();
        status.IsInProgress.ShouldBeFalse();
        status.HasWarnings.ShouldBeFalse();
    }
}
