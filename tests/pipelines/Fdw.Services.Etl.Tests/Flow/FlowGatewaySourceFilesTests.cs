using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Tests.Pipelines;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for the gateway source reading a list of files as one stream (FDW-3089): the files go out one after another in the order of the list on the
/// source's one output, a test run's row cap spans the whole stream, and a file whose fields differ from the first file's fails the run naming it.
/// </summary>
public sealed class FlowGatewaySourceFilesTests
{
    private readonly FlowExecutor _sut = new();

    private static Dictionary<string, object?> Row(int id, string name) => new() { ["Id"] = id, ["Name"] = name };

    private static Dictionary<string, object?> Other(int id, string title) => new() { ["Id"] = id, ["Title"] = title };

    // A gateway that reads a declared container at another address: the file named opens as one stable read of its rows (FDW-4911).
    private static IEtlDataGateway Gateway(Dictionary<string, List<Dictionary<string, object?>>> files, List<string> reads)
    {
        var gateway = new Mock<IEtlDataGateway>();
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataStoreTarget _, string? path, string? name, CancellationToken _) =>
            {
                ((IQueryCommand)command).Paging.ShouldBeNull();
                reads.Add($"open {name}");
                return Task.FromResult<IGenericResult<IRecordSource<DataRecord>>>(
                    GenericResult<IRecordSource<DataRecord>>.Success(new CountingRecordSource([.. files[name!]], reads)));
            });
        return gateway.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheFilesAreReadOneAfterAnotherAsOneStreamInTheOrderOfTheList()
    {
        var reads = new List<string>();
        var gateway = Gateway(
            new() { ["a.csv"] = [Row(1, "x"), Row(2, "y"), Row(3, "z")], ["b.csv"] = [Row(4, "p"), Row(5, "q")] }, reads);
        var (sink, rows) = Sink("dst");
        var graph = Graph(
            [FlowGatewaySource.ForContainerFiles("src", gateway, "Landing", "inbound", "Orders", pageSize: 2, files: ["a.csv", "b.csv"]), sink],
            [Link("src", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Ids(rows).ShouldBe([1, 2, 3, 4, 5]);
        reads.ShouldBe(["open a.csv", "closed after 3", "open b.csv", "closed after 2"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARowCapSpansTheWholeStreamAndALaterFileIsNotOpened()
    {
        var reads = new List<string>();
        var gateway = Gateway(
            new() { ["a.csv"] = [Row(1, "x"), Row(2, "y"), Row(3, "z")], ["b.csv"] = [Row(4, "p"), Row(5, "q"), Row(6, "r")], ["c.csv"] = [Row(7, "s")] }, reads);
        var (sink, rows) = Sink("dst");
        var graph = Graph(
            [FlowGatewaySource.ForContainerFiles("src", gateway, "Landing", null, "Orders", pageSize: 10, files: ["a.csv", "b.csv", "c.csv"], rowCap: 4), sink],
            [Link("src", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Ids(rows).ShouldBe([1, 2, 3, 4]);
        reads.ShouldBe(["open a.csv", "closed after 3", "open b.csv", "closed after 1"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFileWithDifferentFieldsFailsTheRunNamingTheFileAndTheFieldsThatDiffer()
    {
        var reads = new List<string>();
        var gateway = Gateway(new() { ["a.csv"] = [Row(1, "x")], ["b.csv"] = [Other(2, "t")] }, reads);
        var (sink, _) = Sink("dst");
        var graph = Graph(
            [FlowGatewaySource.ForContainerFiles("src", gateway, "Landing", null, "Orders", pageSize: 10, files: ["a.csv", "b.csv"]), sink],
            [Link("src", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        var message = result.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("b.csv");
        message.ShouldContain("a.csv");
        message.ShouldContain("missing Name");
        message.ShouldContain("extra Title");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AListOfFilesNeedsTheEtlDataGatewayAndAtLeastOneFile()
    {
        var plain = new PagedSourceGateway([], []).Provider.ByName("Main")!;
        var etl = Gateway(new(), new());

        Should.Throw<ArgumentException>(() => FlowGatewaySource.ForContainerFiles("src", plain, "Landing", null, "Orders", 10, ["a.csv"]));
        Should.Throw<ArgumentOutOfRangeException>(() => FlowGatewaySource.ForContainerFiles("src", etl, "Landing", null, "Orders", 10, []));
    }
}
