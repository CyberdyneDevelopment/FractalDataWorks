using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Tests.Pipelines;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>Tests for the gateway source (FDW-1386): it pages a data set through the gateway and sends each page as a batch.</summary>
public sealed class FlowGatewaySourceTests
{
    private readonly FlowExecutor _sut = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASourceReadsEveryPageOfADataSetAndStopsAfterTheShortOne()
    {
        var events = new List<string>();
        var fake = new PagedSourceGateway(PagedSourceGateway.Rows(5), events);
        var (sink, rows) = Sink("dst");
        var graph = Graph(
            [FlowGatewaySource.ForDataSet("src", fake.Provider.ByName("Main")!, "Orders", pageSize: 2), sink],
            [Link("src", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Count.ShouldBe(5);
        events.ShouldBe(["read@0", "read@2", "read@4"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AReadThatFailsFailsTheRunNamingTheSourceAndSendsNothing()
    {
        var events = new List<string>();
        var fake = new PagedSourceGateway(PagedSourceGateway.Rows(5), events, failReads: true);
        var (sink, rows) = Sink("dst");
        var graph = Graph(
            [FlowGatewaySource.ForDataSet("src", fake.Provider.ByName("Main")!, "Orders", pageSize: 2), sink],
            [Link("src", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("src");
        rows.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASourceWithoutADataSetOrAPageSizeIsRefusedAtConstruction()
    {
        var fake = new PagedSourceGateway([], []);
        var gateway = fake.Provider.ByName("Main")!;

        Should.Throw<System.ArgumentException>(() => FlowGatewaySource.ForDataSet("src", gateway, " ", 10));
        Should.Throw<System.ArgumentOutOfRangeException>(() => FlowGatewaySource.ForDataSet("src", gateway, "Orders", 0));
        Should.Throw<System.ArgumentException>(() => FlowGatewaySource.ForContainer("src", gateway, "Store", null, "", 10));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASourceThatCannotPageAndAnswersItsWholeSetIsReadOnceNotOncePerPage()
    {
        var events = new List<string>();
        var fake = new PagedSourceGateway(PagedSourceGateway.Rows(5), events, ignoresPaging: true);
        var (sink, rows) = Sink("dst");
        var graph = Graph(
            [FlowGatewaySource.ForDataSet("src", fake.Provider.ByName("Main")!, "Orders", pageSize: 2), sink],
            [Link("src", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Count.ShouldBe(5);
        events.ShouldBe(["read@0"]);
    }
}
