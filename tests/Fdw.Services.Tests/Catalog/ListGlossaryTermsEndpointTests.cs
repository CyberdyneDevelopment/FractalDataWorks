using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Fdw.Web.RestEndpoints.Models;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Catalog;

/// <summary>
/// FDW-2336: the glossary list asks the store for one page of the terms' domain table. The cost of a page does not grow with the glossary
/// because the provider is asked for that page and never for every term.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class ListGlossaryTermsEndpointTests
{
    private static readonly Guid Finance = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Loans = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    // A caller signed in to a tenant with no organization: the places above the tenant are none, so a term has no version to count.
    private static IGlossaryTermVersionConfigurationProvider NoVersions()
    {
        var provider = new Mock<IGlossaryTermVersionConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<GlossaryTermVersionDomainConfiguration>>.Success(
                new ConfigurationPage<GlossaryTermVersionDomainConfiguration>(query.Skip, query.Take, false, _ => Stream(Array.Empty<GlossaryTermVersionDomainConfiguration>()))));
        return provider.Object;
    }

    private static IPlaceAncestry TenantOnly()
    {
        var ancestry = new Mock<IPlaceAncestry>();
        ancestry
            .Setup(a => a.Get(PlaceKinds.Tenant, Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Success([new PlaceStep(PlaceKinds.Tenant, Tenant, 0)]));
        return ancestry.Object;
    }

    private static IAuthenticationContextAccessor SignedInToTheTenant()
    {
        var context = new Mock<IAuthenticationContext>();
        context.SetupGet(c => c.IsAuthenticated).Returns(true);
        context.SetupGet(c => c.ActiveTenantId).Returns(Tenant);
        context.SetupGet(c => c.ActiveOrgId).Returns((Guid?)null);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(context.Object);
        return accessor.Object;
    }

    private static ICategoryConfigurationProvider Categories() => CategoryHolding(
        Node(Finance, null, "finance", "Finance"),
        Node(Loans, Finance, "loans", "Loans"));

    private static ICategoryConfigurationProvider CategoryHolding(params CategoryDomainConfiguration[] rows)
    {
        var provider = new Mock<ICategoryConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<ConfigurationPage<CategoryDomainConfiguration>>.Success(
                new ConfigurationPage<CategoryDomainConfiguration>(0, rows.Length + 1, false, _ => Stream(rows))));
        return provider.Object;
    }

    private static CategoryDomainConfiguration Node(Guid id, Guid? parent, string name, string display) => new()
    {
        Id = id,
        Name = name,
        DisplayName = display,
        ParentCategoryId = parent,
        Implementation = GlossaryCategoryConfiguration.ImplementationName,
    };

    private static async IAsyncEnumerable<T> Stream<T>(IEnumerable<T> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static (Mock<IGlossaryTermConfigurationProvider> Provider, List<DomainQuery> Asked) Terms(params GlossaryTermDomainConfiguration[] rows)
    {
        var asked = new List<DomainQuery>();
        var provider = new Mock<IGlossaryTermConfigurationProvider>(MockBehavior.Strict);
        provider
            .Setup(p => p.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .Callback<DomainQuery, CancellationToken>((query, _) => asked.Add(query))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<GlossaryTermDomainConfiguration>>.Success(
                new ConfigurationPage<GlossaryTermDomainConfiguration>(query.Skip, query.Take, false, _ => Stream(rows), query.IncludeTotal ? rows.Length : null)));
        return (provider, asked);
    }

    private static GlossaryTermDomainConfiguration Term(string name, Guid? category = null, string? standsFor = null, bool? approved = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Definition = name + " defined",
        Owner = "Finance",
        CategoryId = category,
        StandsFor = standsFor,
        IsApproved = approved,
    };

    private static async Task<IGenericResult<ConfigurationPage<GlossaryTermDomainConfiguration>>> Load(
        Mock<IGlossaryTermConfigurationProvider> terms, string query, int skip = 0, int take = 25)
    {
        var endpoint = new Endpoint(terms.Object, Categories(), query);
        endpoint.Ask(query);
        return await endpoint.Page(new PaginatedListRequest { Skip = skip, Take = take }).ConfigureAwait(false);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task APageIsOneQueryForThatPageInNameOrderAndNeverAReadOfEveryTerm()
    {
        var (provider, asked) = Terms(Term("MTD"));

        var page = await Load(provider, string.Empty, skip: 40, take: 10);

        page.IsSuccess.ShouldBeTrue();
        var query = asked.ShouldHaveSingleItem();
        query.Skip.ShouldBe(40);
        query.Take.ShouldBe(10);
        query.Filter.ShouldBeNull();
        query.IncludeTotal.ShouldBeFalse();
        var order = query.Ordering!.OrderedFields.ShouldHaveSingleItem();
        order.PropertyName.ShouldBe("Name");
        order.Direction.Name.ShouldBe("Ascending");
        // The provider is strict: a call to the read of every term would have failed the test.
        provider.Verify(p => p.Get(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task TheSearchTheFiltersAndTheTotalReachTheStoreAsOneQuery()
    {
        var (provider, asked) = Terms();

        await Load(provider, "?q=month&acronym=true&approved=true&total=true");

        var query = asked.ShouldHaveSingleItem();
        query.IncludeTotal.ShouldBeTrue();
        var group = query.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.Name.ShouldBe("And");
        group.Nodes.Count.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ACategoryKeepsTheTermsUnderItAndEveryCategoryBelowIt()
    {
        var (provider, asked) = Terms();

        await Load(provider, "?category=finance");

        var condition = asked.ShouldHaveSingleItem().Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("CategoryId");
        ((IEnumerable<object?>)condition.Value!).Cast<Guid>().ShouldBe([Finance, Loans], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ACategoryThatDoesNotExistRefusesTheListWithoutAskingTheStore()
    {
        var (provider, asked) = Terms();

        var page = await Load(provider, "?category=nosuch");

        page.IsFailure.ShouldBeTrue();
        asked.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ATermIsAnsweredFromItsDomainRowWithItsCategoryByNameAndPath()
    {
        var onThePage = Term("MTD", Loans, "Month to date", true);
        var (provider, _) = Terms(onThePage);
        var endpoint = new Endpoint(provider.Object, Categories(), "?category=loans");
        endpoint.Ask("?category=loans");
        await endpoint.Page(new PaginatedListRequest { Skip = 0, Take = 25 });

        var summary = await endpoint.Summarize(onThePage);

        summary.IsSuccess.ShouldBeTrue();
        summary.Value!.Name.ShouldBe("MTD");
        summary.Value.Definition.ShouldBe("MTD defined");
        summary.Value.Owner.ShouldBe("Finance");
        summary.Value.StandsFor.ShouldBe("Month to date");
        summary.Value.IsApproved.ShouldBe(true);
        summary.Value.Category.ShouldBe("loans");
        summary.Value.CategoryPath.ShouldBe("Finance / Loans");
        summary.Value.VersionCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheListIsRoutedWhereItWasAndIsSearchable()
    {
        var endpoint = new Endpoint(Terms().Provider.Object, Categories(), string.Empty);

        endpoint.RegisteredRoute.ShouldBe("/catalog/glossary");
        endpoint.IsSearchable.ShouldBeTrue();
    }

    private sealed class Endpoint : ListGlossaryTermsEndpointBase
    {
        private IQueryCollection _query;

        public Endpoint(IGlossaryTermConfigurationProvider provider, ICategoryConfigurationProvider categories, string query)
            : base(NullLogger.Instance, provider, categories, NoVersions(), TenantOnly(), new Mock<IScopeAuthority>().Object, SignedInToTheTenant())
        {
            _query = Parse(query);
        }

        public string RegisteredRoute => Route;

        public bool IsSearchable => Searchable;

        protected override IQueryCollection RequestQuery => _query;

        public void Ask(string query)
        {
            _query = Parse(query);
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString(query);
            Search = ListSearch.Parse(context, NullLogger.Instance).Value!;
        }

        public Task<IGenericResult<ConfigurationPage<GlossaryTermDomainConfiguration>>> Page(PaginatedListRequest paging)
            => LoadPage(paging, CancellationToken.None);

        public Task<IGenericResult<GlossaryTermListItem>> Summarize(GlossaryTermDomainConfiguration term)
            => ToSummary(term, CancellationToken.None);

        private static IQueryCollection Parse(string query)
        {
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString(query);
            return context.Request.Query;
        }
    }
}
