using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-2338: a workspace may sit in an organization, and the organization must be one of the workspace's own tenant.
/// An organization that does not exist and one of another tenant are refused alike.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class WorkspaceOrganizationCheckTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    [Fact]
    public async Task AnOrganizationOfTheTenantIsAccepted()
    {
        var organization = new OrganizationConfiguration { Id = Guid.NewGuid(), TenantId = Tenant };

        var result = await Check(Organizations(organization), organization.Id, Tenant);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    public async Task AnOrganizationOfAnotherTenantIsRefused()
    {
        var organization = new OrganizationConfiguration { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() };

        var result = await Check(Organizations(organization), organization.Id, Tenant);

        result.Code.ShouldBeOfType<WorkspaceOrganizationInvalidCode>();
    }

    [Fact]
    public async Task ADeletedOrganizationIsRefused()
    {
        var organization = new OrganizationConfiguration { Id = Guid.NewGuid(), TenantId = Tenant, IsDeleted = true };

        var result = await Check(Organizations(organization), organization.Id, Tenant);

        result.Code.ShouldBeOfType<WorkspaceOrganizationInvalidCode>();
    }

    [Fact]
    public async Task AnUnknownOrganizationIsRefusedForTheSameReasonAsOneOfAnotherTenant()
    {
        var elsewhere = new OrganizationConfiguration { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() };
        var other = await Check(Organizations(elsewhere), elsewhere.Id, Tenant);
        var unknown = await Check(Organizations(null), Guid.NewGuid(), Tenant);

        unknown.Code.ShouldBeOfType<WorkspaceOrganizationInvalidCode>();
        Reason(unknown).ShouldBe(Reason(other));
    }

    [Fact]
    public async Task AnOrganizationTheStoreReportsMissingIsRefusedForTheSameReasonAsOneOfAnotherTenant()
    {
        var elsewhere = new OrganizationConfiguration { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() };
        var other = await Check(Organizations(elsewhere), elsewhere.Id, Tenant);
        var organizations = new Mock<IOrganizationProvider>();
        organizations
            .Setup(provider => provider.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(WorkspacesResultCodes.ByName("WorkspaceNotReadable")));

        var missing = await Check(organizations.Object, Guid.NewGuid(), Tenant);

        missing.Code.ShouldBeOfType<WorkspaceOrganizationInvalidCode>();
        Reason(missing).ShouldBe(Reason(other));
    }

    [Fact]
    public async Task ATenantThatIsNotKnownRefusesInsteadOfAssumingOne()
    {
        var organization = new OrganizationConfiguration { Id = Guid.NewGuid(), TenantId = Tenant };

        var result = await Check(Organizations(organization), organization.Id, null);

        result.Code.ShouldBeOfType<WorkspaceOrganizationInvalidCode>();
        Reason(result).ShouldContain("tenant is not known");
    }

    [Fact]
    public async Task AReadThatFailsIsReturnedAsItIs()
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations
            .Setup(provider => provider.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("organizations unavailable")));

        var result = await Check(organizations.Object, Guid.NewGuid(), Tenant);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldNotBeOfType<WorkspaceOrganizationInvalidCode>();
    }

    [Fact]
    public void TheResponsesCarryTheOrganizationOfTheWorkspace()
    {
        var organizationId = Guid.NewGuid();
        var workspace = new WorkspaceImplementationConfiguration { Id = Guid.NewGuid(), Name = "Quakes", OrganizationId = organizationId };
        var ownership = new WorkspaceOwnership(Tenant, null);

        WorkspaceResponseMapper.ToSummary(workspace, ownership).OrganizationId.ShouldBe(organizationId);
        WorkspaceResponseMapper.ToDetail(workspace, ownership).OrganizationId.ShouldBe(organizationId);
    }

    [Fact]
    public void AWorkspaceDirectlyUnderItsTenantAnswersNoOrganization()
    {
        var workspace = new WorkspaceImplementationConfiguration { Id = Guid.NewGuid(), Name = "Quakes" };
        var ownership = new WorkspaceOwnership(Tenant, null);

        WorkspaceResponseMapper.ToSummary(workspace, ownership).OrganizationId.ShouldBeNull();
        WorkspaceResponseMapper.ToDetail(workspace, ownership).OrganizationId.ShouldBeNull();
    }

    private static Task<IGenericResult> Check(IOrganizationProvider organizations, Guid organizationId, Guid? tenantId)
        => WorkspaceOrganizationCheck.Check(organizations, "Quakes", organizationId, tenantId, NullLogger.Instance, TestContext.Current.CancellationToken);

    private static IOrganizationProvider Organizations(OrganizationConfiguration? organization)
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations
            .Setup(provider => provider.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(organization!));
        return organizations.Object;
    }

    // The message names the organization that was asked about, which differs between the cases compared; the reason is what is left.
    private static string Reason(IGenericResult result)
        => Regex.Replace(result.CurrentMessage ?? string.Empty, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", "<id>", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
}
