using System;
using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// FDW-1937: the request of a pipeline run that waits for approval is kept until someone takes it, once, and is gone after.
/// </summary>
public sealed class PipelineRunHoldsTests
{
    private static PipelineExecutionRequest Request(Guid executionId) => new()
    {
        ExecutionId = executionId,
        PipelineId = Guid.NewGuid(),
        PipelineName = "Nightly",
        TriggerSource = "Api",
        RunRoles = ["Admin"],
        Parameters = new Dictionary<string, object?> { ["Which"] = "BreweryFeed" },
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AHeldRequestIsTakenOnceWithItsValuesAndIsThenGone()
    {
        var holds = new PipelineRunHolds();
        var id = Guid.NewGuid();
        var request = Request(id);

        holds.Hold(request).ShouldBeTrue();

        holds.Take(id).ShouldBeSameAs(request);
        holds.Take(id).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARunThatIsAlreadyHeldIsNotHeldTwiceAndTheFirstRequestStays()
    {
        var holds = new PipelineRunHolds();
        var id = Guid.NewGuid();
        var first = Request(id);

        holds.Hold(first).ShouldBeTrue();
        holds.Hold(Request(id)).ShouldBeFalse();

        holds.Take(id).ShouldBeSameAs(first);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ARunNothingIsHeldForHasNoRequest()
    {
        new PipelineRunHolds().Take(Guid.NewGuid()).ShouldBeNull();
    }
}
