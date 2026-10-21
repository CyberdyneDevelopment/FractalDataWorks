using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Etl.Graphs;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>
/// Unit tests for the dataflow graph's two layers and the reachability that scopes them:
/// <see cref="DataflowBaseLayer"/> (pipelines:read), <see cref="DataflowSourcesLayer"/>
/// (connections:read) and <see cref="DataflowReachability"/>.
/// </summary>
[Trait("Priority", "P1")]
public class DataflowLayersTests
{
    private static (DataSetRecord Record, SimpleDataSetConfiguration Owner) DataSet(
        string name, params DataSetSourceConfiguration[] sources)
        => DataSet(name, null, sources);

    private static (DataSetRecord Record, SimpleDataSetConfiguration Owner) DataSet(
        string name, Guid? categoryId, params DataSetSourceConfiguration[] sources)
    {
        var id = Guid.NewGuid();
        return (
            new DataSetRecord { Id = id, Name = name },
            new SimpleDataSetConfiguration { Id = id, Name = name, CategoryId = categoryId, Sources = sources.ToList() });
    }

    private static DataSetSourceConfiguration Source(
        string store, string container, string connection, string? sourceDataSetName = null)
        => new()
        {
            Id = Guid.NewGuid(),
            SourceName = $"{connection}-{container}",
            DataStoreName = store,
            ContainerName = container,
            ConnectionName = connection,
            SourceDataSetName = sourceDataSetName
        };

    private static PipelineLineageRecord Pipeline(
        string name,
        string? reads = null,
        string? writes = null,
        (string Store, string Container)? readsFrom = null,
        (string Store, string Container)? writesTo = null)
    {
        List<PipelineUse> uses = [];
        if (reads is not null)
            uses.Add(new PipelineUse(PipelineUseRelations.Reads, PipelineUseKinds.DataSet, reads, []));

        if (writes is not null)
            uses.Add(new PipelineUse(PipelineUseRelations.Writes, PipelineUseKinds.DataSet, writes, []));

        if (readsFrom is { } from)
            uses.Add(new PipelineUse(PipelineUseRelations.Reads, PipelineUseKinds.Container, from.Container, [from.Store]));

        if (writesTo is { } to)
            uses.Add(new PipelineUse(PipelineUseRelations.Writes, PipelineUseKinds.Container, to.Container, [to.Store]));

        return new() { Id = Guid.NewGuid(), Name = name, Implementation = "Graph", Uses = uses };
    }

    private static DataflowGraphInputs Inputs(
        IReadOnlyList<(DataSetRecord Record, SimpleDataSetConfiguration Owner)> dataSets,
        params PipelineLineageRecord[] pipelines)
        => Inputs(dataSets, [], pipelines);

    private static DataflowGraphInputs Inputs(
        IReadOnlyList<(DataSetRecord Record, SimpleDataSetConfiguration Owner)> dataSets,
        IReadOnlyList<DataSetVariationDomainConfiguration> variations,
        params PipelineLineageRecord[] pipelines)
        => new(
            dataSets.Select(d => d.Record).ToList(),
            [new DataStoreRecord { Id = Guid.NewGuid(), Name = "Store1", Implementation = "MsSql" }],
            dataSets.Select(d => (IDataSetImplementationConfiguration)d.Owner).ToList(),
            pipelines,
            variations,
            dataSets.ToDictionary(d => d.Owner.Id, d => d.Owner.CategoryId is null ? null : "Sports / NFL"));

