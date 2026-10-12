using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-1029: edges are drawn from what the nodes recorded, and only between nodes that were sent.</summary>
[Collection(nameof(ServicesTestCollection))]
public class MapEdgesTests
{
    private static readonly Guid Orders = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Customers = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    private static readonly Guid Warehouse = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid Load = Guid.Parse("00000000-0000-0000-0000-0000000000c1");

    [Fact]
    public void ADataSetIsStoredInTheDataStoreItsSourcesName()
    {
        var state = new MapState();
        state.DataStoresByName["Warehouse"] = Warehouse;
        state.DataSets.Add(new MapDataSetLinks(Orders, ["Warehouse"], [], []));

        var edge = MapEdges.ForDataSets(state).ShouldHaveSingleItem();

        edge.RelationType.ShouldBe(MapRelations.StoredIn);
        edge.Source.ShouldBe(MapNodeIds.DataSet(Orders));
        edge.Target.ShouldBe(MapNodeIds.DataStore(Warehouse));
    }

    [Fact]
    public void ADataStoreThatWasNotSentGetsNoEdge()
    {
        var state = new MapState();
        state.DataSets.Add(new MapDataSetLinks(Orders, ["Warehouse"], [], []));

        MapEdges.ForDataSets(state).ShouldBeEmpty();
    }

    [Fact]
    public void ADataSetIsDerivedFromTheOneItsSourceNamesByIdOrByNameAndNeverTwice()
    {
        var state = new MapState();
        state.AddNode(MapNodeIds.DataSet(Customers));
        state.DataSetsByName["Customers"] = Customers;
        state.DataSets.Add(new MapDataSetLinks(Orders, [], [Customers], ["customers"]));

        var edge = MapEdges.ForDataSets(state).ShouldHaveSingleItem();

        edge.RelationType.ShouldBe(MapRelations.DerivedFrom);
        edge.Target.ShouldBe(MapNodeIds.DataSet(Customers));
    }

    [Fact]
    public void ADataSetIsNeverDerivedFromItself()
    {
        var state = new MapState();
        state.AddNode(MapNodeIds.DataSet(Orders));
        state.DataSetsByName["Orders"] = Orders;
        state.DataSets.Add(new MapDataSetLinks(Orders, [], [], ["Orders"]));

        MapEdges.ForDataSets(state).ShouldBeEmpty();
    }

    [Fact]
    public void APipelineConsumesItsSourceDataSetAndProducesItsDestination()
    {
        var state = new MapState();
        state.DataSetsByName["Orders"] = Orders;
        state.DataSetsByName["Customers"] = Customers;
        state.Pipelines.Add(new MapPipelineLinks(Load, "orders", "Customers"));

        var edges = MapEdges.ForPipelines(state).ToList();

        edges.Count.ShouldBe(2);
        edges.ShouldContain(edge => edge.RelationType == MapRelations.ConsumedBy
            && edge.Source == MapNodeIds.DataSet(Orders) && edge.Target == MapNodeIds.Pipeline(Load));
        edges.ShouldContain(edge => edge.RelationType == MapRelations.Produces
            && edge.Source == MapNodeIds.Pipeline(Load) && edge.Target == MapNodeIds.DataSet(Customers));
    }

    [Fact]
    public void APipelineThatNamesNoDataSetGetsNoEdges()
    {
        var state = new MapState();
        state.DataSetsByName["Orders"] = Orders;
        state.Pipelines.Add(new MapPipelineLinks(Load, null, ""));

        MapEdges.ForPipelines(state).ShouldBeEmpty();
    }

    [Fact]
    public void AnEdgeHasTheSameIdWhicheverWayItWasDerived()
    {
        MapEdges.Between(MapRelations.StoredIn, "a", "b").Id.ShouldBe(MapEdges.Between(MapRelations.StoredIn, "a", "b").Id);
        MapEdges.Between(MapRelations.StoredIn, "a", "b").Id.ShouldNotBe(MapEdges.Between(MapRelations.PartOf, "a", "b").Id);
    }

    [Fact]
    public void ADeclaredJoinKeepsItsOwnIdAndSaysWhetherBothFieldsAreChosen()
    {
        var relationship = new WorkspaceRelationshipConfiguration
        {
            Id = Guid.Parse("00000000-0000-0000-0000-0000000000d1"),
            Name = "places",
            LeftDataSetId = Orders,
            RightDataSetId = Customers,
            Cardinality = "ManyToOne",
            LeftFieldId = Guid.Parse("00000000-0000-0000-0000-0000000000e1"),
        };

        var edge = MapEdges.Declared(relationship);

        edge.Id.ShouldBe($"{MapRelations.RelatesTo}:{relationship.Id:D}");
        edge.Label.ShouldBe("places");
        edge.Metadata["cardinality"].ShouldBe("ManyToOne");
        edge.Metadata["isDefined"].ShouldBe(false);
        edge.Metadata.ContainsKey("leftFieldId").ShouldBeTrue();
        edge.Metadata.ContainsKey("rightFieldId").ShouldBeFalse();
    }

    [Fact]
    public void AnEdgeIsRecordedOnceInTheState()
    {
        var state = new MapState();

        state.AddEdge("x").ShouldBeTrue();
        state.AddEdge("x").ShouldBeFalse();
        state.EdgeCount.ShouldBe(1);
    }
}
