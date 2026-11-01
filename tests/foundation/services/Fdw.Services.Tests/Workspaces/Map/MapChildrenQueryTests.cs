using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Workspaces.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-1195: an explode request is validated against the node it names, and children are searched in name order and paged.</summary>
[Collection(nameof(ServicesTestCollection))]
public class MapChildrenQueryTests
{
    [Fact]
    public void ARequestWithNoOptionsAsksForEveryContainerAndDescribesNoPage()
    {
        var query = MapChildrenQuery.Parse(new MapChildrenRequest { NodeId = "x" }, MapKinds.DataStore, NullLogger.Instance).Value!;

        query.Containers.ShouldBeTrue();
        query.DataSetFields.ShouldBeFalse();
        query.Skip.ShouldBe(0);
        query.Take.ShouldBeNull();
        query.Describe(Children("a", "b"), Children("a", "b")).ShouldBeNull();
    }

    [Theory]
    [InlineData(-1, null, null, null)]
    [InlineData(0, 0, null, null)]
    [InlineData(null, null, null, "Row")]
    [InlineData(null, null, null, "Container,Nonsense")]
    public void ARequestThatCannotBeAnsweredIsRefusedWithTheReason(int? skip, int? take, string? q, string? kinds)
    {
        var request = new MapChildrenRequest { NodeId = "x", Skip = skip, Take = take, Q = q, Kinds = kinds };

        MapChildrenQuery.Parse(request, MapKinds.DataSet, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void FieldsAreOnlyAskedOfADataSet()
    {
        var request = new MapChildrenRequest { NodeId = "x", Kinds = "DataSetField" };

        MapChildrenQuery.Parse(request, MapKinds.DataStore, NullLogger.Instance).IsFailure.ShouldBeTrue();
        var query = MapChildrenQuery.Parse(request, MapKinds.DataSet, NullLogger.Instance).Value!;
        query.DataSetFields.ShouldBeTrue();
        query.Containers.ShouldBeFalse();
    }

    [Fact]
    public void SearchIgnoresCaseAndTheResultIsInKindThenNameOrder()
    {
        var query = MapChildrenQuery.Parse(new MapChildrenRequest { NodeId = "x", Q = "ORD" }, MapKinds.DataSet, NullLogger.Instance).Value!;

        var matching = query.Match(Children("Reorders", "Customers", "orders", "Orders2"));

        matching.Select(child => child.Node.Label).ShouldBe(["orders", "Orders2", "Reorders"]);
    }

    [Fact]
    public void APageSaysWhereItStartedAndWhetherMoreFollow()
    {
        var query = MapChildrenQuery.Parse(new MapChildrenRequest { NodeId = "x", Skip = 2, Take = 2 }, MapKinds.DataSet, NullLogger.Instance).Value!;
        var matching = query.Match(Children("a", "b", "c", "d", "e"));

        var sent = query.Slice(matching);
        var page = query.Describe(matching, sent)!;

        sent.Select(child => child.Node.Label).ShouldBe(["c", "d"]);
        page.Skip.ShouldBe(2);
        page.Take.ShouldBe(2);
        page.Total.ShouldBe(5);
        page.HasMore.ShouldBeTrue();
    }

    [Fact]
    public void ThePageThatReachesTheEndHasNoMore()
    {
        var query = MapChildrenQuery.Parse(new MapChildrenRequest { NodeId = "x", Skip = 3, Take = 5 }, MapKinds.DataSet, NullLogger.Instance).Value!;
        var matching = query.Match(Children("a", "b", "c", "d", "e"));

        query.Describe(matching, query.Slice(matching))!.HasMore.ShouldBeFalse();
    }

    private static List<MapChild> Children(params string[] names)
        => names.Select(name => new MapChild(
            new MapNodeDto { Id = MapKinds.Container + ":" + name, Label = name, Kind = MapKinds.Container },
            MapEdges.Between(MapRelations.PartOf, name, "parent"))).ToList();
}
