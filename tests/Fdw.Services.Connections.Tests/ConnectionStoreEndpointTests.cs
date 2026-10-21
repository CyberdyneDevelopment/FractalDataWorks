using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// Tests for the endpoints over connection grants and assignments (FDW-1581): grants are for platform administrators, a tenant reads
/// only its own, every change is recorded in the audit, and a change that cannot be recorded answers 500.
/// </summary>
[Trait("Category", "Security")]
public sealed class ConnectionStoreEndpointTests
{
    private static readonly Guid Tenant = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = new("11111111-0000-0000-0000-000000000002");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid GrantId = new("55555555-0000-0000-0000-000000000001");

    private sealed class Grant(IConnectionGrants grants, IAdministrativeAudit audit, ISystemRoleConfiguration roles)
        : GrantConnectionEndpointBase(NullLogger.Instance, grants, audit, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class Revoke(IConnectionGrants grants, IAdministrativeAudit audit, ISystemRoleConfiguration roles)
        : RevokeConnectionGrantEndpointBase(NullLogger.Instance, grants, audit, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class ListGrants(IConnectionGrants grants, ISystemRoleConfiguration roles)
        : ListConnectionGrantsEndpointBase(NullLogger.Instance, grants, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class Assign(IConnectionAssignments assignments, IAdministrativeAudit audit)
        : AssignOrganizationConnectionEndpointBase(NullLogger.Instance, assignments, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class Unassign(IConnectionAssignments assignments, IAdministrativeAudit audit)
        : UnassignOrganizationConnectionEndpointBase(NullLogger.Instance, assignments, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static Action<DefaultHttpContext> ActingIn(Guid tenantId)
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimDefinitions.tenantId.Name, tenantId.ToString())], "test"));

    private static ISystemRoleConfiguration Roles(bool platformAdmin)
    {
        var roles = new Mock<ISystemRoleConfiguration>();
        roles.Setup(r => r.IsPlatformAdmin(It.IsAny<ClaimsPrincipal>())).Returns(platformAdmin);
        return roles.Object;
    }

    private static Mock<IAdministrativeAudit> Audit(List<AdministrativeChangeEntry>? recorded = null, bool succeeds = true)
    {
        var audit = new Mock<IAdministrativeAudit>();
        audit.Setup(a => a.Record(It.IsAny<AdministrativeChangeEntry>(), It.IsAny<CancellationToken>()))
            .Callback((AdministrativeChangeEntry entry, CancellationToken _) => recorded?.Add(entry))
            .ReturnsAsync(succeeds ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the audit store is down")));
        return audit;
    }

    private static ConnectionGrantView AGrant => new(GrantId, Tenant, "RayburnDocs", "documents", "/tenants/a", false);

    private static ConnectionAssignmentView AnAssignment => new(Finance, "documents", GrantId, "RayburnDocs", "/tenants/a", "finance");

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AGrantByAPlatformAdministratorIsAnswered201AndRecordedAtThePlatformLevel()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var grants = new Mock<IConnectionGrants>();
        grants.Setup(g => g.Grant(Tenant, "RayburnDocs", "documents", "/tenants/a", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionGrantView>.Success(AGrant));
        var endpoint = Factory.Create<Grant>(ActingIn(OtherTenant), grants.Object, Audit(recorded).Object, Roles(true));

        await endpoint.HandleAsync(new ConnectionGrantRequest { TenantId = Tenant, ConnectionName = "RayburnDocs", Purpose = "documents", Folder = "/tenants/a" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(201);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("ConnectionGranted");
        entry.ItemId.ShouldBe($"{Tenant}:documents:RayburnDocs");
        entry.Level.Name.ShouldBe("Platform");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AGrantByAnyoneElseIsRefusedAndNothingIsGranted()
    {
        var grants = new Mock<IConnectionGrants>();
        var endpoint = Factory.Create<Grant>(ActingIn(Tenant), grants.Object, Audit().Object, Roles(false));

        await endpoint.HandleAsync(new ConnectionGrantRequest { TenantId = Tenant, ConnectionName = "RayburnDocs", Purpose = "documents" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        grants.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AGrantTheServiceRefusesIsAnsweredFromItsFailureAndRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var grants = new Mock<IConnectionGrants>();
        grants.Setup(g => g.Grant(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionGrantView>.Failure(new GenericMessage("the connection is not a document connection")));
        var endpoint = Factory.Create<Grant>(ActingIn(Tenant), grants.Object, Audit(recorded).Object, Roles(true));

        await endpoint.HandleAsync(new ConnectionGrantRequest { TenantId = Tenant, ConnectionName = "Nope", Purpose = "documents" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AGrantMadeButNotRecordedIsAnswered500()
    {
        var grants = new Mock<IConnectionGrants>();
        grants.Setup(g => g.Grant(Tenant, "RayburnDocs", "documents", null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionGrantView>.Success(AGrant));
        var endpoint = Factory.Create<Grant>(ActingIn(Tenant), grants.Object, Audit(succeeds: false).Object, Roles(true));

        await endpoint.HandleAsync(new ConnectionGrantRequest { TenantId = Tenant, ConnectionName = "RayburnDocs", Purpose = "documents" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARevokeByAPlatformAdministratorIsAnswered204AndRecordedWithTheOldValue()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var grants = new Mock<IConnectionGrants>();
        grants.Setup(g => g.Revoke(Tenant, "RayburnDocs", "documents", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionGrantView>.Success(AGrant));
        var endpoint = Factory.Create<Revoke>(ActingIn(OtherTenant), grants.Object, Audit(recorded).Object, Roles(true));

        await endpoint.HandleAsync(new ConnectionGrantKeyRequest { TenantId = Tenant, ConnectionName = "RayburnDocs", Purpose = "documents" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("ConnectionRevoked");
        entry.OldValue.ShouldNotBeNull();
        entry.NewValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARevokeByAnyoneElseIsRefused()
    {
        var grants = new Mock<IConnectionGrants>();
        var endpoint = Factory.Create<Revoke>(ActingIn(Tenant), grants.Object, Audit().Object, Roles(false));

        await endpoint.HandleAsync(new ConnectionGrantKeyRequest { TenantId = Tenant, ConnectionName = "RayburnDocs", Purpose = "documents" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        grants.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantReadsItsOwnGrants()
    {
        var grants = new Mock<IConnectionGrants>();
        grants.Setup(g => g.List(Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ConnectionGrantView>>.Success([AGrant]));
        var endpoint = Factory.Create<ListGrants>(ActingIn(Tenant), grants.Object, Roles(false));

        await endpoint.HandleAsync(new TenantConnectionsRequest { TenantId = Tenant }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Grants.ShouldHaveSingleItem().ConnectionName.ShouldBe("RayburnDocs");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantCannotReadAnotherTenantsGrants()
    {
        var grants = new Mock<IConnectionGrants>();
        var endpoint = Factory.Create<ListGrants>(ActingIn(OtherTenant), grants.Object, Roles(false));

        await endpoint.HandleAsync(new TenantConnectionsRequest { TenantId = Tenant }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        grants.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APlatformAdministratorReadsAnyTenantsGrants()
    {
        var grants = new Mock<IConnectionGrants>();
        grants.Setup(g => g.List(Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ConnectionGrantView>>.Success([AGrant]));
        var endpoint = Factory.Create<ListGrants>(ActingIn(OtherTenant), grants.Object, Roles(true));

        await endpoint.HandleAsync(new TenantConnectionsRequest { TenantId = Tenant }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnAssignmentIsAnswered200AndRecordedAtTheOrganizationLevel()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var assignments = new Mock<IConnectionAssignments>();
        assignments.Setup(a => a.Assign(Finance, "documents", GrantId, "finance", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionAssignmentView>.Success(AnAssignment));
        var endpoint = Factory.Create<Assign>(ActingIn(Tenant), assignments.Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new OrganizationConnectionRequest { OrganizationId = Finance, Purpose = "documents", GrantId = GrantId, BaseFolder = "finance" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("ConnectionAssigned");
        entry.OrganizationId.ShouldBe(Finance);
        entry.Level.Name.ShouldBe("Organization");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnAssignmentTheServiceRefusesIsAnsweredFromItsFailureAndRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var assignments = new Mock<IConnectionAssignments>();
        assignments.Setup(a => a.Assign(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionAssignmentView>.Failure(new GenericMessage("the grant belongs to another tenant")));
        var endpoint = Factory.Create<Assign>(ActingIn(Tenant), assignments.Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new OrganizationConnectionRequest { OrganizationId = Finance, Purpose = "documents", GrantId = GrantId }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnUnassignmentIsAnswered204AndRecordedWithTheOldValue()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var assignments = new Mock<IConnectionAssignments>();
        assignments.Setup(a => a.Unassign(Finance, "documents", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionAssignmentView>.Success(AnAssignment));
        var endpoint = Factory.Create<Unassign>(ActingIn(Tenant), assignments.Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new OrganizationConnectionKeyRequest { OrganizationId = Finance, Purpose = "documents" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("ConnectionUnassigned");
        entry.OldValue.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnUnassignmentNotRecordedIsAnswered500()
    {
        var assignments = new Mock<IConnectionAssignments>();
        assignments.Setup(a => a.Unassign(Finance, "documents", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionAssignmentView>.Success(AnAssignment));
        var endpoint = Factory.Create<Unassign>(ActingIn(Tenant), assignments.Object, Audit(succeeds: false).Object);

        await endpoint.HandleAsync(new OrganizationConnectionKeyRequest { OrganizationId = Finance, Purpose = "documents" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }
}
