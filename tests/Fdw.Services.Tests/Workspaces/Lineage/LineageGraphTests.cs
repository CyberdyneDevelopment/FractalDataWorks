using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Workspaces.Lineage;
using Shouldly;
using Xunit;
using static Fdw.Services.Tests.Workspaces.Lineage.LineageTestGraphs;

namespace Fdw.Services.Tests.Workspaces.Lineage;

/// <summary>FDW-1091: the lineage graph walks only the neighbourhood asked for, and names the pipeline on every step through one.</summary>
[Collection(nameof(ServicesTestCollection))]
public class LineageGraphTests
{
    [Fact]
    public void AStepThroughAPipelineJoinsWhatItReadsToWhatItWritesAndNamesIt()
    {
        var walk = TwoStepChain().Neighbourhood(DataSetId(2), LineageDirections.Both, 1, KindsOf("DataSet,Pipeline"));

        walk.Flows.Count.ShouldBe(2);
        walk.Flows.ShouldAllBe(flow => flow.Relation == LineageRelations.Pipeline && flow.PipelineId != null);
        walk.Flows.Select(flow => flow.PipelineId).ShouldBe(new[] { PipelineId(1), PipelineId(2) }, ignoreOrder: true);
    }

    [Fact]
    public void UpstreamDepthsAreNegativeAndDownstreamDepthsPositive()
    {
        var walk = TwoStepChain().Neighbourhood(DataSetId(2), LineageDirections.Both, 1, KindsOf("DataSet"));

        walk.Nodes.Single(placed => placed.Node.Id == DataSetId(1)).Depth.ShouldBe(-1);
        walk.Nodes.Single(placed => placed.Node.Id == DataSetId(2)).Depth.ShouldBe(0);
        walk.Nodes.Single(placed => placed.Node.Id == DataSetId(3)).Depth.ShouldBe(1);
    }

    [Fact]
    public void DirectionUpNeverReturnsAnythingDownstream()
    {
        var walk = TwoStepChain().Neighbourhood(DataSetId(2), LineageDirections.Up, 5, KindsOf("DataSet"));

        Ids(walk).ShouldBe(new[] { DataSetId(1), DataSetId(2) }, ignoreOrder: true);
    }

    [Fact]
    public void DepthLimitsHowFarTheWalkGoes()
    {
        var one = TwoStepChain().Neighbourhood(DataSetId(1), LineageDirections.Down, 1, KindsOf("DataSet"));
        var two = TwoStepChain().Neighbourhood(DataSetId(1), LineageDirections.Down, 2, KindsOf("DataSet"));

        Ids(one).ShouldBe(new[] { DataSetId(1), DataSetId(2) }, ignoreOrder: true);
        Ids(two).Count.ShouldBe(3);
    }

    [Fact]
    public void APipelineFocusIsItsInputsUpstreamAndItsOutputsDownstream()
    {
        var walk = TwoStepChain().Neighbourhood(PipelineId(1), LineageDirections.Both, 1, KindsOf("DataSet,Pipeline"));

        walk.Nodes.Single(placed => placed.Node.Id == DataSetId(1)).Depth.ShouldBe(-1);
        walk.Nodes.Single(placed => placed.Node.Id == DataSetId(2)).Depth.ShouldBe(1);
        walk.Nodes.ShouldNotContain(placed => placed.Node.Kind == MapKinds.Pipeline);
        walk.Flows.ShouldHaveSingleItem().PipelineId.ShouldBe(PipelineId(1));
    }

    [Fact]
    public void APipelineWithSeveralInputsAndOutputsGivesAStepForEachPair()
    {
        var graph = LineageProjection.Of(
            [NewDataSet(1, "A"), NewDataSet(2, "B"), NewDataSet(3, "C"), NewDataSet(4, "D"), NewPipeline(1, "Merge")],
            [
                Reads(DataSetId(1), PipelineId(1)), Reads(DataSetId(2), PipelineId(1)),
                Writes(PipelineId(1), DataSetId(3)), Writes(PipelineId(1), DataSetId(4)),
            ],
            null);

        var walk = graph.Neighbourhood(DataSetId(3), LineageDirections.Up, 1, KindsOf("DataSet,Pipeline"));

        graph.Neighbourhood(DataSetId(1), LineageDirections.Down, 1, KindsOf("DataSet")).Flows.Count.ShouldBe(2);
        Ids(walk).ShouldBe(new[] { DataSetId(1), DataSetId(2), DataSetId(3) }, ignoreOrder: true);
    }

    [Fact]
    public void DataStoresAreWalkedOnlyWhenTheKindsNameThem()
    {
        var graph = LineageProjection.Of(
            [NewDataSet(1, "Landing"), NewStore(1, "Sink")],
            [StoredIn(DataSetId(1), StoreId(1))],
            null);

        Ids(graph.Neighbourhood(DataSetId(1), LineageDirections.Up, 1, KindsOf("DataSet"))).ShouldBe(new[] { DataSetId(1) });
        Ids(graph.Neighbourhood(DataSetId(1), LineageDirections.Up, 1, KindsOf("DataSet,DataStore"))).Count.ShouldBe(2);
    }

