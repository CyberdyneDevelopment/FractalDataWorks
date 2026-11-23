using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.DataNodes;
using Fdw.Services.Etl.Flow;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for the component a planner puts in place of a pushed sub-graph (FDW-6969): it sends the store's rows on in the store's order, holds the rows a
/// full join's left side had none for and sends them last, and routes or refuses a lookup's rows with no reference row as the in-memory lookup does.
/// </summary>
public sealed class FlowPushedQueryTests
{
    private static readonly DataStoreTarget Orders = new("Shop", "dbo", "Orders");

    private readonly FlowExecutor _sut = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ARowWithNoStatedSideGoesOutInTheStoresOrder()
    {
        var gateway = Serving(["OrderId", "c_Name"], [[1, "Ada"], [2, null], [3, "Bob"]]);
        var (sink, rows) = Sink("dst");
        var graph = Graph([new FlowPushedQuery("join", gateway.Object, Orders, Query(), "output", null, 2), sink], [Link("join", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Select(row => row["OrderId"]).ShouldBe([1, 2, 3]);
        rows[1].ContainsKey("c_Name").ShouldBeTrue();
        rows[1]["c_Name"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFullJoinsRowsWithNoLeftSideAreSentLastWithoutThePresenceField()
    {
        // A store that sorts nulls first sends the row with no left side first; the in-memory join sends it last.
        var gateway = Serving(["OrderId", "c_Name", "present"], [[null, "Cy", null], [1, "Ada", 1], [2, "Bob", 2]]);
        var (sink, rows) = Sink("dst");
        var absence = new FlowPushedAbsence("present", [], true, null, []);
        var graph = Graph([new FlowPushedQuery("join", gateway.Object, Orders, Query(), "output", absence, 2), sink], [Link("join", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Select(row => row["c_Name"]).ShouldBe(["Ada", "Bob", "Cy"]);
        rows.ShouldAllBe(row => !row.ContainsKey("present"));
        rows[2]["OrderId"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALookupsRowWithNoReferenceRowLeavesByTheUnmatchedOutputWithoutTheBroughtFields()
    {
        var gateway = Serving(["OrderId", "CustomerId", "CustomerName", "present"], [[1, 10, "Ada", 10], [2, 99, null, null]]);
        var (matched, found) = Sink("found");
        var (unmatched, missed) = Sink("missed");
        var absence = new FlowPushedAbsence("present", ["CustomerName"], false, "nomatch", ["CustomerId"]);
        var graph = Graph(
            [new FlowPushedQuery("lookup", gateway.Object, Orders, Query(), "output", absence, 2), matched, unmatched],
            [Link("lookup", "found"), Link("lookup", "missed", output: "nomatch")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        found.Single()["CustomerName"].ShouldBe("Ada");
        found.Single().ContainsKey("present").ShouldBeFalse();
        missed.Single()["OrderId"].ShouldBe(2);
        missed.Single().ContainsKey("CustomerName").ShouldBeFalse();
        missed.Single().ContainsKey("present").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALookupWithNoUnmatchedOutputFailsTheRunOnARowWithNoReferenceRowAsTheInMemoryLookupDoes()
    {
        var gateway = Serving(["OrderId", "CustomerId", "CustomerName", "present"], [[1, 10, "Ada", 10], [2, 99, null, null]]);
        var (sink, _) = Sink("dst");
        var absence = new FlowPushedAbsence("present", ["CustomerName"], false, null, ["CustomerId"]);
        var graph = Graph([new FlowPushedQuery("lookup", gateway.Object, Orders, Query(), "output", absence, 2), sink], [Link("lookup", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(message => message.Code == "FLOW-20916");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AQueryTheGatewayCannotOpenFailsTheRunNamingTheComponent()
    {
        var gateway = new Mock<IDataGateway>(MockBehavior.Strict);
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), Orders, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Failure(new GenericMessage("the store refused the join")));
        var (sink, _) = Sink("dst");
        var graph = Graph([new FlowPushedQuery("join", gateway.Object, Orders, Query(), "output", null, 2), sink], [Link("join", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(message => message.Code == "FLOW-70933");
    }

    private static QueryCommand<Dictionary<string, object?>> Query() => new();

    // A gateway that answers the pushed query with these rows, as the store would after running it.
    private static Mock<IDataGateway> Serving(string[] fields, object?[][] values)
    {
        var schema = new RecordSchema([.. fields.Select((name, index) => new DataField(name, null, null, index, true, DatumTypes.NotYetTyped))]);
        List<IReadOnlyDictionary<string, object?>> rows =
            [.. values.Select(row => (IReadOnlyDictionary<string, object?>)fields.Select((name, index) => (name, row[index])).ToDictionary(pair => pair.name, pair => pair.Item2))];
        var gateway = new Mock<IDataGateway>(MockBehavior.Strict);
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), Orders, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<IRecordSource<DataRecord>>.Success(new RowListRecordSource(schema, rows)));
        return gateway;
    }
}
