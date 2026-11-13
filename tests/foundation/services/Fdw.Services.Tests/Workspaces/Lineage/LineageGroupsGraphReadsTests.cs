using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Services.Workspaces.Endpoints;
using Shouldly;
using Xunit;
using static Fdw.Services.Tests.Workspaces.Lineage.LineageTestGraphs;

namespace Fdw.Services.Tests.Workspaces.Lineage;

/// <summary>FDW-5456: the lines a Graph data set's reads draw on the groups route, the intended ones marked, and nothing else changes.</summary>
[Collection(nameof(ServicesTestCollection))]
public class LineageGroupsGraphReadsTests
{
    private static readonly string Nfl = "Sports" + CategoryTree.PathSeparator + "NFL";
    private const string Finance = "Finance";
    private const int PipelinesPerLine = 10;
    private static readonly Dictionary<Guid, IReadOnlyList<string>> NoTags = [];

    private static MapNodeDto Categorised(int n, string name, string? category)
    {
        var node = NewDataSet(n, name);
        node.Category = category;
        return node;
    }

    // An edge a Graph data set draws, as the map marks it.
    private static MapEdgeDto OfAGraph(MapEdgeDto edge, bool intended = false)
    {
        edge.Metadata[MapEdges.GraphKey] = true;
        if (intended) edge.Metadata[MapEdges.IntendedKey] = true;
        return edge;
    }

    private static LineageGraph Graph(List<MapNodeDto> nodes, List<MapEdgeDto> edges) => LineageProjection.Of(nodes, edges, null);

    private static LineageGroupsQuery ByCategory(string? path) => new(LineageGroups.ByCategory, path, null, NoTags, false, NoTags, PipelinesPerLine);

    private static Dictionary<Guid, IReadOnlyList<string>> TagsOf(int series, params (int Id, string[] Tags)[] tagged)
        => tagged.ToDictionary(t => Guid.Parse($"00000000-0000-0000-{series:D4}-{t.Id:D12}"), t => (IReadOnlyList<string>)t.Tags);

    [Fact]
    public void AGraphDataSetsReadsDrawLinesFromWhatItReadsToItAndAnIntendedOneSaysSo()
    {
        var graph = Graph(
            [Categorised(1, "Weather", Nfl), Categorised(2, "Sales", Finance), NewStore(1, "feed")],
            [OfAGraph(DerivedFrom(DataSetId(1), DataSetId(2))), OfAGraph(StoredIn(DataSetId(1), StoreId(1)), intended: true)]);

        var answer = LineageGroups.Of(graph, ByCategory(null));

        answer.Lines.Count.ShouldBe(2);
        var bound = answer.Lines.Single(line => line.From == "Category:Finance");
        bound.To.ShouldBe("Category:Sports / NFL");
        bound.Reads.ShouldBe(1);
        bound.IntendedReads.ShouldBeNull();
        bound.Count.ShouldBe(0);
        bound.Pipelines.ShouldBeEmpty();
        var intended = answer.Lines.Single(line => line.From == "StorePath:feed");
        intended.To.ShouldBe("Category:Sports / NFL");
        intended.Reads.ShouldBe(1);
        intended.IntendedReads.ShouldBe(1);
    }

    [Fact]
    public void AGroupMadeOnlyForADataStoreHasNoMembers()
    {
        var graph = Graph([Categorised(1, "Weather", Nfl), NewStore(1, "feed")], [OfAGraph(StoredIn(DataSetId(1), StoreId(1)))]);

        var store = LineageGroups.Of(graph, ByCategory(null)).Groups.Single(group => group.Id == "StorePath:feed");

        store.Members.ShouldBe(0);
        store.Kind.ShouldBe(LineageGroups.StorePathKind);
    }

    [Fact]
    public void ADerivedEdgeOfAnyOtherKindOfDataSetDrawsNoLineAndAddsNoGroup()
    {
        var graph = Graph(
            [Categorised(1, "Joined", Nfl), Categorised(2, "Sales", Finance), NewStore(1, "feed")],
            [DerivedFrom(DataSetId(1), DataSetId(2)), StoredIn(DataSetId(1), StoreId(1))]);

        var answer = LineageGroups.Of(graph, ByCategory(null));

        answer.Lines.ShouldBeEmpty();
        answer.Groups.ShouldNotContain(group => group.Id == "StorePath:feed");
    }

    [Fact]
    public void ALineACommonPipelineAndAReadShareCarriesBoth()
    {
        var graph = Graph(
            [Categorised(1, "Landing", Finance), Categorised(2, "Stats", Nfl), NewPipeline(1, "Load")],
            [Reads(DataSetId(1), PipelineId(1)), Writes(PipelineId(1), DataSetId(2)), OfAGraph(DerivedFrom(DataSetId(2), DataSetId(1)))]);

        var line = LineageGroups.Of(graph, ByCategory(null)).Lines.ShouldHaveSingleItem();

        line.Count.ShouldBe(1);
        line.Pipelines.ShouldHaveSingleItem().Name.ShouldBe("Load");
        line.Reads.ShouldBe(1);
    }

    [Fact]
    public void AReadAlongASingleGroupDrawsNoLine()
    {
        var graph = Graph([Categorised(1, "A", Nfl), Categorised(2, "B", Nfl)], [OfAGraph(DerivedFrom(DataSetId(1), DataSetId(2)))]);

        LineageGroups.Of(graph, ByCategory(null)).Lines.ShouldBeEmpty();
    }

    [Fact]
    public void ACategoryLimitLeavesTheDataStoreGroupAndItsLineOut()
    {
        var graph = Graph([Categorised(1, "Weather", Nfl), NewStore(1, "feed")], [OfAGraph(StoredIn(DataSetId(1), StoreId(1)))]);

        var answer = LineageGroups.Of(graph, ByCategory(Nfl));

        answer.Lines.ShouldBeEmpty();
        answer.Groups.ShouldNotContain(group => group.Kind == LineageGroups.StorePathKind);
    }

    [Fact]
    public void WhenContainersTakeStoreTagsADataStoreReadSitsInTheTagsOfItsStore()
    {
        var graph = Graph([NewDataSet(1, "Weather"), NewStore(1, "feed")], [OfAGraph(StoredIn(DataSetId(1), StoreId(1)))]);
        var query = new LineageGroupsQuery(LineageGroups.ByTag, null, null, TagsOf(0, (1, new[] { "forecast" })), true, TagsOf(1, (1, new[] { "external" })), PipelinesPerLine);

        var line = LineageGroups.Of(graph, query).Lines.ShouldHaveSingleItem();

        line.From.ShouldBe("Tag:external");
        line.To.ShouldBe("Tag:forecast");
        line.Reads.ShouldBe(1);
    }

    [Fact]
    public void ATagGroupingThatDoesNotTakeStoreTagsLeavesADataStoreReadOut()
    {
        var graph = Graph([NewDataSet(1, "Weather"), NewStore(1, "feed")], [OfAGraph(StoredIn(DataSetId(1), StoreId(1)))]);
        var query = new LineageGroupsQuery(LineageGroups.ByTag, null, "forecast", TagsOf(0, (1, new[] { "forecast", "daily" })), false, NoTags, PipelinesPerLine);

        LineageGroups.Of(graph, query).Lines.ShouldBeEmpty();
    }
}
