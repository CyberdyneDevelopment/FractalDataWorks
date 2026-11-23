using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.ServiceTypes;
using Fdw.Services.Workspaces.Lineage;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using static Fdw.Services.Tests.Workspaces.Lineage.LineageTestGraphs;

namespace Fdw.Services.Tests.Workspaces.Lineage;

/// <summary>FDW-1091: a lineage request that cannot be answered is refused with the reason, never given a quiet default.</summary>
[Collection(nameof(ServicesTestCollection))]
public class LineageRequestsTests
{
    // The limits a host that changes nothing gets (FDW-7104): the workspace option's declared defaults, resolved the way registration resolves them.
    private static readonly WorkspaceLimits Limits = new(
        Declared(nameof(WorkspaceLimits.LineageMaxNodes)), Declared(nameof(WorkspaceLimits.LineageMaxDepth)), Declared(nameof(WorkspaceLimits.LineageMaxPathDepth)),
        Declared(nameof(WorkspaceLimits.LineageSearchDefaultTake)), Declared(nameof(WorkspaceLimits.LineageSearchMaxTake)), Declared(nameof(WorkspaceLimits.NoteMaxBodyLength)));

    private static int Declared(string field) => DeclaredSetting.Count(new DefaultWorkspaceServiceType(), field, null).Value;

    [Fact]
    public void TheDeclaredLineageLimitsAreFiveHundredNodesDepthFivePathEightAndSearchPagesOfTwentyUpToAHundred()
    {
        Limits.LineageMaxNodes.ShouldBe(500);
        Limits.LineageMaxDepth.ShouldBe(5);
        Limits.LineageMaxPathDepth.ShouldBe(8);
        Limits.LineageSearchDefaultTake.ShouldBe(20);
        Limits.LineageSearchMaxTake.ShouldBe(100);
        Limits.NoteMaxBodyLength.ShouldBe(4000);
    }

    [Fact]
    public void AConfiguredSearchMostIsTheOneATakeIsHeldTo()
    {
        // FDW-7104: the most is configuration, so a host that configures 300 takes 250 and refuses 301, naming 300.
        var wide = new WorkspaceLimits(500, 5, 8, 20, 300, 4000);

        LineageRequests.Search(new LineageSearchRequest { Q = "o", Take = 250 }, wide, NullLogger.Instance).IsSuccess.ShouldBeTrue();
        var refused = LineageRequests.Search(new LineageSearchRequest { Q = "o", Take = 301 }, wide, NullLogger.Instance);
        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage!.ShouldContain("300");
    }

    private static LineageNeighbourhoodRequest Neighbourhood() => new()
    {
        Node = DataSetId(1),
        Direction = "both",
        Depth = 2,
    };

    [Fact]
    public void AValidNeighbourhoodRequestIsAccepted()
    {
        var query = LineageRequests.Neighbourhood(Neighbourhood(), Limits, NullLogger.Instance);

        query.IsSuccess.ShouldBeTrue();
        query.Value!.Direction.ShouldBeSameAs(LineageDirections.Both);
        query.Value.Depth.ShouldBe(2);
        query.Value.Kinds.DataSets.ShouldBeTrue();
        query.Value.Kinds.Pipelines.ShouldBeTrue();
        query.Value.Kinds.DataStores.ShouldBeFalse();
    }

