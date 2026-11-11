using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Tests.Pipelines;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for the gateway source (FDW-1386, FDW-4911): it opens its rows as one stable read through the gateway and sends them out in
/// batches of its batch size; it never pages the read itself.
/// </summary>
public sealed class FlowGatewaySourceTests
{
    private readonly FlowExecutor _sut = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASourceStreamsEveryRowOfADataSetFromOneOpenInBatchesOfItsBatchSize()
    {
        var events = new List<string>();
        var fake = new PagedSourceGateway(PagedSourceGateway.Rows(5), events);
        var batches = new List<int>();
        var rows = new List<IDictionary<string, object?>>();
        var sink = new FlowDestination("dst", (batch, _) =>
        {
            lock (rows)
            {
                batches.Add(batch.Rows.Count);
                rows.AddRange(batch.Rows);
            }

            return Task.FromResult<IGenericResult>(GenericResult.Success());
        });
        var graph = Graph(
            [FlowGatewaySource.ForDataSet("src", fake.Provider.ByName("Main")!, "Orders", pageSize: 2), sink],
            [Link("src", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Ids(rows).ShouldBe([0, 1, 2, 3, 4]);
        batches.ShouldBe([2, 2, 1]);
        events.ShouldBe(["open", "closed after 5"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASourceAsksTheGatewayForAStableReadAndNeverPagesItItself()
    {
        IDataCommand? asked = null;
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataStoreTarget _, CancellationToken _) =>
            {
                asked = command;
                return Task.FromResult<IGenericResult<IRecordSource<DataRecord>>>(
                    GenericResult<IRecordSource<DataRecord>>.Success(new CountingRecordSource([.. PagedSourceGateway.Rows(3)], [])));
            });
        var (sink, rows) = Sink("dst");
        var graph = Graph(
            [FlowGatewaySource.ForContainer("src", gateway.Object, "Store", null, "Orders", pageSize: 2), sink],
            [Link("src", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Count.ShouldBe(3);
        var query = asked.ShouldBeAssignableTo<IQueryCommand>().ShouldNotBeNull();
        query.Paging.ShouldBeNull();
        query.Ordering.ShouldBeNull();
        gateway.Verify(
            g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASourceTheGatewayRefusesFailsTheRunNamingTheSourceAndTheReasonAndSendsNothing()
    {
        var events = new List<string>();
        var fake = new PagedSourceGateway(PagedSourceGateway.Rows(5), events, failReads: true);
        var (sink, rows) = Sink("dst");
        var graph = Graph(
            [FlowGatewaySource.ForDataSet("src", fake.Provider.ByName("Main")!, "Orders", pageSize: 2), sink],
            [Link("src", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        var message = result.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("src");
        message.ShouldContain("source unavailable");
        rows.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AReadThatFailsPartWayFailsTheRunAndReleasesTheRead()
    {
        var events = new List<string>();
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Success(new FailingAfter(2, events)));
        var (sink, _) = Sink("dst");
        var graph = Graph(
            [FlowGatewaySource.ForDataSet("src", gateway.Object, "Orders", pageSize: 10), sink],
            [Link("src", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        var message = result.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("src");
        message.ShouldContain("connection dropped");
        events.ShouldBe(["disposed"]);
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

    // A read that hands out some rows and then fails, as a connection that drops part way does.
    private sealed class FailingAfter(int good, List<string> events) : IRecordSource<DataRecord>
    {
        public RecordSchema Schema { get; } = CountingRecordSource.SchemaOf(["Id"]);

        public IEnumerable<IGenericResult<DataRecord>> Read() => Read(CancellationToken.None).ToBlockingEnumerable();

        public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < good; i++)
                yield return GenericResult<DataRecord>.Success(new DataRecord(Schema, [i]));

            yield return GenericResult<DataRecord>.Failure(new GenericMessage("connection dropped"));
            await Task.CompletedTask;
        }

        public void Dispose() => events.Add("disposed");

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
