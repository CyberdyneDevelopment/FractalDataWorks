using System;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Workspaces.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-4404: the map's kinds are a collection, so a kind the map does not hold is refused and never answered as another kind.</summary>
[Collection(nameof(ServicesTestCollection))]
public class MapKindOptionsTests
{
    private static readonly Guid SomeId = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

    [Theory]
    [InlineData(MapKinds.DataSet)]
    [InlineData(MapKinds.DataStore)]
    [InlineData(MapKinds.Pipeline)]
    [InlineData(MapKinds.Container)]
    [InlineData(MapKinds.DataSetField)]
    [InlineData(MapKinds.Connection)]
    [InlineData(MapKinds.QualityRule)]
    [InlineData(MapKinds.Schedule)]
    public void EveryKindTheMapNamesHasAnOptionThatMakesTheNodeIdTheOldWay(string kind)
    {
        var option = MapKindOptions.ByName(kind);

        ReferenceEquals(option, MapKindOptions.NotFound).ShouldBeFalse();
        option.Name.ShouldBe(kind);
        option.NodeId(SomeId).ShouldBe(MapNodeIds.Other(kind, SomeId));
    }

    [Fact]
    public void TheNodeIdOfTheKindsTheSearchWalksIsTheOneTheOldSwitchBuilt()
    {
        MapKindOptions.ByName(MapKinds.DataSet).NodeId(SomeId).ShouldBe(MapNodeIds.DataSet(SomeId));
        MapKindOptions.ByName(MapKinds.DataStore).NodeId(SomeId).ShouldBe(MapNodeIds.DataStore(SomeId));
        MapKindOptions.ByName(MapKinds.Pipeline).NodeId(SomeId).ShouldBe(MapNodeIds.Pipeline(SomeId));
    }

    [Fact]
    public void AKindNoOptionHoldsIsRefusedNotAnsweredWithAnotherKindsProvider()
    {
        var counts = new MapKindCounts(
            new Mock<IConnectionConfigurationProvider>().Object,
            new Mock<IQualityRuleConfigurationProvider>().Object,
            new Mock<IScheduleConfigurationProvider>().Object);

        var records = counts.Records("Workspace", NullLogger.Instance);

        records.IsFailure.ShouldBeTrue();
        records.Code!.Name.ShouldBe("MapKindUnknown");
    }

    [Fact]
    public void AKindThatHasNoDomainTableToListAnswersTheCodedRefusalOfAProviderThatCannotList()
    {
        var counts = new MapKindCounts(
            new Mock<IConnectionConfigurationProvider>().Object,
            new Mock<IQualityRuleConfigurationProvider>().Object,
            new Mock<IScheduleConfigurationProvider>().Object);

        var records = counts.Records(MapKinds.DataSet, NullLogger.Instance);

        records.IsFailure.ShouldBeTrue();
        records.Code!.Name.ShouldBe("MapSearchUnavailable");
    }
}
