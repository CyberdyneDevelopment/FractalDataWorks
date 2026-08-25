using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
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

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for the 3-tier union in <see cref="EffectivePermissionResolver"/>:
/// global-tenant ∪ current-tenant ∪ current-org. This union is computed once at token-issue time
/// and baked into the access token as <c>perm</c> claims; per-request enforcement
/// (<see cref="DefaultAuthorizationService"/>) reads those baked claims, so the union itself is
/// validated here against the resolver directly.
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
    private static readonly Guid OrgId = Guid.NewGuid();
    // Why: real user IDs are Guids (JWT sub claim); tests must use parseable Guid strings
    // so that EffectivePermissionResolver.ApplyOrgTier succeeds Guid.TryParse and reaches the org tier.
    private static readonly Guid User1Id = Guid.NewGuid();
    private static readonly Guid User2Id = Guid.NewGuid();
    private static readonly Guid User3Id = Guid.NewGuid();

    // ──────────────────────────────────────────────────────────────────────────────
    // 3-tier union tests (resolver = token-issue baking path)
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task ThreeTierUnion_GlobalAndTenantAndOrg_AllPermissionsUnioned()
    {
        var resolver = BuildResolver(
            orgGrants: [new TenantOrgAccessConfiguration
            {
                UserId = User1Id, TenantId = TenantId, OrgId = OrgId,
                PermissionName = "org:read"
            }]);

        var result = await resolver.Resolve(User1Id.ToString(), TenantId, OrgId, isGlobalTenant: false, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("global:admin");
        result.Value!.ShouldContain("org:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task GlobalTenantAdmin_CanSeeAllPermissions_RegardlessOfTenantScope()
    {
        var resolver = BuildResolver(orgGrants: []);

        var result = await resolver.Resolve(User1Id.ToString(), TenantId, OrgId, isGlobalTenant: true, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("tenant:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task OrgOnlyGrant_ResolvesDirectPermission_WhenNoRoleTierMatch()
    {
        var resolver = BuildResolver(
            orgGrants: [new TenantOrgAccessConfiguration
            {
                UserId = User2Id, TenantId = TenantId, OrgId = OrgId,
                PermissionName = "org:read"
            }]);

        var result = await resolver.Resolve(User2Id.ToString(), TenantId, OrgId, isGlobalTenant: false, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("org:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task OrgTier_ResolvesDirectPermission_WhenOrgPresent()
    {
        var resolver = BuildResolver(
            orgGrants: [new TenantOrgAccessConfiguration
            {
                UserId = User3Id, TenantId = TenantId, OrgId = OrgId,
                PermissionName = "reports:read"
            }]);

        var result = await resolver.Resolve(User3Id.ToString(), TenantId, OrgId, isGlobalTenant: false, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("reports:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task TenantScopedRole_NotIncluded_WhenWrongTenantId()
    {
        var wrongTenantId = Guid.NewGuid();
        var resolver = BuildResolver(orgGrants: []);

        // TenantUser role is scoped to TenantId; resolving under a different tenant excludes tenant:read.
        var result = await resolver.Resolve(User1Id.ToString(), wrongTenantId, OrgId, isGlobalTenant: false, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldNotContain("tenant:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task OrgTierSkipped_WhenNoOrgContext()
    {
        var resolver = BuildResolver(
            orgGrants: [new TenantOrgAccessConfiguration
            {
                UserId = User1Id, TenantId = TenantId, OrgId = OrgId,
                PermissionName = "org:read"
            }]);

        // orgId null → org tier skipped → org:read not present.
        var result = await resolver.Resolve(User1Id.ToString(), TenantId, orgId: null, isGlobalTenant: false, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldNotContain("org:read");
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

    // Why: catalog gives user "1" both GlobalAdmin (global tier → global:admin) and TenantUser
    // (tenant-scoped to TenantId → tenant:read). FDW-532 fix requires explicit UserRole assignments
    // — the resolver no longer iterates the full catalog without scoping to the user's assignments.
    // Users "2", "3" etc. have no role assignments; they receive permissions only from the org tier.
    private static EffectivePermissionResolver BuildResolver(
        IReadOnlyList<TenantOrgAccessConfiguration> orgGrants)
    {
        var globalRole = new RoleConfiguration { Id = GlobalRoleId, Name = "GlobalAdmin", IsTenantScoped = false };
        var tenantRole = new RoleConfiguration { Id = TenantRoleId, Name = "TenantUser", IsTenantScoped = true, TenantId = TenantId };

        var globalPerm = new PermissionConfiguration { Id = GlobalPermId, Name = "global:admin" };
        var tenantPerm = new PermissionConfiguration { Id = TenantPermId, Name = "tenant:read" };

        var rolePermissions = new List<RolePermissionConfiguration>
        {
            new() { RoleId = GlobalRoleId, PermissionId = GlobalPermId },
            new() { RoleId = TenantRoleId, PermissionId = TenantPermId }
        };

        // Why: User1Id is explicitly assigned both GlobalAdmin (global scope) and TenantUser
        // (scoped to TenantId). GetByUser filters the full list by userId, so only User1Id
        // gets these roles; other userIds resolve to zero role-tier assignments (org-tier only).
        var userRoleAssignments = new List<UserRoleConfiguration>
        {
            new() { UserId = User1Id.ToString(), RoleId = GlobalRoleId, TenantId = null },
            new() { UserId = User1Id.ToString(), RoleId = TenantRoleId, TenantId = TenantId },
        };

        var roleProviderMock = MockProvider<RoleConfiguration, RoleConfigurationCommand>(
            new List<RoleConfiguration> { globalRole, tenantRole });
        var permProviderMock = MockProvider<PermissionConfiguration, PermissionConfigurationCommand>(
            new List<PermissionConfiguration> { globalPerm, tenantPerm });
        var rolePermProviderMock = MockProvider<RolePermissionConfiguration, RolePermissionConfigurationCommand>(
            rolePermissions);

        var userRoleProviderMock = new Mock<UserRoleConfigurationProvider>(
            MockBehavior.Loose,
            NullLogger<UserRoleConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => Mock.Of<IConfigurationGateway>()),
            "TestStore", "authz");
        // Why: CallBase = true lets GetByUser() delegate to its real body (calls Get() which is mocked).
        // Without CallBase, Loose mock returns null for the virtual GetByUser() causing NullRef.
        userRoleProviderMock.CallBase = true;
        userRoleProviderMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleConfiguration>>.Success(userRoleAssignments));

        var orgAccessMock = new Mock<IOrgAccessProvider>();
        orgAccessMock
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success(orgGrants));

        return new EffectivePermissionResolver(
            roleProviderMock.Object,
            permProviderMock.Object,
            rolePermProviderMock.Object,
            userRoleProviderMock.Object,
            NullLogger<EffectivePermissionResolver>.Instance,
            new Lazy<IOrgAccessProvider>(() => orgAccessMock.Object));
    }

    private static Mock<DefaultConfigurationProvider<TConfig, TCommand>> MockProvider<TConfig, TCommand>(
        List<TConfig> items)
        where TConfig : class, Fdw.Configuration.IGenericConfiguration
        where TCommand : Fdw.Services.Configuration.ConfigurationCommandBase<TConfig>
    {
        var mock = new Mock<DefaultConfigurationProvider<TConfig, TCommand>>(
            MockBehavior.Loose,
            NullLogger<DefaultConfigurationProvider<TConfig, TCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => Mock.Of<IConfigurationGateway>()),
            "TestStore", "cfg");
        mock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TConfig>>.Success(items));
        return mock;
    }
}
