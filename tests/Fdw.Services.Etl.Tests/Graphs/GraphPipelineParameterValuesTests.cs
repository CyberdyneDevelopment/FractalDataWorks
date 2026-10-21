using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the parameter values a graph run builds its components with (FDW-1746): the run's own, found by name whatever the case,
/// and none when the run was started with none.
/// </summary>
public sealed class GraphPipelineParameterValuesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheValuesTheRunWasStartedWithAreFoundByNameWhateverTheCase()
    {
        var options = new PipelineExecutionOptions
        {
            Parameters = new Dictionary<string, object?> { ["Which"] = "BreweryFeed", ["Size"] = 50 },
        };

        var values = GraphPipeline.ParameterValues(options);

        values["which"].ShouldBe("BreweryFeed");
        values["SIZE"].ShouldBe(50);
        values.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARunStartedWithNoValuesHasNone()
    {
        GraphPipeline.ParameterValues(new PipelineExecutionOptions()).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ChangingTheCopyLeavesTheRunsOptionsAlone()
    {
        var supplied = new Dictionary<string, object?> { ["Which"] = "BreweryFeed" };
        var values = GraphPipeline.ParameterValues(new PipelineExecutionOptions { Parameters = supplied });

        values["Which"] = "BreweryRejects";

        supplied["Which"].ShouldBe("BreweryFeed");
    }
}