    [Fact]
    public void ADepthIsRequiredAndHeldToItsMaximum()
    {
        var missing = Neighbourhood();
        missing.Depth = null;
        var tooDeep = Neighbourhood();
        tooDeep.Depth = 6;

        LineageRequests.Neighbourhood(missing, Limits, NullLogger.Instance).IsFailure.ShouldBeTrue();
        LineageRequests.Neighbourhood(tooDeep, Limits, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("up", "Up", true, false)]
    [InlineData("down", "Down", false, true)]
    [InlineData("both", "Both", true, true)]
    public void TheConsolesLowerCaseDirectionsEachNameOneDirection(string wire, string name, bool upstream, bool downstream)
    {
        var request = Neighbourhood();
        request.Direction = wire;

        var direction = LineageRequests.Neighbourhood(request, Limits, NullLogger.Instance).Value!.Direction;

        direction.Name.ShouldBe(name);
        direction.Wire.ShouldBe(wire);
        direction.IncludesUpstream.ShouldBe(upstream);
        direction.IncludesDownstream.ShouldBe(downstream);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("sideways")]
    [InlineData("1")]
    [InlineData("Up")]
    [InlineData("BOTH")]
    public void ADirectionOtherThanExactlyUpDownOrBothIsRefused(string? direction)
    {
        var request = Neighbourhood();
        request.Direction = direction;

        LineageRequests.Neighbourhood(request, Limits, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void AnUnknownDirectionIsRefusedWithACodeThatNamesItAndTheOnesThatExist()
    {
        var request = Neighbourhood();
        request.Direction = "sideways";

        var result = LineageRequests.Neighbourhood(request, Limits, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Name.ShouldBe("MapRequestInvalid");
        var shown = result.Code.FormatClientMessage(result.Details).ShouldNotBeNull();
        shown.ShouldContain("direction 'sideways'");
        shown.ShouldContain("up");
        shown.ShouldContain("down");
        shown.ShouldContain("both");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("orders")]
    [InlineData("Workspace:00000000-0000-0000-0000-000000000001")]
    public void ANodeThatIsNotADataSetDataStoreOrPipelineIdIsRefused(string? node)
    {
        var request = Neighbourhood();
        request.Node = node;

        LineageRequests.Neighbourhood(request, Limits, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void AKindTheLineageDoesNotKnowIsRefusedAndConnectionIsReportedUnavailable()
    {
        var unknown = Neighbourhood();
        unknown.Kinds = "DataSet,Widget";
        var connection = Neighbourhood();
        connection.Kinds = "DataSet,Connection";

        LineageRequests.Neighbourhood(unknown, Limits, NullLogger.Instance).IsFailure.ShouldBeTrue();
        LineageRequests.Neighbourhood(connection, Limits, NullLogger.Instance).Value!.Kinds.Unavailable.ShouldBe(new[] { "Connection" });
    }

    [Fact]
    public void KindsThatNameNothingToWalkAreRefused()
    {
        var request = Neighbourhood();
        request.Kinds = "Pipeline";

        LineageRequests.Neighbourhood(request, Limits, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ASearchNeedsTextAndHoldsTheCountToItsMaximum()
    {
        LineageRequests.Search(new LineageSearchRequest { Q = " " }, Limits, NullLogger.Instance).IsFailure.ShouldBeTrue();
        LineageRequests.Search(new LineageSearchRequest { Q = "o", Take = 101 }, Limits, NullLogger.Instance).IsFailure.ShouldBeTrue();
        LineageRequests.Search(new LineageSearchRequest { Q = "o" }, Limits, NullLogger.Instance).Value!.Take.ShouldBe(20);
    }

    [Fact]
    public void ASearchPageStartsAtAnOffsetAndMatchesByContainsOrStartsWith()
    {
        var plain = LineageRequests.Search(new LineageSearchRequest { Q = "o" }, Limits, NullLogger.Instance).Value!;
        var paged = LineageRequests.Search(new LineageSearchRequest { Q = "o", Skip = 40, Match = "startsWith" }, Limits, NullLogger.Instance).Value!;

        plain.Skip.ShouldBe(0);
        plain.StartsWith.ShouldBeFalse();
        paged.Skip.ShouldBe(40);
        paged.StartsWith.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(null, "like")]
    public void ASkipBelowZeroOrAMatchThatIsNeitherWordIsRefused(int? skip, string? match)
        => LineageRequests.Search(new LineageSearchRequest { Q = "o", Skip = skip, Match = match }, Limits, NullLogger.Instance).IsFailure.ShouldBeTrue();

    [Fact]
    public void ASearchWithNoKindsSearchesDataSetsDataStoresAndPipelines()
    {
        var kinds = LineageRequests.Search(new LineageSearchRequest { Q = "o" }, Limits, NullLogger.Instance).Value!.Kinds;

        kinds.Admits(MapKinds.DataSet).ShouldBeTrue();
        kinds.Admits(MapKinds.DataStore).ShouldBeTrue();
        kinds.Admits(MapKinds.Pipeline).ShouldBeTrue();
    }

    [Fact]
    public void APathNeedsBothEndsAsDataSetsOrDataStoresAndHoldsTheDepthToItsMaximum()
    {
        var ok = new LineagePathRequest { From = DataSetId(1), To = DataSetId(2) };
        var pipelineEnd = new LineagePathRequest { From = DataSetId(1), To = PipelineId(1) };
        var tooDeep = new LineagePathRequest { From = DataSetId(1), To = DataSetId(2), MaxDepth = 9 };

        LineageRequests.Path(ok, Limits, NullLogger.Instance).Value!.MaxDepth.ShouldBe(8);
        LineageRequests.Path(pipelineEnd, Limits, NullLogger.Instance).IsFailure.ShouldBeTrue();
        LineageRequests.Path(tooDeep, Limits, NullLogger.Instance).IsFailure.ShouldBeTrue();
        LineageRequests.Path(new LineagePathRequest { From = DataSetId(1) }, Limits, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("category,tag")]
    [InlineData("owner")]
    public void AGroupsRequestWithoutAValidGroupByIsRefusedAndNothingIsAssumed(string? groupBy)
    {
        var ask = LineageRequests.Groups(new LineageGroupsRequest { GroupBy = groupBy }, NullLogger.Instance);

        ask.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void AGroupsRequestNamesItsGroupingInAnyCase()
    {
        LineageRequests.Groups(new LineageGroupsRequest { GroupBy = "Tag", Tag = "gas", ContainerGroups = "StorePath" }, NullLogger.Instance).Value!
            .ShouldBe(new LineageGroupsAsk(LineageGroups.ByTag, null, "gas", false));
        LineageRequests.Groups(new LineageGroupsRequest { GroupBy = "CATEGORY", Category = "Sports" }, NullLogger.Instance).Value!
            .ShouldBe(new LineageGroupsAsk(LineageGroups.ByCategory, "Sports", null, false));
    }

    [Fact]
    public void ACategoryLimitNeedsTheCategoryGroupingAndATagLimitTheTagGrouping()
    {
        LineageRequests.Groups(new LineageGroupsRequest { GroupBy = "tag", Category = "Sports", ContainerGroups = "storePath" }, NullLogger.Instance).IsFailure.ShouldBeTrue();
        LineageRequests.Groups(new LineageGroupsRequest { GroupBy = "category", Tag = "gas" }, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("both")]
    public void GroupingByTagNeedsTheCallerToSayWhereContainersSit(string? containerGroups)
    {
        LineageRequests.Groups(new LineageGroupsRequest { GroupBy = "tag", ContainerGroups = containerGroups }, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void StoreTagsMakesContainersTakeTheirDataStoresTagsAndIsRefusedWhenGroupingByCategory()
    {
        LineageRequests.Groups(new LineageGroupsRequest { GroupBy = "tag", ContainerGroups = "storeTags" }, NullLogger.Instance).Value!.ContainersTakeStoreTags.ShouldBeTrue();
        LineageRequests.Groups(new LineageGroupsRequest { GroupBy = "category", ContainerGroups = "storeTags" }, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }
}
