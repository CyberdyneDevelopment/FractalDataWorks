using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Results;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Abstractions.DataDestination.Kinds;
using Fdw.Services.Pipelines.Abstractions.DataSource.Kinds;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// A Validate stage whose failing action is Fail stops the run even when the pipeline continues on error (FDW-1120): the
/// continue-on-error setting covers a stage that could not work on a record, not a record the author said must stop the run.
/// </summary>
public sealed class ValidateFailStopsRunTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailingRecordStopsTheRunAndLoadsNothingEvenUnderContinueOnError()
    {
        // Arrange: row 0 passes the check, row 1 fails it
        var events = new List<string>();
        var pipeline = CreatePipeline(Stage("Fail"), PagedSourceGateway.Rows(3), events);

        // Act
        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        events.ShouldNotContain(entry => entry.StartsWith("load", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARejectedRecordIsSteppedPastUnderContinueOnError()
    {
        // Arrange
        var events = new List<string>();
        var pipeline = CreatePipeline(Stage("Reject"), PagedSourceGateway.Rows(3), events);

        // Act
        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        // Assert: only the record that passed the check is loaded
        outcome.IsSuccess.ShouldBeTrue();
        events.ShouldContain("load:1");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void OnlyAStopRunValidationFailureStopsTheRun()
    {
        // Arrange
        var validation = GenericResult<int>.Failure(EtlResultCodes.ByName("RecordFailedValidation"), NullLogger.Instance);
        var other = GenericResult<int>.Failure(EtlResultCodes.ByName("InvalidPipelineStep"), NullLogger.Instance);

        // Assert
        StageFailures.StopsRun(validation).ShouldBeTrue();
        StageFailures.StopsRun(other).ShouldBeFalse();
    }

    private static PipelineTransformConfiguration Stage(string onFail) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Clean",
        OperationType = "Validate",
        ExecutionOrder = 1,
        IsEnabled = true,
        OnFail = onFail,
        Checks = [new PipelineTransformCheckConfiguration { Name = "FirstOnly", Expression = "Id < 1", Ordinal = 1 }],
    };

    private static BatchCopyPipeline CreatePipeline(
        PipelineTransformConfiguration stage,
        IReadOnlyList<Dictionary<string, object?>> source,
        List<string> events)
    {
        // Touch a stage kind so this assembly's kinds are registered before the pipeline looks one up by name
        _ = TransformTypes.ByName("Validate");
        var configuration = new BatchCopyPipelineConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "copy",
            BatchSize = 10,
            ContinueOnError = true,
            LoadMode = "Append",
            SourceKind = new DataSetKind(),
            SourceDataSet = "Source",
            DestinationKind = new DataSetDestinationKind(),
            DestinationDataSet = "Sink",
            Transforms = [stage],
        };
        return new BatchCopyPipeline(configuration, NullLogger<BatchCopyPipeline>.Instance, new PagedSourceGateway(source, events).Provider);
    }
}
