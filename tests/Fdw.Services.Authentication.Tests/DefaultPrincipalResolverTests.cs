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
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Data;

namespace Fdw.Services.Authentication.Tests;

/// <summary>
/// Unit tests for <see cref="DefaultPrincipalResolver"/>, complementary to the broader
/// tenant/org/cross-tenant happy-and-sad-path suite already covered in
/// <c>Fdw.Services.Authentication.OpenIddict.Tests.ProviderEngine.DefaultPrincipalResolverTests</c>.
/// This file focuses on branches that suite does not exercise: constructor guards, the explicit-org
/// resolution path (<c>ResolveOrg</c> when <c>orgId</c> is supplied), role-name loading/merging, and
/// the role-provider failure/skip paths inside <c>LoadRoleNames</c>.
/// </summary>
public sealed class DefaultPrincipalResolverTests
{
    private static IConfigurationGatewayProvider NullGateway() => new ConfigurationGatewayProvider();

    private static Mock<UserTenantConfigurationProvider> CreateTenantProviderMock() => new(
        MockBehavior.Strict,
        NullLogger<UserTenantConfigurationProvider>.Instance,
        NullGateway(),
        "PlatformConfiguration",
        "tenant");

    private static Mock<UserRoleConfigurationProvider> CreateUserRoleProviderMock() => new(
        MockBehavior.Strict,
        NullLogger<UserRoleConfigurationProvider>.Instance,
        NullGateway(),
        "PlatformConfiguration",
        "authz");

    private static Mock<RoleConfigurationProvider> CreateRoleProviderMock() => new(
        MockBehavior.Strict,
        NullLogger<RoleConfigurationProvider>.Instance,
        NullGateway(),
        "PlatformConfiguration",
        "authz");

    private sealed class Fixture
    {
        public Mock<UserTenantConfigurationProvider> TenantProvider { get; } = CreateTenantProviderMock();
        public Mock<IOrganizationProvider> OrgProvider { get; } = new(MockBehavior.Strict);
        public Mock<IEffectivePermissionResolver> PermResolver { get; } = new(MockBehavior.Strict);
        public Mock<UserRoleConfigurationProvider> UserRoleProvider { get; } = CreateUserRoleProviderMock();
        public Mock<RoleConfigurationProvider> RoleProvider { get; } = CreateRoleProviderMock();

