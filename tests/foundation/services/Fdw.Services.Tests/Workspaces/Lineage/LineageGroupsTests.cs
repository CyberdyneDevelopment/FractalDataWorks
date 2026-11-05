using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Services.Workspaces.Endpoints;
using Shouldly;
using Xunit;
using static Fdw.Services.Tests.Workspaces.Lineage.LineageTestGraphs;

namespace Fdw.Services.Tests.Workspaces.Lineage;

/// <summary>FDW-3824: the groups of data sets and containers, and the lines pipelines draw between them.</summary>
[Collection(nameof(ServicesTestCollection))]
public class LineageGroupsTests
{
    private static readonly string Sports = "Sports";
    private static readonly string Nfl = "Sports" + CategoryTree.PathSeparator + "NFL";
    private static readonly string Finance = "Finance";

    private static MapNodeDto Categorised(int n, string name, string? category)
    {
        var node = NewDataSet(n, name);
        node.Category = category;
        return node;
    }

    private static MapNodeDto NewContainer(int n, string name, string store, string path, int storeNumber = 1) => new()
    {
        Id = MapNodeIds.Container(Guid.Parse($"00000000-0000-0000-0003-{n:D12}")),
        Label = name,
        Kind = MapKinds.Container,
        Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["dataStore"] = store,
            ["pathName"] = path,
            ["dataStoreId"] = Guid.Parse($"00000000-0000-0000-0001-{storeNumber:D12}").ToString("D"),
        },
    };

    private static string ContainerId(int n) => MapNodeIds.Container(Guid.Parse($"00000000-0000-0000-0003-{n:D12}"));

    private static LineageGraph Graph(List<MapNodeDto> nodes, List<MapEdgeDto> edges) => LineageProjection.Of(nodes, edges, null);

    private const int PipelinesPerLine = 10;

    private static readonly Dictionary<Guid, IReadOnlyList<string>> NoTags = [];

    private static LineageGroupsQuery ByCategory(string? path)
        => new(LineageGroups.ByCategory, path, null, NoTags, false, NoTags, PipelinesPerLine);

    private static LineageGroupsQuery ByTag(string? tag, params (int DataSet, string[] Tags)[] tagged)
        => new(LineageGroups.ByTag, null, tag, TagsOf(0, tagged), false, NoTags, PipelinesPerLine);

    private static LineageGroupsQuery ByTagWithStoreTags(string? tag, (int DataSet, string[] Tags)[] tagged, params (int Store, string[] Tags)[] stores)
        => new(LineageGroups.ByTag, null, tag, TagsOf(0, tagged), true, TagsOf(1, stores), PipelinesPerLine);

    private static Dictionary<Guid, IReadOnlyList<string>> TagsOf(int series, (int Id, string[] Tags)[] tagged)
        => tagged.ToDictionary(t => Guid.Parse($"00000000-0000-0000-{series:D4}-{t.Id:D12}"), t => (IReadOnlyList<string>)t.Tags);

    [Fact]
    public void DataSetsAreGroupedByCategoryWithTheParentFromThePathAndTheUncategorisedGroupAlone()
    {
        var graph = Graph([Categorised(1, "Games", Nfl), Categorised(2, "Teams", Nfl), Categorised(3, "Ledger", Finance), Categorised(4, "Loose", null)], []);

        var answer = LineageGroups.Of(graph, ByCategory(null));

        answer.Groups.Select(group => group.Id).ShouldBe(
            ["Category:Finance", "Category:Sports", "Category:Sports / NFL", "Uncategorised"], ignoreOrder: true);
        var nfl = answer.Groups.Single(group => group.Id == "Category:Sports / NFL");
        nfl.Name.ShouldBe("NFL");
        nfl.Kind.ShouldBe(LineageGroups.CategoryKind);
        nfl.Parent.ShouldBe("Category:Sports");
        nfl.Members.ShouldBe(2);
        answer.Groups.Single(group => group.Id == "Category:Sports").Members.ShouldBe(0);
        answer.Groups.Single(group => group.Id == "Category:Sports").Parent.ShouldBeNull();
        answer.Groups.Single(group => group.Id == LineageGroups.Uncategorised).Members.ShouldBe(1);
    }

    [Fact]
    public void APipelineBetweenTwoGroupsMakesALineAndOneWithinAGroupMakesNone()
    {
        var graph = Graph(
            [Categorised(1, "Games", Nfl), Categorised(2, "Teams", Nfl), Categorised(3, "Ledger", Finance), NewPipeline(1, "Within"), NewPipeline(2, "Across")],
            [Reads(DataSetId(1), PipelineId(1)), Writes(PipelineId(1), DataSetId(2)), Reads(DataSetId(2), PipelineId(2)), Writes(PipelineId(2), DataSetId(3))]);

        var answer = LineageGroups.Of(graph, ByCategory(null));

        var line = answer.Lines.ShouldHaveSingleItem();
        line.From.ShouldBe("Category:Sports / NFL");
        line.To.ShouldBe("Category:Finance");
        line.Count.ShouldBe(1);
        line.Pipelines.Select(pipeline => pipeline.Name).ShouldBe(["Across"]);
        line.Pipelines[0].Id.ShouldBe(PipelineId(2));
    }

    [Fact]
    public void APipelineWithSeveralStepsAcrossOnePairCountsOnceAndALineNamesAtMostTen()
    {
        var nodes = new List<MapNodeDto> { Categorised(1, "From", Sports), Categorised(2, "To", Finance), Categorised(3, "To2", Finance) };
        var edges = new List<MapEdgeDto>();
        for (var i = 1; i <= 12; i++)
        {
            nodes.Add(NewPipeline(i, $"Pipe{i:D2}"));
            edges.Add(Reads(DataSetId(1), PipelineId(i)));
            edges.Add(Writes(PipelineId(i), DataSetId(2)));
        }

        edges.Add(Writes(PipelineId(1), DataSetId(3)));

        var line = LineageGroups.Of(Graph(nodes, edges), ByCategory(null)).Lines.ShouldHaveSingleItem();

        line.Count.ShouldBe(12);
        line.Pipelines.Count.ShouldBe(PipelinesPerLine);
        line.Pipelines.Select(pipeline => pipeline.Name).ShouldBe(Enumerable.Range(1, 10).Select(i => $"Pipe{i:D2}"));
    }

    [Fact]
    public void ALineRunsOneWayAndTheOtherDirectionIsALineOfItsOwn()
    {
        var graph = Graph(
            [Categorised(1, "A", Sports), Categorised(2, "B", Finance), NewPipeline(1, "Out"), NewPipeline(2, "Back")],
            [Reads(DataSetId(1), PipelineId(1)), Writes(PipelineId(1), DataSetId(2)), Reads(DataSetId(2), PipelineId(2)), Writes(PipelineId(2), DataSetId(1))]);

        var lines = LineageGroups.Of(graph, ByCategory(null)).Lines;

        lines.Select(line => (line.From, line.To)).ShouldBe([("Category:Finance", "Category:Sports"), ("Category:Sports", "Category:Finance")]);
    }

    [Fact]
    public void ContainersAPipelineReadsAreGroupedByDataStoreAndPathAndDrawALineToADataSet()
    {
        var graph = Graph(
            [NewContainer(1, "Raw", "Lake", "landing"), NewContainer(2, "Raw2", "Lake", "landing"), Categorised(1, "Games", Nfl), NewPipeline(1, "Load")],
            [MapEdges.Between(MapRelations.ConsumedBy, ContainerId(1), PipelineId(1)), Writes(PipelineId(1), DataSetId(1))]);

        var answer = LineageGroups.Of(graph, ByCategory(null));

        var lake = answer.Groups.Single(group => group.Kind == LineageGroups.StorePathKind);
        lake.Id.ShouldBe("StorePath:Lake/landing");
        lake.Members.ShouldBe(2);
        var line = answer.Lines.ShouldHaveSingleItem();
        (line.From, line.To).ShouldBe(("StorePath:Lake/landing", "Category:Sports / NFL"));
    }

    [Fact]
    public void ACategoryLimitsTheAnswerToThatCategoryAndTheOnesUnderItAndTheLinesBetweenThem()
    {
        var graph = Graph(
            [Categorised(1, "Games", Nfl), Categorised(2, "Teams", Sports), Categorised(3, "Ledger", Finance), Categorised(4, "Loose", null),
             NewPipeline(1, "InSports"), NewPipeline(2, "Out")],
            [Reads(DataSetId(1), PipelineId(1)), Writes(PipelineId(1), DataSetId(2)), Reads(DataSetId(2), PipelineId(2)), Writes(PipelineId(2), DataSetId(3))]);

        var answer = LineageGroups.Of(graph, ByCategory(Sports));

        answer.Groups.Select(group => group.Id).ShouldBe(["Category:Sports", "Category:Sports / NFL"], ignoreOrder: true);
        answer.Groups.Single(group => group.Id == "Category:Sports").Parent.ShouldBeNull();
        answer.Lines.Select(line => (line.From, line.To)).ShouldBe([("Category:Sports / NFL", "Category:Sports")]);
    }

    [Fact]
    public void ADataStoreAndAPipelineSitInNoGroup()
    {
        var graph = Graph([NewStore(1, "Lake"), NewPipeline(1, "Load")], []);

        var answer = LineageGroups.Of(graph, ByCategory(null));

        answer.Groups.ShouldBeEmpty();
        answer.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void ThreeThousandDataSetsAndFiveHundredPipelinesAreGroupedInWellUnderASecond()
    {
        var nodes = new List<MapNodeDto>();
        var edges = new List<MapEdgeDto>();
        for (var d = 1; d <= 3000; d++)
            nodes.Add(Categorised(d, $"Set{d}", $"Area{d % 40}{CategoryTree.PathSeparator}Part{d % 7}"));

        for (var p = 1; p <= 500; p++)
        {
            nodes.Add(NewPipeline(p, $"Pipe{p}"));
            for (var k = 0; k < 3; k++)
            {
                edges.Add(Reads(DataSetId(1 + (p * 5 + k) % 3000), PipelineId(p)));
                edges.Add(Writes(PipelineId(p), DataSetId(1 + (p * 11 + k * 7) % 3000)));
            }
        }

        var graph = Graph(nodes, edges);
        var clock = Stopwatch.StartNew();
        var answer = LineageGroups.Of(graph, ByCategory(null));
        clock.Stop();

        answer.Groups.Count.ShouldBeGreaterThan(40);
        answer.Lines.ShouldNotBeEmpty();
        clock.ElapsedMilliseconds.ShouldBeLessThan(250);
    }

    [Fact]
    public void ContainersAreAdmittedAsAKindOnlyWhenTheRequestNamesThem()
    {
        KindsOf("DataSet").Admits(MapKinds.Container).ShouldBeFalse();
        KindsOf("DataSet,Container").Admits(MapKinds.Container).ShouldBeTrue();
        KindsOf("Container").Admits(MapKinds.DataSet).ShouldBeFalse();
    }

    [Fact]
    public void ADataSetWithSeveralTagsIsAMemberOfEachTagAndOneWithNoneIsUntagged()
    {
        var graph = Graph([NewDataSet(1, "A"), NewDataSet(2, "B"), NewDataSet(3, "C")], []);

        var answer = LineageGroups.Of(graph, ByTag(null, (1, ["gas", "power"]), (2, ["power"])));

        answer.Groups.Select(group => (group.Id, group.Members)).ShouldBe(
            [("Tag:gas", 1), ("Tag:power", 2), ("Untagged", 1)], ignoreOrder: true);
        answer.Groups.Single(group => group.Id == "Tag:gas").Kind.ShouldBe(LineageGroups.TagKind);
        answer.Groups.Single(group => group.Id == "Tag:gas").Name.ShouldBe("gas");
    }

    [Fact]
    public void EndsThatShareATagDrawNoLineForThatTagButTheOtherTagPairsStillDo()
    {
        var graph = Graph(
            [NewDataSet(1, "A"), NewDataSet(2, "B"), NewPipeline(1, "Copy")],
            [Reads(DataSetId(1), PipelineId(1)), Writes(PipelineId(1), DataSetId(2))]);

        var lines = LineageGroups.Of(graph, ByTag(null, (1, ["gas", "power"]), (2, ["power", "prices"]))).Lines;

        lines.Select(line => (line.From, line.To, line.Count)).ShouldBe(
            [("Tag:gas", "Tag:power", 1), ("Tag:gas", "Tag:prices", 1), ("Tag:power", "Tag:prices", 1)], ignoreOrder: true);
    }

    [Fact]
    public void APipelineIsCountedOnceOnALineHoweverManyTagPairsOrStepsRunAlongIt()
    {
        var graph = Graph(
            [NewDataSet(1, "A"), NewDataSet(2, "B"), NewDataSet(3, "C"), NewPipeline(1, "Copy")],
            [Reads(DataSetId(1), PipelineId(1)), Writes(PipelineId(1), DataSetId(2)), Writes(PipelineId(1), DataSetId(3))]);

        var line = LineageGroups.Of(graph, ByTag(null, (1, ["gas"]), (2, ["power"]), (3, ["power"]))).Lines.ShouldHaveSingleItem();

        line.Count.ShouldBe(1);
    }

    [Fact]
    public void ATagLimitKeepsTheDataSetsThatCarryItGroupedByTheirOtherTags()
    {
        var graph = Graph(
            [NewDataSet(1, "A"), NewDataSet(2, "B"), NewDataSet(3, "C"), NewDataSet(4, "D"), NewPipeline(1, "InTag"), NewPipeline(2, "Outside")],
            [Reads(DataSetId(1), PipelineId(1)), Writes(PipelineId(1), DataSetId(2)), Reads(DataSetId(3), PipelineId(2)), Writes(PipelineId(2), DataSetId(4))]);

        var answer = LineageGroups.Of(graph, ByTag("gas", (1, ["gas", "raw"]), (2, ["gas", "clean"]), (3, ["power", "raw"]), (4, ["power", "clean"])));

        answer.Groups.Select(group => (group.Id, group.Members)).ShouldBe([("Tag:clean", 1), ("Tag:raw", 1)], ignoreOrder: true);
        var line = answer.Lines.ShouldHaveSingleItem();
        (line.From, line.To, line.Count).ShouldBe(("Tag:raw", "Tag:clean", 1));
    }

    [Fact]
    public void ContainersStayInTheirStorePathGroupsWhenGroupingByTag()
    {
        var graph = Graph(
            [NewContainer(1, "Raw", "Lake", "landing"), NewDataSet(1, "A"), NewPipeline(1, "Load")],
            [MapEdges.Between(MapRelations.ConsumedBy, ContainerId(1), PipelineId(1)), Writes(PipelineId(1), DataSetId(1))]);

        var answer = LineageGroups.Of(graph, ByTag(null, (1, ["gas"])));

        answer.Groups.Select(group => group.Id).ShouldBe(["StorePath:Lake/landing", "Tag:gas"], ignoreOrder: true);
        answer.Lines.ShouldHaveSingleItem().From.ShouldBe("StorePath:Lake/landing");
    }

    [Fact]
    public void ATagLimitLeavesTheContainersOut()
    {
        var graph = Graph([NewContainer(1, "Raw", "Lake", "landing"), NewDataSet(1, "A")], []);

        LineageGroups.Of(graph, ByTag("gas", (1, ["gas", "raw"]))).Groups.Select(group => group.Id).ShouldBe(["Tag:raw"]);
    }

    [Fact]
    public void WhenChosenAContainerTakesTheTagsOfItsDataStoreAndIsUntaggedWhenTheStoreHasNone()
    {
        var graph = Graph(
            [NewContainer(1, "Raw", "Lake", "landing", 1), NewContainer(2, "Old", "Vault", "archive", 2), NewDataSet(1, "A"), NewPipeline(1, "Load")],
            [MapEdges.Between(MapRelations.ConsumedBy, ContainerId(1), PipelineId(1)), Writes(PipelineId(1), DataSetId(1))]);

        var answer = LineageGroups.Of(graph, ByTagWithStoreTags(null, [(1, ["power"])], (1, ["lake", "raw"])));

        answer.Groups.Select(group => (group.Id, group.Members)).ShouldBe(
            [("Tag:lake", 1), ("Tag:raw", 1), ("Tag:power", 1), ("Untagged", 1)], ignoreOrder: true);
        answer.Lines.Select(line => (line.From, line.To)).ShouldBe([("Tag:lake", "Tag:power"), ("Tag:raw", "Tag:power")], ignoreOrder: true);
    }

    [Fact]
    public void ATagLimitKeepsTheContainersWhoseDataStoreCarriesTheTagWhenTheyTakeItsTags()
    {
        var graph = Graph(
            [NewContainer(1, "Raw", "Lake", "landing", 1), NewContainer(2, "Old", "Vault", "archive", 2), NewDataSet(1, "A")],
            []);

        var answer = LineageGroups.Of(graph, ByTagWithStoreTags("lake", [(1, ["lake", "power"])], (1, ["lake", "raw"]), (2, ["vault"])));

        answer.Groups.Select(group => (group.Id, group.Members)).ShouldBe([("Tag:power", 1), ("Tag:raw", 1)], ignoreOrder: true);
    }

    [Fact]
    public void ALineNamesAsManyPipelinesAsTheSettingGivesAndStillCountsAllOfThem()
    {
        var nodes = new List<MapNodeDto> { Categorised(1, "From", Sports), Categorised(2, "To", Finance) };
        var edges = new List<MapEdgeDto>();
        for (var i = 1; i <= 5; i++)
        {
            nodes.Add(NewPipeline(i, $"Pipe{i}"));
            edges.Add(Reads(DataSetId(1), PipelineId(i)));
            edges.Add(Writes(PipelineId(i), DataSetId(2)));
        }

        var line = LineageGroups.Of(Graph(nodes, edges), ByCategory(null) with { MaxPipelinesPerLine = 3 }).Lines.ShouldHaveSingleItem();

        line.Count.ShouldBe(5);
        line.Pipelines.Select(pipeline => pipeline.Name).ShouldBe(["Pipe1", "Pipe2", "Pipe3"]);
    }
}
