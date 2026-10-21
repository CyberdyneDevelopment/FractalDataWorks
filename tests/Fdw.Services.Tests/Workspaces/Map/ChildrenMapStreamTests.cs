using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-1195: a data set explodes into its fields, searched and paged, joined to the data set by part_of.</summary>
[Collection(nameof(ServicesTestCollection))]
public class ChildrenMapStreamTests
{
    private static readonly Guid OrdersId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

    [Fact]
    public async Task AFieldsExplodeSendsTheFieldsPagedAndSearchedWithEdgesToTheDataSet()
    {
        var request = new MapChildrenRequest { NodeId = MapNodeIds.DataSet(OrdersId), Kinds = "DataSetField", Q = "date", Take = 1 };

        var (nodes, edges, stream) = await Explode(request, "OrderDate", "ShipDate", "Total", "Customer");

        nodes.ShouldHaveSingleItem().Label.ShouldBe("OrderDate");
        nodes[0].Kind.ShouldBe(MapKinds.DataSetField);
        nodes[0].CanExplode.ShouldBeFalse();
        edges.ShouldHaveSingleItem().Target.ShouldBe(MapNodeIds.DataSet(OrdersId));
        edges[0].RelationType.ShouldBe(MapRelations.PartOf);
        stream.Page!.Total.ShouldBe(2);
        stream.Page.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task ASecondPageContinuesWhereTheFirstStopped()
    {
        var request = new MapChildrenRequest { NodeId = MapNodeIds.DataSet(OrdersId), Kinds = "DataSetField", Skip = 1, Take = 5 };

        var (nodes, _, stream) = await Explode(request, "b", "a", "c");

        nodes.Select(node => node.Label).ShouldBe(["b", "c"]);
        stream.Page!.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task WithoutPagingOptionsNoPageIsDescribed()
    {
        var request = new MapChildrenRequest { NodeId = MapNodeIds.DataSet(OrdersId), Kinds = "DataSetField" };

        var (nodes, _, stream) = await Explode(request, "a", "b");

        nodes.Count.ShouldBe(2);
        stream.Page.ShouldBeNull();
    }

    [Fact]
    public void ARefusedExplodeIsACodedForbiddenWithItsReason()
    {
        var code = WorkspacesResultCodes.ByName("MapExplodeNotPermitted");

        code.Severity.IsSuccess.ShouldBeFalse();
        code.Id.ShouldBe(100008);
        var result = GenericResult<MapStream>.Failure(
            code, NullLogger.Instance, ResultDetails.Create("node", "n", "children", "DataSetField", "reason", "the data sets permission"));
        result.IsFailure.ShouldBeTrue();
    }

    private static async Task<(List<MapNodeDto> Nodes, List<MapEdgeDto> Edges, ChildrenMapStream Stream)> Explode(
        MapChildrenRequest request, params string[] fieldNames)
    {
        var dataSet = new Mock<IDataSetImplementationConfiguration>();
        dataSet.SetupGet(d => d.Id).Returns(OrdersId);
        dataSet.SetupGet(d => d.Name).Returns("Orders");
        dataSet.SetupGet(d => d.Fields).Returns(fieldNames.Select(name => new DataSetFieldConfiguration { Id = Guid.NewGuid(), Name = name, TypeName = "string" }).ToList());
        dataSet.SetupGet(d => d.Sources).Returns(new List<DataSetSourceConfiguration>());
        var provider = new Mock<IDataSetConfigurationProvider>();
        provider
            .Setup(p => p.Get(OrdersId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(dataSet.Object));
        var sources = new MapSources(provider.Object, null!, new Mock<Fdw.Services.Pipelines.Abstractions.IPipelineConfigurationProvider>().Object, new Mock<IDataSetVariationConfigurationProvider>().Object, CategoryProviders.Empty());
        var editing = (await MapEditing.Resolve(MapEditingTests.Authorization().Object, MapEditingTests.Caller, false, CancellationToken.None)).Value!;
        var query = MapChildrenQuery.Parse(request, MapKinds.DataSet, NullLogger.Instance).Value!;

        var opened = await ChildrenMapStream.Open(request.NodeId, MapKinds.DataSet, OrdersId, sources, editing, query, NullLogger.Instance, CancellationToken.None);
        var stream = opened.Value!;
        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(CancellationToken.None))
            nodes.Add(node);

        return (nodes, stream.Edges().ToList(), stream);
    }
}
