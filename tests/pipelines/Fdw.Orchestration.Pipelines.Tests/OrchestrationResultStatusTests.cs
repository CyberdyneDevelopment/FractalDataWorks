using Fdw.Orchestration.Abstractions;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;
using Fdw.Orchestration.Execution;
using ExecutionStatusesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions.ExecutionStatuses;

namespace Fdw.Orchestration.Pipelines.Tests;

/// <summary>
/// FDW-6325: OrchestrationResult (13 lookups) and OrchestrationStepResult (7 lookups) set their Status through
/// ExecutionStatuses.ByName. Each factory must set the declared status, never the NotFound sentinel, and the
/// metrics must count steps by their real status: when every lookup answers NotFound, every status shares one Id
/// and a failed step counts as succeeded, failed and skipped at once.
/// </summary>
public sealed class OrchestrationResultStatusTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    private static void ShouldBeTheDeclaredStatus(IExecutionStatus status, string name)
    {
        ReferenceEquals(status, ExecutionStatusesCollection.NotFound).ShouldBeFalse();
        status.Name.ShouldBe(name);
    }

    [Fact]
    public void EveryStepResultFactorySetsTheDeclaredStatus()
    {
        ShouldBeTheDeclaredStatus(OrchestrationStepResult.Success("s1", "Load", Start).Status, "Succeeded");
        ShouldBeTheDeclaredStatus(OrchestrationStepResult.SuccessWithWarnings("s1", "Load", Start, "two rows trimmed").Status, "SucceededWithWarnings");
        ShouldBeTheDeclaredStatus(OrchestrationStepResult.Failure("s1", "Load", Start, "the source refused").Status, "Failed");
        ShouldBeTheDeclaredStatus(OrchestrationStepResult.Skipped("s1", "Load", "its dependency failed").Status, "Cancelled");
        ShouldBeTheDeclaredStatus(OrchestrationStepResult.TimedOut("s1", "Load", Start, TimeSpan.FromSeconds(30)).Status, "TimedOut");
        ShouldBeTheDeclaredStatus(OrchestrationStepResult<string>.Success("s1", "Load", Start, "rows").Status, "Succeeded");
        ShouldBeTheDeclaredStatus(OrchestrationStepResult<string>.Failure("s1", "Load", Start, "the source refused").Status, "Failed");
    }

    [Fact]
    public void EveryOrchestrationResultFactorySetsTheDeclaredStatus()
    {
        IOrchestrationStepResult[] steps = [OrchestrationStepResult.Success("s1", "Load", Start)];

        ShouldBeTheDeclaredStatus(OrchestrationResult.Success("e1", "o1", Start, steps).Status, "Succeeded");
        ShouldBeTheDeclaredStatus(OrchestrationResult.SuccessWithWarnings("e1", "o1", Start, steps, "two rows trimmed").Status, "SucceededWithWarnings");
        ShouldBeTheDeclaredStatus(OrchestrationResult.Failure("e1", "o1", Start, steps, "a step failed").Status, "Failed");
        ShouldBeTheDeclaredStatus(OrchestrationResult.Cancelled("e1", "o1", Start, steps, "stopped by the caller").Status, "Cancelled");
        ShouldBeTheDeclaredStatus(OrchestrationResult.TimedOut("e1", "o1", Start, steps, TimeSpan.FromMinutes(5)).Status, "TimedOut");
        ShouldBeTheDeclaredStatus(OrchestrationResult<string>.Success("e1", "o1", Start, steps, "done").Status, "Succeeded");
        ShouldBeTheDeclaredStatus(OrchestrationResult<string>.Failure("e1", "o1", Start, steps, "a step failed").Status, "Failed");
    }

    [Fact]
    public void TheMetricsCountEachStepByItsOwnStatus()
    {
        IOrchestrationStepResult[] steps =
        [
            OrchestrationStepResult.Success("s1", "Load", Start),
            OrchestrationStepResult.SuccessWithWarnings("s2", "Clean", Start, "two rows trimmed"),
            OrchestrationStepResult.Failure("s3", "Write", Start, "the target refused"),
            OrchestrationStepResult.Skipped("s4", "Notify", "its dependency failed"),
        ];

        var metrics = OrchestrationResult.Failure("e1", "o1", Start, steps, "a step failed").Metrics;

        metrics.ShouldNotBeNull();
        metrics.TotalSteps.ShouldBe(4);
        metrics.SucceededSteps.ShouldBe(2);
        metrics.FailedSteps.ShouldBe(1);
        metrics.SkippedSteps.ShouldBe(1);
    }
}
