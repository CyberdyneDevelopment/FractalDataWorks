using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// The authorization readers decide who holds a permission and show no description, so every read of a role, a permission or a grant states
/// <see cref="ConfigurationReadOptions.WithoutFilledMembers"/> (FDW-7341). The providers here are strict: a read that states anything else, or that states nothing, is
/// a read nothing set up, and the mock throws.
/// </summary>
public sealed class AuthorizationReadsAreLeanTests
{
    private static readonly Guid RoleId = new("77777777-0000-0000-0000-000000000001");
    private static readonly Guid PermissionId = new("88888888-0000-0000-0000-000000000001");
    private static readonly Guid TenantId = new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private const string PermissionName = "pipelines:execute";

    private static Mock<IRoleConfigurationProvider> Roles()
    {
        var roles = new Mock<IRoleConfigurationProvider>(MockBehavior.Strict);
        roles.Setup(r => r.Get(ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IRoleImplementationConfiguration>>.Success(
                [new RoleImplementationConfiguration { Id = RoleId, Name = "ServicePipelineRunner", IsTenantScoped = false, TenantId = TenantId }]));
        return roles;
    }

    private static Mock<IPermissionConfigurationProvider> Permissions()
    {
        var permissions = new Mock<IPermissionConfigurationProvider>(MockBehavior.Strict);
        permissions.Setup(p => p.Get(ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IPermissionImplementationConfiguration>>.Success(
                [new PermissionImplementationConfiguration { Id = PermissionId, Name = PermissionName }]));
        return permissions;
    }

    private static Mock<IRolePermissionConfigurationProvider> Grants()
    {
        var grants = new Mock<IRolePermissionConfigurationProvider>(MockBehavior.Strict);
        grants.Setup(g => g.Get(ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IRolePermissionImplementationConfiguration>>.Success(
                [new RolePermissionImplementationConfiguration { RoleId = RoleId, PermissionId = PermissionId, TenantId = TenantId }]));
        return grants;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task RoleGrantsReadsTheRolesPermissionsAndGrantsWithoutTheFilledMembers()
    {
        var roleGrants = new RoleGrants(Roles().Object, Permissions().Object, Grants().Object, new AuthenticationContextAccessor(), NullLogger<RoleGrants>.Instance);

        var result = await roleGrants.Get(["ServicePipelineRunner"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());
        result.Value!.ShouldBe([PermissionName]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task PermissionHoldersReadsTheRolesPermissionsAndGrantsWithoutTheFilledMembers()
    {
        var holders = Holders(Permissions().Object);

        var result = await holders.Get(PermissionName, TenantId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());
        result.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task PermissionHoldersReadsThePermissionsOfAnOrganizationWithoutTheFilledMembers()
    {
        var access = new Mock<TenantOrgAccessConfigurationProvider>(Mock.Of<IConfigurationGatewayProvider>(), NullLogger<TenantOrgAccessConfigurationProvider>.Instance);
        access.Setup(a => a.Members(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success([]));
        var holders = Holders(Permissions().Object, access.Object);

        var result = await holders.GetInOrganization(PermissionName, TenantId, Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());
        result.Value!.ShouldBeEmpty();
    }

    private static PermissionHolders Holders(IPermissionConfigurationProvider permissions, TenantOrgAccessConfigurationProvider? access = null)
    {
        var userRoles = new Mock<IUserRoleConfigurationProvider>(MockBehavior.Strict);
        userRoles.Setup(u => u.Find(It.IsAny<Func<UserRoleImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleImplementationConfiguration>>.Success([]));

        return new PermissionHolders(
            Roles().Object,
            permissions,
            Grants().Object,
            userRoles.Object,
            Mock.Of<ITenantProvider>(),
            access ?? new Mock<TenantOrgAccessConfigurationProvider>(Mock.Of<IConfigurationGatewayProvider>(), NullLogger<TenantOrgAccessConfigurationProvider>.Instance).Object,
            new AuthenticationContextAccessor(),
            NullLogger<PermissionHolders>.Instance);
    }
}
