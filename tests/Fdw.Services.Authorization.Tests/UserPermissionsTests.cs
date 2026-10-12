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
using Fdw.Services.Abstractions;
using Moq;
using Shouldly;
using Fdw.Services.Data;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for <see cref="UserPermissions"/> — the roles held in the tenant being resolved, plus the org tier
/// used at token-issue time (perm claims baking) and per-request authorization fallback.
///
/// Adversarial tests (FDW-532) verify that permission resolution is STRICTLY scoped to the
/// user's actual role assignments, not the full role catalog.
/// </summary>
public sealed class UserPermissionsTests
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

    // The tenant a shared role is seeded in. A role's grants count only when its own tenant owns them.
    private static readonly Guid SharedTenantId = new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid OrgId    = new("66666666-0000-0000-0000-000000000006");
    private static readonly Guid User1Id  = new("00000001-0000-0000-0000-000000000001");
    private static readonly string User1IdStr = User1Id.ToString();

    // Full catalog used by adversarial tests: GlobalRole + TenantRole + AdminRole + ViewerRole.
    // Total catalog = 4 roles, 5 permissions.
    private static readonly RoleImplementationConfiguration[] AllRoles =
    [
        new() { Id = GlobalRoleId, Name = "GlobalAdmin",  IsTenantScoped = false, TenantId = SharedTenantId },
        new() { Id = TenantRoleId, Name = "TenantUser",   IsTenantScoped = true, TenantId = TenantId },
        new() { Id = AdminRoleId,  Name = "Admin",        IsTenantScoped = false, TenantId = SharedTenantId },
        new() { Id = ViewerRoleId, Name = "Viewer",       IsTenantScoped = false, TenantId = SharedTenantId },
    ];

    private static readonly PermissionImplementationConfiguration[] AllPermissions =
    [
        new() { Id = GlobalPermId,   Name = "global:admin" },
        new() { Id = TenantPermId,   Name = "tenant:read" },
        new() { Id = AdminOnlyPermId, Name = "admin:delete" },
        new() { Id = ViewerPermId1,  Name = "viewer:read1" },
        new() { Id = ViewerPermId2,  Name = "viewer:read2" },
    ];

    private static readonly RolePermissionImplementationConfiguration[] AllRolePermissions =
    [
        new() { RoleId = GlobalRoleId, PermissionId = GlobalPermId, TenantId = SharedTenantId },
        new() { RoleId = TenantRoleId, PermissionId = TenantPermId, TenantId = TenantId },
        new() { RoleId = AdminRoleId,  PermissionId = AdminOnlyPermId, TenantId = SharedTenantId },
        new() { RoleId = AdminRoleId,  PermissionId = GlobalPermId, TenantId = SharedTenantId },   // admin also has global:admin
        new() { RoleId = ViewerRoleId, PermissionId = ViewerPermId1, TenantId = SharedTenantId },
        new() { RoleId = ViewerRoleId, PermissionId = ViewerPermId2, TenantId = SharedTenantId },
    ];

    // ----- Builder helpers -----

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ResolveIgnoresAGrantATenantWroteOntoASharedRoleInEveryTenant()
    {
        // FDW-1072: tenant B's admin added a grant to the shared Viewer role. The catalogue is read across
        // every tenant, so counting it would give every Viewer everywhere that permission. It counts nowhere,
        // not even in tenant B: a role grants what its owning tenant wrote.
        var tenantB = new Guid("77777777-0000-0000-0000-00000000000b");
        var grants = AllRolePermissions
            .Append(new RolePermissionImplementationConfiguration { RoleId = ViewerRoleId, PermissionId = AdminOnlyPermId, TenantId = tenantB })
            .ToArray();
        UserPermissions For(Guid tenant) => new(
            ConfigurationCatalog.Roles(AllRoles),
            ConfigurationCatalog.Permissions(AllPermissions),
            ConfigurationCatalog.RolePermissions(grants),
            ConfigurationCatalog.UserRoles([new UserRoleImplementationConfiguration { UserId = User1IdStr, RoleId = ViewerRoleId, TenantId = tenant }]),
            new Fdw.Services.Authentication.Abstractions.Security.AuthenticationContextAccessor(),
            NullLogger<UserPermissions>.Instance);

        var inA = await For(TenantId).Get(User1IdStr, TenantId, orgId: null, TestContext.Current.CancellationToken);
        var inB = await For(tenantB).Get(User1IdStr, tenantB, orgId: null, TestContext.Current.CancellationToken);

        var a = inA.Value.ShouldNotBeNull();
        var b = inB.Value.ShouldNotBeNull();
        a.Permissions.ShouldNotContain("admin:delete");
        b.Permissions.ShouldNotContain("admin:delete");
        a.Permissions.ShouldContain("viewer:read1");
    }

    /// <summary>
    /// Builds a UserPermissions backed by the FULL catalog (AllRoles / AllPermissions / AllRolePermissions),
    /// with user role assignments determined by <paramref name="userAssignments"/>.
    /// </summary>
    private static UserPermissions BuildWithAssignments(
        IEnumerable<UserRoleImplementationConfiguration> userAssignments,
        IReadOnlyList<TenantOrgAccessConfiguration>? orgGrants = null,
        IOrgAccessProvider? orgAccessProvider = null)
    {
        var roleProvider     = ConfigurationCatalog.Roles(AllRoles);
        var permProvider     = ConfigurationCatalog.Permissions(AllPermissions);
        var rolePermProvider = ConfigurationCatalog.RolePermissions(AllRolePermissions);
        var userRoleProvider = ConfigurationCatalog.UserRoles(userAssignments);

        if (orgAccessProvider is null && orgGrants is not null)
        {
            var orgMock = new Mock<IOrgAccessProvider>();
            orgMock
                .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success(orgGrants));
            orgAccessProvider = orgMock.Object;
        }

        return new UserPermissions(
            roleProvider,
            permProvider,
            rolePermProvider,
            userRoleProvider,
            new Fdw.Services.Authentication.Abstractions.Security.AuthenticationContextAccessor(),
            NullLogger<UserPermissions>.Instance,
            orgAccessProvider);
    }

    /// <summary>
    /// Builds a UserPermissions backed by the simple TWO-ROLE catalog (GlobalRole + TenantRole only),
    /// with user assigned to BOTH roles. Used by pre-FDW-532 smoke tests.
    /// </summary>
    private static UserPermissions Build(
        IReadOnlyList<TenantOrgAccessConfiguration>? orgGrants = null,
        IOrgAccessProvider? orgAccessProvider = null,
        string userId = "00000000-0000-0000-0000-000000000002",
        bool includeGlobalRole = true,
        bool includeTenantRole = true)
    {
        var assignments = new List<UserRoleImplementationConfiguration>();
        if (includeGlobalRole)
            assignments.Add(new UserRoleImplementationConfiguration { UserId = userId, RoleId = GlobalRoleId, TenantId = TenantId });
        if (includeTenantRole)
            assignments.Add(new UserRoleImplementationConfiguration { UserId = userId, RoleId = TenantRoleId, TenantId = TenantId });

        return BuildWithAssignments(assignments, orgGrants, orgAccessProvider);
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
        var sut = Build(orgGrants: orgGrants, userId: User1IdStr);

        // Act
        var result = await sut.Get(User1IdStr, TenantId, OrgId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Permissions.ShouldContain("global:admin");   // global tier
        result.Value.Permissions.ShouldContain("tenant:read");    // tenant tier
        result.Value.Permissions.ShouldContain("org:read");       // org tier
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task Resolve_ForAnotherTenant_RolesHeldElsewhereContributeNothing()
    {
        // Arrange: both roles are held in TenantId. Resolving for any other tenant -- the global
        // tenant included -- must not carry them: roles are held per tenant (FDW-825). Acting in the
        // global tenant used to union every tenant's roles, which made an Admin of one tenant an
        // Admin of the platform the moment they held any membership in global.
        var globalTenantId = Guid.NewGuid();
        var sut = Build(orgGrants: [], userId: "00000000-0000-0000-0000-000000000001");

        // Act
        var result = await sut.Get("00000000-0000-0000-0000-000000000001", globalTenantId, orgId: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Permissions.ShouldBeEmpty();
        result.Value.RoleNames.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task Resolve_NoTenant_HoldsNoRoles()
    {
        // Arrange: a caller acting in no tenant holds no roles -- there is no "global assignment"
        // tier to fall back to.
        var sut = Build(orgGrants: [], userId: "00000000-0000-0000-0000-000000000001");

        // Act
        var result = await sut.Get("00000000-0000-0000-0000-000000000001", tenantId: null, orgId: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Permissions.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task Resolve_SameRoleHeldInTwoTenants_EachTokenCarriesOnlyItsOwn()
    {
        // Arrange: Admin in tenant A, Viewer in tenant B.
        var tenantB = Guid.NewGuid();
        var userId = "00000000-0000-0000-0000-00000000000a";
        var sut = BuildWithAssignments(
        [
            new() { UserId = userId, RoleId = AdminRoleId,  TenantId = TenantId },
            new() { UserId = userId, RoleId = ViewerRoleId, TenantId = tenantB },
        ], orgGrants: []);

        // Act
        var inA = await sut.Get(userId, TenantId, orgId: null, TestContext.Current.CancellationToken);
        var inB = await sut.Get(userId, tenantB, orgId: null, TestContext.Current.CancellationToken);

        // Assert
        inA.Value!.RoleNames.ShouldBe(["Admin"]);
        inA.Value.Permissions.ShouldContain("admin:delete");
        inB.Value!.RoleNames.ShouldBe(["Viewer"]);
        inB.Value.Permissions.ShouldNotContain("admin:delete");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task Resolve_OrgIdNull_SkipsOrgTier()
    {
        // Arrange: no org context
        var sut = Build(orgGrants: null, userId: "00000000-0000-0000-0000-000000000002");

        // Act
        var result = await sut.Get("00000000-0000-0000-0000-000000000002", TenantId, orgId: null, TestContext.Current.CancellationToken);

        // Assert — global+tenant tiers present, no org permission
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Permissions.ShouldContain("global:admin");
        result.Value.Permissions.ShouldContain("tenant:read");
        result.Value.Permissions.ShouldNotContain("org:read");
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task Resolve_RoleProviderFails_ReturnsFailure()
    {
        // Arrange: role provider returns failure — fail-closed
        var sut = new UserPermissions(
            ConfigurationCatalog.UnreadableRoles("Role query failed"),
            ConfigurationCatalog.Permissions(new[] { new PermissionImplementationConfiguration() }),
            ConfigurationCatalog.RolePermissions(Array.Empty<RolePermissionImplementationConfiguration>()),
            ConfigurationCatalog.UserRoles([]),
            new Fdw.Services.Authentication.Abstractions.Security.AuthenticationContextAccessor(),
            NullLogger<UserPermissions>.Instance);

        // Act
        var result = await sut.Get("00000000-0000-0000-0000-000000000002", TenantId, orgId: null, TestContext.Current.CancellationToken);

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
        var viewerAssignment = new UserRoleImplementationConfiguration
        {
            UserId = "00000000-0000-0000-0000-000000000003",
            RoleId = ViewerRoleId,
            TenantId = TenantId // a role is held in the tenant being resolved
        };
        var sut = BuildWithAssignments([viewerAssignment], orgGrants: []);

        // Act
        var result = await sut.Get("00000000-0000-0000-0000-000000000003", TenantId, orgId: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        // Viewer MUST have their own permissions
        result.Value.Permissions.ShouldContain("viewer:read1");
        result.Value.Permissions.ShouldContain("viewer:read2");

        // Viewer MUST NOT have admin-only permissions (this was the escalation bug)
        result.Value.Permissions.ShouldNotContain("admin:delete",
            "Viewer must not receive admin:delete — this was the FDW-532 privilege escalation");
        result.Value.Permissions.ShouldNotContain("global:admin",
            "Viewer must not receive global:admin — they are not assigned the GlobalAdmin role");
        result.Value.Permissions.ShouldNotContain("tenant:read",
            "Viewer must not receive tenant:read — they are not assigned the TenantUser role");

        // Critical count assertion: 2 viewer perms only, not the full 5-perm catalog
        result.Value.Permissions.Count.ShouldBe(2,
            $"Viewer with 1 role (2 perms) must resolve to exactly 2 permissions, not {result.Value.Permissions.Count} (which would indicate catalog bleed)");
    }

    [Fact]
    [Trait("Category", "Authorization.FDW532")]
    public async Task Resolve_AdminUser_GetsAdminPermissions_StrictlyAdminSet()
    {
        // Arrange: Admin user assigned ONLY the AdminRole (admin:delete + global:admin).
        // Before FDW-532 fix, admin would have gotten all 5 perms (including viewer perms).
        // After fix, they get exactly the 2 admin role permissions.
        var adminAssignment = new UserRoleImplementationConfiguration
        {
            UserId = "00000000-0000-0000-0000-000000000004",
            RoleId = AdminRoleId,
            TenantId = TenantId
        };
        var sut = BuildWithAssignments([adminAssignment], orgGrants: []);

        // Act
        var result = await sut.Get("00000000-0000-0000-0000-000000000004", TenantId, orgId: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Permissions.ShouldContain("admin:delete");
        result.Value.Permissions.ShouldContain("global:admin");

        // Admin MUST NOT have viewer-only permissions
        result.Value.Permissions.ShouldNotContain("viewer:read1",
            "Admin must not receive viewer:read1 — they are not assigned the Viewer role");
        result.Value.Permissions.ShouldNotContain("viewer:read2",
            "Admin must not receive viewer:read2 — they are not assigned the Viewer role");
        result.Value.Permissions.ShouldNotContain("tenant:read",
            "Admin must not receive tenant:read — they are not assigned the TenantUser role");

        result.Value.Permissions.Count.ShouldBe(2,
            $"Admin with 1 role (2 perms) must resolve to exactly 2 permissions, not {result.Value.Permissions.Count}");
    }

    [Fact]
    [Trait("Category", "Authorization.FDW532")]
    public async Task Resolve_UserWithNoRoleAssignments_GetsZeroPermissions()
    {
        // Arrange: user with NO role assignments at all.
        // Before FDW-532 fix, they would have received the FULL catalog (all 5 perms).
        // After fix, they must receive exactly 0 permissions.
        var sut = BuildWithAssignments([], orgGrants: []);

        // Act
        var result = await sut.Get("00000000-0000-0000-0000-000000000005", TenantId, orgId: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue("Zero assignments is a valid state — empty result, not failure");
        result.Value.ShouldNotBeNull();
        result.Value.Permissions.Count.ShouldBe(0,
            "User with no role assignments must get zero permissions, not the full catalog");
    }

    [Fact]
    [Trait("Category", "Authorization.FDW532")]
    public async Task Resolve_UserRoleProviderFails_ReturnsFailure_NotFullCatalog()
    {
        // Arrange: the user-role provider fails (e.g. DB unavailable).
        // Before FDW-532 fix (the bug): there was no user-role provider call at all;
        // UserPermissions would bake all perms regardless.
        // After fix: failure MUST return Failure (fail-closed). No token issued.
        var sut = new UserPermissions(
            ConfigurationCatalog.Roles(AllRoles),
            ConfigurationCatalog.Permissions(AllPermissions),
            ConfigurationCatalog.RolePermissions(AllRolePermissions),
            ConfigurationCatalog.UnreadableUserRoles("DB unavailable"),
            new Fdw.Services.Authentication.Abstractions.Security.AuthenticationContextAccessor(),
            NullLogger<UserPermissions>.Instance);

        // Act
        var result = await sut.Get("00000000-0000-0000-0000-000000000006", TenantId, orgId: null, TestContext.Current.CancellationToken);

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
        var viewerAssignment = new UserRoleImplementationConfiguration
        {
            UserId = "00000000-0000-0000-0000-000000000007",
            RoleId = ViewerRoleId,
            TenantId = TenantId
        };
        const int totalCatalogPermissions = 5; // admin:delete, global:admin, tenant:read, viewer:read1, viewer:read2

        var sut = BuildWithAssignments([viewerAssignment], orgGrants: []);
        var result = await sut.Get("00000000-0000-0000-0000-000000000007", TenantId, orgId: null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        // Viewer count must be far below total catalog — not 88 real perms all assigned to everyone
        result.Value.Permissions.Count.ShouldBeLessThan(totalCatalogPermissions,
            $"Viewer perm count ({result.Value.Permissions.Count}) must be less than total catalog ({totalCatalogPermissions})");

        // No admin-only perms
        result.Value.Permissions.ShouldNotContain("admin:delete");
        result.Value.Permissions.ShouldNotContain("global:admin");

        // Explicitly verify the "escalation gap": before the fix a viewer would get ALL perms
        var unexpectedPermsIfBugExists = new[] { "admin:delete", "global:admin", "tenant:read" };
        var leakedPerms = unexpectedPermsIfBugExists.Where(p => result.Value.Permissions.Contains(p)).ToList();
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
        var assignments = new List<UserRoleImplementationConfiguration>
        {
            new() { UserId = "00000000-0000-0000-0000-000000000008", RoleId = ViewerRoleId, TenantId = TenantId },
            new() { UserId = "00000000-0000-0000-0000-000000000008", RoleId = AdminRoleId,  TenantId = TenantId },
        };
        var sut = BuildWithAssignments(assignments, orgGrants: []);

        var result = await sut.Get("00000000-0000-0000-0000-000000000008", TenantId, orgId: null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Permissions.ShouldContain("viewer:read1");
        result.Value.Permissions.ShouldContain("viewer:read2");
        result.Value.Permissions.ShouldContain("admin:delete");
        result.Value.Permissions.ShouldContain("global:admin");
        result.Value.Permissions.ShouldNotContain("tenant:read",
            "User is not assigned TenantUser role — tenant:read must not appear");
        result.Value.Permissions.Count.ShouldBe(4);
    }

    [Fact]
    [Trait("Category", "Authorization.FDW532")]
    public async Task Resolve_TenantScopedAssignment_OnlyContributesForMatchingTenant()
    {
        // Arrange: user has tenant-scoped assignment for TenantId only.
        // When resolving for a DIFFERENT tenant, that tenant role must not contribute.
        var otherTenantId = Guid.NewGuid();
        var tenantAssignment = new UserRoleImplementationConfiguration
        {
            UserId = "00000000-0000-0000-0000-000000000009",
            RoleId = TenantRoleId,
            TenantId = TenantId // assigned only for TenantId
        };
        var sut = BuildWithAssignments([tenantAssignment], orgGrants: []);

        // Act: resolve for a DIFFERENT tenant (not the one the role is scoped to)
        var result = await sut.Get("00000000-0000-0000-0000-000000000009", otherTenantId, orgId: null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        // The user's assignment targets TenantId but we're resolving for otherTenantId.
        // Assignment-tier filter: TenantId != otherTenantId → role excluded from assignedRoleIds.
        result.Value.Permissions.Count.ShouldBe(0,
            "User has a tenant-scoped assignment for a different tenant — zero perms for this tenant context");
    }

    [Fact]
    [Trait("Category", "Authorization.FDW862")]
    public async Task Resolve_ExpiredAssignment_ContributesNothing()
    {
        // Arrange: the assignment's ExpiresAt is in the past. FDW-862 -- an expired role assignment
        // must not grant permissions, even though the assignment row itself is still IsCurrent.
        var expiredAssignment = new UserRoleImplementationConfiguration
        {
            UserId = "00000000-0000-0000-0000-00000000000b",
            RoleId = ViewerRoleId,
            TenantId = TenantId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        var sut = BuildWithAssignments([expiredAssignment], orgGrants: []);

        // Act
        var result = await sut.Get(
            "00000000-0000-0000-0000-00000000000b", TenantId, orgId: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Permissions.ShouldBeEmpty(
            "An assignment with ExpiresAt in the past must not contribute any permissions");
        result.Value.RoleNames.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Authorization.FDW862")]
    public async Task Resolve_UnexpiredAssignment_StillContributes()
    {
        // Arrange: ExpiresAt in the future must still contribute -- guards against an off-by-sign
        // regression on the FDW-862 comparison.
        var futureAssignment = new UserRoleImplementationConfiguration
        {
            UserId = "00000000-0000-0000-0000-00000000000c",
            RoleId = ViewerRoleId,
            TenantId = TenantId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        };
        var sut = BuildWithAssignments([futureAssignment], orgGrants: []);

        // Act
        var result = await sut.Get(
            "00000000-0000-0000-0000-00000000000c", TenantId, orgId: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Permissions.ShouldContain("viewer:read1");
        result.Value.Permissions.ShouldContain("viewer:read2");
    }

    [Fact]
    [Trait("Category", "Authorization.FDW862")]
    public async Task Resolve_ValidUntil_IsTheEarliestExpiryAmongCountedAssignments()
    {
        // Arrange: two assignments count, expiring in 3 hours and in 1 day. Two more do not count, one
        // already expired and one in another tenant, and neither may pull the answer earlier.
        var soon = DateTimeOffset.UtcNow.AddHours(3);
        const string userId = "00000000-0000-0000-0000-00000000000d";
        var sut = BuildWithAssignments(
        [
            new UserRoleImplementationConfiguration { UserId = userId, RoleId = ViewerRoleId, TenantId = TenantId, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) },
            new UserRoleImplementationConfiguration { UserId = userId, RoleId = TenantRoleId, TenantId = TenantId, ExpiresAt = soon },
            new UserRoleImplementationConfiguration { UserId = userId, RoleId = ViewerRoleId, TenantId = TenantId, ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new UserRoleImplementationConfiguration { UserId = userId, RoleId = ViewerRoleId, TenantId = Guid.NewGuid(), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5) },
        ], orgGrants: []);

        // Act
        var result = await sut.Get(userId, TenantId, orgId: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ValidUntil.ShouldBe(soon,
            "The result stops being true when the first counted assignment expires, and only a counted one");
    }

    [Fact]
    [Trait("Category", "Authorization.FDW862")]
    public async Task Resolve_ValidUntil_IsNullWhenNoCountedAssignmentExpires()
    {
        // Arrange
        const string userId = "00000000-0000-0000-0000-00000000000e";
        var sut = BuildWithAssignments(
        [
            new UserRoleImplementationConfiguration { UserId = userId, RoleId = ViewerRoleId, TenantId = TenantId },
        ], orgGrants: []);

        // Act
        var result = await sut.Get(userId, TenantId, orgId: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ValidUntil.ShouldBeNull();
    }

    // ----- Mock helpers -----

    // Mocks the named provider interface UserPermissions actually asks for, rather than the concrete
    // base it happens to be built on.
}
