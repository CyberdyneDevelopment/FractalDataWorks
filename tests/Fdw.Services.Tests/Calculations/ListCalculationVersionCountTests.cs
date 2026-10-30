using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Calculations;

/// <summary>
/// FDW-2560: each calculation of the list says how many versions of it the caller may read, the calculation itself counting as one, and the versions of
/// every calculation on the page are read in one query.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class ListCalculationVersionCountTests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = Guid.Parse("44444444-0000-0000-0000-000000000001");

    private sealed class Rig
    {
        public List<DomainQuery> VersionsAsked { get; } = [];

        public Mock<IPlaceAncestry> Ancestry { get; } = new();

        public Mock<IScopeAuthority> Authority { get; } = new();

        public Mock<ICalculationVersionConfigurationProvider> Versions { get; } = new();

        public Mock<ICalculationConfigurationProvider> Calculations { get; } = new();
    }

    private static async IAsyncEnumerable<T> Stream<T>(IEnumerable<T> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static CalculationEntityDomainConfiguration Calculation(string name) =>
        new() { Id = Guid.NewGuid(), Name = name, Implementation = "Windowed", Description = name + " described" };

    private static CalculationVersionDomainConfiguration Version(CalculationEntityDomainConfiguration calculation, string kind, Guid place) => new()
    {
        Id = Guid.NewGuid(), Name = $"{calculation.Name}@{kind}:{place:D}", BaseCalculationId = calculation.Id, CalculationName = calculation.Name,
        ScopeKind = kind, ScopeId = place, Description = "d",
    };

    private static Rig Create(
        IReadOnlyList<CalculationEntityDomainConfiguration> calculations,
        IReadOnlyList<CalculationVersionDomainConfiguration> versions,
        Func<ThingScope, bool>? may = null)
    {
        var rig = new Rig();
        var allowed = may ?? (_ => true);
        rig.Calculations
            .Setup(p => p.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<CalculationEntityDomainConfiguration>>.Success(
                new ConfigurationPage<CalculationEntityDomainConfiguration>(query.Skip, query.Take, false, _ => Stream(calculations))));
        rig.Versions
            .Setup(v => v.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .Callback((DomainQuery query, CancellationToken _) => rig.VersionsAsked.Add(query))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<CalculationVersionDomainConfiguration>>.Success(
                new ConfigurationPage<CalculationVersionDomainConfiguration>(query.Skip, query.Take, false, _ => Stream(versions))));
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

    private static async Task<(IGenericResult<ConfigurationPage<CalculationEntityDomainConfiguration>> Page, IReadOnlyList<CalculationEntitySummaryDto> Items)> List(Rig rig)
    {
        var endpoint = new Endpoint(rig);
        var page = await endpoint.Page().ConfigureAwait(false);
        var items = new List<CalculationEntitySummaryDto>();
        if (page.IsSuccess)
        {
            await foreach (var calculation in page.Value!.Items)
            {
                var item = await endpoint.Summarize(calculation).ConfigureAwait(false);
                item.IsSuccess.ShouldBeTrue();
                items.Add(item.Value!);
            }
        }

        return (page, items);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ACalculationWithAVersionAtAPlaceTheCallerMayReadCountsTwoAndTheOtherCountsOne()
    {
        var netRevenue = Calculation("NetRevenue");
        var margin = Calculation("Margin");
        var rig = Create([netRevenue, margin], [Version(netRevenue, PlaceKinds.Organization, Finance)]);

        var (page, items) = await List(rig);

        page.IsSuccess.ShouldBeTrue();
        items.Single(item => item.Name == "NetRevenue").VersionCount.ShouldBe(2);
        items.Single(item => item.Name == "Margin").VersionCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task TheVersionsOfEveryCalculationOnThePageAreOneQuery()
    {
        var rig = Create([Calculation("NetRevenue"), Calculation("Margin"), Calculation("Yield")], []);

        await List(rig);

        var query = rig.VersionsAsked.ShouldHaveSingleItem();
        query.Take.ShouldBe(3);
        var group = query.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.OfType<FilterCondition>().Select(node => node.PropertyName).ShouldBe(["BaseCalculationId", "ScopeId"], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task APlaceTheCallerMayNotReadIsNotCounted()
    {
        var netRevenue = Calculation("NetRevenue");
        var rig = Create([netRevenue], [Version(netRevenue, PlaceKinds.Organization, Finance)], may: scope => scope.OrganizationId != Finance);

        var (_, items) = await List(rig);

        items.ShouldHaveSingleItem().VersionCount.ShouldBe(1);
        rig.VersionsAsked.ShouldBeEmpty();
    }

    private sealed class Endpoint(Rig rig) : ListCalculationEntitiesEndpointBase(
        NullLogger<ListCalculationEntitiesEndpointBase>.Instance, rig.Calculations.Object, rig.Versions.Object, rig.Ancestry.Object, rig.Authority.Object, ActingInFinance())
    {
        public Task<IGenericResult<ConfigurationPage<CalculationEntityDomainConfiguration>>> Page()
            => LoadPage(new PaginatedListRequest { Skip = 0, Take = 25 }, CancellationToken.None);

        public Task<IGenericResult<CalculationEntitySummaryDto>> Summarize(CalculationEntityDomainConfiguration calculation) => ToSummary(calculation, CancellationToken.None);
    }
}
