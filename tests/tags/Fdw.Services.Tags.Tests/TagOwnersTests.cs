using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        var listed = await new TagOwners(tenant.Object, organizations.Object, workspaces.Object).List(TestContext.Current.CancellationToken);

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
            .Setup(o => o.Get(It.Is<DomainQuery>(q => q.Filter == null && q.Skip == 0 && q.Take == TagOwners.EveryOrganization && q.Take + 1 > 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConfigurationPage<OrganizationDomainConfiguration>>.Success(new ConfigurationPage<OrganizationDomainConfiguration>(
                0, TagOwners.EveryOrganization, false, _ => Rows(new OrganizationDomainConfiguration { Id = new Guid("22222222-0000-0000-0000-000000000001"), Name = "Finance" }))));
        var workspaces = new Mock<IWorkspaceConfigurationProvider>(MockBehavior.Strict);
        workspaces
            .Setup(w => w.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IWorkspaceImplementationConfiguration>>.Success(
                [new WorkspaceImplementationConfiguration { Id = new Guid("33333333-0000-0000-0000-000000000001"), Name = "AFC" }]));

        var listed = await new TagOwners(context.Object, organizations.Object, workspaces.Object).List(TestContext.Current.CancellationToken);

        listed.IsSuccess.ShouldBeTrue();
        listed.Value!.Select(owner => $"{owner.Kind}:{owner.Name}").ShouldBe(["Tenant:AFC", "Organization:Finance", "Workspace:AFC"]);
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