    private static DataSetVariationDomainConfiguration Variation(string name, Guid baseDataSetId, string implementation = "Scoped")
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Implementation = implementation,
            VariationName = name,
            BaseDataSetId = baseDataSetId,
            OwnerWorkspaceId = Guid.NewGuid()
        };

    private static IEnumerable<string> NodeTypes(DataflowGraphResponse graph)
        => graph.Nodes.Select(n => n.NodeType).Distinct(StringComparer.Ordinal);

    [Fact]
    public void BaseLayer_StopsAtTheDataStoreAndContainer()
    {
        var a = DataSet("A", Source("Store1", "Orders", "Conn1"));
        var graph = DataflowBaseLayer.Build(Inputs([a], Pipeline("P", reads: "A")));

        NodeTypes(graph).OrderBy(t => t, StringComparer.Ordinal)
            .ShouldBe(["container", "dataset", "datastore", "pipeline"]);
        graph.Nodes.ShouldAllBe(n => !n.Metadata.ContainsKey("connectionName"));
        graph.Stats.SourceCount.ShouldBe(0);
        graph.Stats.ConnectionCount.ShouldBe(0);
    }

    [Fact]
    public void BaseLayer_NamesThePathOfTheCategoryADataSetSitsUnder()
    {
        var sorted = DataSet("A", Guid.NewGuid(), Source("Store1", "Orders", "Conn1"));
        var unsorted = DataSet("B", Source("Store1", "Orders", "Conn1"));
        var graph = DataflowBaseLayer.Build(Inputs([sorted, unsorted], Pipeline("P", reads: "A", writes: "B")));

        graph.Nodes.Single(n => n.NodeType == "dataset" && n.Label == "A").Category.ShouldBe("Sports / NFL");
        graph.Nodes.Single(n => n.NodeType == "dataset" && n.Label == "B").Category.ShouldBeNull();
    }

    [Fact]
    public void BaseLayer_DrawsADataSetNodeFromTheDataSetItself()
    {
        var a = DataSet("A", Source("Store1", "Orders", "Conn1"));
        a.Owner.Version = "2.3";
        a.Owner.Description = "Orders placed online";
        var graph = DataflowBaseLayer.Build(Inputs([a], Pipeline("P", reads: "A")));

        var node = graph.Nodes.Single(n => n.NodeType == "dataset");
        node.Metadata["version"].ShouldBe("2.3");
        node.Metadata["description"].ShouldBe("Orders placed online");
    }

    [Fact]
    public void BaseLayer_LeavesOutDescriptionAndRecordTypeADataSetDoesNotHold()
    {
        var a = DataSet("A", Source("Store1", "Orders", "Conn1"));
        var graph = DataflowBaseLayer.Build(Inputs([a], Pipeline("P", reads: "A")));

        var node = graph.Nodes.Single(n => n.NodeType == "dataset");
        node.Metadata.ContainsKey("description").ShouldBeFalse();
        node.Metadata.ContainsKey("recordType").ShouldBeFalse();
    }

    [Fact]
    public void BaseLayer_OmitsADataSetNoPipelineReaches()
    {
        var a = DataSet("A", Source("Store1", "Orders", "Conn1"));
        var b = DataSet("B", Source("Store1", "Audit", "Conn2"));
        var graph = DataflowBaseLayer.Build(Inputs([a, b], Pipeline("P", reads: "A")));

        graph.Nodes.Where(n => n.NodeType == "dataset").Select(n => n.Label).ShouldBe(["A"]);
        graph.Nodes.ShouldNotContain(n => n.Label == "Audit");
    }

    [Fact]
    public void BaseLayer_JoinsPipelineToItsDataSets()
    {
        var a = DataSet("A");
        var b = DataSet("B");
        var graph = DataflowBaseLayer.Build(Inputs([a, b], Pipeline("P", reads: "A", writes: "B")));

        graph.Edges.Select(e => e.RelationType).OrderBy(r => r, StringComparer.Ordinal)
            .ShouldBe(["consumed_by", "produces"]);
        graph.Edges.Single(e => e.RelationType == "consumed_by").Source.ShouldBe(DataflowNodeIds.DataSet(a.Record.Id));
        graph.Edges.Single(e => e.RelationType == "produces").Target.ShouldBe(DataflowNodeIds.DataSet(b.Record.Id));
    }

    [Fact]
    public void BaseLayer_DrawsAContainerOnceForTwoSources()
    {
        var a = DataSet("A", Source("Store1", "Orders", "Conn1"), Source("Store1", "Orders", "Conn2"));
        var graph = DataflowBaseLayer.Build(Inputs([a], Pipeline("P", reads: "A")));

        graph.Stats.ContainerCount.ShouldBe(1);
        graph.Edges.Count(e => e.RelationType == "stored_in").ShouldBe(1);
    }

    [Fact]
    public void Reachability_FollowsDerivationInBothDirections()
    {
        var a = DataSet("A");
        var b = DataSet("B", Source("Store1", "Orders", "Conn1", sourceDataSetName: "A"));
        var c = DataSet("C");

        DataflowReachability.Owners([Pipeline("P", reads: "A")], [a.Owner, b.Owner, c.Owner])
            .Select(o => o.Name).ShouldBe(["A", "B"]);
        DataflowReachability.Owners([Pipeline("P", reads: "B")], [a.Owner, b.Owner, c.Owner])
            .Select(o => o.Name).ShouldBe(["A", "B"]);
    }

    [Fact]
    public void BaseLayer_DrawsEachVariationOfAReachableDataSetWithItsKind()
    {
        var main = DataSet("Main");
        var scoped = Variation("sales/Main", main.Record.Id);
        var view = Variation("alice/Main chart", main.Record.Id, "DataView");
        var graph = DataflowBaseLayer.Build(Inputs([main], [scoped, view], Pipeline("P", reads: "Main")));

        graph.Nodes.Where(n => n.NodeType == "variation").Select(n => n.Category).OrderBy(c => c, StringComparer.Ordinal)
            .ShouldBe(["DataView", "Scoped"]);
        graph.Stats.VariationCount.ShouldBe(2);
        graph.Edges.Where(e => e.RelationType == "variation_of").ShouldAllBe(e => e.Target == DataflowNodeIds.DataSet(main.Record.Id));
        graph.Edges.Single(e => e.Source == DataflowNodeIds.Variation(scoped.Id)).RelationType.ShouldBe("variation_of");
    }

    [Fact]
    public void BaseLayer_OmitsVariationsOfADataSetNoPipelineReaches()
    {
        var reached = DataSet("Reached");
        var unreached = DataSet("Unreached");
        var graph = DataflowBaseLayer.Build(Inputs(
            [reached, unreached],
            [Variation("sales/Unreached", unreached.Record.Id)],
            Pipeline("P", reads: "Reached")));

        graph.Nodes.ShouldNotContain(n => n.NodeType == "variation");
        graph.Stats.VariationCount.ShouldBe(0);
    }

    [Fact]
    public void Reachability_IsNotExtendedByAVariation()
    {
        var main = DataSet("Main");
        var other = DataSet("Other");
        var inputs = Inputs([main, other], [Variation("sales/Other", other.Record.Id)], Pipeline("P", reads: "Main"));

        inputs.ReachableOwners.Select(o => o.Name).ShouldBe(["Main"]);
        inputs.Variations.ShouldBeEmpty();
    }

    [Fact]
    public void SourcesLayer_DrawsNoVariations()
    {
        var main = DataSet("Main", Source("Store1", "Orders", "Conn1"));
        var graph = DataflowSourcesLayer.Build(
            Inputs([main], [Variation("sales/Main", main.Record.Id)], Pipeline("P", reads: "Main")));

        graph.Nodes.ShouldNotContain(n => n.NodeType == "variation");
    }

    [Fact]
    public void Reachability_ReachesNothingWhenNoPipelineExists()
    {
        var a = DataSet("A", Source("Store1", "Orders", "Conn1"));

        DataflowReachability.Owners([], [a.Owner]).ShouldBeEmpty();
    }

    [Fact]
    public void SourcesLayer_DrawsOnlyTheSourcesOfReachableDataSetsAndThePipelinesContainers()
    {
        var a = DataSet("A", Source("Store1", "Orders", "Conn1"));
        var b = DataSet("B", Source("Store1", "Audit", "Orphan"));
        var graph = DataflowSourcesLayer.Build(
            Inputs([a, b], Pipeline("P", reads: "A", readsFrom: ("Feed", "feedOrders"), writesTo: ("Warehouse", "whOrders"))));

        graph.Nodes.Where(n => n.NodeType == "connection").Select(n => n.Label).ShouldBe(["Conn1"]);
        graph.Nodes.Where(n => n.NodeType == "container").Select(n => n.Label).OrderBy(l => l, StringComparer.Ordinal)
            .ShouldBe(["feedOrders", "whOrders"]);
        graph.Stats.SourceCount.ShouldBe(1);
        graph.Edges.Select(e => e.RelationType).Distinct(StringComparer.Ordinal).OrderBy(r => r, StringComparer.Ordinal)
            .ShouldBe(["reads_from", "uses_connection", "uses_source", "writes_to"]);
    }

    [Fact]
    public void SourcesLayer_EdgesOnlyJoinNodesOfTheTwoLayers()
    {
        var a = DataSet("A", Source("Store1", "Orders", "Conn1"));
        var inputs = Inputs([a], Pipeline("P", reads: "A", readsFrom: ("Feed", "feedOrders")));
        var drawn = DataflowBaseLayer.Build(inputs).Nodes.Concat(DataflowSourcesLayer.Build(inputs).Nodes)
            .Select(n => n.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var edge in DataflowSourcesLayer.Build(inputs).Edges)
        {
            drawn.ShouldContain(edge.Source);
            drawn.ShouldContain(edge.Target);
        }
    }

    [Fact]
    public void ForPipeline_ScopesBothLayersToThatPipeline()
    {
        var a = DataSet("A", Source("Store1", "Orders", "Conn1"));
        var b = DataSet("B", Source("Store1", "Audit", "Conn2"));
        var inputs = Inputs([a, b], Pipeline("First", reads: "A"), Pipeline("Second", reads: "B"));

        inputs.HasPipeline("first").ShouldBeTrue();
        inputs.HasPipeline("missing").ShouldBeFalse();
        var scoped = inputs.ForPipeline("Second");
        DataflowBaseLayer.Build(scoped).Nodes.Where(n => n.NodeType == "dataset").Select(n => n.Label).ShouldBe(["B"]);
        DataflowSourcesLayer.Build(scoped).Nodes.Where(n => n.NodeType == "connection").Select(n => n.Label).ShouldBe(["Conn2"]);
    }
}
