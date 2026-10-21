using System;
using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for the options a queued run executes with once the host has read the pipeline's watermark (FDW-1982): the run states when it
/// started, and offers the last successful run's start time as the <c>LastSuccessfulRun</c> parameter unless the caller supplied one.
/// </summary>
public sealed class PipelineRunFactsOptionsTests
{
    private static readonly DateTimeOffset Started = new(2026, 10, 19, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Last = new(2026, 10, 18, 14, 0, 0, TimeSpan.Zero);

    private static PipelineExecutionRequest RequestWith(IReadOnlyDictionary<string, object?>? parameters) => new()
    {
        ExecutionId = Guid.NewGuid(),
        PipelineId = Guid.NewGuid(),
        PipelineName = "TestPipeline",
        TriggerSource = "Test",
        RunRoles = ["DataWriter"],
        TenantId = Guid.NewGuid(),
        Parameters = parameters
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheRunStatesWhenItStarted()
    {
        var options = PipelineExecutionBackgroundService.OptionsOf(RequestWith(null), Started, null);

        options.RunStarted.ShouldBe(Started);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ThePipelinesLastSuccessfulRunIsOfferedAsTheParameter()
    {
        var options = PipelineExecutionBackgroundService.OptionsOf(RequestWith(null), Started, Last);

        options.Parameters.ShouldNotBeNull();
        options.Parameters[RunFacts.LastSuccessfulRun].ShouldBe(Last);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AValueTheCallerSuppliedOverridesTheStoredOne()
    {
        var supplied = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var request = RequestWith(new Dictionary<string, object?> { [RunFacts.LastSuccessfulRun] = supplied });

        var options = PipelineExecutionBackgroundService.OptionsOf(request, Started, Last);

        options.Parameters.ShouldNotBeNull();
        options.Parameters[RunFacts.LastSuccessfulRun].ShouldBe(supplied);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheOtherParametersOfTheRequestAreKept()
    {
        var request = RequestWith(new Dictionary<string, object?> { ["File"] = "BreweryMicro" });

        var options = PipelineExecutionBackgroundService.OptionsOf(request, Started, Last);

        options.Parameters.ShouldNotBeNull();
        options.Parameters["File"].ShouldBe("BreweryMicro");
        options.Parameters[RunFacts.LastSuccessfulRun].ShouldBe(Last);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ARunWithNoSuccessfulRunOnRecordOffersNoValue()
    {
        var options = PipelineExecutionBackgroundService.OptionsOf(RequestWith(null), Started, null);

        options.Parameters.ShouldBeNull();
    }
}