        public DefaultPrincipalResolver CreateSut() => new(
            TenantProvider.Object,
            OrgProvider.Object,
            PermResolver.Object,
            UserRoleProvider.Object,
            RoleProvider.Object,
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
            null!, f.OrgProvider.Object, f.PermResolver.Object, f.UserRoleProvider.Object, f.RoleProvider.Object,
            NullLogger<DefaultPrincipalResolver>.Instance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorNullOrganizationProviderThrowsArgumentNullException()
    {
        var f = new Fixture();
        Should.Throw<ArgumentNullException>(() => new DefaultPrincipalResolver(
            f.TenantProvider.Object, null!, f.PermResolver.Object, f.UserRoleProvider.Object, f.RoleProvider.Object,
            NullLogger<DefaultPrincipalResolver>.Instance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorNullPermissionResolverThrowsArgumentNullException()
    {
        var f = new Fixture();
        Should.Throw<ArgumentNullException>(() => new DefaultPrincipalResolver(
            f.TenantProvider.Object, f.OrgProvider.Object, null!, f.UserRoleProvider.Object, f.RoleProvider.Object,
            NullLogger<DefaultPrincipalResolver>.Instance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorNullUserRoleProviderThrowsArgumentNullException()
    {
        var f = new Fixture();
        Should.Throw<ArgumentNullException>(() => new DefaultPrincipalResolver(
            f.TenantProvider.Object, f.OrgProvider.Object, f.PermResolver.Object, null!, f.RoleProvider.Object,
            NullLogger<DefaultPrincipalResolver>.Instance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorNullRoleProviderThrowsArgumentNullException()
    {
        var f = new Fixture();
        Should.Throw<ArgumentNullException>(() => new DefaultPrincipalResolver(
            f.TenantProvider.Object, f.OrgProvider.Object, f.PermResolver.Object, f.UserRoleProvider.Object, null!,
            NullLogger<DefaultPrincipalResolver>.Instance));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorNullLoggerFallsBackToNullLoggerInstance()
    {
        var f = new Fixture();
        Should.NotThrow(() => new DefaultPrincipalResolver(
            f.TenantProvider.Object, f.OrgProvider.Object, f.PermResolver.Object, f.UserRoleProvider.Object, f.RoleProvider.Object,
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

        f.TenantProvider
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));
        f.OrgProvider
            .Setup(o => o.Get(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(
                new OrganizationConfiguration { Id = orgId, TenantId = tenantId }));
        f.PermResolver
            .Setup(p => p.Resolve(userId.ToString(), tenantId, orgId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(new[] { "data.read" }));
        f.UserRoleProvider
            .Setup(p => p.GetByUser(userId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleConfiguration>>.Success(Array.Empty<UserRoleConfiguration>()));
        f.RoleProvider
            .Setup(p => p.GetAllRoles(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RoleConfiguration>());

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

        f.TenantProvider
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));
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

        f.TenantProvider
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));
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

        f.TenantProvider
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));
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

        f.TenantProvider
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));
        f.OrgProvider
            .Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("no default org")));
        f.PermResolver
            .Setup(p => p.Resolve(userId.ToString(), tenantId, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(new[] { "data.read" }));
        f.UserRoleProvider
            .Setup(p => p.GetByUser(userId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleConfiguration>>.Failure(new GenericMessage("assignments query failed")));

        var sut = f.CreateSut();

        var result = await sut.Resolve(userId, tenantId: null, orgId: null, Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.FindAll(ClaimDefinitions.roles.Name).ShouldBeEmpty();
        f.RoleProvider.Verify(p => p.GetAllRoles(It.IsAny<CancellationToken>()), Times.Never);
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

        f.TenantProvider
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));
        f.OrgProvider
            .Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("no default org")));
        f.PermResolver
            .Setup(p => p.Resolve(userId.ToString(), tenantId, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(new[] { "data.read" }));
        f.UserRoleProvider
            .Setup(p => p.GetByUser(userId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleConfiguration>>.Success(new[]
            {
                new UserRoleConfiguration { RoleId = matchedRoleId },
                new UserRoleConfiguration { RoleId = blankNameRoleId },
                new UserRoleConfiguration { RoleId = danglingAssignmentRoleId },
            }));
        f.RoleProvider
            .Setup(p => p.GetAllRoles(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new RoleConfiguration { Id = matchedRoleId, Name = "Editor" },
                new RoleConfiguration { Id = blankNameRoleId, Name = string.Empty },
                new RoleConfiguration { Id = unassignedRoleId, Name = "NeverAssigned" },
            });

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

        f.TenantProvider
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));
        f.OrgProvider
            .Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("no default org")));
        f.PermResolver
            .Setup(p => p.Resolve(userId.ToString(), tenantId, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(new[] { "data.read" }));
        f.UserRoleProvider
            .Setup(p => p.GetByUser(userId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleConfiguration>>.Success(new[]
            {
                new UserRoleConfiguration { RoleId = editorRoleId },
            }));
        f.RoleProvider
            .Setup(p => p.GetAllRoles(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new RoleConfiguration { Id = editorRoleId, Name = "editor" } });

        var sut = f.CreateSut();

        var result = await sut.Resolve(userId, tenantId: null, orgId: null, new[] { "Editor", "Agent" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var roles = result.Value!.FindAll(ClaimDefinitions.roles.Name).Select(c => c.Value).ToList();
        roles.Count.ShouldBe(2);
        roles.ShouldContain("Editor");
        roles.ShouldContain("Agent");
    }
}
