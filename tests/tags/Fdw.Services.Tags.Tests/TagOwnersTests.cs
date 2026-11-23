using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Configuration;
using Fdw.Services.Tags.Abstractions;
using Fdw.Services.Workspaces;
using Moq;

namespace Fdw.Services.Tags.Tests;

/// <summary>Tests for the tag owners service (FDW-4100): a host without the tenant services refuses to list, and a caller with no tenant is refused.</summary>
public sealed class TagOwnersTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Tags")]
    public async Task AHostWithoutTheTenantServicesRefusesToListTheOwners()
    {
        var listed = await new UnavailableTagOwners().List(TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Tags")]
    public async Task ACallerWithNoTenantIsRefusedAndNoProviderIsRead()
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(t => t.CurrentTenant).Returns((ITenant?)null);
        var organizations = new Mock<IOrganizationConfigurationProvider>(MockBehavior.Strict);
        var workspaces = new Mock<IWorkspaceConfigurationProvider>(MockBehavior.Strict);

        var listed = await new TagOwners(tenant.Object, organizations.Object, workspaces.Object, new ReadSize(100)).List(TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Tags")]
    public async Task TheOwnersAreTheTenantItsOrganizationsFromTheDomainRecordPageAndItsWorkspaces()
    {
        var tenant = new Mock<ITenant>();
        tenant.SetupGet(t => t.Id).Returns(new Guid("11111111-0000-0000-0000-000000000001"));
        tenant.SetupGet(t => t.DisplayName).Returns("AFC");
        var context = new Mock<ITenantContext>();
        context.SetupGet(c => c.CurrentTenant).Returns(tenant.Object);
        var organizations = new Mock<IOrganizationConfigurationProvider>(MockBehavior.Strict);
        organizations
            .Setup(o => o.Get(It.Is<DomainQuery>(q => q.Skip == 0 && q.Take == 100 && q.Filter != null && q.Filter.Root is IFilterCondition && ((IFilterCondition)q.Filter.Root).PropertyName == "TenantId"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConfigurationPage<OrganizationDomainConfiguration>>.Success(new ConfigurationPage<OrganizationDomainConfiguration>(
                0, 100, false, _ => Rows(new OrganizationDomainConfiguration { Id = new Guid("22222222-0000-0000-0000-000000000001"), Name = "Finance" }))));
        var workspaces = new Mock<IWorkspaceConfigurationProvider>(MockBehavior.Strict);
        workspaces
            .Setup(w => w.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IWorkspaceImplementationConfiguration>>.Success(
                [new WorkspaceImplementationConfiguration { Id = new Guid("33333333-0000-0000-0000-000000000001"), Name = "AFC" }]));

        var listed = await new TagOwners(context.Object, organizations.Object, workspaces.Object, new ReadSize(100)).List(TestContext.Current.CancellationToken);

        listed.IsSuccess.ShouldBeTrue();
        listed.Value!.Select(owner => $"{owner.Kind}:{owner.Name}").ShouldBe(["Tenant:AFC", "Organization:Finance", "Workspace:AFC"]);
    }

    // FDW-4828: the tenant schema has no row-level security, so the list must narrow itself to the caller's tenant. The provider here holds the organizations of two tenants and answers a page
    // as the table does: a query that names an equality on TenantId gets that tenant's rows, a query with no filter gets every row.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Tags")]
    public async Task TheOwnersListOnlyTheCallersTenantsOrganizationsAndNeverAnotherTenantsNamesOrIds()
    {
        var mine = new Guid("11111111-0000-0000-0000-000000000001");
        var theirs = new Guid("11111111-0000-0000-0000-000000000002");
        var tenant = new Mock<ITenant>();
        tenant.SetupGet(t => t.Id).Returns(mine);
        tenant.SetupGet(t => t.DisplayName).Returns("AFC");
        var context = new Mock<ITenantContext>();
        context.SetupGet(c => c.CurrentTenant).Returns(tenant.Object);
        OrganizationDomainConfiguration[] all =
        [
            new() { Id = new Guid("22222222-0000-0000-0000-000000000001"), Name = "Finance", TenantId = mine },
            new() { Id = new Guid("22222222-0000-0000-0000-000000000002"), Name = "Another tenant's organization", TenantId = theirs },
        ];
        var organizations = new Mock<IOrganizationConfigurationProvider>(MockBehavior.Strict);
        organizations
            .Setup(o => o.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainQuery query, CancellationToken _) => GenericResult<ConfigurationPage<OrganizationDomainConfiguration>>.Success(
                new ConfigurationPage<OrganizationDomainConfiguration>(query.Skip, query.Take, false, _ => Rows([.. all.Where(row => query.Filter?.Root is not IFilterCondition { PropertyName: "TenantId" } condition || Equals(condition.Value, row.TenantId))]))));
        var workspaces = new Mock<IWorkspaceConfigurationProvider>(MockBehavior.Strict);
        workspaces
            .Setup(w => w.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IWorkspaceImplementationConfiguration>>.Success([]));

        var listed = await new TagOwners(context.Object, organizations.Object, workspaces.Object, new ReadSize(100)).List(TestContext.Current.CancellationToken);

        listed.IsSuccess.ShouldBeTrue();
        listed.Value!.Select(owner => $"{owner.Kind}:{owner.Name}").ShouldBe(["Tenant:AFC", "Organization:Finance"]);
    }

    // FDW-6737: the read size is a setting the host states, not a value in code; one that is not set, or is below one, refuses the list and names it, and a tenant with more organizations than
    // the size is refused instead of listed in part.
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-3)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Tags")]
    public async Task AReadSizeThatIsNotSetOrBelowOneRefusesTheListNamingTheSettingAndNoProviderIsRead(int? size)
    {
        var tenant = new Mock<ITenant>();
        tenant.SetupGet(t => t.Id).Returns(new Guid("11111111-0000-0000-0000-000000000001"));
        var context = new Mock<ITenantContext>();
        context.SetupGet(c => c.CurrentTenant).Returns(tenant.Object);

        var listed = await new TagOwners(
            context.Object, new Mock<IOrganizationConfigurationProvider>(MockBehavior.Strict).Object, new Mock<IWorkspaceConfigurationProvider>(MockBehavior.Strict).Object, new ReadSize(size))
            .List(TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
        listed.CurrentMessage.ShouldNotBeNull().ShouldContain(Fdw.Services.Settings.SettingDefinitions.TagOwnerReadSize);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Tags")]
    public async Task MoreOrganizationsThanTheReadSizeRefuseTheListInsteadOfListingAPartOfThem()
    {
        var tenant = new Mock<ITenant>();
        tenant.SetupGet(t => t.Id).Returns(new Guid("11111111-0000-0000-0000-000000000001"));
        var context = new Mock<ITenantContext>();
        context.SetupGet(c => c.CurrentTenant).Returns(tenant.Object);
        var organizations = new Mock<IOrganizationConfigurationProvider>(MockBehavior.Strict);
        organizations
            .Setup(o => o.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConfigurationPage<OrganizationDomainConfiguration>>.Success(new ConfigurationPage<OrganizationDomainConfiguration>(
                0, 1, true, _ => Rows(new OrganizationDomainConfiguration { Id = new Guid("22222222-0000-0000-0000-000000000001"), Name = "Finance" }))));

        var listed = await new TagOwners(context.Object, organizations.Object, new Mock<IWorkspaceConfigurationProvider>(MockBehavior.Strict).Object, new ReadSize(1)).List(TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
    }

    // The one setting the tag owners read: how many organizations one list takes.
    private sealed class ReadSize(int? size) : Fdw.Services.Settings.IEffectiveSettingsProvider
    {
        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null)
            => TryGetEffectiveValue<T>(settingName, out var value, tenantId, roleName) ? value : throw new InvalidOperationException(settingName);

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            if (settingName == Fdw.Services.Settings.SettingDefinitions.TagOwnerReadSize && size is { } stated && stated is T typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }
    }

    private static async IAsyncEnumerable<T> Rows<T>(params T[] rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
