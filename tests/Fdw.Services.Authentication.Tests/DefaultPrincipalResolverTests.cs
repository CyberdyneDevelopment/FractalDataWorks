using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Tests;

/// <summary>
/// Unit tests for <see cref="DefaultPrincipalResolver"/>, complementary to the broader
/// tenant/org/cross-tenant happy-and-sad-path suite already covered in
/// <c>Fdw.Services.Authentication.OpenIddict.Tests.ProviderEngine.DefaultPrincipalResolverTests</c>.
/// This file focuses on branches that suite does not exercise: constructor guards, the explicit-org
/// resolution path (<c>ResolveOrg</c> when <c>orgId</c> is supplied), role-name loading/merging, and
/// the role-provider failure/skip paths inside <c>LoadRoleNames</c>.
/// </summary>
/// <remarks>
/// The three configuration providers are REAL, over a faked store (see
/// <see cref="ConfigurationCatalog"/>). They are sealed and their reads are not virtual, so they
/// cannot be mocked -- and the resolver takes the providers themselves, not interfaces. A test
/// therefore states what the STORE holds rather than what a call returns.
/// </remarks>
public sealed class DefaultPrincipalResolverTests
{
    private static void SetupNonGlobalTenant(Fixture f, Guid tenantId)
    {
        var tenant = new Mock<ITenant>(MockBehavior.Strict);
        tenant.Setup(t => t.IsGlobal).Returns(false);
        f.TenantRecordProvider
            .Setup(t => t.GetTenant(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Success(tenant.Object));
    }

    /// <summary>The one default membership the resolver requires to resolve a tenant implicitly.</summary>
    private static void GiveUserDefaultTenant(Fixture f, Guid userId, Guid tenantId)
        => f.UserTenantRows.Add(new UserTenantImplementationConfiguration
        {
            Id = Guid.NewGuid(),
            Name = $"{userId}:{tenantId}",
            UserId = userId,
            TenantId = tenantId,
            IsDefault = true,
        });

    /// <summary>A role assignment for this user. UserId must be set or the by-user read cannot match it.</summary>
    private static UserRoleImplementationConfiguration AssignmentFor(Guid userId, Guid roleId)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = $"{userId}:{roleId}",
            UserId = userId.ToString(),
            RoleId = roleId,
        };

    private sealed class Fixture
    {
        public List<IUserTenantImplementationConfiguration> UserTenantRows { get; } = [];

        public List<IUserRoleImplementationConfiguration> UserRoleRows { get; } = [];

        public List<IRoleImplementationConfiguration> RoleRows { get; } = [];

        /// <summary>When set, the user-role store fails to read rather than returning rows.</summary>
        public string? UserRoleStoreFailure { get; set; }

        public Mock<IOrganizationProvider> OrgProvider { get; } = new(MockBehavior.Strict);

        public Mock<ITenantProvider> TenantRecordProvider { get; } = new(MockBehavior.Strict);

        public Mock<IEffectivePermissionResolver> PermResolver { get; } = new(MockBehavior.Strict);

        public UserTenantConfigurationProvider TenantProvider => ConfigurationCatalog.UserTenants(UserTenantRows);

        public UserRoleConfigurationProvider UserRoleProvider => UserRoleStoreFailure is { } reason
            ? ConfigurationCatalog.UnreadableUserRoles(reason)
            : ConfigurationCatalog.UserRoles(UserRoleRows);

        public RoleConfigurationProvider RoleProvider => ConfigurationCatalog.Roles(RoleRows);

        public DefaultPrincipalResolver CreateSut() => new(
            TenantProvider,
            OrgProvider.Object,
            TenantRecordProvider.Object,
            PermResolver.Object,
            UserRoleProvider,
            RoleProvider,
            NullLogger<DefaultPrincipalResolver>.Instance);
    }

