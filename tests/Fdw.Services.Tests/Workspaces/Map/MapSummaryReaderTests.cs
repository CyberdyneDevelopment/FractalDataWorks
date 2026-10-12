using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-1195: the summary counts from the domain tables, and a count it cannot give is null and named, never 0.</summary>
[Collection(nameof(ServicesTestCollection))]
public class MapSummaryReaderTests
{
    [Fact]
    public async Task EachCountedKindIsTheTotalOfItsDomainTableOrTheSizeOfItsAttachmentList()
    {
        var dataSets = Records<IDataSetConfigurationProvider>(total: 12);
        var pipelines = Records<IPipelineConfigurationProvider>(total: 3);
        var held = new Dictionary<string, IReadOnlyCollection<Guid>>(StringComparer.Ordinal)
        {
            [MapKinds.DataStore] = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],
        };
        var sources = new MapSources(dataSets.Object, null!, pipelines.Object, new Mock<IDataSetVariationConfigurationProvider>().Object);

        var summary = await MapSummaryReader.Count(new DomainOnlyMapSelection(held: held), sources, Platform(), NullLogger.Instance, TestContext.Current.CancellationToken);

        summary.IsSuccess.ShouldBeTrue();
        summary.Value!.Nodes[MapKinds.DataSet].ShouldBe(12);
        summary.Value.Nodes[MapKinds.Pipeline].ShouldBe(3);
        summary.Value.Nodes[MapKinds.DataStore].ShouldBe(3);
    }

    [Fact]
    public async Task AKindWithNoCountIsNullAndNamedNotZero()
    {
        var dataSets = Records<IDataSetConfigurationProvider>(total: null);
        var pipelines = Records<IPipelineConfigurationProvider>(total: 0);
        var held = new Dictionary<string, IReadOnlyCollection<Guid>>(StringComparer.Ordinal) { [MapKinds.DataStore] = [] };
        var sources = new MapSources(dataSets.Object, null!, pipelines.Object, new Mock<IDataSetVariationConfigurationProvider>().Object);

        var summary = (await MapSummaryReader.Count(new DomainOnlyMapSelection(held: held), sources, Platform(), NullLogger.Instance, TestContext.Current.CancellationToken)).Value!;

        summary.Nodes[MapKinds.DataSet].ShouldBeNull();
        summary.NotCounted.ShouldContain("nodes." + MapKinds.DataSet);
        summary.Nodes[MapKinds.Pipeline].ShouldBe(0);
        summary.NotCounted.ShouldNotContain("nodes." + MapKinds.Pipeline);
        summary.Nodes["Stage"].ShouldBeNull();
        summary.Edges[MapRelations.ConsumedBy].ShouldBeNull();
        summary.NotCounted.ShouldContain("edges." + MapRelations.ConsumedBy);
    }

    [Fact]
    public async Task AnOrganizationScopeIsChosenByReadingPipelinesSoNothingInItIsCounted()
    {
        var sources = new MapSources(
            new Mock<IDataSetConfigurationProvider>().Object, null!, new Mock<IPipelineConfigurationProvider>().Object,
            new Mock<IDataSetVariationConfigurationProvider>().Object);

        var summary = (await MapSummaryReader.Count(
            new DomainOnlyMapSelection(MapScopeKinds.Organization), sources, Platform(), NullLogger.Instance, TestContext.Current.CancellationToken)).Value!;

        summary.Nodes.Values.ShouldAllBe(count => count == null);
    }

    [Fact]
    public async Task ConnectionsQualityRulesAndSchedulesAreCountedFromTheirDomainTablesInTheEverythingScope()
    {
        var sources = new MapSources(
            Records<IDataSetConfigurationProvider>(total: 12).Object, null!, Records<IPipelineConfigurationProvider>(total: 3).Object,
            new Mock<IDataSetVariationConfigurationProvider>().Object);
        var held = new Dictionary<string, IReadOnlyCollection<Guid>>(StringComparer.Ordinal) { [MapKinds.DataStore] = [] };
        var counts = new MapKindCounts(
            Records<IConnectionConfigurationProvider>(total: 7).Object,
            Records<IQualityRuleConfigurationProvider>(total: 5).Object,
            Records<IScheduleConfigurationProvider>(total: 2).Object);

        var summary = (await MapSummaryReader.Count(new DomainOnlyMapSelection(held: held), sources, counts, NullLogger.Instance, TestContext.Current.CancellationToken)).Value!;

        summary.Nodes[MapKinds.Connection].ShouldBe(7);
        summary.Nodes[MapKinds.QualityRule].ShouldBe(5);
        summary.Nodes[MapKinds.Schedule].ShouldBe(2);
        summary.NotCounted.ShouldNotContain("nodes." + MapKinds.Connection);
        summary.Nodes["Workspace"].ShouldBeNull();
        summary.NotCounted.ShouldContain("nodes.Workspace");
    }

    [Fact]
    public async Task AWorkspaceScopeHoldsNoConnectionsQualityRulesOrSchedulesSoNoneIsCounted()
    {
        var sources = new MapSources(
            Records<IDataSetConfigurationProvider>(total: 12).Object, null!, Records<IPipelineConfigurationProvider>(total: 3).Object,
            new Mock<IDataSetVariationConfigurationProvider>().Object);
        var held = new Dictionary<string, IReadOnlyCollection<Guid>>(StringComparer.Ordinal)
        {
            [MapKinds.DataSet] = [Guid.NewGuid()],
            [MapKinds.DataStore] = [],
            [MapKinds.Pipeline] = [],
        };
        var counts = new MapKindCounts(
            Records<IConnectionConfigurationProvider>(total: 7).Object,
            Records<IQualityRuleConfigurationProvider>(total: 5).Object,
            Records<IScheduleConfigurationProvider>(total: 2).Object);

        var summary = (await MapSummaryReader.Count(
            new DomainOnlyMapSelection(MapScopeKinds.Workspace, workspace: "sales", held: held), sources, counts, NullLogger.Instance,
            TestContext.Current.CancellationToken)).Value!;

        summary.Nodes[MapKinds.Connection].ShouldBeNull();
        summary.Nodes[MapKinds.QualityRule].ShouldBeNull();
        summary.Nodes[MapKinds.Schedule].ShouldBeNull();
        summary.NotCounted.ShouldContain("nodes." + MapKinds.Schedule);
    }

    [Fact]
    public async Task AProviderThatCannotListItsDomainTableFailsTheSummaryWithTheCodedRefusal()
    {
        var sources = new MapSources(
            Records<IDataSetConfigurationProvider>(total: 12).Object, null!, Records<IPipelineConfigurationProvider>(total: 3).Object,
            new Mock<IDataSetVariationConfigurationProvider>().Object);
        var held = new Dictionary<string, IReadOnlyCollection<Guid>>(StringComparer.Ordinal) { [MapKinds.DataStore] = [] };
        var counts = new MapKindCounts(
            new Mock<IConnectionConfigurationProvider>().Object,
            Records<IQualityRuleConfigurationProvider>(total: 5).Object,
            Records<IScheduleConfigurationProvider>(total: 2).Object);

        var summary = await MapSummaryReader.Count(new DomainOnlyMapSelection(held: held), sources, counts, NullLogger.Instance, TestContext.Current.CancellationToken);

        summary.IsFailure.ShouldBeTrue();
        summary.Code!.Name.ShouldBe("MapSearchUnavailable");
    }

    // The platform providers behind the kinds no selection holds, each listing a table of three.
    private static MapKindCounts Platform()
        => new(
            Records<IConnectionConfigurationProvider>(total: 3).Object,
            Records<IQualityRuleConfigurationProvider>(total: 3).Object,
            Records<IScheduleConfigurationProvider>(total: 3).Object);

    // A provider that lists its domain table, asked for a page of one row and its total.
    private static Mock<TProvider> Records<TProvider>(int? total)
        where TProvider : class
    {
        var provider = new Mock<TProvider>();
        provider.As<IDomainRecordProvider<DomainConfiguration>>()
            .Setup(records => records.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConfigurationPage<DomainConfiguration>>.Success(
                new ConfigurationPage<DomainConfiguration>(0, 1, false, _ => Empty(), total)));
        return provider;
    }

    private static async IAsyncEnumerable<DomainConfiguration> Empty()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }
}
