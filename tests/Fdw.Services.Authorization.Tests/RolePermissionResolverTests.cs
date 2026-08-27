using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
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
/// Tests for <see cref="RolePermissionResolver"/> — the role-name-keyed expansion an authentication
/// service uses for a principal that has no user row to look assignments up by.
/// </summary>
public sealed class RolePermissionResolverTests
{
    private static readonly Guid RunnerRoleId = new("77777777-0000-0000-0000-000000000001");
    private static readonly Guid ViewerRoleId = new("77777777-0000-0000-0000-000000000002");
    private static readonly Guid ExecutePermId = new("88888888-0000-0000-0000-000000000001");
    private static readonly Guid ReadPermId = new("88888888-0000-0000-0000-000000000002");
    private static readonly Guid OrphanPermId = new("88888888-0000-0000-0000-00000000dead");

    private static readonly RoleConfiguration[] Roles =
    [
        new() { Id = RunnerRoleId, Name = "ServicePipelineRunner", IsTenantScoped = false },
        new() { Id = ViewerRoleId, Name = "Viewer", IsTenantScoped = false },
    ];

    private static readonly PermissionConfiguration[] Permissions =
    [
        new() { Id = ExecutePermId, Name = "pipelines:execute" },
        new() { Id = ReadPermId, Name = "pipelines:read" },
    ];

    private static readonly RolePermissionConfiguration[] RolePermissions =
    [
        new() { RoleId = RunnerRoleId, PermissionId = ExecutePermId },
        new() { RoleId = ViewerRoleId, PermissionId = ReadPermId },
    ];

    [Fact]
    public async Task Resolve_expands_a_role_to_the_permissions_it_grants()
    {
        var result = await Build().Resolve(["ServicePipelineRunner"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(["pipelines:execute"]);
    }

    [Fact]
    public async Task Resolve_matches_a_role_name_without_regard_to_case()
    {
        var result = await Build().Resolve(["servicepipelinerunner"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(["pipelines:execute"]);
    }

    [Fact]
    public async Task Resolve_unions_the_permissions_of_every_named_role()
    {
        var result = await Build().Resolve(["ServicePipelineRunner", "Viewer"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.OrderBy(p => p, StringComparer.Ordinal)
            .ShouldBe(["pipelines:execute", "pipelines:read"]);
    }

    // Why this is a failure and not an empty set: a role name matching nothing is a declaration
    // pointing at a row that is not there, and returning nothing turns it into a 403 that reads as a
    // missing grant.
    [Fact]
    public async Task Resolve_fails_when_a_named_role_is_not_in_the_catalogue()
    {
        var result = await Build().Resolve(["NoSuchRole"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Resolve_fails_when_no_role_is_named()
    {
        var result = await Build().Resolve([], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // A junction row pointing at a permission that is not in the catalogue means the two disagree.
    // Silently dropping it grants less than the role says it does.
    [Fact]
    public async Task Resolve_fails_when_a_granted_permission_is_not_in_the_catalogue()
    {
        var resolver = Build(rolePermissions:
        [
            new RolePermissionConfiguration { RoleId = RunnerRoleId, PermissionId = OrphanPermId },
        ]);

        var result = await resolver.Resolve(["ServicePipelineRunner"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Resolve_fails_when_the_role_catalogue_cannot_be_read()
    {
        var roles = MockProvider<RoleConfiguration, RoleConfigurationCommand>(Roles);
        roles.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<RoleConfiguration>>.Failure(new GenericMessage("the catalogue is unreachable")));

        var resolver = new RolePermissionResolver(
            roles.Object,
            MockProvider<PermissionConfiguration, PermissionConfigurationCommand>(Permissions).Object,
            MockProvider<RolePermissionConfiguration, RolePermissionConfigurationCommand>(RolePermissions).Object,
            NullLogger<RolePermissionResolver>.Instance);

        var result = await resolver.Resolve(["ServicePipelineRunner"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        // The provider's own reason travels rather than being restated as "no such role".
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("unreachable");
    }

    private static IRolePermissionResolver Build(
        IReadOnlyList<RolePermissionConfiguration>? rolePermissions = null)
        => new RolePermissionResolver(
            MockProvider<RoleConfiguration, RoleConfigurationCommand>(Roles).Object,
            MockProvider<PermissionConfiguration, PermissionConfigurationCommand>(Permissions).Object,
            MockProvider<RolePermissionConfiguration, RolePermissionConfigurationCommand>(
                rolePermissions ?? RolePermissions).Object,
            NullLogger<RolePermissionResolver>.Instance);

    private static Mock<ImplementationConfigurationProviderBase<TConfig, TCommand>> MockProvider<TConfig, TCommand>(
        IEnumerable<TConfig> items)
        where TConfig : class, Fdw.Configuration.IGenericConfiguration
        where TCommand : ConfigurationCommandBase<TConfig>
    {
        var mock = new Mock<ImplementationConfigurationProviderBase<TConfig, TCommand>>(
            MockBehavior.Loose,
            NullLogger<ImplementationConfigurationProviderBase<TConfig, TCommand>>.Instance,
            new ConfigurationGatewayProvider(),
            "TestStore", "authz");
        mock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TConfig>>.Success(new List<TConfig>(items)));
        return mock;
    }
}
