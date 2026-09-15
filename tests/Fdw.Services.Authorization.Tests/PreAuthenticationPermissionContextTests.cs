using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

public sealed class PreAuthenticationPermissionContextTests
{
    [Fact]
    public async Task RoleExpansionReadsAsSystemAndRestoresCallerOnCancellation()
    {
        var previous = Mock.Of<IAuthenticationContext>();
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var roles = new Mock<IRoleConfigurationProvider>();
        roles.Setup(p => p.Get(It.IsAny<CancellationToken>())).Returns(async (CancellationToken _) =>
        {
            await Task.Yield();
            accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
            throw new OperationCanceledException();
        });
        var resolver = new RolePermissionResolver(roles.Object, Mock.Of<IPermissionConfigurationProvider>(),
            Mock.Of<IRolePermissionConfigurationProvider>(), accessor, NullLogger<RolePermissionResolver>.Instance);
        await Should.ThrowAsync<OperationCanceledException>(() => resolver.Resolve(["Admin"], TestContext.Current.CancellationToken));
        accessor.Current.ShouldBeSameAs(previous);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolveReadsCatalogAsSystemAndOnlyGrantsAssignedPermissions(bool hasCaller)
    {
        var previous = hasCaller ? Mock.Of<IAuthenticationContext>() : null;
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var roles = new Mock<IRoleConfigurationProvider>();
        roles.Setup(p => p.Get(It.IsAny<CancellationToken>())).Returns(async (CancellationToken _) =>
        {
            await Task.Yield();
            accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
            return GenericResult<IReadOnlyList<IRoleImplementationConfiguration>>.Success(
                [new RoleImplementationConfiguration { Id = roleId, Name = "Admin", IsTenantScoped = false }]);
        });
        var permissions = new Mock<IPermissionConfigurationProvider>();
        permissions.Setup(p => p.Get(It.IsAny<CancellationToken>())).Returns(() =>
        {
            accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
            return Task.FromResult(GenericResult<IReadOnlyList<IPermissionImplementationConfiguration>>.Success(
                [new PermissionImplementationConfiguration { Id = permissionId, Name = "connections:write" }]));
        });
        var grants = new Mock<IRolePermissionConfigurationProvider>();
        grants.Setup(p => p.Get(It.IsAny<CancellationToken>())).Returns(() =>
        {
            accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
            return Task.FromResult(GenericResult<IReadOnlyList<IRolePermissionImplementationConfiguration>>.Success(
                [new RolePermissionImplementationConfiguration { RoleId = roleId, PermissionId = permissionId }]));
        });
        var assignments = new Mock<IUserRoleConfigurationProvider>();
        assignments.Setup(p => p.Find(It.IsAny<Func<UserRoleImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<UserRoleImplementationConfiguration, bool> predicate, CancellationToken _) =>
            {
                accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
                var rows = new[] { new UserRoleImplementationConfiguration { UserId = userId.ToString(), RoleId = roleId } };
                return Task.FromResult(GenericResult<IReadOnlyList<UserRoleImplementationConfiguration>>.Success(rows.Where(predicate).ToArray()));
            });
        var resolver = new EffectivePermissionResolver(roles.Object, permissions.Object, grants.Object,
            assignments.Object, accessor, NullLogger<EffectivePermissionResolver>.Instance);

        var assigned = await resolver.Resolve(userId.ToString(), null, null, false, TestContext.Current.CancellationToken);
        var stranger = await resolver.Resolve(Guid.NewGuid().ToString(), null, null, false, TestContext.Current.CancellationToken);

        assigned.IsSuccess.ShouldBeTrue();
        assigned.Value!.Permissions.ShouldHaveSingleItem().ShouldBe("connections:write");
        stranger.IsSuccess.ShouldBeTrue();
        stranger.Value!.Permissions.ShouldBeEmpty();
        accessor.Current.ShouldBeSameAs(previous);
    }

    [Fact]
    public async Task ResolveRestoresCallerWhenCatalogReadIsCancelled()
    {
        var previous = Mock.Of<IAuthenticationContext>();
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var roles = new Mock<IRoleConfigurationProvider>();
        roles.Setup(p => p.Get(It.IsAny<CancellationToken>())).Returns(async (CancellationToken _) =>
        {
            await Task.Yield();
            accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
            throw new OperationCanceledException();
        });
        var resolver = new EffectivePermissionResolver(roles.Object, Mock.Of<IPermissionConfigurationProvider>(),
            Mock.Of<IRolePermissionConfigurationProvider>(), Mock.Of<IUserRoleConfigurationProvider>(),
            accessor, NullLogger<EffectivePermissionResolver>.Instance);

        await Should.ThrowAsync<OperationCanceledException>(() => resolver.Resolve(Guid.NewGuid().ToString(), null,
            null, false, TestContext.Current.CancellationToken));

        accessor.Current.ShouldBeSameAs(previous);
    }
}
