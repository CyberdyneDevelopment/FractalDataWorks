using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Workspaces.Results;
using Fdw.Services.Etl;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-1029: the overview sends each object once, joins only what it sent, and counts what it sent.</summary>
[Collection(nameof(ServicesTestCollection))]
public class OverviewMapStreamTests
{
    private static readonly Guid OrdersId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid CustomersId = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    private static readonly Guid WarehouseId = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid LoadId = Guid.Parse("00000000-0000-0000-0000-0000000000c1");

    private static readonly IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>> NoVariations =
        new Dictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>();

    [Fact]
    public async Task EachObjectIsSentOnceAndEdgesJoinOnlyNodesThatWereSent()
    {
        var orders = DataSet(OrdersId, "Orders", ["Warehouse", "Elsewhere"]);
        var customers = DataSet(CustomersId, "Customers", []);
        var selection = new FixedSelection(
            [Pipeline(LoadId, "Load", "Orders", "Customers")],
            [orders, customers, orders],
            [DataStore(WarehouseId, "Warehouse")],
            NoVariations);
        var stream = new OverviewMapStream(selection, null!, await Editing("datasets:write"), NullLogger.Instance);

        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
            nodes.Add(node);
        var edges = stream.Edges().ToList();

        nodes.Select(node => node.Id).Order(StringComparer.Ordinal).ShouldBe(
            new[] { MapNodeIds.DataSet(OrdersId), MapNodeIds.DataSet(CustomersId), MapNodeIds.DataStore(WarehouseId), MapNodeIds.Pipeline(LoadId) }
                .Order(StringComparer.Ordinal));

        // Stored in the one data store that was sent; "Elsewhere" was not, so it has no edge to nothing.
        edges.Select(edge => edge.RelationType).Order(StringComparer.Ordinal).ShouldBe(
            new[] { MapRelations.ConsumedBy, MapRelations.Produces, MapRelations.StoredIn }.Order(StringComparer.Ordinal));
        edges.ShouldAllBe(edge => stream.State.HasNode(edge.Source) && stream.State.HasNode(edge.Target));

        // The caller holds datasets:write only: the data set is editable, the data store and pipeline are not.
        nodes.Single(node => node.Kind == MapKinds.DataSet && node.Label == "Orders").CanEdit.ShouldBeTrue();
        nodes.Single(node => node.Kind == MapKinds.DataStore).CanEdit.ShouldBeFalse();
        nodes.Single(node => node.Kind == MapKinds.Pipeline).CanEdit.ShouldBeFalse();

        stream.State.NodeCount.ShouldBe(4);
        stream.State.EdgeCount.ShouldBe(3);
        stream.State.Outcome.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ADataSetCarriesItsVariationsAsTokensNotNodes()
    {
        var variations = new Dictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>
        {
            [OrdersId] = [new WorkspaceMapVariationCountDto { Kind = "Scoped", Count = 2 }],
        };
        var selection = new FixedSelection([], [DataSet(OrdersId, "Orders", [])], [], variations);
        var stream = new OverviewMapStream(selection, null!, await Editing(), NullLogger.Instance);

        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
            nodes.Add(node);

        var orders = nodes.ShouldHaveSingleItem();
        orders.Kind.ShouldBe(MapKinds.DataSet);
        orders.CanExplode.ShouldBeTrue();
        orders.Variations.ShouldHaveSingleItem().Count.ShouldBe(2);
    }

    [Fact]
    public async Task AFailedVariationReadEndsTheStreamAndSaysSo()
    {
        var selection = new FixedSelection(
            [], [DataSet(OrdersId, "Orders", [])], [], NoVariations,
            GenericResult.Failure(WorkspacesResultCodes.ByName("MapRequestInvalid"), ResultDetails.Create("reason", "the variations could not be read")));
        var stream = new OverviewMapStream(selection, null!, await Editing(), NullLogger.Instance);

        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
            nodes.Add(node);

        nodes.ShouldBeEmpty();
        stream.State.Outcome.IsFailure.ShouldBeTrue();
    }

    private static async Task<MapEditing> Editing(params string[] granted)
    {
        var editing = await MapEditing.Resolve(MapEditingTests.Authorization(granted).Object, MapEditingTests.Caller, mayEditWorkspace: false, CancellationToken.None);
        return editing.Value!;
    }

    private static IDataSetImplementationConfiguration DataSet(Guid id, string name, string[] dataStoreNames)
    {
        var dataSet = new Mock<IDataSetImplementationConfiguration>();
        dataSet.SetupGet(d => d.Id).Returns(id);
        dataSet.SetupGet(d => d.Name).Returns(name);
        dataSet.SetupGet(d => d.Category).Returns("Sales");
        dataSet.SetupGet(d => d.Description).Returns(string.Empty);
        dataSet.SetupGet(d => d.Version).Returns("1.0");
        dataSet.SetupGet(d => d.RecordTypeName).Returns(string.Empty);
        dataSet.SetupGet(d => d.SourceDataSetName).Returns((string?)null);
        dataSet.SetupGet(d => d.Sources).Returns(dataStoreNames
            .Select(dataStoreName => new DataSetSourceConfiguration { DataStoreName = dataStoreName })
            .ToList());
        return dataSet.Object;
    }

    private static IDataStoreImplementationConfiguration DataStore(Guid id, string name)
    {
        var dataStore = new Mock<IDataStoreImplementationConfiguration>();
        dataStore.SetupGet(d => d.Id).Returns(id);
        dataStore.SetupGet(d => d.Name).Returns(name);
        dataStore.SetupGet(d => d.Implementation).Returns("MsSql");
        dataStore.SetupGet(d => d.IsActive).Returns(true);
        return dataStore.Object;
    }

    private static IPipelineImplementationConfiguration Pipeline(Guid id, string name, string source, string destination)
    {
        var pipeline = new Mock<IEtlPipelineImplementationConfiguration>();
        pipeline.SetupGet(p => p.Id).Returns(id);
        pipeline.SetupGet(p => p.Name).Returns(name);
        pipeline.SetupGet(p => p.Implementation).Returns("BatchCopy");
        pipeline.SetupGet(p => p.IsEnabled).Returns(true);
        pipeline.SetupGet(p => p.SourceDataSet).Returns(source);
        pipeline.SetupGet(p => p.DestinationDataSet).Returns(destination);
        return pipeline.Object;
    }

    // A selection that yields exactly what it was given, so the stream is tested apart from every provider.
    private sealed class FixedSelection(
        IReadOnlyList<IPipelineImplementationConfiguration> pipelines,
        IReadOnlyList<IDataSetImplementationConfiguration> dataSets,
        IReadOnlyList<IDataStoreImplementationConfiguration> dataStores,
        IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>> variations,
        IGenericResult? failure = null) : MapSelection
    {
        public override MapScopeDto Describe() => new() { Kind = MapScopeKinds.Everything };

        public override IAsyncEnumerable<IPipelineImplementationConfiguration> Pipelines(MapSources sources, MapState state, CancellationToken ct)
            => Items(pipelines);

        public override IAsyncEnumerable<IDataSetImplementationConfiguration> DataSets(MapSources sources, MapState state, CancellationToken ct)
            => Items(dataSets);

        public override IAsyncEnumerable<IDataStoreImplementationConfiguration> DataStores(MapSources sources, MapState state, CancellationToken ct)
            => Items(dataStores);

        public override Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>> VariationCounts(
            MapSources sources, CancellationToken ct)
            => Task.FromResult(failure is not null
                ? failure.ToNewResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>()
                : GenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>.Success(variations));

        private static async IAsyncEnumerable<T> Items<T>(IReadOnlyList<T> items)
        {
            foreach (var item in items)
                yield return item;

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
