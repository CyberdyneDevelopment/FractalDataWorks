using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Graphs;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-5298: every way a graph reads data goes through the one gateway the run was built with, so a read of a Graph data set reads each source as
/// the caller whose read it is. A stage tool (a lookup, a compute table bound to a data set) is not given a gateway of its own.
/// </summary>
public sealed class BuildContextGatewayTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5298")]
    public void TheGatewayARunIsBuiltWithIsTheOneTheSourcesAndTheStageToolsReadThrough()
    {
        var gateway = new Mock<IDataGateway>().Object;

        var context = ContextOn(new GraphPipelineConfiguration { Name = "Gas" }, gateway);

        context.Gateway.ShouldBeSameAs(gateway);
        context.NewTransformContext().DataGateway.ShouldBeSameAs(gateway);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5298")]
    public void TwoRunsBuiltWithTwoGatewaysEachKeepTheirOwnAndShareNothing()
    {
        var first = new Mock<IDataGateway>().Object;
        var second = new Mock<IDataGateway>().Object;

        var one = ContextOn(new GraphPipelineConfiguration { Name = "Gas" }, first);
        var other = ContextOn(new GraphPipelineConfiguration { Name = "Gas" }, second);

        one.NewTransformContext().DataGateway.ShouldBeSameAs(first);
        other.NewTransformContext().DataGateway.ShouldBeSameAs(second);
    }
}
