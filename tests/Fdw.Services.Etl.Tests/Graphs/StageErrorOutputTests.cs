using System.Collections.Generic;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using Fdw.Services.Etl.Transforms;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the error output of a stage component (FDW-1589): a stage that rejects records is built only when its component
/// names an error output to take them, and a stage that rejects none needs none.
/// </summary>
public sealed class StageErrorOutputTests
{
    private static PipelineComponentConfiguration ValidateStage(string onFail, string? errorOutput)
    {
        var component = new PipelineComponentConfiguration { Name = "check", ComponentType = "Validate", ErrorOutput = errorOutput };
        component.Stages.Add(new PipelineTransformConfiguration
        {
            Name = "check",
            OperationType = "Validate",
            OnFail = onFail,
            IsEnabled = true,
            Checks = [new PipelineTransformCheckConfiguration { Name = "Big", Expression = "Id > 3", Ordinal = 1 }],
        });
        return component;
    }

    private static GraphPipelineConfiguration GraphOf(PipelineComponentConfiguration validate)
    {
        List<PipelineDroppedOutputConfiguration> dropped = [Dropped("check")];
        if (validate.ErrorOutput is { } error)
        {
            dropped.Add(Dropped("check", error));
        }

        return Graph([DataSetSource("src"), validate], [Link("src", "check")], [.. dropped]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValidateThatRejectsIsRefusedWhenItsComponentNamesNoErrorOutput()
    {
        var graph = GraphOf(ValidateStage("Reject", null));
        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("errorOutput");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValidateThatRejectsIsBuiltWhenItsComponentNamesAnErrorOutput()
    {
        var graph = GraphOf(ValidateStage("Reject", "bad"));
        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValidateThatFailsTheRunNeedsNoErrorOutput()
    {
        var graph = GraphOf(ValidateStage("Fail", null));
        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
    }
}
