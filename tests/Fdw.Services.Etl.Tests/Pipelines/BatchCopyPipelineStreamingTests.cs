using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Abstractions.DataDestination.Kinds;
using Fdw.Services.Pipelines.Abstractions.DataSource.Kinds;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// <see cref="BatchCopyPipeline"/> moves the source through extract, transform and load a page at a
/// time instead of holding the whole source between phases (FDW-946). These tests read the order in
/// which the gateway is called, because that order is the behavior: a page is loaded before the next is
/// read, and truncation lands between the first read and the first write, never before a read that
/// could still fail.
/// </summary>
public sealed class BatchCopyPipelineStreamingTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ExecuteLoadsEachPageBeforeReadingTheNext()
    {
        // Arrange
        var events = new List<string>();
        var pipeline = CreatePipeline(Configuration(batchSize: 2), PagedSourceGateway.Rows(5), events);

        // Act
        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue();
        events.ShouldBe(new[] { "read@0", "load:2", "read@2", "load:2", "read@4", "load:1" });
        outcome.Value!.RecordsExtracted.ShouldBe(5);
        outcome.Value.RecordsTransformed.ShouldBe(5);
        outcome.Value.RecordsLoaded.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ExecuteStopsReadingAfterAnEmptyPageWhenTheSourceFillsWholePages()
    {
        // Arrange
        var events = new List<string>();
        var pipeline = CreatePipeline(Configuration(batchSize: 2), PagedSourceGateway.Rows(4), events);

        // Act
        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue();
        events.ShouldBe(new[] { "read@0", "load:2", "read@2", "load:2", "read@4" });
        outcome.Value!.RecordsLoaded.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteTruncatesOnceAfterTheFirstReadAndBeforeTheFirstLoad()
    {
        // Arrange
        var events = new List<string>();
        var pipeline = CreatePipeline(Configuration(batchSize: 2, truncate: true), PagedSourceGateway.Rows(5), events);

        // Act
        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue();
        events.ShouldBe(new[] { "read@0", "truncate", "load:2", "read@2", "load:2", "read@4", "load:1" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteDoesNotTruncateWhenTheSourceIsEmpty()
    {
        // Arrange
        var events = new List<string>();
        var pipeline = CreatePipeline(Configuration(batchSize: 2, truncate: true), PagedSourceGateway.Rows(0), events);

        // Act
        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue();
        events.ShouldBe(new[] { "read@0" });
        outcome.Value!.RecordsLoaded.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteDoesNotTruncateWhenTheFirstReadFails()
    {
        // Arrange
        var events = new List<string>();
        var pipeline = CreatePipeline(Configuration(batchSize: 2, truncate: true), PagedSourceGateway.Rows(5), events, failReads: true);

        // Act
        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        events.ShouldBe(new[] { "read@0" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteDoesNotTruncateWhenTheFirstPageFailsToTransform()
    {
        // Arrange
        var events = new List<string>();
        var configuration = Configuration(batchSize: 2, truncate: true);
        configuration.Transforms =
        [
            new PipelineTransformConfiguration
            {
                Id = Guid.NewGuid(),
                Name = "Unregistered",
                OperationType = "NoSuchTransform",
                ExecutionOrder = 1,
                IsEnabled = true,
            },
        ];
        var pipeline = CreatePipeline(configuration, PagedSourceGateway.Rows(5), events);

        // Act
        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        events.ShouldBe(new[] { "read@0" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ExecuteInTestModeReadsEveryPageButWritesNothing()
    {
        // Arrange
        var events = new List<string>();
        var pipeline = CreatePipeline(Configuration(batchSize: 2, truncate: true), PagedSourceGateway.Rows(5), events);

        // Act
        var outcome = await pipeline.Execute(PipelineExecutionOptions.DefaultTestMode, TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue();
        events.ShouldBe(new[] { "read@0", "read@2", "read@4" });
        outcome.Value!.RecordsExtracted.ShouldBe(5);
        outcome.Value.RecordsLoaded.ShouldBe(5);
    }

    private static BatchCopyPipeline CreatePipeline(
        BatchCopyPipelineConfiguration configuration,
        IReadOnlyList<Dictionary<string, object?>> source,
        List<string> events,
        bool failReads = false)
        => new(configuration, NullLogger<BatchCopyPipeline>.Instance, new PagedSourceGateway(source, events, failReads).Provider);

    private static BatchCopyPipelineConfiguration Configuration(int batchSize, bool truncate = false)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "copy",
            BatchSize = batchSize,

            // A pipeline states its load mode (FDW-1109); truncating before the load is what Replace means.
            LoadMode = truncate ? "Replace" : "Append",
            TruncateBeforeLoad = truncate,
            SourceKind = new DataSetKind(),
            SourceDataSet = "Source",
            DestinationKind = new DataSetDestinationKind(),
            DestinationDataSet = "Sink",
        };
}
