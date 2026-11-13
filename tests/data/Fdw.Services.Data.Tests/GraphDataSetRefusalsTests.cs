using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>FDW-5287: a Graph data set's graph refuses a destination, an action and a loop, each with its own result code, and accepts the kinds a data set graph allows.</summary>
public sealed class GraphDataSetRefusalsTests
{
    private static GraphDataSetConfiguration GraphWith(params string[] kinds)
    {
        var graph = new GraphDataSetConfiguration { Name = "GasPrices" };
        foreach (var kind in kinds)
        {
            graph.Components.Add(new GraphDataSetComponentConfiguration { Name = kind + "1", ComponentType = kind });
        }

        return graph;
    }

    [Theory]
    [InlineData("ContainerDestination", typeof(GraphDataSetDestinationRefusedCode))]
    [InlineData("DataSetDestination", typeof(GraphDataSetDestinationRefusedCode))]
    [InlineData("ExecuteSql", typeof(GraphDataSetActionRefusedCode))]
    [InlineData("ExecutePipeline", typeof(GraphDataSetActionRefusedCode))]
    [InlineData("Notify", typeof(GraphDataSetActionRefusedCode))]
    [InlineData("SetVariable", typeof(GraphDataSetActionRefusedCode))]
    [InlineData("ReadDataOptions", typeof(GraphDataSetActionRefusedCode))]
    [InlineData("DoWhile", typeof(GraphDataSetLoopRefusedCode))]
    [InlineData("ForEach", typeof(GraphDataSetLoopRefusedCode))]
    [InlineData("ForLoop", typeof(GraphDataSetLoopRefusedCode))]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5287")]
    public void AComponentOfARefusedKindAnswersItsFamilysOwnCode(string kind, System.Type code)
    {
        var result = GraphDataSetRefusals.Check(GraphWith("ContainerSource", kind), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType(code);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5287")]
    public void TheKindsADataSetGraphAllowsAreNotRefused()
    {
        var result = GraphDataSetRefusals.Check(
            GraphWith("ContainerSource", "DataSetSource", "SqlSource", "MergeJoin", "Lookup", "Filter", "Aggregate", "Calculate", "ConditionalSplit", "UnionAll"),
            NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5287")]
    public void AnAbstractGraphDataSetWithNoComponentsIsNotRefused()
    {
        GraphDataSetRefusals.Check(new GraphDataSetConfiguration { Name = "Abstract" }, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }
}
