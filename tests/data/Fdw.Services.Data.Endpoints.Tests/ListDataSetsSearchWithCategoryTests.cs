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
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6119: on <c>GET /datasets</c>, <c>?q=</c> narrows the list the <c>?category=</c> selection makes, so a data set is kept only when its name, display name or
/// abbreviation was found and it sits under the category (or one below it). The record provider answers the search and the data set provider the rows, both stand-ins,
/// so what is asserted is what the list keeps and in which order.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Api")]
public sealed class ListDataSetsSearchWithCategoryTests
{
    private static readonly Guid Finance = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    private static readonly Guid Loans = Guid.Parse("00000000-0000-0000-0000-0000000000c2");
    private static readonly Guid Sports = Guid.Parse("00000000-0000-0000-0000-0000000000c3");

    private static readonly SimpleDataSetConfiguration InFinance = Simple("GasInFinance", Finance);
    private static readonly SimpleDataSetConfiguration InLoans = Simple("GasInLoans", Loans);
    private static readonly SimpleDataSetConfiguration InFinanceNotFound = Simple("OilInFinance", Finance);
    private static readonly SimpleDataSetConfiguration InSports = Simple("GasInSports", Sports);

    private static SimpleDataSetConfiguration Simple(string name, Guid category) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Implementation = SimpleDataSetConfiguration.ImplementationName,
        CategoryId = category,
    };

    private static CategoryDomainConfiguration Node(Guid id, Guid? parent, string name, string display) => new()
    {
        Id = id,
        Name = name,
        DisplayName = display,
        ParentCategoryId = parent,
        Implementation = DataSetCategoryConfiguration.ImplementationName,
    };

    private static async IAsyncEnumerable<T> Stream<T>(IEnumerable<T> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static Mock<ICategoryConfigurationProvider> Categories()
    {
        CategoryDomainConfiguration[] rows = [Node(Finance, null, "finance", "Finance"), Node(Loans, Finance, "loans", "Loans"), Node(Sports, null, "sports", "Sports")];
        var categories = new Mock<ICategoryConfigurationProvider>();
        categories
            .Setup(c => c.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<CategoryDomainConfiguration>>.Success(
                new ConfigurationPage<CategoryDomainConfiguration>(query.Skip, query.Take, false, _ => Stream(rows))));
        return categories;
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

    // The data sets the search finds are the three that mention gas; the one about oil is not found.
    private static Mock<IDataSetRecordProvider> Records()
    {
        DataSetDomainConfiguration[] found = [new() { Id = InFinance.Id, Name = InFinance.Name }, new() { Id = InLoans.Id, Name = InLoans.Name }, new() { Id = InSports.Id, Name = InSports.Name }];
        var records = new Mock<IDataSetRecordProvider>();
        records
            .Setup(r => r.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<DataSetDomainConfiguration>>.Success(
                new ConfigurationPage<DataSetDomainConfiguration>(query.Skip, query.Take, false, _ => Stream(found))));
        return records;
    }

    private static Mock<IDataSetConfigurationProvider> DataSets()
    {
        IDataSetImplementationConfiguration[] all = [InFinance, InLoans, InFinanceNotFound, InSports];
        var dataSets = new Mock<IDataSetConfigurationProvider>();
        dataSets
            .Setup(p => p.Get(It.IsAny<int>(), It.IsAny<int>(), ConfigurationLevels.RecordOnly, It.IsAny<ConfigurationReadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int skip, int take, int _, ConfigurationReadOptions _, CancellationToken _) => GenericResult<ConfigurationPage<IDataSetImplementationConfiguration>>.Success(
                new ConfigurationPage<IDataSetImplementationConfiguration>(skip, take, false, _ => Stream(all))));
        return dataSets;
    }

    private static Probe Endpoint(string query)
        => Factory.Create<Probe>(
            (DefaultHttpContext context) => context.Request.QueryString = new QueryString(query),
            DataSets().Object, Records().Object, Categories().Object, Statuses().Object);

    private static async Task<List<string>> Listed(string query)
    {
        var page = await Endpoint(query).Page();
        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        var names = new List<string>();
        await foreach (var item in page.Value!.Items.WithCancellation(TestContext.Current.CancellationToken))
        {
            names.Add(item.Name);
        }

        return names;
    }

    [Fact]
    public async Task ASearchWithACategoryKeepsTheFoundDataSetsUnderItAndUnderTheCategoriesBelowIt()
    {
        var names = await Listed("?q=gas&category=finance");

        names.ShouldBe(["GasInFinance", "GasInLoans"]);
    }

    [Fact]
    public async Task ASearchWithACategoryLeavesOutADataSetUnderItThatWasNotFound()
    {
        var names = await Listed("?q=gas&category=finance");

        names.ShouldNotContain("OilInFinance");
        names.ShouldNotContain("GasInSports");
    }

    [Fact]
    public async Task ACategoryWithNoSearchKeepsEveryDataSetUnderIt()
    {
        var names = await Listed("?category=finance");

        names.ShouldBe(["GasInFinance", "GasInLoans", "OilInFinance"]);
    }

    [Fact]
    public async Task ASearchWithNoCategoryKeepsTheFoundDataSetsOfEveryCategory()
    {
        var names = await Listed("?q=gas");

        names.ShouldBe(["GasInFinance", "GasInLoans", "GasInSports"]);
    }

    private sealed class Probe : ListDataSetsEndpointBase
    {
        public Probe(IDataSetConfigurationProvider dataSets, IDataSetRecordProvider records, ICategoryConfigurationProvider categories, IDataSetStatuses statuses)
            : base(NullLogger<ListDataSetsEndpointBase>.Instance, dataSets, records, categories, statuses)
        {
        }

        public Task<IGenericResult<ConfigurationPage<IDataSetImplementationConfiguration>>> Page()
        {
            Search = ListSearch.Parse(HttpContext, NullLogger.Instance).Value!;
            return LoadPage(new PaginatedListRequest { Skip = 0, Take = 25 }, CancellationToken.None);
        }
    }
}
