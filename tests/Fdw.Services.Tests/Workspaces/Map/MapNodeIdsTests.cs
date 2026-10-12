using System;
using Fdw.Services.Workspaces.Endpoints;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-1029: a node id is the kind of the object and its logical id, and reads back to exactly those.</summary>
[Collection(nameof(ServicesTestCollection))]
public class MapNodeIdsTests
{
    private static readonly Guid Id = Guid.Parse("6f1c2c9e-0d4a-4a5e-9b7e-2f3a4b5c6d7e");

    [Theory]
    [InlineData(MapKinds.DataSet)]
    [InlineData(MapKinds.DataStore)]
    [InlineData(MapKinds.Pipeline)]
    [InlineData(MapKinds.Container)]
    public void AnIdReadsBackToTheKindAndLogicalIdItWasBuiltFrom(string kind)
    {
        var nodeId = kind switch
        {
            MapKinds.DataSet => MapNodeIds.DataSet(Id),
            MapKinds.DataStore => MapNodeIds.DataStore(Id),
            MapKinds.Pipeline => MapNodeIds.Pipeline(Id),
            _ => MapNodeIds.Container(Id),
        };

        MapNodeIds.TryParse(nodeId, out var readKind, out var readId).ShouldBeTrue();
        readKind.ShouldBe(kind);
        readId.ShouldBe(Id);
    }

    [Fact]
    public void TheSameObjectAlwaysGetsTheSameId()
    {
        MapNodeIds.DataSet(Id).ShouldBe(MapNodeIds.DataSet(Id));
        MapNodeIds.DataSet(Id).ShouldNotBe(MapNodeIds.DataStore(Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("DataSet")]
    [InlineData("DataSet:")]
    [InlineData(":6f1c2c9e-0d4a-4a5e-9b7e-2f3a4b5c6d7e")]
    [InlineData("DataSet:not-a-guid")]
    [InlineData("Calculation:6f1c2c9e-0d4a-4a5e-9b7e-2f3a4b5c6d7e")]
    [InlineData("dataset:6f1c2c9e-0d4a-4a5e-9b7e-2f3a4b5c6d7e")]
    public void AnIdTheMapDidNotIssueIsNotParsed(string? nodeId)
    {
        MapNodeIds.TryParse(nodeId, out _, out _).ShouldBeFalse();
    }
}
