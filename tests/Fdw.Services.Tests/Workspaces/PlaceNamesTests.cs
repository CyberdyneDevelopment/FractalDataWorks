using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-2620: the name of an organization or a workspace is read from its configuration, and a place that does not exist, the tenant and a kind that is
/// not known have no name to show.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class PlaceNamesTests
{
    private static readonly Guid Finance = Guid.Parse("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Desk = Guid.Parse("66666666-0000-0000-0000-000000000001");

    private static PlaceNames Names(bool organizationDeleted = false, bool organizationHeld = true, bool workspaceHeld = true)
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations
            .Setup(o => o.Get(Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(
                organizationHeld ? new OrganizationConfiguration { Id = Finance, Name = "Finance", IsDeleted = organizationDeleted } : default!));
        var workspace = new Mock<IWorkspaceImplementationConfiguration>();
        workspace.SetupGet(w => w.Name).Returns("Desk");
        workspace.SetupGet(w => w.IsDeleted).Returns(false);
        var workspaces = new Mock<IWorkspaceConfigurationProvider>();
        workspaces
            .Setup(w => w.Get(Desk, ConfigurationLevels.RecordOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IWorkspaceImplementationConfiguration>.Success(workspaceHeld ? workspace.Object : default!));
        return new PlaceNames(organizations.Object, workspaces.Object, NullLogger<PlaceNames>.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnOrganizationAndAWorkspaceAreNamedByTheirConfiguration()
    {
        var names = Names();

        var organization = await names.Get(PlaceKinds.Organization, Finance, TestContext.Current.CancellationToken);
        var workspace = await names.Get(PlaceKinds.Workspace, Desk, TestContext.Current.CancellationToken);

        organization.Value.ShouldBe("Finance");
        workspace.Value.ShouldBe("Desk");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnOrganizationThatIsDeletedHasNoName()
    {
        var names = Names(organizationDeleted: true);

        var organization = await names.Get(PlaceKinds.Organization, Finance, TestContext.Current.CancellationToken);

        organization.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnOrganizationThatIsNotHeldHasNoName()
    {
        var names = Names(organizationHeld: false);

        var organization = await names.Get(PlaceKinds.Organization, Finance, TestContext.Current.CancellationToken);

        organization.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AWorkspaceThatIsNotHeldHasNoName()
    {
        var names = Names(workspaceHeld: false);

        var workspace = await names.Get(PlaceKinds.Workspace, Desk, TestContext.Current.CancellationToken);

        workspace.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    [InlineData("Tenant")]
    [InlineData("Team")]
    public async Task TheTenantAndAKindThatIsNotKnownHaveNoScopeName(string kind)
    {
        var names = Names();

        var named = await names.Get(kind, Finance, TestContext.Current.CancellationToken);

        named.IsFailure.ShouldBeTrue();
    }
}
