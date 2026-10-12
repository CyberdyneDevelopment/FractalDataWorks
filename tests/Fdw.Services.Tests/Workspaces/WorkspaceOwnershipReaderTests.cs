using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Workspaces;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// Verifies that a workspace's ownership names the tenant that owns it and the organization that names it as its
/// default (FDW-1101): a name is unique within a tenant only, so a caller who sees several tenants needs both.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class WorkspaceOwnershipReaderTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Workspaces")]
    public async Task NamesTheOrganizationWhoseDefaultItIs()
    {
        var workspace = Workspace();
        var organization = new OrganizationConfiguration { Id = Guid.NewGuid(), Name = "AFC", TenantId = Tenant, DefaultWorkspaceId = workspace.Id };

        var result = await WorkspaceOwnershipReader.Read(Organizations(organization), workspace, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.TenantId.ShouldBe(Tenant);
        result.Value.DefaultFor.ShouldBeSameAs(organization);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Workspaces")]
    public async Task NamesNoOrganizationWhenTheDefaultIsAnotherWorkspace()
    {
        var workspace = Workspace();
        var organization = new OrganizationConfiguration { Id = Guid.NewGuid(), Name = "AFC", TenantId = Tenant, DefaultWorkspaceId = Guid.NewGuid() };

        var result = await WorkspaceOwnershipReader.Read(Organizations(organization), workspace, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.DefaultFor.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Workspaces")]
    public async Task RefusesAWorkspaceReadWithNoTenant()
    {
        var workspace = Workspace();
        workspace.TenantId = null;

        var result = await WorkspaceOwnershipReader.Read(
            Organizations(new OrganizationConfiguration()), workspace, NullLogger.Instance, TestContext.Current.CancellationToken);

        // The column is NOT NULL; a read without it is a defect to surface, never a tenant to invent.
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Workspaces")]
    public async Task PassesOnAnOrganizationReadThatFailed()
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations
            .Setup(provider => provider.GetDefault(Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("no default organization")));

        var result = await WorkspaceOwnershipReader.Read(organizations.Object, Workspace(), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    private static WorkspaceImplementationConfiguration Workspace()
        => new() { Id = Guid.NewGuid(), Name = "afc-default", TenantId = Tenant };

    private static IOrganizationProvider Organizations(OrganizationConfiguration organization)
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations
            .Setup(provider => provider.GetDefault(Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(organization));
        return organizations.Object;
    }
}
