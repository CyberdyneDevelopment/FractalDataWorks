using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Workspaces.Results;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Graphs;
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
        var stream = new OverviewMapStream(selection, Sources(), await Editing("datasets:write"), NullLogger.Instance);

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
        var stream = new OverviewMapStream(selection, Sources(), await Editing(), NullLogger.Instance);

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
        var stream = new OverviewMapStream(selection, Sources(), await Editing(), NullLogger.Instance);

        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
            nodes.Add(node);

        nodes.ShouldBeEmpty();
        stream.State.Outcome.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task ADataSetNodeNamesThePathOfTheCategoryItSitsUnder()
    {
        var sports = Guid.NewGuid();
        var nfl = Guid.NewGuid();
        var categories = CategoryProviders.Holding(
            CategoryProviders.Node(sports, null, "sports", "Sports"),
            CategoryProviders.Node(nfl, sports, "nfl", "NFL"));
        var selection = new FixedSelection(
            [], [DataSet(OrdersId, "Orders", [], nfl), DataSet(CustomersId, "Customers", [])], [], NoVariations);
        var stream = new OverviewMapStream(selection, Sources(categories), await Editing(), NullLogger.Instance);

        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
            nodes.Add(node);

        nodes.Single(node => node.Label == "Orders").Category.ShouldBe("Sports / NFL");
        nodes.Single(node => node.Label == "Customers").Category.ShouldBeNull();
        stream.State.Outcome.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ADataSetThatCitesACategoryNotInTheTreeEndsTheStreamAndSaysSo()
    {
        var selection = new FixedSelection([], [DataSet(OrdersId, "Orders", [], Guid.NewGuid())], [], NoVariations);
        var stream = new OverviewMapStream(selection, Sources(), await Editing(), NullLogger.Instance);

        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
            nodes.Add(node);

        nodes.ShouldBeEmpty();
        stream.State.Outcome.IsFailure.ShouldBeTrue();
        stream.State.Outcome.Code.ShouldNotBeNull().Name.ShouldBe("CategoryTreeUnsound");

        // The node that failed was never sent, so the state does not claim it: no edge can be drawn to it.
        stream.State.HasNode(MapNodeIds.DataSet(OrdersId)).ShouldBeFalse();
        stream.State.NodeCount.ShouldBe(0);
    }

    [Fact]
    public async Task AStreamThatDrawsNoCategoryDoesNotReadTheTreeNorFailBecauseOfIt()
    {
        var failure = GenericResult.Failure(WorkspacesResultCodes.ByName("MapRequestInvalid"), ResultDetails.Create("reason", "the categories could not be read"));
        var selection = new FixedSelection([], [DataSet(OrdersId, "Orders", [], Guid.NewGuid())], [], NoVariations);
        var stream = new OverviewMapStream(
            selection, Sources(CategoryProviders.Failing(failure)), await Editing(), NullLogger.Instance, readVariations: false, readCategories: false);

        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
            nodes.Add(node);

        // Not read means no category on the node, which is not the same as sitting under none.
        nodes.ShouldHaveSingleItem().Category.ShouldBeNull();
        stream.State.Outcome.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AnUnreadableCategoryTreeEndsTheStreamAfterThePipelinesAndSaysSo()
    {
        var failure = GenericResult.Failure(WorkspacesResultCodes.ByName("MapRequestInvalid"), ResultDetails.Create("reason", "the categories could not be read"));
        var selection = new FixedSelection(
            [Pipeline(LoadId, "Load", "Orders", "Customers")], [DataSet(OrdersId, "Orders", [])], [], NoVariations);
        var stream = new OverviewMapStream(selection, Sources(CategoryProviders.Failing(failure)), await Editing(), NullLogger.Instance);

        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
            nodes.Add(node);

        nodes.ShouldHaveSingleItem().Kind.ShouldBe(MapKinds.Pipeline);
        stream.State.Outcome.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task ADataSetNodeNamesTheWorkspaceItSitsInAndOneInNoneNamesNone()
    {
        // FDW-4345: the console groups the Data map by workspace and reads workspaceId and workspace off the data set node.
        var workspaceId = Guid.Parse("00000000-0000-0000-0000-0000000000d1");
        var selection = new FixedSelection([], [DataSet(OrdersId, "Orders", []), DataSet(CustomersId, "Customers", [])], [], NoVariations);
        var inWorkspaces = new Dictionary<Guid, MapWorkspaceRef> { [OrdersId] = new(workspaceId, "Sales") };
        var stream = new OverviewMapStream(selection, Sources(), await Editing(), NullLogger.Instance, dataSetWorkspaces: inWorkspaces);

        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
            nodes.Add(node);

        var orders = nodes.Single(node => node.Label == "Orders");
        orders.Metadata["workspaceId"].ShouldBe(workspaceId.ToString("D"));
        orders.Metadata["workspace"].ShouldBe("Sales");
        var customers = nodes.Single(node => node.Label == "Customers");
        customers.Metadata.ContainsKey("workspaceId").ShouldBeFalse();
        customers.Metadata.ContainsKey("workspace").ShouldBeFalse();
    }

    [Fact]
    public async Task AStreamGivenNoWorkspacesPutsNoWorkspaceOnAnyNode()
    {
        var selection = new FixedSelection([], [DataSet(OrdersId, "Orders", [])], [], NoVariations);
        var stream = new OverviewMapStream(selection, Sources(), await Editing(), NullLogger.Instance);

        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
            nodes.Add(node);

        nodes.Single().Metadata.ContainsKey("workspace").ShouldBeFalse();
    }

    [Fact]
    public async Task APipelineNodeNamesItsLatestRunsStateAndOneThatNeverRanNamesNone()
    {
        // FDW-4345: the console marks a pipeline whose latest run Failed; a pipeline with no run is not marked as one that did not fail.
        var runs = new Mock<IExecutionTracker>();
        runs.Setup(r => r.GetLatestRuns(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<string, IExecutionItem>>.Success(
                new Dictionary<string, IExecutionItem> { ["Load"] = Run(ExecutionStateTypes.Failed), ["Nightly"] = Run(ExecutionStateTypes.Completed) }));
        var selection = new FixedSelection(
            [Pipeline(LoadId, "Load", "Orders", "Customers"), Pipeline(Guid.Parse("00000000-0000-0000-0000-0000000000c2"), "Nightly", "Orders", "Customers"),
             Pipeline(Guid.Parse("00000000-0000-0000-0000-0000000000c3"), "NeverRan", "Orders", "Customers")],
            [], [], NoVariations);
        var stream = new OverviewMapStream(selection, Sources(), await Editing(), NullLogger.Instance, runs: runs.Object);

        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
            nodes.Add(node);

        var load = nodes.Single(node => node.Label == "Load");
        load.Metadata["lastRunState"].ShouldBe("Failed");
        load.Metadata["failed"].ShouldBe(true);
        var nightly = nodes.Single(node => node.Label == "Nightly");
        nightly.Metadata["lastRunState"].ShouldBe("Completed");
        nightly.Metadata["failed"].ShouldBe(false);
        var neverRan = nodes.Single(node => node.Label == "NeverRan");
        neverRan.Metadata.ContainsKey("lastRunState").ShouldBeFalse();
        neverRan.Metadata.ContainsKey("failed").ShouldBeFalse();
    }

    [Fact]
    public async Task TheLatestRunsOfAllThePipelinesAreReadOnceNotOncePerPipeline()
    {
        var runs = new Mock<IExecutionTracker>();
        runs.Setup(r => r.GetLatestRuns(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<string, IExecutionItem>>.Success(new Dictionary<string, IExecutionItem>()));
        var pipelines = Enumerable.Range(1, 30)
            .Select(i => Pipeline(Guid.Parse($"00000000-0000-0000-0000-{i:D12}"), "P" + i, "Orders", "Customers"))
            .ToArray();
        var stream = new OverviewMapStream(new FixedSelection(pipelines, [], [], NoVariations), Sources(), await Editing(), NullLogger.Instance, runs: runs.Object);

        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
        {
            node.ShouldNotBeNull();
        }

        runs.Verify(r => r.GetLatestRuns(It.Is<IReadOnlyCollection<string>>(names => names.Count == 30), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ALatestRunsReadThatFailsFailsTheMap()
    {
        var runs = new Mock<IExecutionTracker>();
        runs.Setup(r => r.GetLatestRuns(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(WorkspacesResultCodes.ByName("MapRequestInvalid"), ResultDetails.Create("reason", "the runs could not be read")).ToNewResult<IReadOnlyDictionary<string, IExecutionItem>>());
        var stream = new OverviewMapStream(
            new FixedSelection([Pipeline(LoadId, "Load", "Orders", "Customers")], [], [], NoVariations), Sources(), await Editing(), NullLogger.Instance, runs: runs.Object);

        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
        {
            node.ShouldNotBeNull();
        }

        stream.State.Outcome.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AStreamGivenNoTrackerPutsNoRunStateOnAnyPipeline()
    {
        var selection = new FixedSelection([Pipeline(LoadId, "Load", "Orders", "Customers")], [], [], NoVariations);
        var stream = new OverviewMapStream(selection, Sources(), await Editing(), NullLogger.Instance);

        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(TestContext.Current.CancellationToken))
            nodes.Add(node);

        nodes.Single().Metadata.ContainsKey("failed").ShouldBeFalse();
    }

    private static IExecutionItem Run(IExecutionStateType state)
    {
        var run = new Mock<IExecutionItem>();
        run.SetupGet(r => r.Id).Returns(Guid.NewGuid());
        run.SetupGet(r => r.State).Returns(state);
        return run.Object;
    }

    private static MapSources Sources(ICategoryConfigurationProvider? categories = null)
        => new(null!, null!, null!, null!, categories ?? CategoryProviders.Empty());

    private static async Task<MapEditing> Editing(params string[] granted)
    {
        var editing = await MapEditing.Resolve(MapEditingTests.Authorization(granted).Object, MapEditingTests.Caller, mayEditWorkspace: false, CancellationToken.None);
        return editing.Value!;
    }

    private static IDataSetImplementationConfiguration DataSet(Guid id, string name, string[] dataStoreNames, Guid? categoryId = null)
    {
        var dataSet = new Mock<IDataSetImplementationConfiguration>();
        dataSet.SetupGet(d => d.Id).Returns(id);
        dataSet.SetupGet(d => d.Name).Returns(name);
        dataSet.SetupGet(d => d.Description).Returns(string.Empty);
        dataSet.SetupGet(d => d.Version).Returns("1.0");
        dataSet.SetupGet(d => d.CategoryId).Returns(categoryId);
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

    private static IPipelineImplementationConfiguration Pipeline(Guid id, string name, string source, string destination) => new GraphPipelineConfiguration
    {
        Id = id,
        Name = name,
        Implementation = "Graph",
        IsEnabled = true,
        Components =
        [
            new PipelineComponentConfiguration { Name = "src", ComponentType = "DataSetSource", DataSetName = source },
            new PipelineComponentConfiguration { Name = "out", ComponentType = "DataSetDestination", DataSetName = destination },
        ],
    };

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
