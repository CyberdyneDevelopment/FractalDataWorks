using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Tests.Execution;

/// <summary>
/// FDW-4488: a state says whether a run in it is executing (taken its turn, not waiting or finished) and whether it stopped (terminal, neither success nor failure), so a rule
/// over a run never lists state names.
/// </summary>
public sealed class ExecutionStateFlagsTests
{
    [Theory]
    [InlineData("Triggered", true)]
    [InlineData("Initialized", true)]
    [InlineData("Running", true)]
    [InlineData("Paused", true)]
    [InlineData("Retrying", true)]
    [InlineData("Compensating", true)]
    [InlineData("Scheduled", false)]
    [InlineData("AwaitingApproval", false)]
    [InlineData("Completed", false)]
    [InlineData("Failed", false)]
    [InlineData("Cancelled", false)]
    [InlineData("TimedOut", false)]
    [InlineData("Interrupted", false)]
    [Trait("Priority", "P1")]
    [Trait("Category", "Operations")]
    public void AStateSaysWhetherARunInItIsExecuting(string state, bool executing)
    {
        ExecutionStateTypes.ByName(state).IsExecuting.ShouldBe(executing);
    }

    [Theory]
    [InlineData("Cancelled", true)]
    [InlineData("TimedOut", true)]
    [InlineData("Interrupted", true)]
    [InlineData("Completed", false)]
    [InlineData("Failed", false)]
    [InlineData("Running", false)]
    [InlineData("Scheduled", false)]
    [Trait("Priority", "P1")]
    [Trait("Category", "Operations")]
    public void OnlyTheTerminalStatesThatAreNeitherSuccessNorFailureAreStopped(string state, bool stopped)
    {
        ExecutionStateTypes.ByName(state).IsStopped.ShouldBe(stopped);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Operations")]
    public void NoStateIsBothExecutingAndTerminal()
    {
        foreach (var state in ExecutionStateTypes.All())
            (state.IsExecuting && state.IsTerminal).ShouldBeFalse(state.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Operations")]
    public void TheNotFoundSentinelIsNeitherExecutingNorStoppedByAccident()
    {
        ExecutionStateTypes.NotFound.IsExecuting.ShouldBeFalse();
    }
}