    [Fact]
    public void ADerivedDataSetIsDownstreamOfItsOrigin()
    {
        var graph = LineageProjection.Of(
            [NewDataSet(1, "Origin"), NewDataSet(2, "Derived")], [DerivedFrom(DataSetId(2), DataSetId(1))], null);

        var walk = graph.Neighbourhood(DataSetId(1), LineageDirections.Down, 1, KindsOf("DataSet"));

        walk.Nodes.Single(placed => placed.Node.Id == DataSetId(2)).Depth.ShouldBe(1);
        walk.Flows.ShouldHaveSingleItem().Relation.ShouldBe(LineageRelations.DerivedFrom);
    }

    [Fact]
    public void TheNodeCapStopsTheWalkAndSaysSo()
    {
        var nodes = new List<MapNodeDto> { NewDataSet(0, "Hub") };
        var edges = new List<MapEdgeDto>();
        for (var n = 1; n <= LineageLimitsForTests.MaxNodes + 20; n++)
        {
            nodes.Add(NewDataSet(n, $"Child {n}"));
            edges.Add(DerivedFrom(DataSetId(n), DataSetId(0)));
        }

        var walk = LineageProjection.Of(nodes, edges, null).Neighbourhood(DataSetId(0), LineageDirections.Down, 1, KindsOf("DataSet"));

        walk.Truncated.ShouldBeTrue();
        walk.Nodes.Count.ShouldBe(LineageLimitsForTests.MaxNodes);
    }

    [Fact]
    public void APathFollowsTheWayDataFlowsAndNamesThePipelines()
    {
        var walk = TwoStepChain().Path(DataSetId(1), DataSetId(3), 8).ShouldNotBeNull();

        walk.Nodes.Select(placed => placed.Node.Id).ShouldBe(new[] { DataSetId(1), DataSetId(2), DataSetId(3) });
        walk.Nodes.Select(placed => placed.Depth).ShouldBe(new[] { 0, 1, 2 });
        walk.Flows.Select(flow => flow.PipelineId).ShouldBe(new[] { PipelineId(1), PipelineId(2) });
    }

    [Fact]
    public void APathAgainstTheFlowOrBeyondTheDepthIsNotFound()
    {
        TwoStepChain().Path(DataSetId(3), DataSetId(1), 8).ShouldBeNull();
        TwoStepChain().Path(DataSetId(1), DataSetId(3), 1).ShouldBeNull();
    }

    [Fact]
    public void SearchFindsNamesIgnoringCaseWithTheClosestFirst()
    {
        var graph = LineageProjection.Of(
            [NewDataSet(1, "Order lines"), NewDataSet(2, "Orders"), NewDataSet(3, "Backorders"), NewPipeline(1, "Orders load")], [], null);

        var hits = graph.Search("orders", KindsOf("DataSet,Pipeline", pipelinesAreNodes: true), 10);

        hits.Select(hit => hit.Name).ShouldBe(new[] { "Orders", "Orders load", "Backorders" });
    }

    [Fact]
    public void ASearchHonoursTheKindsAndTheCount()
    {
        var graph = LineageProjection.Of([NewDataSet(1, "Orders"), NewDataSet(2, "Orders 2"), NewPipeline(1, "Orders load")], [], null);

        graph.Search("orders", KindsOf("DataSet"), 1).ShouldHaveSingleItem().Kind.ShouldBe(MapKinds.DataSet);
    }

    [Fact]
    public void ANodeThePipelineReachCannotSeeIsAbsentSoItsStepsAreToo()
    {
        var graph = LineageProjection.Of(
            [NewDataSet(1, "Visible"), NewPipeline(1, "Load")], [Reads(DataSetId(1), PipelineId(1)), Writes(PipelineId(1), DataSetId(2))], null);

        graph.Node(DataSetId(2)).ShouldBeNull();
        graph.Neighbourhood(DataSetId(1), LineageDirections.Down, 5, KindsOf("DataSet")).Flows.ShouldBeEmpty();
    }

    [Fact]
    public void AScopedWorkspaceNamesItselfOnEveryNodeAndAnUnscopedOneDoesNot()
    {
        var scoped = LineageProjection.Of([NewDataSet(1, "Orders")], [], "Sales");
        var unscoped = LineageProjection.Of([NewDataSet(1, "Orders")], [], null);

        scoped.Node(DataSetId(1))!.Workspace.ShouldBe("Sales");
        unscoped.Node(DataSetId(1))!.Workspace.ShouldBeNull();
    }

    [Fact]
    public void ThePipelineCanEditOnlyWhenTheMapSaidTheCallerMayEditPipelines()
    {
        var graph = LineageProjection.Of([NewDataSet(1, "A"), NewPipeline(1, "Load", canEdit: false)], [], null);

        graph.Pipeline(PipelineId(1))!.CanEdit.ShouldBeFalse();
        graph.Pipeline(PipelineId(1))!.IsEnabled.ShouldBe(true);
    }
}
