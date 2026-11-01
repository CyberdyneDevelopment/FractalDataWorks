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
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// Tests for the endpoints that make and change organizations (FDW-1758): an organization is made in the tenant the caller acts in, every
/// change is recorded in the audit at the Organization level naming the organization, and a failure is answered with no audit entry.
/// </summary>
[Trait("Category", "Security")]
public sealed class OrganizationAdminEndpointTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = new("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = new("44444444-0000-0000-0000-000000000009");
    private static readonly Guid Jo = new("22222222-0000-0000-0000-000000000001");

    private sealed class Create(IOrganizationFounding founding, IOrganizationProvider organizations, IAdministrativeAudit audit)
        : CreateOrganizationEndpointBase(NullLogger.Instance, founding, organizations, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class Update(IOrganizationWriter writer, IOrganizationProvider organizations, IAdministrativeAudit audit, ISystemRoleConfiguration roles)
        : UpdateOrganizationEndpointBase(NullLogger.Instance, writer, organizations, audit, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static Action<DefaultHttpContext> ActingIn(Guid? tenantId)
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity(tenantId is { } id ? [new Claim(ClaimDefinitions.tenantId.Name, id.ToString())] : [], "test"));

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

    private static Mock<IOrganizationProvider> Organizations(bool isActive = true, bool isDefault = false)
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations.Setup(o => o.Get(Legal, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(
            new OrganizationConfiguration { Id = Legal, TenantId = Tenant, Name = "Legal", Slug = "legal", IsActive = isActive, IsDefault = isDefault }));
        return organizations;
    }

    private static CreateOrganizationRequest MakeLegal()
        => new() { Name = "Legal", Slug = "legal", CopyRolesFrom = Finance, FirstAdministrator = Jo, AdministratorRole = "Administrator" };

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationIsMadeInTheCallersTenantAndRecordedAtTheOrganizationLevel()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var founding = new Mock<IOrganizationFounding>();
        founding.Setup(f => f.Found(It.Is<NewOrganization>(o => o.TenantId == Tenant && o.Slug == "legal"), Finance, Jo, "Administrator", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Success(Legal));
        var endpoint = Factory.Create<Create>(ActingIn(Tenant), founding.Object, Organizations().Object, Audit(recorded).Object);

        await endpoint.HandleAsync(MakeLegal(), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(201);
        endpoint.Response.Id.ShouldBe(Legal);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("OrganizationMade");
        entry.Level.Name.ShouldBe("Organization");
        entry.OrganizationId.ShouldBe(Legal);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWhoActsInNoTenantMakesNothing()
    {
        var founding = new Mock<IOrganizationFounding>();
        var endpoint = Factory.Create<Create>(ActingIn(null), founding.Object, Organizations().Object, Audit().Object);

        await endpoint.HandleAsync(MakeLegal(), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        founding.Verify(f => f.Found(It.IsAny<NewOrganization>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFoundingThatFailsIsAnsweredFromItsFailureAndRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var founding = new Mock<IOrganizationFounding>();
        founding.Setup(f => f.Found(It.IsAny<NewOrganization>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Failure(new GenericMessage("the person has no account")));
        var endpoint = Factory.Create<Create>(ActingIn(Tenant), founding.Object, Organizations().Object, Audit(recorded).Object);

        await endpoint.HandleAsync(MakeLegal(), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationMadeButNotRecordedIsAnswered500()
    {
        var founding = new Mock<IOrganizationFounding>();
        founding.Setup(f => f.Found(It.IsAny<NewOrganization>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Success(Legal));
        var endpoint = Factory.Create<Create>(ActingIn(Tenant), founding.Object, Organizations().Object, Audit(succeeds: false).Object);

        await endpoint.HandleAsync(MakeLegal(), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARenameAndADisableAreEachRecordedWithTheOldAndNewValue()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var writer = new Mock<IOrganizationWriter>();
        writer.Setup(w => w.Update(Legal, It.IsAny<OrganizationChange>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var endpoint = Factory.Create<Update>(ActingIn(Tenant), writer.Object, Organizations().Object, Audit(recorded).Object, Roles(false));

        await endpoint.HandleAsync(new UpdateOrganizationRequest { OrganizationId = Legal, Name = "Legal Affairs", IsActive = false }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        recorded.Count.ShouldBe(2);
        recorded[0].ChangeKind.ShouldBe("OrganizationRenamed");
        recorded[0].OldValue.ShouldBe("Legal");
        recorded[0].NewValue.ShouldBe("Legal Affairs");
        recorded[1].ChangeKind.ShouldBe("OrganizationDisabled");
        recorded.ShouldAllBe(entry => entry.OrganizationId == Legal && entry.Level.Name == "Organization");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AChangeToWhatTheOrganizationAlreadyIsRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var writer = new Mock<IOrganizationWriter>();
        writer.Setup(w => w.Update(Legal, It.IsAny<OrganizationChange>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var endpoint = Factory.Create<Update>(ActingIn(Tenant), writer.Object, Organizations().Object, Audit(recorded).Object, Roles(false));

        await endpoint.HandleAsync(new UpdateOrganizationRequest { OrganizationId = Legal, Name = "Legal", IsActive = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnotherTenantsCallerCannotChangeTheOrganizationButAPlatformAdministratorCan()
    {
        var writer = new Mock<IOrganizationWriter>();
        writer.Setup(w => w.Update(Legal, It.IsAny<OrganizationChange>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var refused = Factory.Create<Update>(ActingIn(OtherTenant), writer.Object, Organizations().Object, Audit().Object, Roles(false));
        var allowed = Factory.Create<Update>(ActingIn(OtherTenant), writer.Object, Organizations().Object, Audit().Object, Roles(true));

        await refused.HandleAsync(new UpdateOrganizationRequest { OrganizationId = Legal, IsActive = false }, CancellationToken.None);
        await allowed.HandleAsync(new UpdateOrganizationRequest { OrganizationId = Legal, IsActive = false }, CancellationToken.None);

        refused.HttpContext.Response.StatusCode.ShouldBe(403);
        allowed.HttpContext.Response.StatusCode.ShouldBe(200);
        writer.Verify(w => w.Update(Legal, It.IsAny<OrganizationChange>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AWriterThatRefusesIsAnsweredFromItsFailureAndRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var writer = new Mock<IOrganizationWriter>();
        writer.Setup(w => w.Update(Legal, It.IsAny<OrganizationChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage("the default organization cannot be disabled")));
        var endpoint = Factory.Create<Update>(ActingIn(Tenant), writer.Object, Organizations(isDefault: true).Object, Audit(recorded).Object, Roles(false));

        await endpoint.HandleAsync(new UpdateOrganizationRequest { OrganizationId = Legal, IsActive = false }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationThatDoesNotExistIsRefusedBeforeAnythingIsWritten()
    {
        var writer = new Mock<IOrganizationWriter>();
        var organizations = new Mock<IOrganizationProvider>();
        organizations.Setup(o => o.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("not found")));
        var endpoint = Factory.Create<Update>(ActingIn(Tenant), writer.Object, organizations.Object, Audit().Object, Roles(true));

        await endpoint.HandleAsync(new UpdateOrganizationRequest { OrganizationId = Guid.NewGuid(), IsActive = false }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        writer.Verify(w => w.Update(It.IsAny<Guid>(), It.IsAny<OrganizationChange>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
