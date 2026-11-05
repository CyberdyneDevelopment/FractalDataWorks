using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-3668: an organization's default workspace is made with the id the organization names, in the organization's tenant, with every
/// lifecycle value stated and its own permissions, and a failure to give it permissions leaves no workspace behind.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class DefaultWorkspaceFoundingTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid CallerTenant = Guid.NewGuid();

    private sealed class Accessor : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; }
    }

    private static NewDefaultWorkspace Request(bool organizationIsDefault) => new(
        WorkspaceId: Guid.NewGuid(),
        TenantId: Tenant,
        TenantSlug: "afc",
        OrganizationId: Guid.NewGuid(),
        OrganizationName: "Finance",
        OrganizationSlug: "finance",
        OrganizationIsDefault: organizationIsDefault,
        OwnerUserId: Guid.NewGuid());

    private sealed class Fixture
    {
        public Mock<IWorkspaceConfigurationProvider> Workspaces { get; } = new();

        public Mock<IWorkspacePermissions> Permissions { get; } = new();

        public Accessor Authentication { get; } = new();

        public WorkspaceImplementationConfiguration? Saved { get; set; }

        public Guid? TenantWhenSaved { get; set; }

        public DefaultWorkspaceFounding Founding => new(Workspaces.Object, Permissions.Object, Authentication, NullLogger<DefaultWorkspaceFounding>.Instance);
    }

    private static Fixture Holding(bool saves = true, bool permits = true, bool removes = true)
    {
        var fixture = new Fixture();
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.ActiveTenantId).Returns(CallerTenant);
        fixture.Authentication.Current = caller.Object;

        fixture.Workspaces
            .Setup(w => w.Save(It.IsAny<WorkspaceImplementationConfiguration>(), "Workspace", "Workspace", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((WorkspaceImplementationConfiguration saved, string domain, string implementation, string name, CancellationToken token) =>
            {
                fixture.Saved = saved;
                fixture.TenantWhenSaved = fixture.Authentication.Current?.ActiveTenantId;
                return Task.FromResult(saves ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the workspace could not be saved")));
            });
        fixture.Workspaces.Setup(w => w.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(removes ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the workspace could not be removed")));
        fixture.Permissions.Setup(p => p.Create(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(permits ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the permissions could not be made")));
        fixture.Permissions.Setup(p => p.Remove(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(removes ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the permissions could not be removed")));
        return fixture;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task TheDefaultOrganizationsWorkspaceIsNamedForTheTenantAndMadeWithTheIdTheOrganizationNames()
    {
        var fixture = Holding();
        var request = Request(organizationIsDefault: true);

        var made = await fixture.Founding.Found(request, TestContext.Current.CancellationToken);

        made.IsSuccess.ShouldBeTrue();
        made.Value.ShouldBe(request.WorkspaceId);
        fixture.Saved!.Id.ShouldBe(request.WorkspaceId);
        fixture.Saved.Name.ShouldBe("afc-default");
        fixture.Saved.OrganizationId.ShouldBe(request.OrganizationId);
        fixture.Saved.OwnerUserId.ShouldBe(request.OwnerUserId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task AnotherOrganizationsWorkspaceCarriesItsSlugAndEveryLifecycleValueIsStated()
    {
        var fixture = Holding();

        var made = await fixture.Founding.Found(Request(organizationIsDefault: false), TestContext.Current.CancellationToken);

        made.IsSuccess.ShouldBeTrue();
        fixture.Saved!.Name.ShouldBe("afc-finance-default");
        fixture.Saved.Status.ShouldBe("Active");
        fixture.Saved.Visibility.ShouldBe("Private");
        fixture.Saved.JoinPolicy.ShouldBe("Closed");
        fixture.Saved.AutoApproveRole.ShouldBeNull();
        fixture.Saved.Implementation.ShouldBe("Workspace");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task TheWorkspaceIsSavedInTheOrganizationsTenantAndTheCallersContextIsPutBack()
    {
        var fixture = Holding();
        var caller = fixture.Authentication.Current;

        await fixture.Founding.Found(Request(organizationIsDefault: true), TestContext.Current.CancellationToken);

        fixture.TenantWhenSaved.ShouldBe(Tenant);
        fixture.Authentication.Current.ShouldBeSameAs(caller);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task AWorkspaceThatCannotBeSavedMakesNoPermissions()
    {
        var fixture = Holding(saves: false);

        var made = await fixture.Founding.Found(Request(organizationIsDefault: true), TestContext.Current.CancellationToken);

        made.IsFailure.ShouldBeTrue();
        fixture.Permissions.Verify(p => p.Create(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task PermissionsThatCannotBeMadeLeaveNoWorkspaceBehind()
    {
        var fixture = Holding(permits: false);

        var made = await fixture.Founding.Found(Request(organizationIsDefault: true), TestContext.Current.CancellationToken);

        made.IsFailure.ShouldBeTrue();
        fixture.Workspaces.Verify(w => w.Delete(fixture.Saved!.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task RemovingTheWorkspaceRemovesItsPermissionsAndItself()
    {
        var fixture = Holding();
        var request = Request(organizationIsDefault: true);

        var removed = await fixture.Founding.Remove(request, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue();
        fixture.Permissions.Verify(p => p.Remove(request.WorkspaceId, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Workspaces.Verify(w => w.Delete(request.WorkspaceId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
