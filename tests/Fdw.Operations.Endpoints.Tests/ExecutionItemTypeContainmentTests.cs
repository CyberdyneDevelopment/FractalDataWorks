using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>
/// Tests for execution item containment (FDW-1381): a type contains types at a deeper level, and a type that states it nests also
/// contains its own type, which is how a pipeline run (a Job) starts another pipeline run and how the record of a run keeps a stage inside a stage.
/// </summary>
[Trait("Priority", "P0")]
public class ExecutionItemTypeContainmentTests
{
    [Fact]
    [Trait("Category", "Etl")]
    public void AJobContainsAJobBecauseARunMayStartARun()
    {
        new JobItemType().CanContain(new JobItemType()).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "DataIntegrity")]
    public void NoOtherTypeContainsItsOwnType()
    {
        new StepItemType().CanContain(new StepItemType()).ShouldBeFalse();
        new TaskItemType().CanContain(new TaskItemType()).ShouldBeFalse();
        new WorkflowItemType().CanContain(new WorkflowItemType()).ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "Etl")]
    public void AStageContainsAStageBecauseARunRecordsAnActionAndARoundInsideTheStageTheyRunIn()
    {
        new StageItemType().CanContain(new StageItemType()).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "DataIntegrity")]
    public void TheLevelRulesStillHold()
    {
        new JobItemType().CanContain(new StageItemType()).ShouldBeTrue();
        new JobItemType().CanContain(new TaskItemType()).ShouldBeTrue();
        new TaskItemType().CanContain(new JobItemType()).ShouldBeFalse();
        new StageItemType().CanContain(new JobItemType()).ShouldBeFalse();
        new JobItemType().CanContain(new WorkflowItemType()).ShouldBeFalse();
    }
}
