using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Fdw.Services.Data;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for <see cref="EffectivePermissionResolver"/> — the extracted 3-tier union logic
/// used at token-issue time (perm claims baking) and per-request authorization fallback.
///
/// Adversarial tests (FDW-532) verify that permission resolution is STRICTLY scoped to the
/// user's actual role assignments, not the full role catalog.
/// </summary>
public sealed class EffectivePermissionResolverTests
{
    // ----- Catalog fixtures -----
    private static readonly Guid GlobalRoleId = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid TenantRoleId = new("22222222-0000-0000-0000-000000000002");
    private static readonly Guid AdminRoleId  = new("aaaaaaaa-0000-0000-0000-000000000003");
    private static readonly Guid ViewerRoleId = new("bbbbbbbb-0000-0000-0000-000000000004");

    private static readonly Guid GlobalPermId  = new("33333333-0000-0000-0000-000000000003");
    private static readonly Guid TenantPermId  = new("44444444-0000-0000-0000-000000000004");
    private static readonly Guid AdminOnlyPermId = new("cccccccc-0000-0000-0000-000000000005");
    private static readonly Guid ViewerPermId1   = new("dddddddd-0000-0000-0000-000000000006");
    private static readonly Guid ViewerPermId2   = new("eeeeeeee-0000-0000-0000-000000000007");

    private static readonly Guid TenantId = new("55555555-0000-0000-0000-000000000005");
    private static readonly Guid OrgId    = new("66666666-0000-0000-0000-000000000006");
    // Why: real user IDs are Guids (JWT sub claim); the org-tier parse guard uses Guid.TryParse.
    private static readonly Guid User1Id  = new("00000001-0000-0000-0000-000000000001");
    private static readonly string User1IdStr = User1Id.ToString();

    // Full catalog used by adversarial tests: GlobalRole + TenantRole + AdminRole + ViewerRole.
    // Total catalog = 4 roles, 5 permissions.
    private static readonly RoleConfiguration[] AllRoles =
    [
        new() { Id = GlobalRoleId, Name = "GlobalAdmin",  IsTenantScoped = false },
        new() { Id = TenantRoleId, Name = "TenantUser",   IsTenantScoped = true, TenantId = TenantId },
        new() { Id = AdminRoleId,  Name = "Admin",        IsTenantScoped = false },
        new() { Id = ViewerRoleId, Name = "Viewer",       IsTenantScoped = false },
    ];

    private static readonly PermissionConfiguration[] AllPermissions =
    [
        new() { Id = GlobalPermId,   Name = "global:admin" },
        new() { Id = TenantPermId,   Name = "tenant:read" },
        new() { Id = AdminOnlyPermId, Name = "admin:delete" },
        new() { Id = ViewerPermId1,  Name = "viewer:read1" },
        new() { Id = ViewerPermId2,  Name = "viewer:read2" },
    ];

    private static readonly RolePermissionConfiguration[] AllRolePermissions =
    [
        new() { RoleId = GlobalRoleId, PermissionId = GlobalPermId },
        new() { RoleId = TenantRoleId, PermissionId = TenantPermId },
        new() { RoleId = AdminRoleId,  PermissionId = AdminOnlyPermId },
        new() { RoleId = AdminRoleId,  PermissionId = GlobalPermId },   // admin also has global:admin
        new() { RoleId = ViewerRoleId, PermissionId = ViewerPermId1 },
        new() { RoleId = ViewerRoleId, PermissionId = ViewerPermId2 },
    ];

    // ----- Builder helpers -----

    /// <summary>
    /// Builds a resolver backed by the FULL catalog (AllRoles / AllPermissions / AllRolePermissions),
    /// with user role assignments determined by <paramref name="userAssignments"/>.
    /// </summary>
    private static IEffectivePermissionResolver BuildResolverWithAssignments(
        IEnumerable<UserRoleConfiguration> userAssignments,
        IReadOnlyList<TenantOrgAccessConfiguration>? orgGrants = null,
        Lazy<IOrgAccessProvider>? orgAccessProvider = null)
    {
        var roleProvider     = MockCatalogProvider<RoleConfiguration, RoleConfigurationCommand>(AllRoles);
        var permProvider     = MockCatalogProvider<PermissionConfiguration, PermissionConfigurationCommand>(AllPermissions);
        var rolePermProvider = MockCatalogProvider<RolePermissionConfiguration, RolePermissionConfigurationCommand>(AllRolePermissions);
        var userRoleProvider = MockUserRoleProvider(userAssignments);

        if (orgAccessProvider is null && orgGrants is not null)
        {
            var orgMock = new Mock<IOrgAccessProvider>();
            orgMock
                .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success(orgGrants));
            orgAccessProvider = new Lazy<IOrgAccessProvider>(() => orgMock.Object);
        }

