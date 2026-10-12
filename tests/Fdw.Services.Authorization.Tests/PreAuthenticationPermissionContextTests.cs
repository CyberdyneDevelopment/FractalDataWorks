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
        var roleGrants = new RoleGrants(roles.Object, Mock.Of<IPermissionConfigurationProvider>(),
            Mock.Of<IRolePermissionConfigurationProvider>(), accessor, NullLogger<RoleGrants>.Instance);
        await Should.ThrowAsync<OperationCanceledException>(() => roleGrants.Get(["Admin"], TestContext.Current.CancellationToken));
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
        var tenantId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var roles = new Mock<IRoleConfigurationProvider>();
        roles.Setup(p => p.Get(It.IsAny<CancellationToken>())).Returns(async (CancellationToken _) =>
        {
            await Task.Yield();
            accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
            return GenericResult<IReadOnlyList<IRoleImplementationConfiguration>>.Success(
                [new RoleImplementationConfiguration { Id = roleId, Name = "Admin", IsTenantScoped = false, TenantId = tenantId }]);
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
                [new RolePermissionImplementationConfiguration { RoleId = roleId, PermissionId = permissionId, TenantId = tenantId }]));
        });
        var assignments = new Mock<IUserRoleConfigurationProvider>();
        assignments.Setup(p => p.Find(It.IsAny<Func<UserRoleImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<UserRoleImplementationConfiguration, bool> predicate, CancellationToken _) =>
            {
                accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
                var rows = new[] { new UserRoleImplementationConfiguration { UserId = userId.ToString(), RoleId = roleId, TenantId = tenantId } };
                return Task.FromResult(GenericResult<IReadOnlyList<UserRoleImplementationConfiguration>>.Success(rows.Where(predicate).ToArray()));
            });
        var userPermissions = new UserPermissions(roles.Object, permissions.Object, grants.Object,
            assignments.Object, accessor, NullLogger<UserPermissions>.Instance);

        var assigned = await userPermissions.Get(userId.ToString(), tenantId, null, TestContext.Current.CancellationToken);
        var stranger = await userPermissions.Get(Guid.NewGuid().ToString(), tenantId, null, TestContext.Current.CancellationToken);

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
        var userPermissions = new UserPermissions(roles.Object, Mock.Of<IPermissionConfigurationProvider>(),
            Mock.Of<IRolePermissionConfigurationProvider>(), Mock.Of<IUserRoleConfigurationProvider>(),
            accessor, NullLogger<UserPermissions>.Instance);

        await Should.ThrowAsync<OperationCanceledException>(() => userPermissions.Get(Guid.NewGuid().ToString(), null,
            null, TestContext.Current.CancellationToken));

        accessor.Current.ShouldBeSameAs(previous);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrgGrantReadFailureDoesNotReturnSuccessfulPermissions(bool nullRows)
    {
        var roles = new Mock<IRoleConfigurationProvider>();
        roles.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IRoleImplementationConfiguration>>.Success([]));
        var permissions = new Mock<IPermissionConfigurationProvider>();
        permissions.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IPermissionImplementationConfiguration>>.Success([]));
        var grants = new Mock<IRolePermissionConfigurationProvider>();
        grants.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IRolePermissionImplementationConfiguration>>.Success([]));
        var assignments = new Mock<IUserRoleConfigurationProvider>();
        assignments.Setup(p => p.Find(It.IsAny<Func<UserRoleImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleImplementationConfiguration>>.Success([]));
        var orgAccess = new Mock<IOrgAccessProvider>();
        orgAccess.Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nullRows ? GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success(null!)
                : GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Failure(Mock.Of<Fdw.Messages.IGenericMessage>()));
        var accessor = new AuthenticationContextAccessor();
        var userPermissions = new UserPermissions(roles.Object, permissions.Object, grants.Object,
            assignments.Object, accessor, NullLogger<UserPermissions>.Instance, orgAccess.Object);

        var result = await userPermissions.Get(Guid.NewGuid().ToString(), Guid.NewGuid(), Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        accessor.Current.ShouldBeNull();
        orgAccess.VerifyAll();
    }
}