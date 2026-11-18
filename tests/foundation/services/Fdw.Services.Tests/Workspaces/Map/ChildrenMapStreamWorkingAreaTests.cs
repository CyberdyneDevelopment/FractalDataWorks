using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Workspaces.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>
/// FDW-6392: the console map leaves a data store's working areas out. Opening a data store lists no container of a working area, and a data set whose source sits in one
/// shows no container under it and counts nothing unresolved; a source that names a container the store does not hold is still unresolved. A store that names none lists
/// what it listed before.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class ChildrenMapStreamWorkingAreaTests
{
    private static readonly Guid StoreId = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid SetId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

    private static IDataStoreImplementationConfiguration Store(params string[] workingPaths)
    {
        var store = new Mock<IDataStoreImplementationConfiguration>(MockBehavior.Strict);
        store.SetupGet(s => s.Id).Returns(StoreId);
        store.SetupGet(s => s.Name).Returns("Warehouse");
        store.SetupGet(s => s.WorkingPaths).Returns([.. workingPaths]);
        store.SetupGet(s => s.Paths).Returns(
        [
            new DataPathConfiguration { Name = "hist", Containers = [new DataContainerConfiguration { Id = Guid.CreateVersion7(), Name = "Historical" }] },
            new DataPathConfiguration { Name = "con", Containers = [new DataContainerConfiguration { Id = Guid.CreateVersion7(), Name = "Consolidation" }] },
            new DataPathConfiguration { Name = "tmp", Containers = [new DataContainerConfiguration { Id = Guid.CreateVersion7(), Name = "X" }] },
        ]);
        return store.Object;
    }

    private static MapSources Sources(IDataStoreImplementationConfiguration store, IDataSetImplementationConfiguration? dataSet = null)
    {
        var stores = new Mock<IDataStoreConfigurationProvider>(MockBehavior.Strict);
        stores
            .Setup(p => p.Get(StoreId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(store));
        stores
            .Setup(p => p.Get("Warehouse", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(store));
        var dataSets = new Mock<IDataSetConfigurationProvider>(MockBehavior.Strict);
        if (dataSet is not null)
        {
            dataSets
                .Setup(p => p.Get(SetId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(dataSet));
        }

        return new MapSources(
            dataSets.Object,
            stores.Object,
            new Mock<Fdw.Services.Pipelines.Abstractions.IPipelineConfigurationProvider>(MockBehavior.Strict).Object,
            new Mock<IDataSetVariationConfigurationProvider>(MockBehavior.Strict).Object,
            CategoryProviders.Empty());
    }

    private static IDataSetImplementationConfiguration DataSet(params string[] containerNames)
    {
        // Loose, stating each member the stream reads (the interface is wide); the working-area behavior is what the tests hold.
        var dataSet = new Mock<IDataSetImplementationConfiguration>();
        dataSet.SetupGet(d => d.Id).Returns(SetId);
        dataSet.SetupGet(d => d.Name).Returns("Orders");
        dataSet.SetupGet(d => d.Fields).Returns(new List<DataSetFieldConfiguration>());
        dataSet.SetupGet(d => d.Sources).Returns(containerNames
            .Select(name => new DataSetSourceConfiguration { DataStoreName = "Warehouse", ContainerName = name })
            .ToList());
        return dataSet.Object;
    }

    private static async Task<(List<MapNodeDto> Nodes, ChildrenMapStream Stream)> Explode(
        string nodeId, string kind, Guid id, MapSources sources)
    {
        var request = new MapChildrenRequest { NodeId = nodeId };
        var editing = (await MapEditing.Resolve(MapEditingTests.Authorization().Object, MapEditingTests.Caller, false, CancellationToken.None)).Value!;
        var query = MapChildrenQuery.Parse(request, kind, NullLogger.Instance).Value!;
        var opened = await ChildrenMapStream.Open(nodeId, kind, id, sources, editing, query, NullLogger.Instance, CancellationToken.None);
        var stream = opened.Value!;
        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(CancellationToken.None))
            nodes.Add(node);

        return (nodes, stream);
    }

    [Fact]
    public async Task ADataStoreThatNamesAWorkingAreaListsNoContainerOfIt()
    {
        var (nodes, _) = await Explode(MapNodeIds.DataStore(StoreId), MapKinds.DataStore, StoreId, Sources(Store("tmp")));

        nodes.Select(node => node.Label).Order(StringComparer.Ordinal).ToList().ShouldBe(["Consolidation", "Historical"]);
    }

    [Fact]
    public async Task TheWorkingAreaIsMatchedWhateverTheCase()
    {
        var (nodes, _) = await Explode(MapNodeIds.DataStore(StoreId), MapKinds.DataStore, StoreId, Sources(Store("TMP")));

        nodes.Select(node => node.Label).ShouldNotContain("X");
    }

    [Fact]
    public async Task ADataStoreThatNamesNoWorkingAreaListsEveryContainerAsBefore()
    {
        var (nodes, _) = await Explode(MapNodeIds.DataStore(StoreId), MapKinds.DataStore, StoreId, Sources(Store()));

        nodes.Select(node => node.Label).Order(StringComparer.Ordinal).ToList().ShouldBe(["Consolidation", "Historical", "X"]);
    }

    [Fact]
    public async Task ADataSetWhoseSourceSitsInAWorkingAreaShowsNoContainerAndCountsNothingUnresolved()
    {
        var store = Store("tmp");
        var (nodes, stream) = await Explode(MapNodeIds.DataSet(SetId), MapKinds.DataSet, SetId, Sources(store, DataSet("X")));

        nodes.ShouldBeEmpty();
        stream.State.Unresolved.ShouldBe(0);
    }

    [Fact]
    public async Task ADataSetWhoseSourceSitsInANormalPathShowsItsContainer()
    {
        var (nodes, _) = await Explode(MapNodeIds.DataSet(SetId), MapKinds.DataSet, SetId, Sources(Store("tmp"), DataSet("Historical")));

        nodes.ShouldHaveSingleItem().Label.ShouldBe("Historical");
    }

    [Fact]
    public async Task ASourceThatNamesAContainerTheStoreDoesNotHoldIsStillUnresolved()
    {
        var (nodes, stream) = await Explode(MapNodeIds.DataSet(SetId), MapKinds.DataSet, SetId, Sources(Store("tmp"), DataSet("Missing")));

        nodes.ShouldBeEmpty();
        stream.State.Unresolved.ShouldBe(1);
    }
}
