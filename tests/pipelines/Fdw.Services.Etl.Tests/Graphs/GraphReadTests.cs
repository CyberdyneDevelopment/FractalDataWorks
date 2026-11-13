using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for reading a graph (FDW-5291): the graph has no destination, a read adds one component after the output that carries the rows, and
/// the rows that reach it are the rows of the read.
/// </summary>
public sealed class GraphReadTests
{
    private readonly FlowExecutor _executor = new();

    private static GraphReadTerminal Collect(string component, List<IDictionary<string, object?>> rows) =>
        new(component, "output", (batch, _) =>
        {
            rows.AddRange(batch.Rows);
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        });

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl.FDW5291")]
    public async Task ARunOfAGraphWithNoDestinationHandsTheRowsOfTheTerminalOutputToTheTerminal()
    {
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(5), []);
        var graph = Graph([DataSetSource("src"), Mapped("map", ("Key", "Id"))], [Link("src", "map")]);
        var rows = new List<IDictionary<string, object?>>();

        var built = GraphFlowBuilder.BuildRead(graph, Context(graph, gateway), Collect("map", rows));
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var ran = await built.Value!.Run(StepContext(new FlowVariables(), new FlowRunTally(_executor)), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        rows.Count.ShouldBe(5);
        rows.ShouldAllBe(row => row.ContainsKey("Key"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl.FDW5291")]
    public async Task AWriteThatFailsStopsTheRunAndTheRunAnswersThatFailure()
    {
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(5), []);
        var graph = Graph([DataSetSource("src")], []);
        var terminal = new GraphReadTerminal("src", "output", (_, _) => Task.FromResult<IGenericResult>(GenericResult.Failure(new GenericMessage("the read stopped"))));

        var built = GraphFlowBuilder.BuildRead(graph, Context(graph, gateway), terminal);
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var ran = await built.Value!.Run(StepContext(new FlowVariables(), new FlowRunTally(_executor)), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStoredComponentWithTheNameTheReadUsesIsRefusedAsARepeatedName()
    {
        var gateway = new PagedSourceGateway([], []);
        var graph = Graph([DataSetSource("$read")], []);

        var built = GraphFlowBuilder.BuildRead(graph, Context(graph, gateway), Collect("$read", []));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("$read");
    }
}
