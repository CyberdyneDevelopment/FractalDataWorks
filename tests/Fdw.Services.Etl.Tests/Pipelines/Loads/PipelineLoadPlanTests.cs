using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for <see cref="PipelineLoadPlan"/> (FDW-1109): what a run does to load its output is worked out once from the
/// load mode or the load steps, after the shared validator has accepted it.
/// </summary>
public sealed class PipelineLoadPlanTests
{
    private static BatchCopyPipelineConfiguration Pipeline(string? loadMode, params PipelineLoadStepConfiguration[] rows) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "copy",
            LoadMode = loadMode,
            LoadSteps = rows.ToList(),
            DestinationDataSet = "Orders",
        };

    private static PipelineLoadColumnConfiguration Column(string role, string column, int ordinal = 0) =>
        new() { Name = column, Role = role, ColumnName = column, Ordinal = ordinal };

    private static PipelineLoadStepConfiguration OwnLoad(params PipelineLoadColumnConfiguration[] columns) =>
        new() { Name = PipelineLoadStepConfiguration.PipelineLoadName, Phase = "Load", StepOrder = null, IsEnabled = true, Columns = columns };

    private static PipelineLoadStepConfiguration Step(string kind, int order, string name, bool enabled = true)
    {
        var step = new PipelineLoadStepConfiguration { Phase = "Load", Name = name, StepOrder = order, Kind = kind, DestinationDataSet = "Orders", IsEnabled = enabled };
        if (kind == "BulkInsert")
            step.Columns = [Column("Write", "Name")];
        if (kind == "Execute")
        {
            step.DestinationDataSet = "Orders";
            step.DestinationDataStoreName = "Main";
            step.DestinationPathName = "dbo";
            step.DestinationContainerName = "LoadSummary";
        }

        return step;
    }

    private static PipelineLoadPlan Plan(BatchCopyPipelineConfiguration configuration)
    {
        var result = PipelineLoadPlan.Create(configuration, NullLogger.Instance);
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        return result.Value!;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnAppendIsAnInsertThatStreamsAndTruncatesNothing()
    {
        // Act
        var plan = Plan(Pipeline("Append"));

        // Assert
        plan.HasSteps.ShouldBeFalse();
        plan.OwnLoad!.Kind.Name.ShouldBe("BulkInsert");
        plan.TruncatesFirst.ShouldBeFalse();
        plan.Keyed.ShouldBeFalse();
        plan.NeedsCompleteSet.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AReplaceIsAnInsertThatTruncatesOnceFirst()
    {
        // Act
        var plan = Plan(Pipeline("Replace"));

        // Assert
        plan.OwnLoad!.Kind.Name.ShouldBe("BulkInsert");
        plan.TruncatesFirst.ShouldBeTrue();
        plan.NeedsCompleteSet.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnUpsertIsAKeyedBulkUpsertThatStillStreams()
    {
        // Act
        var plan = Plan(Pipeline("Upsert", OwnLoad(Column("Key", "Id"), Column("Write", "Id"), Column("Write", "Name", 1))));

        // Assert
        plan.OwnLoad!.Kind.Name.ShouldBe("BulkUpsert");
        plan.Keyed.ShouldBeTrue();
        plan.FlagsAbsentKeys.ShouldBeFalse();
        plan.NeedsCompleteSet.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnUpsertThatFlagsTheKeysAbsentFromTheSourceNeedsTheWholeOutput()
    {
        // Arrange
        var own = OwnLoad(Column("Key", "Id"), Column("Write", "Id"), Column("Write", "Name", 1));
        own.SoftDeleteMode = "AbsentFromSource";
        own.DeletedFlagColumn = "IsDeleted";

        // Act
        var plan = Plan(Pipeline("Upsert", own));

        // Assert
        plan.FlagsAbsentKeys.ShouldBeTrue();
        plan.NeedsCompleteSet.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheOwnLoadWritesWhereThePipelinesDestinationSays()
    {
        // Arrange
        var toDataSet = Plan(Pipeline("Append"));
        var toContainer = Pipeline("Append");
        toContainer.DestinationDataSet = string.Empty;
        toContainer.DestinationConnectionName = "Warehouse";
        toContainer.DestinationContainerPath = "dbo.Orders";

        // Act
        var container = Plan(toContainer);

        // Assert
        toDataSet.OwnLoad!.DataSet.ShouldBe("Orders");
        container.OwnLoad!.DataSet.ShouldBeNull();
        container.OwnLoad.DataStore.ShouldBe("Warehouse");
        container.OwnLoad.Container.ShouldBe("dbo.Orders");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void LoadStepsRunInOrderOnTheWholeOutputAndDisabledOnesDropOut()
    {
        // Act
        var plan = Plan(Pipeline(null, Step("Execute", 3, "summarise"), Step("BulkInsert", 1, "load"), Step("Truncate", 2, "clear", enabled: false)));

        // Assert
        plan.HasSteps.ShouldBeTrue();
        plan.OwnLoad.ShouldBeNull();
        plan.NeedsCompleteSet.ShouldBeTrue();
        plan.Steps.Select(s => s.Row.Name).ShouldBe(new[] { "load", "summarise" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnExecuteStepNamesItsContainerAndOtherStepsNameTheirDataSet()
    {
        // Act
        var plan = Plan(Pipeline(null, Step("BulkInsert", 1, "load"), Step("Execute", 2, "summarise")));

        // Assert
        plan.Steps[0].DataSet.ShouldBe("Orders");
        plan.Steps[0].DataStore.ShouldBeNull();
        plan.Steps[1].DataSet.ShouldBeNull();
        (plan.Steps[1].DataStore, plan.Steps[1].Path, plan.Steps[1].Container).ShouldBe(("Main", "dbo", "LoadSummary"));
        plan.Steps[1].Owner.ShouldBe("load step 'summarise'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALoadWithNoLoadModeAndNoStepsIsRefusedBecauseNothingIsAssumed()
    {
        // Act
        var result = PipelineLoadPlan.Create(Pipeline(null), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("must state a load mode");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheValidatorsRefusalsReachTheRun()
    {
        // Act: a load mode together with load steps
        var result = PipelineLoadPlan.Create(Pipeline("Append", Step("BulkInsert", 1, "load")), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("never both");
    }

    [Theory]
    [InlineData("Append")]
    [InlineData("Upsert")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TruncateBeforeLoadBesideAnyModeButReplaceIsRefused(string mode)
    {
        // Arrange
        var pipeline = mode == "Upsert"
            ? Pipeline(mode, OwnLoad(Column("Key", "Id"), Column("Write", "Id")))
            : Pipeline(mode);
        pipeline.TruncateBeforeLoad = true;

        // Act
        var result = PipelineLoadPlan.Create(pipeline, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20229");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TruncateBeforeLoadBesideReplaceSaysTheSameThingAndIsAccepted()
    {
        // Arrange
        var pipeline = Pipeline("Replace");
        pipeline.TruncateBeforeLoad = true;

        // Act / Assert
        Plan(pipeline).TruncatesFirst.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TruncateBeforeLoadBesideLoadStepsIsRefused()
    {
        // Arrange
        var pipeline = Pipeline(null, Step("BulkInsert", 1, "load"));
        pipeline.TruncateBeforeLoad = true;

        // Act
        var result = PipelineLoadPlan.Create(pipeline, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20229");
    }
}
