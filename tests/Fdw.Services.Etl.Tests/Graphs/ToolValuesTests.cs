using System.Collections.Generic;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Transforms;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-1984: a tool's <c>{{Name}}</c> references are resolved from the run's parameters, so a tool whose text names something that is not one of
/// them can only mean a variable an earlier stage calculates. It is built when its stage starts, with the variables laid over the parameters; a tool
/// that names only run parameters is built up front, as before.
/// </summary>
public sealed class ToolValuesTests
{
    private static readonly GraphPipelineConfiguration Graph = new() { Name = "Probe" };

    private static Dictionary<string, object?> Parameters(params (string Name, object? Value)[] values)
    {
        var parameters = new Dictionary<string, object?>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in values)
        {
            parameters[name] = value;
        }

        return parameters;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AFilterThatNamesOnlyRunParametersIsBuiltUpFront()
    {
        var context = ContextWith(Graph, Parameters(("Tag", "micro")));

        GraphFlowBuilder.NamesCalculatedValue(FilterStage("keep", "Label == '{{Tag}}'"), context).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AFilterThatNamesAValueNoRunParameterHoldsIsBuiltWhenItsStageStarts()
    {
        var context = ContextWith(Graph, Parameters(("Tag", "micro")));

        GraphFlowBuilder.NamesCalculatedValue(FilterStage("keep", "Label == '{{Kind}}'"), context).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AValidateCheckThatNamesACalculatedValueIsBuiltWhenItsStageStarts()
    {
        var component = new PipelineComponentConfiguration { Name = "check", ComponentType = "Validate" };
        var stage = new PipelineTransformConfiguration { Name = "check", OperationType = "Validate", IsEnabled = true };
        stage.Checks.Add(new PipelineTransformCheckConfiguration { Name = "Adult", Expression = "Age >= {{MinimumAge}}" });
        component.Stages.Add(stage);

        GraphFlowBuilder.NamesCalculatedValue(component, ContextWith(Graph, Parameters())).ShouldBeTrue();
        GraphFlowBuilder.NamesCalculatedValue(component, ContextWith(Graph, Parameters(("MinimumAge", 18)))).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AToolWithNoReferenceIsBuiltUpFront()
    {
        GraphFlowBuilder.NamesCalculatedValue(FilterStage("keep", "Amount > 100"), ContextWith(Graph, Parameters())).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheVariablesAreLaidOverTheRunParametersAndARunParameterKeepsItsValue()
    {
        var context = ContextWith(Graph, Parameters(("Tag", "micro")));

        var later = context.WithValues(Parameters(("Kind", "kind-micro"), ("Tag", "other")));

        later.Parameters["Kind"].ShouldBe("kind-micro");
        later.Parameters["Tag"].ShouldBe("micro");
        context.Parameters.ContainsKey("Kind").ShouldBeFalse();
    }
}
