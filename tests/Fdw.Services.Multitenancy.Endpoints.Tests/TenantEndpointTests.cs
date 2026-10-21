using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Clients.Models;
using Fdw.Services.Multitenancy.Endpoints;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// Tests for the endpoints that make and change tenants (FDW-1578): only a platform administrator may, the first administrator must
/// exist, every change is recorded in the audit, and a failure is answered from its result code with no audit entry.
/// </summary>
[Trait("Category", "Security")]
public sealed class TenantEndpointTests
{
    private static readonly Guid Administrator = new("22222222-0000-0000-0000-000000000001");
    private static readonly Guid TenantId = new("11111111-0000-0000-0000-000000000009");

    private sealed class CreateTenant(
        ITenantFounding founding, ITenantProvider tenants, IUserConfigurationProvider users, IAdministrativeAudit audit, ISystemRoleConfiguration roles)
        : CreateTenantEndpointBase(NullLogger.Instance, founding, tenants, users, audit, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class UpdateTenant(ITenantWriter writer, ITenantProvider tenants, IAdministrativeAudit audit, ISystemRoleConfiguration roles)
        : UpdateTenantEndpointBase(NullLogger.Instance, writer, tenants, audit, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static Action<DefaultHttpContext> Caller()
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test"));

    private static Mock<ISystemRoleConfiguration> Roles(bool platformAdmin)
    {
        var roles = new Mock<ISystemRoleConfiguration>();
        roles.Setup(r => r.IsPlatformAdmin(It.IsAny<ClaimsPrincipal>())).Returns(platformAdmin);
        return roles;
    }

    private static ITenant Tenant(string name, bool isActive, string? connection)
    {
        var tenant = new Mock<ITenant>();
        tenant.SetupGet(t => t.Id).Returns(TenantId);
        tenant.SetupGet(t => t.Slug).Returns("beta");
        tenant.SetupGet(t => t.DisplayName).Returns(name);
        tenant.SetupGet(t => t.IsActive).Returns(isActive);
        tenant.SetupGet(t => t.ConnectionName).Returns(connection);
        tenant.SetupGet(t => t.AvailableRoles).Returns(["Admin"]);
        return tenant.Object;
    }

    private static Mock<ITenantProvider> Tenants(ITenant tenant)
    {
        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(t => t.GetTenant(TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<ITenant>.Success(tenant));
        return tenants;
    }

    private static Mock<IAdministrativeAudit> Audit(List<AdministrativeChangeEntry>? recorded = null, bool succeeds = true)
    {
        var audit = new Mock<IAdministrativeAudit>();
        audit.Setup(a => a.Record(It.IsAny<AdministrativeChangeEntry>(), It.IsAny<CancellationToken>()))
            .Callback((AdministrativeChangeEntry entry, CancellationToken _) => recorded?.Add(entry))
            .ReturnsAsync(succeeds ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the audit store is down")));
        return audit;
    }

    private static Mock<IUserConfigurationProvider> Users(bool exists)
    {
        var user = new Mock<IUserImplementationConfiguration>();
        user.SetupGet(u => u.Id).Returns(Administrator);
        var found = exists
            ? GenericResult<IUserImplementationConfiguration>.Success(user.Object)
            : GenericResult<IUserImplementationConfiguration>.Failure(new GenericMessage("not found"));
        var users = new Mock<IUserConfigurationProvider>();
        users.Setup(u => u.Get(Administrator, It.IsAny<CancellationToken>())).ReturnsAsync(found);
        users.Setup(u => u.Get("vera", It.IsAny<CancellationToken>())).ReturnsAsync(found);
        return users;
    }

    private static CreateTenantRequest Request(string administrator = "22222222-0000-0000-0000-000000000001") => new()
    {
        Name = "Beta", Slug = "beta", FirstAdministrator = administrator, AdministratorRole = "Admin",
    };

    private static Mock<ITenantFounding> Founding(IGenericResult<Guid> outcome)
    {
        var founding = new Mock<ITenantFounding>();
        founding.Setup(f => f.Found(It.IsAny<NewTenant>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(outcome);
        return founding;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWhoIsNotAPlatformAdministratorMakesNoTenant()
    {
        var founding = Founding(GenericResult<Guid>.Success(TenantId));
        var endpoint = Factory.Create<CreateTenant>(Caller(), founding.Object, Tenants(Tenant("Beta", true, null)).Object, Users(true).Object, Audit().Object, Roles(false).Object);

        await endpoint.HandleAsync(Request(), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        founding.Verify(f => f.Found(It.IsAny<NewTenant>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFirstAdministratorWithNoAccountMakesNoTenant()
    {
        var founding = Founding(GenericResult<Guid>.Success(TenantId));
        var endpoint = Factory.Create<CreateTenant>(Caller(), founding.Object, Tenants(Tenant("Beta", true, null)).Object, Users(false).Object, Audit().Object, Roles(true).Object);

        await endpoint.HandleAsync(Request("vera"), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        founding.Verify(f => f.Found(It.IsAny<NewTenant>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantIsMadeByTheAdministratorsIdOrNameAndRecordedInTheAudit()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var founding = Founding(GenericResult<Guid>.Success(TenantId));
        var endpoint = Factory.Create<CreateTenant>(Caller(), founding.Object, Tenants(Tenant("Beta", true, null)).Object, Users(true).Object, Audit(recorded).Object, Roles(true).Object);

        await endpoint.HandleAsync(Request("vera"), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(201);
        founding.Verify(f => f.Found(It.Is<NewTenant>(t => t.Name == "Beta" && t.Slug == "beta"), Administrator, "Admin", It.IsAny<CancellationToken>()), Times.Once);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("TenantMade");
        entry.ItemId.ShouldBe(TenantId.ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFoundingThatFailsIsAnsweredFromItsFailureAndRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var founding = Founding(GenericResult<Guid>.Failure(new GenericMessage("the slug is taken")));
        var endpoint = Factory.Create<CreateTenant>(Caller(), founding.Object, Tenants(Tenant("Beta", true, null)).Object, Users(true).Object, Audit(recorded).Object, Roles(true).Object);

        await endpoint.HandleAsync(Request(), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AChangeThatCannotBeRecordedIsAnsweredWith500()
    {
        var founding = Founding(GenericResult<Guid>.Success(TenantId));
        var endpoint = Factory.Create<CreateTenant>(Caller(), founding.Object, Tenants(Tenant("Beta", true, null)).Object, Users(true).Object, Audit(succeeds: false).Object, Roles(true).Object);

        await endpoint.HandleAsync(Request(), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWhoIsNotAPlatformAdministratorChangesNoTenant()
    {
        var writer = new Mock<ITenantWriter>();
        var endpoint = Factory.Create<UpdateTenant>(Caller(), writer.Object, Tenants(Tenant("Beta", true, null)).Object, Audit().Object, Roles(false).Object);

        await endpoint.HandleAsync(new UpdateTenantRequest { Id = TenantId, Name = "Beta Corp" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        writer.Verify(w => w.Update(It.IsAny<Guid>(), It.IsAny<TenantChange>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantThatDoesNotExistIsNotFoundAndNothingIsWritten()
    {
        var writer = new Mock<ITenantWriter>();
        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(t => t.GetTenant(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<ITenant>.Failure(new GenericMessage("not found")));
        var endpoint = Factory.Create<UpdateTenant>(Caller(), writer.Object, tenants.Object, Audit().Object, Roles(true).Object);

        await endpoint.HandleAsync(new UpdateTenantRequest { Id = TenantId, Name = "Beta Corp" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        writer.Verify(w => w.Update(It.IsAny<Guid>(), It.IsAny<TenantChange>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARenameAndADisableAreWrittenOnceAndRecordedOneEntryEachWithOldAndNewValues()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var writer = new Mock<ITenantWriter>();
        writer.Setup(w => w.Update(TenantId, It.IsAny<TenantChange>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var endpoint = Factory.Create<UpdateTenant>(Caller(), writer.Object, Tenants(Tenant("Beta", true, null)).Object, Audit(recorded).Object, Roles(true).Object);

        await endpoint.HandleAsync(new UpdateTenantRequest { Id = TenantId, Name = "Beta Corp", IsActive = false }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        writer.Verify(w => w.Update(TenantId, It.Is<TenantChange>(c => c.Name == "Beta Corp" && c.IsActive == false), It.IsAny<CancellationToken>()), Times.Once);
        recorded.ConvertAll(e => e.ChangeKind).ShouldBe(["TenantRenamed", "TenantDisabled"]);
        recorded[0].OldValue.ShouldBe("Beta");
        recorded[0].NewValue.ShouldBe("Beta Corp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ARequestThatChangesNothingRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var writer = new Mock<ITenantWriter>();
        writer.Setup(w => w.Update(TenantId, It.IsAny<TenantChange>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var endpoint = Factory.Create<UpdateTenant>(Caller(), writer.Object, Tenants(Tenant("Beta", true, null)).Object, Audit(recorded).Object, Roles(true).Object);

        await endpoint.HandleAsync(new UpdateTenantRequest { Id = TenantId, Name = "Beta", IsActive = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AWriterThatRefusesTheChangeIsAnsweredFromItsFailureAndRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var writer = new Mock<ITenantWriter>();
        writer.Setup(w => w.Update(TenantId, It.IsAny<TenantChange>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Failure(new GenericMessage("the global tenant cannot be disabled")));
        var endpoint = Factory.Create<UpdateTenant>(Caller(), writer.Object, Tenants(Tenant("Beta", true, null)).Object, Audit(recorded).Object, Roles(true).Object);

        await endpoint.HandleAsync(new UpdateTenantRequest { Id = TenantId, IsActive = false }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        recorded.ShouldBeEmpty();
    }
}
