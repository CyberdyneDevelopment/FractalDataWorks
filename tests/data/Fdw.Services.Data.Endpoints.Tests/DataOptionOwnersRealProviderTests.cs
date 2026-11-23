using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Logging;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Configuration;
using Fdw.Services.Settings;
using Fdw.Services.Workspaces;
using Fdw.Web.Endpoints.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6717: the data option owner picker lists each kind of owner through the REAL providers. The organization domain lists fields of its own (its tenant, slug and
/// flags) that live on its domain record and that its implementation does not carry, so the composing read of the whole organization fails with
/// ListFieldNotOnImplementation; the domain serves a page of domain records for exactly this. Stand-ins for the providers answered either way and hid it.
/// </summary>
public sealed class DataOptionOwnersRealProviderTests
{
    private static readonly Guid Tenant = Guid.Parse("0193a100-0000-7000-8000-0000000000aa");
    private static readonly Guid North = Guid.Parse("0193a3e0-1111-7d4e-8f50-6a7b8c9d0e1f");
    private static readonly Guid South = Guid.Parse("0193a3e0-2222-7d4e-8f50-6a7b8c9d0e1f");
    private static readonly Guid Scouting = Guid.Parse("0193a3e0-3333-7d4e-8f50-6a7b8c9d0e1f");
    private static readonly Guid Hidden = Guid.Parse("0193a3e0-4444-7d4e-8f50-6a7b8c9d0e1f");

    private readonly Mock<IDataConfigurationRuleNames> _names = new(MockBehavior.Strict);

    private static RealProviderStore<OrganizationConfigurationProvider, IOrganizationImplementationConfiguration, OrganizationDomainConfiguration> Organizations()
    {
        var store = RealProviderStore<OrganizationConfigurationProvider, IOrganizationImplementationConfiguration, OrganizationDomainConfiguration>.Empty(
            gateways => new OrganizationConfigurationProvider(NullLogger<OrganizationConfigurationProvider>.Instance, gateways, RealProviderStore<OrganizationConfigurationProvider, IOrganizationImplementationConfiguration, OrganizationDomainConfiguration>.StoreName, "tenant", "Organization"),
            "Organizations");
        store.Holds(Organization(South, "South district", "south"), new OrganizationImplementationConfiguration { Id = South, Name = "South district" });
        store.Holds(Organization(North, "North district", "north"), new OrganizationImplementationConfiguration { Id = North, Name = "North district" });
        return store;
    }

    // An organization as its table holds it: the list fields are on the domain record, and the implementation carries none of them.
    private static OrganizationDomainConfiguration Organization(Guid id, string name, string slug, Guid? tenant = null)
        => new() { Id = id, Name = name, TenantId = tenant ?? Tenant, Slug = slug, IsActive = true };

    private static RealProviderStore<WorkspaceConfigurationProvider, IWorkspaceImplementationConfiguration, DomainConfiguration> Workspaces()
    {
        var reader = new Mock<IDescriptionReader>();
        reader
            .Setup(r => r.Fill(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<IWorkspaceImplementationConfiguration>>(), It.IsAny<Func<IWorkspaceImplementationConfiguration, Guid>>(), It.IsAny<Action<IWorkspaceImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        var store = RealProviderStore<WorkspaceConfigurationProvider, IWorkspaceImplementationConfiguration, DomainConfiguration>.Empty(
            gateways => new WorkspaceConfigurationProvider(
                NullLogger<WorkspaceConfigurationProvider>.Instance, gateways, RealProviderStore<WorkspaceConfigurationProvider, IWorkspaceImplementationConfiguration, DomainConfiguration>.StoreName, "workspace", "Workspace",
                Mock.Of<IDescriptionWriter>(), reader.Object),
            "Workspaces");
        store.Holds(new DomainConfiguration { Id = Scouting, Name = "Scouting" }, new WorkspaceImplementationConfiguration { Id = Scouting, Name = "Scouting" });
        store.Holds(new DomainConfiguration { Id = Hidden, Name = "Secret" }, new WorkspaceImplementationConfiguration { Id = Hidden, Name = "Secret" });
        return store;
    }

    private const int OwnerReadSize = 100;

    private static void WithPaging(Microsoft.AspNetCore.Http.DefaultHttpContext context, int? ownerReadSize = OwnerReadSize)
        => context.RequestServices = new ServiceCollection().AddLogging().AddSingleton<IEffectiveSettingsProvider>(new PagingSettings(ownerReadSize)).BuildServiceProvider();

    // The caller signed in as a member of one tenant: the tenantId claim of the token, the one the organizations route filters on.
    private static Action<Microsoft.AspNetCore.Http.DefaultHttpContext> AsTenant(Guid tenantId, int? ownerReadSize = OwnerReadSize) => context =>
    {
        WithPaging(context, ownerReadSize);
        context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([new System.Security.Claims.Claim("tenantId", tenantId.ToString())], "test"));
    };

    private static async Task<(int Status, PaginatedResponse<DataOptionOwnerChoicePayload>? Body)> Call(
        IDataConfigurationRuleNames names, IOrganizationConfigurationProvider organizations, IWorkspaceConfigurationProvider workspaces, IWorkspaceAccessPolicy access, string kind,
        Action<Microsoft.AspNetCore.Http.DefaultHttpContext>? context = null)
    {
        var endpoint = Factory.Create<ListOwners>(context ?? (http => WithPaging(http)), names, organizations, workspaces, access);
        await endpoint.HandleAsync(new ListDataOptionOwnersRequest { Kind = kind }, TestContext.Current.CancellationToken);
        var status = endpoint.HttpContext.Response.StatusCode;
        return (status, status is >= 200 and < 300 ? endpoint.Response : null);
    }

    [Fact]
    public async Task TheOwnerPickerListsTheOrganizationsOfTheRealOrganizationProviderByNameInOrder()
    {
        var organizations = Organizations();

        var (status, page) = await Call(
            _names.Object, organizations.Provider, Mock.Of<IWorkspaceConfigurationProvider>(MockBehavior.Strict), Mock.Of<IWorkspaceAccessPolicy>(MockBehavior.Strict), "Organization", AsTenant(Tenant));

        status.ShouldBe(200);
        page.ShouldNotBeNull().Items.Select(choice => choice.Name).ShouldBe(["North district", "South district"]);
        page.Items.Select(choice => choice.Id).ShouldBe([North, South]);
    }

    // FDW-4828: the tenant schema has no row-level security, so a list of organizations must narrow itself to the caller's tenant; the store here holds the organizations of two tenants.
    [Fact]
    public async Task TheOwnerPickerListsOnlyTheOrganizationsOfTheCallersTenantAndNeverAnotherTenantsOrganizationNamesOrIds()
    {
        var otherTenant = Guid.Parse("0193a100-0000-7000-8000-0000000000bb");
        var other = Guid.Parse("0193a3e0-5555-7d4e-8f50-6a7b8c9d0e1f");
        var organizations = Organizations();
        organizations.Holds(Organization(other, "Other tenant's organization", "other", otherTenant), new OrganizationImplementationConfiguration { Id = other, Name = "Other tenant's organization" });

        var (status, page) = await Call(
            _names.Object, organizations.Provider, Mock.Of<IWorkspaceConfigurationProvider>(MockBehavior.Strict), Mock.Of<IWorkspaceAccessPolicy>(MockBehavior.Strict), "Organization", AsTenant(Tenant));

        status.ShouldBe(200);
        page.ShouldNotBeNull().Items.Select(choice => choice.Name).ShouldBe(["North district", "South district"]);
        page.Items.Select(choice => choice.Id).ShouldNotContain(other);
    }

    [Fact]
    public async Task TheOwnerPickerListsTheViewableWorkspacesOfTheRealWorkspaceProvider()
    {
        var workspaces = Workspaces();
        var access = new Mock<IWorkspaceAccessPolicy>(MockBehavior.Strict);
        access.Setup(a => a.MayView(It.Is<IWorkspaceImplementationConfiguration>(w => w.Id == Scouting), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        access.Setup(a => a.MayView(It.Is<IWorkspaceImplementationConfiguration>(w => w.Id == Hidden), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(Fdw.Services.Workspaces.Results.WorkspacesResultCodes.ByName("WorkspaceNotVisible"), NullLogger.Instance));
        _names.Setup(n => n.Owners("Workspace", It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, IEnumerable<Guid> ids, CancellationToken _) => GenericResult<IReadOnlyDictionary<Guid, string>>.Success(ids.ToDictionary(id => id, _ => "Scouting")));

        var (status, page) = await Call(_names.Object, Mock.Of<IOrganizationConfigurationProvider>(MockBehavior.Strict), workspaces.Provider, access.Object, "Workspace");

        status.ShouldBe(200);
        page.ShouldNotBeNull().Items.ShouldHaveSingleItem().Id.ShouldBe(Scouting);
    }

    // The stand-in provider answers the domain page and states what it was asked, so what the route asks of it is checked and not assumed.
    private static Mock<IOrganizationConfigurationProvider> PageOf(bool hasMore, out List<DomainQuery> asked, params OrganizationDomainConfiguration[] rows)
    {
        var queries = new List<DomainQuery>();
        asked = queries;
        var provider = new Mock<IOrganizationConfigurationProvider>(MockBehavior.Strict);
        provider
            .Setup(o => o.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .Callback((DomainQuery query, CancellationToken _) => queries.Add(query))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<OrganizationDomainConfiguration>>.Success(
                new ConfigurationPage<OrganizationDomainConfiguration>(query.Skip, query.Take, hasMore, _ => Rows(rows))));
        return provider;
    }

    private static async IAsyncEnumerable<OrganizationDomainConfiguration> Rows(IEnumerable<OrganizationDomainConfiguration> rows)
    {
        foreach (var row in rows)
            yield return row;

        await Task.CompletedTask;
    }

    [Fact]
    public async Task TheOrganizationReadAsksForExactlyTheSizeTheSettingStatesForTheCallersTenantAndNamesEachOrganizationByItsRecord()
    {
        var provider = PageOf(false, out var asked, Organization(North, "North district", "north"));

        var (status, page) = await Call(
            _names.Object, provider.Object, Mock.Of<IWorkspaceConfigurationProvider>(MockBehavior.Strict), Mock.Of<IWorkspaceAccessPolicy>(MockBehavior.Strict), "Organization", AsTenant(Tenant, 250));

        status.ShouldBe(200);
        page.ShouldNotBeNull().Items.ShouldHaveSingleItem().Name.ShouldBe("North district");
        var query = asked.ShouldHaveSingleItem();
        query.Skip.ShouldBe(0);
        query.Take.ShouldBe(250);
        query.Filter.ShouldNotBeNull().Root.ShouldBeOfType<FilterCondition>().ShouldSatisfyAllConditions(
            condition => condition.PropertyName.ShouldBe("TenantId"),
            condition => condition.Value.ShouldBe(Tenant));
    }

    [Fact]
    public async Task AReadSizeThatIsNotSetRefusesTheOrganizationListAndNeverReadsTheProvider()
    {
        var (status, _) = await Call(
            _names.Object, Mock.Of<IOrganizationConfigurationProvider>(MockBehavior.Strict), Mock.Of<IWorkspaceConfigurationProvider>(MockBehavior.Strict), Mock.Of<IWorkspaceAccessPolicy>(MockBehavior.Strict), "Organization",
            AsTenant(Tenant, ownerReadSize: null));

        status.ShouldBeInRange(400, 599);
    }

    [Fact]
    public async Task ACallerWhoseTokenNamesNoTenantIsRefusedAndNoOrganizationIsRead()
    {
        var (status, _) = await Call(
            _names.Object, Mock.Of<IOrganizationConfigurationProvider>(MockBehavior.Strict), Mock.Of<IWorkspaceConfigurationProvider>(MockBehavior.Strict), Mock.Of<IWorkspaceAccessPolicy>(MockBehavior.Strict), "Organization");

        status.ShouldBeInRange(400, 599);
    }

    [Fact]
    public async Task MoreOrganizationsThanTheReadSizeRefuseTheListInsteadOfShowingAPartOfThem()
    {
        var provider = PageOf(true, out _, Organization(North, "North district", "north"));

        var (status, page) = await Call(
            _names.Object, provider.Object, Mock.Of<IWorkspaceConfigurationProvider>(MockBehavior.Strict), Mock.Of<IWorkspaceAccessPolicy>(MockBehavior.Strict), "Organization", AsTenant(Tenant, 1));

        status.ShouldBeInRange(400, 599);
        page.ShouldBeNull();
    }

    [Fact]
    public async Task AnOrganizationRecordWithNoNameRefusesTheListAndIsNotShownEmpty()
    {
        var provider = PageOf(false, out _, Organization(North, "  ", "north"));

        var (status, _) = await Call(
            _names.Object, provider.Object, Mock.Of<IWorkspaceConfigurationProvider>(MockBehavior.Strict), Mock.Of<IWorkspaceAccessPolicy>(MockBehavior.Strict), "Organization", AsTenant(Tenant));

        status.ShouldBeInRange(400, 599);
    }

    private sealed class ListOwners(IDataConfigurationRuleNames names, IOrganizationConfigurationProvider organizations, IWorkspaceConfigurationProvider workspaces, IWorkspaceAccessPolicy access)
        : ListDataOptionOwnersEndpointBase(NullLogger.Instance, names, organizations, workspaces, access);

    private sealed class PagingSettings(int? ownerReadSize) : IEffectiveSettingsProvider
    {
        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null)
            => TryGetEffectiveValue<T>(settingName, out var value, tenantId, roleName) ? value : throw new InvalidOperationException(settingName);

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            object? found = settingName switch
            {
                SettingDefinitions.MaxPaginationSize => 200,
                SettingDefinitions.DefaultPaginationSize => 50,
                SettingDefinitions.DataOptionOwnerReadSize => ownerReadSize,
                _ => null,
            };
            if (found is T typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }
    }
}
