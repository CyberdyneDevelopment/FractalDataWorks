using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// <see cref="StreamingPipeline"/> polls its source until it is stopped. Nothing in it removes or
/// acknowledges a source row, so a poll that always read from row 0 would return, and load again, the
/// same first rows every time (FDW-948). These tests read the order of gateway calls.
/// </summary>
public sealed class StreamingPipelineTests
{
    // Reads are capped so that a pipeline that never advances fails the assertion instead of polling forever.
    private const int MaxReads = 6;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteReadsPastTheRowsItHasAlreadyExtracted()
    {
        // Arrange
        var events = new List<string>();
        var source = PagedSourceGateway.Rows(4);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var reads = 0;
        var gateway = new PagedSourceGateway(
            source,
            events,
            onRead: skip =>
            {
                reads++;
                if (skip >= source.Count || reads >= MaxReads)
                {
                    stop.Cancel();
                }
            });
        var pipeline = new StreamingPipeline(Configuration(bufferSize: 2), NullLogger<StreamingPipeline>.Instance, gateway.Provider);

        // Act
        var outcome = await pipeline.Execute(stop.Token);

        // Assert -- each poll starts where the last stopped, and no row is loaded twice.
        outcome.IsSuccess.ShouldBeTrue();
        events.ShouldBe(new[] { "read@0", "load:2", "read@2", "load:2", "read@4" });
        outcome.Value!.RecordsExtracted.ShouldBe(4);
        outcome.Value.RecordsLoaded.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ExecuteInTestModeReadsOnePageFromTheStart()
    {
        // Arrange
        var events = new List<string>();
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(4), events);
        var pipeline = new StreamingPipeline(Configuration(bufferSize: 2), NullLogger<StreamingPipeline>.Instance, gateway.Provider);

        // Act
        var outcome = await pipeline.Execute(PipelineExecutionOptions.DefaultTestMode, TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue();
        events.ShouldBe(new[] { "read@0" });
    }

    private static StreamingPipelineConfiguration Configuration(int bufferSize)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "stream",
            SourceDataSet = "Source",
            DestinationDataSet = "Sink",
            BufferSize = bufferSize,
            FlushIntervalMs = 0,
        };
}
