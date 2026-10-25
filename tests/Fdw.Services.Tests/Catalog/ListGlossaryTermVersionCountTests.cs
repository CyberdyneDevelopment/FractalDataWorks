using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
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
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Catalog;

/// <summary>
/// FDW-2548: each term of the glossary list says how many versions of it the caller may read, the term itself counting as one. The versions of every
/// term on the page are read in one query, so the cost of a page does not grow with the terms on it.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class ListGlossaryTermVersionCountTests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Loans = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class Rig
    {
        public List<DomainQuery> VersionsAsked { get; } = [];

        public Mock<IPlaceAncestry> Ancestry { get; } = new();

        public Mock<IScopeAuthority> Authority { get; } = new();

        public Mock<IGlossaryTermVersionConfigurationProvider> Versions { get; } = new();

        public IGlossaryTermConfigurationProvider Terms { get; set; } = null!;
    }

    private static async IAsyncEnumerable<T> Stream<T>(IEnumerable<T> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static GlossaryTermDomainConfiguration Term(string name) => new() { Id = Guid.NewGuid(), Name = name, Definition = name + " defined", Owner = "Finance", CategoryId = Loans };

    private static GlossaryTermVersionDomainConfiguration Version(GlossaryTermDomainConfiguration term, string kind, Guid place) => new()
    {
        Id = Guid.NewGuid(), Name = $"{term.Name}@{kind}:{place:D}", BaseTermId = term.Id, TermName = term.Name, ScopeKind = kind, ScopeId = place, Definition = "d", Owner = "o",
    };

    private static ICategoryConfigurationProvider Categories()
    {
        var rows = new[]
        {
            new CategoryDomainConfiguration { Id = Loans, Name = "loans", DisplayName = "Loans", ParentCategoryId = null, Implementation = GlossaryCategoryConfiguration.ImplementationName },
        };
        var provider = new Mock<ICategoryConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<ConfigurationPage<CategoryDomainConfiguration>>.Success(
                new ConfigurationPage<CategoryDomainConfiguration>(0, rows.Length + 1, false, _ => Stream(rows))));
        return provider.Object;
    }

    // A caller who acts in the Finance organization, which sits in the tenant; versions held are the rows the version provider answers.
    private static Rig Create(
        IReadOnlyList<GlossaryTermDomainConfiguration> terms,
        IReadOnlyList<GlossaryTermVersionDomainConfiguration> versions,
        Func<ThingScope, bool>? may = null)
    {
        var rig = new Rig();
        var allowed = may ?? (_ => true);
        var provider = new Mock<IGlossaryTermConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<GlossaryTermDomainConfiguration>>.Success(
                new ConfigurationPage<GlossaryTermDomainConfiguration>(query.Skip, query.Take, false, _ => Stream(terms))));
        rig.Terms = provider.Object;

        rig.Versions
            .Setup(v => v.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .Callback((DomainQuery query, CancellationToken _) => rig.VersionsAsked.Add(query))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<GlossaryTermVersionDomainConfiguration>>.Success(
                new ConfigurationPage<GlossaryTermVersionDomainConfiguration>(query.Skip, query.Take, false, _ => Stream(versions))));
        rig.Ancestry
            .Setup(a => a.Get(PlaceKinds.Organization, Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Success([new PlaceStep(PlaceKinds.Organization, Finance, 0), new PlaceStep(PlaceKinds.Tenant, Tenant, 1)]));
        rig.Authority
            .Setup(a => a.May(It.IsAny<string>(), It.IsAny<ThingScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, ThingScope scope, CancellationToken _) => allowed(scope)
                ? GenericResult.Success()
                : GenericResult.Failure(new GenericMessage("the caller does not hold the right there")));
        return rig;
    }

    private static IAuthenticationContextAccessor ActingInFinance()
    {
        var context = new Mock<IAuthenticationContext>();
        context.SetupGet(c => c.IsAuthenticated).Returns(true);
        context.SetupGet(c => c.ActiveTenantId).Returns(Tenant);
        context.SetupGet(c => c.ActiveOrgId).Returns(Finance);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(context.Object);
        return accessor.Object;
    }

    private static async Task<(IGenericResult<ConfigurationPage<GlossaryTermDomainConfiguration>> Page, IReadOnlyList<GlossaryTermListItem> Items)> List(Rig rig)
    {
        var endpoint = new Endpoint(rig);
        var page = await endpoint.Page().ConfigureAwait(false);
        var items = new List<GlossaryTermListItem>();
        if (page.IsSuccess)
        {
            await foreach (var term in page.Value!.Items)
            {
                var item = await endpoint.Summarize(term).ConfigureAwait(false);
                item.IsSuccess.ShouldBeTrue();
                items.Add(item.Value!);
            }
        }

        return (page, items);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ATermWithNoVersionAtAPlaceTheCallerMayReadCountsOneAndTheOtherCountsTwo()
    {
        var churn = Term("Churn");
        var margin = Term("Margin");
        var rig = Create([churn, margin], [Version(churn, PlaceKinds.Organization, Finance)]);

        var (page, items) = await List(rig);

        page.IsSuccess.ShouldBeTrue();
        items.Single(item => item.Name == "Churn").VersionCount.ShouldBe(2);
        items.Single(item => item.Name == "Margin").VersionCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task TheVersionsOfEveryTermOnThePageAreOneQueryNarrowedToTheTermsAndThePlacesTheCallerMayRead()
    {
        var terms = new[] { Term("Churn"), Term("Margin"), Term("Yield") };
        var rig = Create(terms, []);

        await List(rig);

        var query = rig.VersionsAsked.ShouldHaveSingleItem();
        query.Take.ShouldBe(terms.Length);
        var group = query.Filter!.Root.ShouldBeOfType<FilterGroup>();
        var byTerm = group.Nodes.OfType<FilterCondition>().Single(node => node.PropertyName == "BaseTermId");
        ((IEnumerable<object?>)byTerm.Value!).Cast<Guid>().ShouldBe(terms.Select(term => term.Id), ignoreOrder: true);
        var byPlace = group.Nodes.OfType<FilterCondition>().Single(node => node.PropertyName == "ScopeId");
        ((IEnumerable<object?>)byPlace.Value!).Cast<Guid>().ShouldBe([Finance]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task APlaceTheCallerMayNotReadIsNotCountedAndNothingIsReadForIt()
    {
        var churn = Term("Churn");
        var rig = Create([churn], [Version(churn, PlaceKinds.Organization, Finance)], may: scope => scope.OrganizationId != Finance);

        var (_, items) = await List(rig);

        items.ShouldHaveSingleItem().VersionCount.ShouldBe(1);
        rig.VersionsAsked.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ARowAtAnotherKindOfPlaceWithTheSameIdIsNotCounted()
    {
        var churn = Term("Churn");
        var rig = Create([churn], [Version(churn, PlaceKinds.Workspace, Finance)]);

        var (_, items) = await List(rig);

        items.ShouldHaveSingleItem().VersionCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task APageWithNoTermsAsksNeitherForThePlacesNorForVersions()
    {
        var rig = Create([], []);

        var (page, items) = await List(rig);

        page.IsSuccess.ShouldBeTrue();
        items.ShouldBeEmpty();
        rig.VersionsAsked.ShouldBeEmpty();
        rig.Ancestry.Verify(a => a.Get(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AFailureToReadThePlacesFailsThePageInsteadOfCountingOne()
    {
        var rig = Create([Term("Churn")], []);
        rig.Ancestry
            .Setup(a => a.Get(PlaceKinds.Organization, Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PlaceStep>>.Failure(new GenericMessage("the places could not be read")));

        var (page, _) = await List(rig);

        page.IsFailure.ShouldBeTrue();
        rig.VersionsAsked.ShouldBeEmpty();
    }

    private sealed class Endpoint(Rig rig) : ListGlossaryTermsEndpointBase(
        NullLogger.Instance, rig.Terms, Categories(), rig.Versions.Object, rig.Ancestry.Object, rig.Authority.Object, ActingInFinance())
    {
        protected override IQueryCollection RequestQuery => new DefaultHttpContext().Request.Query;

        public Task<IGenericResult<ConfigurationPage<GlossaryTermDomainConfiguration>>> Page()
        {
            Search = ListSearch.Parse(new DefaultHttpContext(), NullLogger.Instance).Value!;
            return LoadPage(new PaginatedListRequest { Skip = 0, Take = 25 }, CancellationToken.None);
        }

        public Task<IGenericResult<GlossaryTermListItem>> Summarize(GlossaryTermDomainConfiguration term) => ToSummary(term, CancellationToken.None);
    }
}