        return new EffectivePermissionResolver(
            roleProvider.Object,
            permProvider.Object,
            rolePermProvider.Object,
            userRoleProvider.Object,
            NullLogger<EffectivePermissionResolver>.Instance,
            orgAccessProvider);
    }

    /// <summary>
    /// Builds a resolver backed by the simple TWO-ROLE catalog (GlobalRole + TenantRole only),
    /// with user assigned to BOTH roles. Used by pre-FDW-532 smoke tests.
    /// </summary>
    private static IEffectivePermissionResolver BuildResolver(
        IReadOnlyList<TenantOrgAccessConfiguration>? orgGrants = null,
        Lazy<IOrgAccessProvider>? orgAccessProvider = null,
        string userId = "1",
        bool includeGlobalRole = true,
        bool includeTenantRole = true)
    {
        var assignments = new List<UserRoleConfiguration>();
        if (includeGlobalRole)
            assignments.Add(new UserRoleConfiguration { UserId = userId, RoleId = GlobalRoleId, TenantId = null });
        if (includeTenantRole)
            assignments.Add(new UserRoleConfiguration { UserId = userId, RoleId = TenantRoleId, TenantId = TenantId });

        return BuildResolverWithAssignments(assignments, orgGrants, orgAccessProvider);
    }

    // ----- Smoke tests (pre-FDW-532 compatibility) -----

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task Resolve_GlobalAndTenantAndOrg_UnionsAllThreeTiers()
    {
        // Arrange: user with User1Id, assigned to both GlobalAdmin + TenantUser
        var orgGrants = new List<TenantOrgAccessConfiguration>
        {
            new() { UserId = User1Id, TenantId = TenantId, OrgId = OrgId, PermissionName = "org:read" }
        };
        var sut = BuildResolver(orgGrants: orgGrants, userId: User1IdStr);

        // Act
        var result = await sut.Resolve(User1IdStr, TenantId, OrgId, isGlobalTenant: false, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("global:admin");   // global tier
        result.Value.ShouldContain("tenant:read");    // tenant tier
        result.Value.ShouldContain("org:read");       // org tier
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task Resolve_GlobalTenantFlag_IncludesTenantScopedRole()
    {
        // Arrange: isGlobalTenant = true → tenant-scoped role contributes even if tenantId doesn't match
        var differentTenantId = Guid.NewGuid();
        // User has TenantRole assignment scoped to original TenantId; isGlobalTenant overrides matching
        var sut = BuildResolver(orgGrants: [], userId: "99");

        // Act — passing a different tenantId but isGlobalTenant = true
        var result = await sut.Resolve("99", differentTenantId, orgId: null, isGlobalTenant: true, TestContext.Current.CancellationToken);

        // Assert — TenantUser role still contributes because isGlobalTenant = true
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("tenant:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task Resolve_OrgIdNull_SkipsOrgTier()
    {
        // Arrange: no org context
        var sut = BuildResolver(orgGrants: null, userId: "1");

        // Act
        var result = await sut.Resolve("1", TenantId, orgId: null, isGlobalTenant: false, TestContext.Current.CancellationToken);

        // Assert — global+tenant tiers present, no org permission
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.ShouldContain("global:admin");
        result.Value.ShouldContain("tenant:read");
        result.Value.ShouldNotContain("org:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task Resolve_RoleProviderFails_ReturnsFailure()
    {
        // Arrange: role provider returns failure — fail-closed
        var roleProviderMock = new Mock<ImplementationConfigurationProviderBase<RoleConfiguration, RoleConfigurationCommand>>(
            MockBehavior.Loose,
            NullLogger<ImplementationConfigurationProviderBase<RoleConfiguration, RoleConfigurationCommand>>.Instance,
            new ConfigurationGatewayProvider(),
            "TestStore", "cfg");
        roleProviderMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<RoleConfiguration>>.Failure(new GenericMessage("Role query failed")));

        var permProvider     = MockCatalogProvider<PermissionConfiguration, PermissionConfigurationCommand>(new[] { new PermissionConfiguration() });
        var rolePermProvider = MockCatalogProvider<RolePermissionConfiguration, RolePermissionConfigurationCommand>(Array.Empty<RolePermissionConfiguration>());
        var userRoleProvider = MockUserRoleProvider([]);

        var sut = new EffectivePermissionResolver(
            roleProviderMock.Object,
            permProvider.Object,
            rolePermProvider.Object,
            userRoleProvider.Object,
            NullLogger<EffectivePermissionResolver>.Instance);

        // Act
        var result = await sut.Resolve("1", TenantId, orgId: null, isGlobalTenant: false, TestContext.Current.CancellationToken);

        // Assert — fail-closed: provider failure returns failure result
        result.IsSuccess.ShouldBeFalse();
    }

    // ----- Adversarial tests (FDW-532) -----

    [Fact]
    [Trait("Category", "Authorization.FDW532")]
    public async Task Resolve_ViewerUser_GetsOnlyViewerPermissions_NotAdminOrGlobalOrTenantPerms()
    {
        // Arrange: Viewer user assigned ONLY the ViewerRole (2 read perms).
        // The catalog has 4 roles (including Admin with admin:delete + global:admin) and 5 perms total.
        // Before FDW-532 fix, this user would have received all 5 permissions.
        // After fix, they must receive only viewer:read1 and viewer:read2.
        var viewerAssignment = new UserRoleConfiguration
        {
            UserId = "viewer-user",
            RoleId = ViewerRoleId,
            TenantId = null // global role assignment
        };
        var sut = BuildResolverWithAssignments([viewerAssignment], orgGrants: []);

        // Act
        var result = await sut.Resolve("viewer-user", TenantId, orgId: null, isGlobalTenant: false, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        // Viewer MUST have their own permissions
        result.Value.ShouldContain("viewer:read1");
        result.Value.ShouldContain("viewer:read2");

        // Viewer MUST NOT have admin-only permissions (this was the escalation bug)
        result.Value.ShouldNotContain("admin:delete",
            "Viewer must not receive admin:delete — this was the FDW-532 privilege escalation");
        result.Value.ShouldNotContain("global:admin",
            "Viewer must not receive global:admin — they are not assigned the GlobalAdmin role");
        result.Value.ShouldNotContain("tenant:read",
            "Viewer must not receive tenant:read — they are not assigned the TenantUser role");

        // Critical count assertion: 2 viewer perms only, not the full 5-perm catalog
        result.Value.Count.ShouldBe(2,
            $"Viewer with 1 role (2 perms) must resolve to exactly 2 permissions, not {result.Value.Count} (which would indicate catalog bleed)");
    }

    [Fact]
    [Trait("Category", "Authorization.FDW532")]
    public async Task Resolve_AdminUser_GetsAdminPermissions_StrictlyAdminSet()
    {
        // Arrange: Admin user assigned ONLY the AdminRole (admin:delete + global:admin).
        // Before FDW-532 fix, admin would have gotten all 5 perms (including viewer perms).
        // After fix, they get exactly the 2 admin role permissions.
        var adminAssignment = new UserRoleConfiguration
        {
            UserId = "admin-user",
            RoleId = AdminRoleId,
            TenantId = null
        };
        var sut = BuildResolverWithAssignments([adminAssignment], orgGrants: []);

        // Act
        var result = await sut.Resolve("admin-user", TenantId, orgId: null, isGlobalTenant: false, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("admin:delete");
        result.Value.ShouldContain("global:admin");

        // Admin MUST NOT have viewer-only permissions
        result.Value.ShouldNotContain("viewer:read1",
            "Admin must not receive viewer:read1 — they are not assigned the Viewer role");
        result.Value.ShouldNotContain("viewer:read2",
            "Admin must not receive viewer:read2 — they are not assigned the Viewer role");
        result.Value.ShouldNotContain("tenant:read",
            "Admin must not receive tenant:read — they are not assigned the TenantUser role");

        result.Value.Count.ShouldBe(2,
            $"Admin with 1 role (2 perms) must resolve to exactly 2 permissions, not {result.Value.Count}");
    }

    [Fact]
    [Trait("Category", "Authorization.FDW532")]
    public async Task Resolve_UserWithNoRoleAssignments_GetsZeroPermissions()
    {
        // Arrange: user with NO role assignments at all.
        // Before FDW-532 fix, they would have received the FULL catalog (all 5 perms).
        // After fix, they must receive exactly 0 permissions.
        var sut = BuildResolverWithAssignments([], orgGrants: []);

        // Act
        var result = await sut.Resolve("unassigned-user", TenantId, orgId: null, isGlobalTenant: false, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue("Zero assignments is a valid state — empty result, not failure");
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(0,
            "User with no role assignments must get zero permissions, not the full catalog");
    }

    [Fact]
    [Trait("Category", "Authorization.FDW532")]
    public async Task Resolve_UserRoleProviderFails_ReturnsFailure_NotFullCatalog()
    {
        // Arrange: the user-role provider fails (e.g. DB unavailable).
        // Before FDW-532 fix (the bug): there was no user-role provider call at all;
        // the resolver would bake all perms regardless.
        // After fix: failure MUST return Failure (fail-closed). No token issued.
        var roleProvider     = MockCatalogProvider<RoleConfiguration, RoleConfigurationCommand>(AllRoles);
        var permProvider     = MockCatalogProvider<PermissionConfiguration, PermissionConfigurationCommand>(AllPermissions);
        var rolePermProvider = MockCatalogProvider<RolePermissionConfiguration, RolePermissionConfigurationCommand>(AllRolePermissions);

        // UserRoleProvider fails
        var userRoleProviderMock = new Mock<UserRoleConfigurationProvider>(
            MockBehavior.Loose,
            NullLogger<UserRoleConfigurationProvider>.Instance,
            new ConfigurationGatewayProvider(),
            "TestStore", "authz");
        // Why: CallBase = true lets GetByUser() run its real body (calls Get() → Failure).
        // Without it, Loose mock returns null for the virtual GetByUser() call.
        userRoleProviderMock.CallBase = true;
        userRoleProviderMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleConfiguration>>.Failure(new GenericMessage("DB unavailable")));

        var sut = new EffectivePermissionResolver(
            roleProvider.Object,
            permProvider.Object,
            rolePermProvider.Object,
            userRoleProviderMock.Object,
            NullLogger<EffectivePermissionResolver>.Instance);

        // Act
        var result = await sut.Resolve("any-user", TenantId, orgId: null, isGlobalTenant: false, TestContext.Current.CancellationToken);

        // Assert — MUST be failure, not a fallback to the full permission catalog
        result.IsSuccess.ShouldBeFalse(
            "When user role assignment load fails, token issuance must fail-closed — no partial or full catalog must be returned");
    }

    [Fact]
    [Trait("Category", "Authorization.FDW532")]
    public async Task Resolve_ViewerPermCountIsStrictSubsetOfTotalCatalog()
    {
        // Arrange: Viewer with 2 perms out of 5 total in catalog.
        // This test explicitly quantifies the attack surface closed by FDW-532.
        var viewerAssignment = new UserRoleConfiguration
        {
            UserId = "viewer-sub",
            RoleId = ViewerRoleId,
            TenantId = null
        };
        const int totalCatalogPermissions = 5; // admin:delete, global:admin, tenant:read, viewer:read1, viewer:read2
        // Why: 2 admin-only perms (admin:delete + global:admin) are explicitly checked via ShouldNotContain below.

        var sut = BuildResolverWithAssignments([viewerAssignment], orgGrants: []);
        var result = await sut.Resolve("viewer-sub", TenantId, orgId: null, isGlobalTenant: false, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        // Viewer count must be far below total catalog — not 88 real perms all assigned to everyone
        result.Value.Count.ShouldBeLessThan(totalCatalogPermissions,
            $"Viewer perm count ({result.Value.Count}) must be less than total catalog ({totalCatalogPermissions})");

        // No admin-only perms
        result.Value.ShouldNotContain("admin:delete");
        result.Value.ShouldNotContain("global:admin");

        // Explicitly verify the "escalation gap": before the fix a viewer would get ALL perms
        var unexpectedPermsIfBugExists = new[] { "admin:delete", "global:admin", "tenant:read" };
        var leakedPerms = unexpectedPermsIfBugExists.Where(p => result.Value.Contains(p)).ToList();
        leakedPerms.ShouldBeEmpty(
            $"FDW-532 regression: viewer received elevated permissions: [{string.Join(", ", leakedPerms)}]");
    }

    [Fact]
    [Trait("Category", "Authorization.FDW532")]
    public async Task Resolve_UserAssignedToMultipleRoles_UnionsOnlyAssignedRoles()
    {
        // Arrange: user assigned to both ViewerRole and AdminRole (not TenantUser).
        // Should get viewer:read1, viewer:read2, admin:delete, global:admin (4 perms).
        // Must NOT get tenant:read (not assigned to TenantUser role).
        var assignments = new List<UserRoleConfiguration>
        {
            new() { UserId = "multi-role-user", RoleId = ViewerRoleId, TenantId = null },
            new() { UserId = "multi-role-user", RoleId = AdminRoleId,  TenantId = null },
        };
        var sut = BuildResolverWithAssignments(assignments, orgGrants: []);

        var result = await sut.Resolve("multi-role-user", TenantId, orgId: null, isGlobalTenant: false, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("viewer:read1");
        result.Value.ShouldContain("viewer:read2");
        result.Value.ShouldContain("admin:delete");
        result.Value.ShouldContain("global:admin");
        result.Value.ShouldNotContain("tenant:read",
            "User is not assigned TenantUser role — tenant:read must not appear");
        result.Value.Count.ShouldBe(4);
    }

    [Fact]
    [Trait("Category", "Authorization.FDW532")]
    public async Task Resolve_TenantScopedAssignment_OnlyContributesForMatchingTenant()
    {
        // Arrange: user has tenant-scoped assignment for TenantId only.
        // When resolving for a DIFFERENT tenant, that tenant role must not contribute.
        var otherTenantId = Guid.NewGuid();
        var tenantAssignment = new UserRoleConfiguration
        {
            UserId = "tenant-user",
            RoleId = TenantRoleId,
            TenantId = TenantId // assigned only for TenantId
        };
        var sut = BuildResolverWithAssignments([tenantAssignment], orgGrants: []);

        // Act: resolve for a DIFFERENT tenant (not the one the role is scoped to)
        var result = await sut.Resolve("tenant-user", otherTenantId, orgId: null, isGlobalTenant: false, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        // The user's assignment targets TenantId but we're resolving for otherTenantId.
        // Assignment-tier filter: TenantId != otherTenantId → role excluded from assignedRoleIds.
        result.Value.Count.ShouldBe(0,
            "User has a tenant-scoped assignment for a different tenant — zero perms for this tenant context");
    }

    // ----- Mock helpers -----

    private static Mock<ImplementationConfigurationProviderBase<TConfig, TCommand>> MockCatalogProvider<TConfig, TCommand>(
        IEnumerable<TConfig> items)
        where TConfig : class, Fdw.Configuration.IGenericConfiguration
        where TCommand : ConfigurationCommandBase<TConfig>
    {
        var mock = new Mock<ImplementationConfigurationProviderBase<TConfig, TCommand>>(
            MockBehavior.Loose,
            NullLogger<ImplementationConfigurationProviderBase<TConfig, TCommand>>.Instance,
            new ConfigurationGatewayProvider(),
            "TestStore", "cfg");
        mock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TConfig>>.Success(new List<TConfig>(items)));
        return mock;
    }

    private static Mock<UserRoleConfigurationProvider> MockUserRoleProvider(
        IEnumerable<UserRoleConfiguration> assignments)
    {
        var list = new List<UserRoleConfiguration>(assignments);
        var mock = new Mock<UserRoleConfigurationProvider>(
            MockBehavior.Loose,
            NullLogger<UserRoleConfigurationProvider>.Instance,
            new ConfigurationGatewayProvider(),
            "TestStore", "authz");
        // Why: CallBase = true lets GetByUser() delegate to its real body, which calls Get().
        // Get() is mocked to return the seeded list so GetByUser() filters by userId as normal.
        // Without CallBase, Moq returns null for the virtual GetByUser() call (Loose mock default).
        mock.CallBase = true;
        mock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleConfiguration>>.Success(list));
        return mock;
    }
}
