using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Authorization.Authorization;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Fdw.Services.Data;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for the 3-tier union in <see cref="UserPermissions"/>:
/// global-tenant ∪ current-tenant ∪ current-org. This union is computed once at token-issue time
/// and baked into the access token as <c>perm</c> claims; per-request enforcement
/// (<see cref="DefaultAuthorizationService"/>) reads those baked claims, so the union itself is
/// validated here against the userPermissions directly.
/// Also covers the <see cref="Authorization.FdwAuthorizationPolicyProvider"/> epPolicy: fix.
/// </summary>
public sealed class OrgAwareAuthorizationTests
{
    // -- Shared catalog GUIDs --
    private static readonly Guid GlobalRoleId = Guid.NewGuid();
    private static readonly Guid TenantRoleId = Guid.NewGuid();
    private static readonly Guid GlobalPermId = Guid.NewGuid();
    private static readonly Guid TenantPermId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    // The tenant a shared role is seeded in. A role's grants count only when its own tenant owns them.
    private static readonly Guid SharedTenantId = new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid User1Id = Guid.NewGuid();
    private static readonly Guid User2Id = Guid.NewGuid();
    private static readonly Guid User3Id = Guid.NewGuid();

    // ──────────────────────────────────────────────────────────────────────────────
    // 3-tier union tests (userPermissions = token-issue baking path)
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task ThreeTierUnion_GlobalAndTenantAndOrg_AllPermissionsUnioned()
    {
        var userPermissions = Build(
            orgGrants: [new TenantOrgAccessConfiguration
            {
                UserId = User1Id, TenantId = TenantId, OrgId = OrgId,
                PermissionName = "org:read"
            }]);

        var result = await userPermissions.Get(User1Id.ToString(), TenantId, OrgId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Permissions.ShouldContain("global:admin");
        result.Value!.Permissions.ShouldContain("org:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task RolesHeldInTheTenant_Contribute_WithTheOrgTier()
    {
        var userPermissions = Build(orgGrants: []);

        var result = await userPermissions.Get(User1Id.ToString(), TenantId, OrgId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Permissions.ShouldContain("tenant:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task OrgOnlyGrant_ResolvesDirectPermission_WhenNoRoleTierMatch()
    {
        var userPermissions = Build(
            orgGrants: [new TenantOrgAccessConfiguration
            {
                UserId = User2Id, TenantId = TenantId, OrgId = OrgId,
                PermissionName = "org:read"
            }]);

        var result = await userPermissions.Get(User2Id.ToString(), TenantId, OrgId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Permissions.ShouldContain("org:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task OrgTier_ResolvesDirectPermission_WhenOrgPresent()
    {
        var userPermissions = Build(
            orgGrants: [new TenantOrgAccessConfiguration
            {
                UserId = User3Id, TenantId = TenantId, OrgId = OrgId,
                PermissionName = "reports:read"
            }]);

        var result = await userPermissions.Get(User3Id.ToString(), TenantId, OrgId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Permissions.ShouldContain("reports:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task TenantScopedRole_NotIncluded_WhenWrongTenantId()
    {
        var wrongTenantId = Guid.NewGuid();
        var userPermissions = Build(orgGrants: []);

        // TenantUser role is scoped to TenantId; resolving under a different tenant excludes tenant:read.
        var result = await userPermissions.Get(User1Id.ToString(), wrongTenantId, OrgId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Permissions.ShouldNotContain("tenant:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task OrgTierSkipped_WhenNoOrgContext()
    {
        var userPermissions = Build(
            orgGrants: [new TenantOrgAccessConfiguration
            {
                UserId = User1Id, TenantId = TenantId, OrgId = OrgId,
                PermissionName = "org:read"
            }]);

        // orgId null → org tier skipped → org:read not present.
        var result = await userPermissions.Get(User1Id.ToString(), TenantId, orgId: null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Permissions.ShouldNotContain("org:read");
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // FdwAuthorizationPolicyProvider — epPolicy: fix
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task PolicyProvider_EpPolicyName_DelegatesToFallback()
    {
        var options = new Mock<IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>>();
        options.Setup(o => o.Value).Returns(new Microsoft.AspNetCore.Authorization.AuthorizationOptions());
        var provider = new Authorization.FdwAuthorizationPolicyProvider(options.Object, new Mock<ISystemRoleConfiguration>().Object);

        var policy = await provider.GetPolicyAsync("epPolicy:Reference.Api.Endpoints.ListUsersEndpoint");

        policy.ShouldBeNull();
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task PolicyProvider_RealPermissionName_ReturnsFdwPermissionRequirement()
    {
        var options = new Mock<IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>>();
        options.Setup(o => o.Value).Returns(new Microsoft.AspNetCore.Authorization.AuthorizationOptions());
        var provider = new Authorization.FdwAuthorizationPolicyProvider(options.Object, new Mock<ISystemRoleConfiguration>().Object);

        var policy = await provider.GetPolicyAsync("users:read");

        policy.ShouldNotBeNull();
        policy!.Requirements.Count.ShouldBe(1);
        policy.Requirements[0].ShouldBeOfType<FdwPermissionRequirement>();
        var req = (FdwPermissionRequirement)policy.Requirements[0];
        req.Resource.ShouldBe("users");
        req.Action.ShouldBe("read");
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────────

    private static UserPermissions Build(
        IReadOnlyList<TenantOrgAccessConfiguration> orgGrants)
    {
        var globalRole = new RoleImplementationConfiguration { Id = GlobalRoleId, Name = "GlobalAdmin", IsTenantScoped = false, TenantId = SharedTenantId };
        var tenantRole = new RoleImplementationConfiguration { Id = TenantRoleId, Name = "TenantUser", IsTenantScoped = true, TenantId = TenantId };

        var globalPerm = new PermissionImplementationConfiguration { Id = GlobalPermId, Name = "global:admin" };
        var tenantPerm = new PermissionImplementationConfiguration { Id = TenantPermId, Name = "tenant:read" };

        var rolePermissions = new List<RolePermissionImplementationConfiguration>
        {
            new() { RoleId = GlobalRoleId, PermissionId = GlobalPermId, TenantId = SharedTenantId },
            new() { RoleId = TenantRoleId, PermissionId = TenantPermId, TenantId = TenantId }
        };

        var userRoleAssignments = new List<UserRoleImplementationConfiguration>
        {
            new() { UserId = User1Id.ToString(), RoleId = GlobalRoleId, TenantId = TenantId },
            new() { UserId = User1Id.ToString(), RoleId = TenantRoleId, TenantId = TenantId },
        };

        var grants = UserPermissionGrantsFake.Of(
            new List<RoleImplementationConfiguration> { globalRole, tenantRole },
            new List<PermissionImplementationConfiguration> { globalPerm, tenantPerm },
            rolePermissions,
            userRoleAssignments);

        var orgAccessMock = new Mock<IOrgAccessProvider>();
        orgAccessMock
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success(orgGrants));

        return new UserPermissions(
            grants,
            new Fdw.Services.Authentication.Abstractions.Security.AuthenticationContextAccessor(),
            NullLogger<UserPermissions>.Instance,
            orgAccessMock.Object);
    }

}
