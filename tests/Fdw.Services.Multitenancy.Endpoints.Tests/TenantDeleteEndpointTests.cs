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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// Tests for the endpoints that delete and restore a company (FDW-1759): only a platform administrator may, the writer's refusals are
/// answered from their codes with no audit entry, and each delete and restore is recorded in the audit at the Platform level.
/// </summary>
[Trait("Category", "Security")]
public sealed class TenantDeleteEndpointTests
{
    private static readonly Guid TenantId = new("11111111-0000-0000-0000-000000000009");

    private sealed class Delete(ITenantWriter writer, IAdministrativeAudit audit, ISystemRoleConfiguration roles)
        : DeleteTenantEndpointBase(NullLogger.Instance, writer, audit, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class Restore(ITenantWriter writer, IAdministrativeAudit audit, ISystemRoleConfiguration roles)
        : RestoreTenantEndpointBase(NullLogger.Instance, writer, audit, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static Action<DefaultHttpContext> Caller()
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test"));

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

    private static Mock<ITenantWriter> Writer(bool succeeds = true)
    {
        var writer = new Mock<ITenantWriter>();
        writer.Setup(w => w.SetDeleted(TenantId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(succeeds ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the company is still active")));
        return writer;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APlatformAdministratorDeletesACompanyAndItIsRecordedAtThePlatformLevel()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var writer = Writer();
        var endpoint = Factory.Create<Delete>(Caller(), writer.Object, Audit(recorded).Object, Roles(true));

        await endpoint.HandleAsync(new TenantKeyRequest { Id = TenantId }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        writer.Verify(w => w.SetDeleted(TenantId, true, It.IsAny<CancellationToken>()), Times.Once);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("TenantDeleted");
        entry.ItemId.ShouldBe(TenantId.ToString());
        entry.Level.Name.ShouldBe("Platform");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OnlyAPlatformAdministratorDeletesOrRestoresACompany()
    {
        var writer = Writer();
        var delete = Factory.Create<Delete>(Caller(), writer.Object, Audit().Object, Roles(false));
        var restore = Factory.Create<Restore>(Caller(), writer.Object, Audit().Object, Roles(false));

        await delete.HandleAsync(new TenantKeyRequest { Id = TenantId }, CancellationToken.None);
        await restore.HandleAsync(new TenantKeyRequest { Id = TenantId }, CancellationToken.None);

        delete.HttpContext.Response.StatusCode.ShouldBe(403);
        restore.HttpContext.Response.StatusCode.ShouldBe(403);
        writer.Verify(w => w.SetDeleted(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARestoreClearsTheMarkAndIsRecorded()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var writer = Writer();
        var endpoint = Factory.Create<Restore>(Caller(), writer.Object, Audit(recorded).Object, Roles(true));

        await endpoint.HandleAsync(new TenantKeyRequest { Id = TenantId }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        writer.Verify(w => w.SetDeleted(TenantId, false, It.IsAny<CancellationToken>()), Times.Once);
        recorded.ShouldHaveSingleItem().ChangeKind.ShouldBe("TenantRestored");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AWriterThatRefusesIsAnsweredFromItsFailureAndRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<Delete>(Caller(), Writer(succeeds: false).Object, Audit(recorded).Object, Roles(true));

        await endpoint.HandleAsync(new TenantKeyRequest { Id = TenantId }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADeleteMadeButNotRecordedIsAnswered500()
    {
        var endpoint = Factory.Create<Delete>(Caller(), Writer().Object, Audit(succeeds: false).Object, Roles(true));

        await endpoint.HandleAsync(new TenantKeyRequest { Id = TenantId }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }
}
