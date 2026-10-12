using Fdw.Results;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for the reject steps of <see cref="PipelineLoadValidator"/> (FDW-1142): a reject step is judged as a load step is,
/// writes a kind that can take rejected records, names the two reserved columns, and does not write where the pipeline loads.
/// </summary>
public sealed class PipelineRejectStepValidatorTests
{
    private static PipelineLoadColumnConfiguration Write(string column, int ordinal) =>
        new() { Name = column, Role = "Write", ColumnName = column, Ordinal = ordinal };

    private static PipelineLoadStepConfiguration Reject(string name = "rejects", int order = 1, string destination = "Rejects", string kind = "BulkInsert") => new()
    {
        Name = name,
        Phase = "Reject",
        StepOrder = order,
        Kind = kind,
        DestinationDataSet = destination,
        IsEnabled = true,
        Columns = [Write("Name", 0), Write(RejectedRecordColumns.Reason, 1), Write(RejectedRecordColumns.Stage, 2)],
    };

    private static PipelineLoadStepConfiguration Load(string name = "load", int order = 1, string destination = "Orders") => new()
    {
        Name = name,
        Phase = "Load",
        StepOrder = order,
        Kind = "BulkInsert",
        DestinationDataSet = destination,
        IsEnabled = true,
        Columns = [Write("Name", 0)],
    };

    private static PipelineLoadStepConfiguration OwnLoad() => new() { Name = PipelineLoadStepConfiguration.PipelineLoadName, Phase = "Load", IsEnabled = true };

    private static IGenericResult Check(string? mode, params PipelineLoadStepConfiguration[] rows) =>
        PipelineLoadValidator.Validate(mode, rows, null, "Orders", NullLogger.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARejectStepBesideTheOwnLoadAndBesideLoadStepsIsAccepted()
    {
        Check("Append", OwnLoad(), Reject()).IsSuccess.ShouldBeTrue();
        Check(null, Load(), Reject()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RejectStepsAloneAreNotALoad()
    {
        var result = Check(null, Reject());

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("must state a load mode");
    }

    [Theory]
    [InlineData("Update")]
    [InlineData("Truncate")]
    [InlineData("Execute")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AKindThatCannotWriteRejectedRecordsIsRefusedNamingTheOnesThatCan(string kind)
    {
        var result = Check("Append", OwnLoad(), Reject(kind: kind));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("BulkInsert");
    }

    [Theory]
    [InlineData(RejectedRecordColumns.Reason)]
    [InlineData(RejectedRecordColumns.Stage)]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARejectStepThatLeavesOutAReservedColumnIsRefusedNamingIt(string column)
    {
        var step = Reject();
        step.Columns = [.. step.Columns.Where(c => !string.Equals(c.ColumnName, column, StringComparison.Ordinal))];

        var result = Check("Append", OwnLoad(), step);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(column);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARejectStepWritingALoadStepsDestinationIsRefused()
    {
        var result = Check(null, Load(destination: "Orders"), Reject(destination: "Orders"));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("reject destination");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARejectStepMayNotShareANameWithALoadStep()
    {
        var result = Check(null, Load(name: "same"), Reject(name: "same"));

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RejectStepsAreOrderedFromOneApartFromTheLoadSteps()
    {
        Check(null, Load(order: 1), Reject(name: "a", order: 1), Reject(name: "b", order: 2, destination: "Other")).IsSuccess.ShouldBeTrue();
        Check(null, Load(order: 1), Reject(name: "a", order: 1), Reject(name: "b", order: 3, destination: "Other")).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARejectStepWithoutAStepOrderIsRefused()
    {
        var step = Reject();
        step.StepOrder = null;

        Check("Append", OwnLoad(), step).IsSuccess.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Sideways")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARowThatStatesNoPhaseOrAnUnknownOneIsRefused(string phase)
    {
        var step = Load();
        step.Phase = phase;

        var result = Check(null, step);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("phase");
    }
}
