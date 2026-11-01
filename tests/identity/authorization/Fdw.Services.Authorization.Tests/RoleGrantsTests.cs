using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Fdw.Services.Data;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for <see cref="RoleGrants"/> — the role-name-keyed expansion an authentication
/// service uses for a principal that has no user row to look assignments up by.
/// </summary>
public sealed class RoleGrantsTests
{
    private static readonly Guid RunnerRoleId = new("77777777-0000-0000-0000-000000000001");
    // The tenant a shared role is seeded in. A role's grants count only when its own tenant owns them.
    private static readonly Guid SharedTenantId = new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid ViewerRoleId = new("77777777-0000-0000-0000-000000000002");
    private static readonly Guid ExecutePermId = new("88888888-0000-0000-0000-000000000001");
    private static readonly Guid ReadPermId = new("88888888-0000-0000-0000-000000000002");
    private static readonly Guid OrphanPermId = new("88888888-0000-0000-0000-00000000dead");

    private static readonly RoleImplementationConfiguration[] Roles =
    [
        new() { Id = RunnerRoleId, Name = "ServicePipelineRunner", IsTenantScoped = false, TenantId = SharedTenantId },
        new() { Id = ViewerRoleId, Name = "Viewer", IsTenantScoped = false, TenantId = SharedTenantId },
    ];

    private static readonly PermissionImplementationConfiguration[] Permissions =
    [
        new() { Id = ExecutePermId, Name = "pipelines:execute" },
        new() { Id = ReadPermId, Name = "pipelines:read" },
    ];

    private static readonly RolePermissionImplementationConfiguration[] RolePermissions =
    [
        new() { RoleId = RunnerRoleId, PermissionId = ExecutePermId, TenantId = SharedTenantId },
        new() { RoleId = ViewerRoleId, PermissionId = ReadPermId, TenantId = SharedTenantId },
    ];

    [Fact]
    public async Task Resolve_expands_a_role_to_the_permissions_it_grants()
    {
        var result = await Build().Get(["ServicePipelineRunner"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(["pipelines:execute"]);
    }

    [Fact]
    public async Task Resolve_matches_a_role_name_without_regard_to_case()
    {
        var result = await Build().Get(["servicepipelinerunner"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(["pipelines:execute"]);
    }

    [Fact]
    public async Task Resolve_unions_the_permissions_of_every_named_role()
    {
        var result = await Build().Get(["ServicePipelineRunner", "Viewer"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.OrderBy(p => p, StringComparer.Ordinal)
            .ShouldBe(["pipelines:execute", "pipelines:read"]);
    }

    [Fact]
    public async Task Resolve_fails_when_a_named_role_is_not_in_the_catalogue()
    {
        var result = await Build().Get(["NoSuchRole"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Resolve_fails_when_no_role_is_named()
    {
        var result = await Build().Get([], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // A junction row pointing at a permission that is not in the catalogue means the two disagree.
    // Silently dropping it grants less than the role says it does.
    [Fact]
    public async Task Resolve_fails_when_a_granted_permission_is_not_in_the_catalogue()
    {
        var roleGrants = Build(rolePermissions:
        [
            new RolePermissionImplementationConfiguration { RoleId = RunnerRoleId, PermissionId = OrphanPermId, TenantId = SharedTenantId },
        ]);

        var result = await roleGrants.Get(["ServicePipelineRunner"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Resolve_fails_when_the_role_catalogue_cannot_be_read()
    {
        var roleGrants = new RoleGrants(
            ConfigurationCatalog.UnreadableRoles("the catalogue is unreachable"),
            ConfigurationCatalog.Permissions(Permissions),
            ConfigurationCatalog.RolePermissions(RolePermissions),
            new Fdw.Services.Authentication.Abstractions.Security.AuthenticationContextAccessor(),
            NullLogger<RoleGrants>.Instance);

        var result = await roleGrants.Get(["ServicePipelineRunner"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        // The provider's own reason travels rather than being restated as "no such role".
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("unreachable");
    }

    // FDW-1072: the catalogue is read across every tenant, so a grant another tenant wrote onto a shared role
    // must not count -- otherwise one tenant's admin changes what the role grants in every tenant.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Resolve_does_not_count_a_grant_another_tenant_wrote_onto_a_shared_role()
    {
        var otherTenant = new Guid("99999999-0000-0000-0000-000000000009");
        var roleGrants = Build(rolePermissions:
        [
            new RolePermissionImplementationConfiguration { RoleId = RunnerRoleId, PermissionId = ExecutePermId, TenantId = SharedTenantId },
            new RolePermissionImplementationConfiguration { RoleId = RunnerRoleId, PermissionId = ReadPermId, TenantId = otherTenant },
        ]);

        var result = await roleGrants.Get(["ServicePipelineRunner"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(["pipelines:execute"]);
    }

    // A role whose owning tenant is not recorded has no owner whose grants could count; none is assumed.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Resolve_counts_no_grant_of_a_role_whose_owning_tenant_is_not_recorded()
    {
        var roleGrants = new RoleGrants(
            ConfigurationCatalog.Roles([new RoleImplementationConfiguration { Id = RunnerRoleId, Name = "ServicePipelineRunner", IsTenantScoped = false }]),
            ConfigurationCatalog.Permissions(Permissions),
            ConfigurationCatalog.RolePermissions(RolePermissions),
            new Fdw.Services.Authentication.Abstractions.Security.AuthenticationContextAccessor(),
            NullLogger<RoleGrants>.Instance);

        var result = await roleGrants.Get(["ServicePipelineRunner"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    private static RoleGrants Build(
        IReadOnlyList<RolePermissionImplementationConfiguration>? rolePermissions = null)
        => new RoleGrants(
            ConfigurationCatalog.Roles(Roles),
            ConfigurationCatalog.Permissions(Permissions),
            ConfigurationCatalog.RolePermissions(rolePermissions ?? RolePermissions),
            new Fdw.Services.Authentication.Abstractions.Security.AuthenticationContextAccessor(),
            NullLogger<RoleGrants>.Instance);

}
