using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Tests.Workspaces.Map;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using static Fdw.Services.Tests.Workspaces.Lineage.LineageTestGraphs;

namespace Fdw.Services.Tests.Workspaces.Lineage;

/// <summary>
/// FDW-1195: a search reads the domain tables and nothing else. The scope it runs against throws if any object is composed,
/// so a search that passes never loaded the whole snapshot.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class LineageSearchReaderTests
{
    [Fact]
    public async Task ASearchAsksTheDomainTableForNamesAndComposesNothing()
    {
        var dataSets = Provider<IDataSetConfigurationProvider>(Row("Reorders"), Row("Orders"), Row("Customers"), Row("Orders2"));

        var page = await Search(dataSets, new LineageSearchQuery("orders", KindsOf("DataSet"), 10));

        page.IsSuccess.ShouldBeTrue();
        // Exact name first, then names that start with the text, then names that hold it.
        page.Value!.Items.Select(node => node.Name).ShouldBe(["Orders", "Orders2", "Reorders"]);
        page.Value.HasMore.ShouldBeFalse();
        dataSets.As<IDomainRecordProvider<DomainConfiguration>>()
            .Verify(records => records.Get(It.Is<DomainQuery>(query => query.Filter != null), It.IsAny<CancellationToken>()), Times.Once());
        dataSets.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task APageSaysWhetherMoreFollow()
    {
        var dataSets = Provider<IDataSetConfigurationProvider>(Row("a1"), Row("a2"), Row("a3"));

        var page = (await Search(dataSets, new LineageSearchQuery("a", KindsOf("DataSet"), 2, 1))).Value!;

        page.Items.Select(node => node.Name).ShouldBe(["a2", "a3"]);
        page.HasMore.ShouldBeFalse();
        (await Search(dataSets, new LineageSearchQuery("a", KindsOf("DataSet"), 1))).Value!.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task StartsWithKeepsOnlyNamesThatStartWithTheText()
    {
        var dataSets = Provider<IDataSetConfigurationProvider>(Row("Orders"), Row("Reorders"));

        var page = (await Search(dataSets, new LineageSearchQuery("ord", KindsOf("DataSet"), 10, StartsWith: true))).Value!;

        page.Items.Select(node => node.Name).ShouldBe(["Orders"]);
    }

    [Fact]
    public async Task ADomainRowWithNoImplementationIsNotAMember()
    {
        var dataSets = Provider<IDataSetConfigurationProvider>(Row("Orders"), new DomainConfiguration { Id = Guid.NewGuid(), Name = "Orders Draft", Implementation = null });

        var page = (await Search(dataSets, new LineageSearchQuery("orders", KindsOf("DataSet"), 10))).Value!;

        page.Items.Select(node => node.Name).ShouldBe(["Orders"]);
    }

    [Fact]
    public async Task AWorkspaceIsSearchedByTheIdsItAttachesAndTheHitsCarryThePathFromTheWorkspace()
    {
        var inside = Row("Orders");
        var attached = new[] { inside.Id };
        var workspaceId = Guid.NewGuid();
        var held = new Dictionary<string, IReadOnlyCollection<Guid>>(StringComparer.Ordinal) { [MapKinds.DataSet] = attached };
        var dataSets = new Mock<IDataSetConfigurationProvider>();
        dataSets.As<IDomainRecordProvider<DomainConfiguration>>()
            .Setup(records => records.Get(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DomainConfiguration>>.Success([inside]));
        var selection = new DomainOnlyMapSelection(MapScopeKinds.Workspace, "Sales", workspaceId, held);

        var page = (await Search(dataSets, new LineageSearchQuery("ord", KindsOf("DataSet"), 10), selection)).Value!;

        page.Items.ShouldHaveSingleItem().Name.ShouldBe("Orders");
        page.Root.ShouldNotBeNull();
        page.Root!.Name.ShouldBe("Sales");
        page.Root.Kind.ShouldBe("Workspace");
    }

    [Fact]
    public async Task AProviderThatCannotListItsDomainTableIsAFailureNotAnEmptyAnswer()
    {
        // A plain configuration provider with no record listing: the probe for it finds nothing.
        var dataSets = new Mock<IDataSetConfigurationProvider>();

        var page = await Search(dataSets, new LineageSearchQuery("o", KindsOf("DataSet"), 10));

        page.IsFailure.ShouldBeTrue();
    }

    private static Task<IGenericResult<LineageSearchPage>> Search(Mock<IDataSetConfigurationProvider> dataSets, LineageSearchQuery query, MapSelection? selection = null)
        => LineageSearchReader.Search(
            selection ?? new DomainOnlyMapSelection(),
            new MapSources(dataSets.Object, null!, new Mock<IPipelineConfigurationProvider>().Object, new Mock<IDataSetVariationConfigurationProvider>().Object, CategoryProviders.Empty()),
            query,
            NullLogger.Instance,
            CancellationToken.None);

    private static DomainConfiguration Row(string name) => new() { Id = Guid.NewGuid(), Name = name, Implementation = "Sql" };

    // A provider that lists its domain table: any filtered page it is asked for is the rows given, all on one page.
    private static Mock<TProvider> Provider<TProvider>(params DomainConfiguration[] rows)
        where TProvider : class
    {
        var provider = new Mock<TProvider>();
        provider.As<IDomainRecordProvider<DomainConfiguration>>()
            .Setup(records => records.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConfigurationPage<DomainConfiguration>>.Success(
                new ConfigurationPage<DomainConfiguration>(0, 200, false, _ => Rows(rows))));
        return provider;
    }

    private static async IAsyncEnumerable<DomainConfiguration> Rows(DomainConfiguration[] rows)
    {
        foreach (var row in rows)
            yield return row;

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
