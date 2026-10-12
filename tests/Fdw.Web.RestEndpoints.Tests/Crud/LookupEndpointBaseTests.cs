using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.Tests.Crud;

/// <summary>
/// FDW-1194: a lookup is one query on the domain table with the search as its filter, and answers
/// <see cref="LookupItem"/>s from the domain row alone.
/// </summary>
/// <remarks>The query and the route are what an endpoint states; a test subclass exposes them without a running host.</remarks>
[Trait("Priority", "P1")]
[Trait("Category", "Endpoints")]
public sealed class LookupEndpointBaseTests
{
    private static ListSearch Search(string query)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(query);
        return ListSearch.Parse(context, NullLogger.Instance).Value!;
    }

    [Fact]
    public void ALookupIsRoutedUnderItsResourceAndIsSearchable()
    {
        var endpoint = new WidgetLookup();

        endpoint.RegisteredRoute.ShouldBe("/widgets/lookup");
        endpoint.IsSearchable.ShouldBeTrue();
    }

    [Fact]
    public async Task WithNoSearchTheQueryHasNoFilterAndTheRequestsPage()
    {
        var endpoint = new WidgetLookup();

        await endpoint.Load(new PaginatedListRequest { Skip = 20, Take = 10 });

        endpoint.Asked.ShouldNotBeNull();
        endpoint.Asked!.Filter.ShouldBeNull();
        endpoint.Asked.Ordering.ShouldBeNull();
        endpoint.Asked.Skip.ShouldBe(20);
        endpoint.Asked.Take.ShouldBe(10);
        endpoint.Asked.IncludeTotal.ShouldBeFalse();
    }

    [Fact]
    public async Task TheSearchIsTheFilterOnTheNameAndTotalIsAskedWhenSaidSo()
    {
        var endpoint = new WidgetLookup();
        endpoint.Ask(Search("?q=brew&match=startsWith&total=true"));

        await endpoint.Load(new PaginatedListRequest { Skip = 0, Take = 25 });

        var condition = endpoint.Asked!.Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe(nameof(DomainConfiguration.Name));
        condition.Operator.Name.ShouldBe("StartsWith");
        condition.Value.ShouldBe("brew");
        endpoint.Asked.IncludeTotal.ShouldBeTrue();
    }

    [Fact]
    public async Task IdsAndTextTogetherAreAnAndOfTheNameAndTheIdentifier()
    {
        var endpoint = new WidgetLookup();
        var id = Guid.NewGuid();
        endpoint.Ask(Search($"?q=brew&id={id}"));

        await endpoint.Load(new PaginatedListRequest { Skip = 0, Take = 25 });

        var group = endpoint.Asked!.Filter!.Root.ShouldBeOfType<FilterGroup>();
        var conditions = group.Nodes.OfType<FilterCondition>().ToList();
        conditions.Select(condition => condition.PropertyName).ShouldBe([nameof(DomainConfiguration.Name), nameof(DomainConfiguration.Id)]);
        conditions[1].Operator.Name.ShouldBe("In");
    }

    [Fact]
    public async Task AnItemCarriesTheDomainRowAndNothingElse()
    {
        var endpoint = new WidgetLookup();
        var id = Guid.NewGuid();

        var item = await endpoint.Summarize(new DomainConfiguration { Id = id, Name = "Breweries", Domain = "DataSet", Implementation = "Simple" });

        item.IsSuccess.ShouldBeTrue();
        item.Value!.Id.ShouldBe(id);
        item.Value.Name.ShouldBe("Breweries");
        item.Value.Implementation.ShouldBe("Simple");
    }

    [Fact]
    public void ByDefaultEveryRowIsOffered()
    {
        new WidgetLookup().Offers(new DomainConfiguration { Name = string.Empty }).ShouldBeTrue();
    }

    private sealed class WidgetLookup : LookupEndpointBase<DomainConfiguration>
    {
        public WidgetLookup()
            : base(NullLogger.Instance)
        {
        }

        public DomainQuery? Asked { get; private set; }

        public string RegisteredRoute => Route;

        public bool IsSearchable => Searchable;

        protected override string ResourceName => "widgets";

        public void Ask(ListSearch search) => Search = search;

        public Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> Load(PaginatedListRequest paging)
            => LoadPage(paging, CancellationToken.None);

        public Task<IGenericResult<LookupItem>> Summarize(DomainConfiguration row) => ToSummary(row, CancellationToken.None);

        public bool Offers(DomainConfiguration row) => Includes(row);

        protected override Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> Read(DomainQuery query, CancellationToken ct)
        {
            Asked = query;
            return Task.FromResult(GenericResult<ConfigurationPage<DomainConfiguration>>.Success(
                new ConfigurationPage<DomainConfiguration>(query.Skip, query.Take, false, _ => AsyncEnumerable.Empty<DomainConfiguration>())));
        }
    }
}
