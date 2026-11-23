using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Tests.Workspaces.Lineage;
using Fdw.Services.Workspaces.Lineage;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using static Fdw.Services.Tests.Workspaces.Lineage.LineageTestGraphs;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-4404: the lineage reads an edge's relation through a collection, so a relation the map did not build is refused and never left out.</summary>
[Collection(nameof(ServicesTestCollection))]
public class MapRelationOptionsTests
{

    [Theory]
    [InlineData(MapRelations.PartOf)]
    [InlineData(MapRelations.RelatesTo)]
    public void ARelationThatMovesNoDataAddsNothingToTheLineage(string relation)
    {
        var graph = LineageProjection.Of(
            [NewDataSet(1, "Orders"), NewDataSet(2, "Orders 2")],
            [MapEdges.Between(relation, DataSetId(1), DataSetId(2))],
            null,
            NullLogger.Instance);

        graph.IsSuccess.ShouldBeTrue();
        graph.Value!.Neighbourhood(DataSetId(1), LineageDirections.Both, 1, KindsOf("DataSet"), LineageLimitsForTests.MaxNodes).Flows.ShouldBeEmpty();
    }

    [Fact]
    public void AnEdgeWithARelationNoOptionHoldsRefusesTheGraph()
    {
        var graph = LineageProjection.Of(
            [NewDataSet(1, "Orders"), NewDataSet(2, "Orders 2")],
            [MapEdges.Between("copied_by", DataSetId(1), DataSetId(2))],
            null,
            NullLogger.Instance);

        graph.IsFailure.ShouldBeTrue();
        graph.Code!.Name.ShouldBe("MapRelationUnknown");
    }
}
