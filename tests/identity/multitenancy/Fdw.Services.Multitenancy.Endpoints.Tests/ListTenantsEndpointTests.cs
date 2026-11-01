using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Endpoints.Tests;

/// <summary>
/// FDW-2891: a platform administrator can list deleted companies to find one to restore. Nobody else can, and a list that does not ask
/// for them never reads them.
/// </summary>
[Trait("Category", "Security")]
public sealed class ListTenantsEndpointTests
{
    private static readonly Guid ActiveId = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid DeletedId = new("11111111-0000-0000-0000-000000000002");

    private sealed class ListTenants(ITenantProvider tenants, ISystemRoleConfiguration roles)
        : ListTenantsEndpointBase(NullLogger.Instance, tenants, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static Action<DefaultHttpContext> Caller()
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"));

    private static ISystemRoleConfiguration Roles(bool platformAdmin)
    {
        var roles = new Mock<ISystemRoleConfiguration>();
        roles.Setup(r => r.IsPlatformAdmin(It.IsAny<ClaimsPrincipal>())).Returns(platformAdmin);
        return roles.Object;
    }

    private static ITenant Tenant(Guid id, string slug)
    {
        var tenant = new Mock<ITenant>();
        tenant.SetupGet(t => t.Id).Returns(id);
        tenant.SetupGet(t => t.Slug).Returns(slug);
        tenant.SetupGet(t => t.DisplayName).Returns(slug);
        tenant.SetupGet(t => t.IsActive).Returns(true);
        tenant.SetupGet(t => t.Theme).Returns(new Mock<ITenantTheme>().Object);
        tenant.SetupGet(t => t.AvailableRoles).Returns(["Admin"]);
        return tenant.Object;
    }

    private static Mock<ITenantProvider> Tenants(IGenericResult<IEnumerable<ITenant>>? deleted = null)
    {
        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(t => t.GetActiveTenants(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ITenant>>.Success([Tenant(ActiveId, "active")]));
        tenants.Setup(t => t.GetDeletedTenants(It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleted ?? GenericResult<IEnumerable<ITenant>>.Success([Tenant(DeletedId, "deleted")]));
        return tenants;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWhoIsNotAPlatformAdministratorIsRefusedTheDeletedListAndNothingIsRead()
    {
        var tenants = Tenants();
        var endpoint = Factory.Create<ListTenants>(Caller(), tenants.Object, Roles(false));

        await endpoint.HandleAsync(new ListTenantsRequest { IncludeDeleted = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        tenants.Verify(t => t.GetDeletedTenants(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APlatformAdministratorGetsTheActiveTenantsAndTheDeletedOnesMarkedDeleted()
    {
        var endpoint = Factory.Create<ListTenants>(Caller(), Tenants().Object, Roles(true));

        await endpoint.HandleAsync(new ListTenantsRequest { IncludeDeleted = true }, CancellationToken.None);

        endpoint.Response.Single(t => t.Id == ActiveId).IsDeleted.ShouldBeFalse();
        endpoint.Response.Single(t => t.Id == DeletedId).IsDeleted.ShouldBeTrue();
        endpoint.Response.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AListThatDoesNotAskForDeletedTenantsNeverReadsThem()
    {
        var tenants = Tenants();
        var endpoint = Factory.Create<ListTenants>(Caller(), tenants.Object, Roles(true));

        await endpoint.HandleAsync(new ListTenantsRequest(), CancellationToken.None);

        endpoint.Response.ShouldHaveSingleItem().Id.ShouldBe(ActiveId);
        tenants.Verify(t => t.GetDeletedTenants(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AFailedReadOfDeletedTenantsIsAnsweredAsAFailureNotAsAShorterList()
    {
        var failed = GenericResult<IEnumerable<ITenant>>.Failure(new GenericMessage("the store is down"));
        var endpoint = Factory.Create<ListTenants>(Caller(), Tenants(failed).Object, Roles(true));

        await endpoint.HandleAsync(new ListTenantsRequest { IncludeDeleted = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }
}
