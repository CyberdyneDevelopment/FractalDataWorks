using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Configuration;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6124: <c>GET /datasets</c> reads each data set's description as it did, and <c>?description=false</c> leaves the text out of the summaries, says so in the
/// page (<c>leftOut</c>) and asks the provider for a read that does not fill it. The provider is strict, so a read the test did not expect fails it.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Api")]
public sealed class ListDataSetsLeanListTests
{
    private static readonly Guid OrdersId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

    private readonly Mock<IDataSetConfigurationProvider> _dataSets = new(MockBehavior.Strict);
    private readonly List<ConfigurationReadOptions> _asked = [];

    private static SimpleDataSetConfiguration Orders() => new()
    {
        Id = OrdersId,
        Name = "Orders",
        Implementation = SimpleDataSetConfiguration.ImplementationName,
        Description = "Orders placed",
    };

    private static async IAsyncEnumerable<IDataSetImplementationConfiguration> Stream(IEnumerable<IDataSetImplementationConfiguration> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    // The provider answers the data set whatever the page asked, and notes the options the read stated.
    private void ProviderReads()
        => _dataSets
            .Setup(p => p.Get(It.IsAny<int>(), It.IsAny<int>(), ConfigurationLevels.RecordOnly, It.IsAny<ConfigurationReadOptions>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, int, ConfigurationReadOptions, CancellationToken>((_, _, _, options, _) => _asked.Add(options))
            .ReturnsAsync((int skip, int take, int _, ConfigurationReadOptions _, CancellationToken _) => GenericResult<ConfigurationPage<IDataSetImplementationConfiguration>>.Success(
                new ConfigurationPage<IDataSetImplementationConfiguration>(skip, take, false, _ => Stream([Orders()]))));

    private static Mock<ICategoryConfigurationProvider> NoCategories()
    {
        var categories = new Mock<ICategoryConfigurationProvider>();
        categories
            .Setup(c => c.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<CategoryDomainConfiguration>>.Success(
                new ConfigurationPage<CategoryDomainConfiguration>(query.Skip, query.Take, false, _ => Empty())));
        return categories;
    }

    private static async IAsyncEnumerable<CategoryDomainConfiguration> Empty()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }

    private static Mock<IDataSetStatuses> Statuses()
    {
        var view = new Mock<IDataSetStatusView>();
        view
            .Setup(v => v.StatusOfNested(It.IsAny<IDataSetImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetStatus?>.Success(null));
        var statuses = new Mock<IDataSetStatuses>();
        statuses.Setup(s => s.Read(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataSetStatusView>.Success(view.Object));
        return statuses;
    }

    private Probe Endpoint(string query)
        => Factory.Create<Probe>(
            (DefaultHttpContext context) => context.Request.QueryString = new QueryString(query),
            _dataSets.Object, new Mock<IDataSetRecordProvider>(MockBehavior.Strict).Object, NoCategories().Object, Statuses().Object);

    private async Task<(IGenericResult<ConfigurationPage<IDataSetImplementationConfiguration>> Page, DataSetSummaryResponse? Summary, Probe Endpoint)> Listed(string query)
    {
        var endpoint = Endpoint(query);
        var page = await endpoint.Page();
        if (page.IsFailure || page.Value is null)
        {
            return (page, null, endpoint);
        }

        var summaries = new List<DataSetSummaryResponse>();
        await foreach (var item in page.Value.Items.WithCancellation(TestContext.Current.CancellationToken))
        {
            summaries.Add((await endpoint.Summarize(item)).Value!);
        }

        return (page, summaries.Single(), endpoint);
    }

    [Fact]
    public async Task AListThatStatesNothingReadsTheDescriptionAndSaysNothingWasLeftOut()
    {
        ProviderReads();

        var (page, summary, endpoint) = await Listed(string.Empty);

        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        _asked.ShouldHaveSingleItem().ShouldBeSameAs(ConfigurationReadOptions.Whole);
        summary!.Description.ShouldBe("Orders placed");
        endpoint.LeftOutOfPage.ShouldBeNull();
    }

    [Fact]
    public async Task AListThatStatesTheDescriptionIsShownIsTheSameAsOneThatStatesNothing()
    {
        ProviderReads();

        var (_, summary, endpoint) = await Listed("?description=true");

        _asked.ShouldHaveSingleItem().ShouldBeSameAs(ConfigurationReadOptions.Whole);
        summary!.Description.ShouldBe("Orders placed");
        endpoint.LeftOutOfPage.ShouldBeNull();
    }

    [Fact]
    public async Task ALeanListAsksForAReadThatDoesNotFillAndLeavesTheTextOutAndSaysSo()
    {
        ProviderReads();

        var (page, summary, endpoint) = await Listed("?description=false");

        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        _asked.ShouldHaveSingleItem().ShouldBeSameAs(ConfigurationReadOptions.WithoutFilledMembers);
        summary!.Description.ShouldBeNull();
        summary.Name.ShouldBe("Orders");
        endpoint.LeftOutOfPage.ShouldNotBeNull().ShouldBe(["description"]);
    }

    [Fact]
    public async Task ALeanListNarrowedByKindStillAsksForTheReadThatDoesNotFill()
    {
        ProviderReads();

        var (page, summary, _) = await Listed("?kind=Simple&description=false");

        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        _asked.ShouldHaveSingleItem().ShouldBeSameAs(ConfigurationReadOptions.WithoutFilledMembers);
        summary!.Description.ShouldBeNull();
    }

    [Fact]
    public async Task AValueThatIsNeitherTrueNorFalseIsRefusedAndNothingIsRead()
    {
        var (page, _, _) = await Listed("?description=maybe");

        page.IsFailure.ShouldBeTrue();
        _asked.ShouldBeEmpty();
        _dataSets.VerifyNoOtherCalls();
    }

    private sealed class Probe : ListDataSetsEndpointBase
    {
        public Probe(IDataSetConfigurationProvider dataSets, IDataSetRecordProvider records, ICategoryConfigurationProvider categories, IDataSetStatuses statuses)
            : base(NullLogger<ListDataSetsEndpointBase>.Instance, dataSets, records, categories, statuses)
        {
        }

        public IReadOnlyList<string>? LeftOutOfPage => LeftOut;

        public Task<IGenericResult<ConfigurationPage<IDataSetImplementationConfiguration>>> Page()
            => LoadPage(new PaginatedListRequest { Skip = 0, Take = 25 }, CancellationToken.None);

        public Task<IGenericResult<DataSetSummaryResponse>> Summarize(IDataSetImplementationConfiguration item)
            => ToSummary(item, CancellationToken.None);
    }
}