    // ── Constructor guards ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorNullUserTenantProviderThrowsArgumentNullException()
    {
        var f = new Fixture();
        Should.Throw<ArgumentNullException>(() => new DefaultPrincipalResolver(
            null!, f.OrgProvider.Object, f.TenantRecordProvider.Object, f.PermResolver.Object, f.UserRoleProvider, f.RoleProvider,
            NullLogger<DefaultPrincipalResolver>.Instance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorNullOrganizationProviderThrowsArgumentNullException()
    {
        var f = new Fixture();
        Should.Throw<ArgumentNullException>(() => new DefaultPrincipalResolver(
            f.TenantProvider, null!, f.TenantRecordProvider.Object, f.PermResolver.Object, f.UserRoleProvider, f.RoleProvider,
            NullLogger<DefaultPrincipalResolver>.Instance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorNullTenantProviderThrowsArgumentNullException()
    {
        var f = new Fixture();
        Should.Throw<ArgumentNullException>(() => new DefaultPrincipalResolver(
            f.TenantProvider, f.OrgProvider.Object, null!, f.PermResolver.Object, f.UserRoleProvider, f.RoleProvider,
            NullLogger<DefaultPrincipalResolver>.Instance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorNullPermissionResolverThrowsArgumentNullException()
    {
        var f = new Fixture();
        Should.Throw<ArgumentNullException>(() => new DefaultPrincipalResolver(
            f.TenantProvider, f.OrgProvider.Object, f.TenantRecordProvider.Object, null!, f.UserRoleProvider, f.RoleProvider,
            NullLogger<DefaultPrincipalResolver>.Instance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorNullUserRoleProviderThrowsArgumentNullException()
    {
        var f = new Fixture();
        Should.Throw<ArgumentNullException>(() => new DefaultPrincipalResolver(
            f.TenantProvider, f.OrgProvider.Object, f.TenantRecordProvider.Object, f.PermResolver.Object, null!, f.RoleProvider,
            NullLogger<DefaultPrincipalResolver>.Instance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorNullRoleProviderThrowsArgumentNullException()
    {
        var f = new Fixture();
        Should.Throw<ArgumentNullException>(() => new DefaultPrincipalResolver(
            f.TenantProvider, f.OrgProvider.Object, f.TenantRecordProvider.Object, f.PermResolver.Object, f.UserRoleProvider, null!,
            NullLogger<DefaultPrincipalResolver>.Instance));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorNullLoggerFallsBackToNullLoggerInstance()
    {
        var f = new Fixture();
        Should.NotThrow(() => new DefaultPrincipalResolver(
            f.TenantProvider, f.OrgProvider.Object, f.TenantRecordProvider.Object, f.PermResolver.Object, f.UserRoleProvider, f.RoleProvider,
            logger: null));
    }

    // ── ResolveOrg: explicit orgId supplied ──────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async System.Threading.Tasks.Task Resolve_ExplicitOrgId_ValidatesBelongsToTenant_ThenBakesOrgClaim()
    {
        // Arrange — Why: an explicit orgId must be validated against the resolved tenant before
        // being baked; this exercises the "requestedOrgId.HasValue" branch of ResolveOrg that the
        // default-org happy-path tests elsewhere never reach.
        var f = new Fixture();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        GiveUserDefaultTenant(f, userId, tenantId);
        f.OrgProvider
            .Setup(o => o.Get(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(
                new OrganizationConfiguration { Id = orgId, TenantId = tenantId }));
        SetupNonGlobalTenant(f, tenantId);
        f.PermResolver
            .Setup(p => p.Resolve(userId.ToString(), tenantId, orgId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(new[] { "data.read" }));

        var sut = f.CreateSut();

        // Act
        var result = await sut.Resolve(userId, tenantId: null, orgId: orgId, Array.Empty<string>(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.FindFirst(ClaimDefinitions.orgId.Name)?.Value.ShouldBe(orgId.ToString());
        f.OrgProvider.Verify(o => o.Get(orgId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async System.Threading.Tasks.Task Resolve_ExplicitOrgId_NotFound_FailsLoud()
    {
        // Arrange — orgResult.IsSuccess is true but Value is null (not found) collapses to the
        // same OrgResolutionFailed branch as a hard query failure.
        var f = new Fixture();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        GiveUserDefaultTenant(f, userId, tenantId);
        f.OrgProvider
            .Setup(o => o.Get(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(null!));

        var sut = f.CreateSut();

        // Act
        var result = await sut.Resolve(userId, tenantId: null, orgId: orgId, Array.Empty<string>(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async System.Threading.Tasks.Task Resolve_ExplicitOrgId_QueryFails_FailsLoud()
    {
        var f = new Fixture();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        GiveUserDefaultTenant(f, userId, tenantId);
        f.OrgProvider
            .Setup(o => o.Get(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("org gateway error")));

        var sut = f.CreateSut();

        var result = await sut.Resolve(userId, tenantId: null, orgId: orgId, Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async System.Threading.Tasks.Task Resolve_ExplicitOrgId_BelongsToDifferentTenant_FailsWithOrgTenantMismatch()
    {
        // Arrange — Why: the org exists but its TenantId differs from the resolved tenant; the RLS
        // VisibilityGroup join would silently return nothing downstream if this were allowed through.
        var f = new Fixture();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        GiveUserDefaultTenant(f, userId, tenantId);
        f.OrgProvider
            .Setup(o => o.Get(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(
                new OrganizationConfiguration { Id = orgId, TenantId = otherTenantId }));

        var sut = f.CreateSut();

        var result = await sut.Resolve(userId, tenantId: null, orgId: orgId, Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    // ── LoadRoleNames branches ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async System.Threading.Tasks.Task Resolve_RoleAssignmentsQueryFails_DegradesToNoRolesRatherThanFailing()
    {
        // Arrange — Why: LoadRoleNames treats a failed/absent assignments query as "no roles" (empty
        // list), not a hard failure — token issuance should not be blocked by a role-lookup hiccup.
        var f = new Fixture();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        GiveUserDefaultTenant(f, userId, tenantId);
        f.OrgProvider
            .Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("no default org")));
        SetupNonGlobalTenant(f, tenantId);
        f.PermResolver
            .Setup(p => p.Resolve(userId.ToString(), tenantId, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(new[] { "data.read" }));
        f.UserRoleStoreFailure = "assignments query failed";

        // A role that WOULD resolve if the assignments had been readable, so an empty roles claim
        // is evidence the failure short-circuited rather than evidence the catalogue was empty.
        f.RoleRows.Add(new RoleImplementationConfiguration { Id = Guid.NewGuid(), Name = "Editor" });

        var sut = f.CreateSut();

        var result = await sut.Resolve(userId, tenantId: null, orgId: null, Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.FindAll(ClaimDefinitions.roles.Name).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async System.Threading.Tasks.Task Resolve_RoleAssignmentsMatchRoles_BakesResolvedRoleNamesAndSkipsUnmatchedOrBlankOnes()
    {
        // Arrange — Why: exercises the FirstOrDefault-miss skip (assignment with no matching role),
        // the blank-name skip (role.Name is empty), and the successful name resolution path together.
        var f = new Fixture();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var matchedRoleId = Guid.NewGuid();
        var blankNameRoleId = Guid.NewGuid();
        var unassignedRoleId = Guid.NewGuid(); // present in allRoles but never assigned — must not leak into roles.
        var danglingAssignmentRoleId = Guid.NewGuid(); // assigned but absent from allRoles — FirstOrDefault miss.

        GiveUserDefaultTenant(f, userId, tenantId);
        f.OrgProvider
            .Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("no default org")));
        SetupNonGlobalTenant(f, tenantId);
        f.PermResolver
            .Setup(p => p.Resolve(userId.ToString(), tenantId, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(new[] { "data.read" }));

        f.UserRoleRows.Add(AssignmentFor(userId, matchedRoleId));
        f.UserRoleRows.Add(AssignmentFor(userId, blankNameRoleId));
        f.UserRoleRows.Add(AssignmentFor(userId, danglingAssignmentRoleId));

        f.RoleRows.Add(new RoleImplementationConfiguration { Id = matchedRoleId, Name = "Editor" });
        f.RoleRows.Add(new RoleImplementationConfiguration { Id = blankNameRoleId, Name = string.Empty });
        f.RoleRows.Add(new RoleImplementationConfiguration { Id = unassignedRoleId, Name = "NeverAssigned" });

        var sut = f.CreateSut();

        var result = await sut.Resolve(userId, tenantId: null, orgId: null, Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var roles = result.Value!.FindAll(ClaimDefinitions.roles.Name).Select(c => c.Value).ToList();
        roles.ShouldBe(new[] { "Editor" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async System.Threading.Tasks.Task Resolve_AdditionalRolesAndLoadedRoles_MergeAndDeduplicateCaseInsensitively()
    {
        // Arrange — Why: MergeRoles must union additionalRoles with loaded role names and dedupe
        // case-insensitively (a caller-supplied "Editor" and a DB-loaded "editor" collapse to one).
        var f = new Fixture();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var editorRoleId = Guid.NewGuid();

        GiveUserDefaultTenant(f, userId, tenantId);
        f.OrgProvider
            .Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("no default org")));
        SetupNonGlobalTenant(f, tenantId);
        f.PermResolver
            .Setup(p => p.Resolve(userId.ToString(), tenantId, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(new[] { "data.read" }));

        f.UserRoleRows.Add(AssignmentFor(userId, editorRoleId));
        f.RoleRows.Add(new RoleImplementationConfiguration { Id = editorRoleId, Name = "editor" });

        var sut = f.CreateSut();

        var result = await sut.Resolve(userId, tenantId: null, orgId: null, new[] { "Editor", "Agent" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var roles = result.Value!.FindAll(ClaimDefinitions.roles.Name).Select(c => c.Value).ToList();
        roles.Count.ShouldBe(2);
        roles.ShouldContain("Editor");
        roles.ShouldContain("Agent");
    }
}
