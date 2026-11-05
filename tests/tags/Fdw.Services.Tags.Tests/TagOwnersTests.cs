using System.Threading.Tasks;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
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
}
