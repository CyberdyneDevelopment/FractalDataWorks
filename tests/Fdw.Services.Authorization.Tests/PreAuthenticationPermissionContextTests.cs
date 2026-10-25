using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
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
    public async Task ResolveReadsGrantsAsSystemAndOnlyGrantsAssignedPermissions(bool hasCaller)
    {
        var previous = hasCaller ? Mock.Of<IAuthenticationContext>() : null;
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var grants = new Mock<IUserPermissionGrants>();
        grants.Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(async (Guid asked, CancellationToken _) =>
            {
                await Task.Yield();
                accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
                IReadOnlyList<UserPermissionGrant> rows = asked == userId
                    ? [new UserPermissionGrant(tenantId, null, roleId, "Admin", false, tenantId, "connections:write")]
                    : [];
                return GenericResult<IReadOnlyList<UserPermissionGrant>>.Success(rows);
            });
        var userPermissions = new UserPermissions(grants.Object, accessor, NullLogger<UserPermissions>.Instance);

        var assigned = await userPermissions.Get(userId.ToString(), tenantId, null, TestContext.Current.CancellationToken);
        var stranger = await userPermissions.Get(Guid.NewGuid().ToString(), tenantId, null, TestContext.Current.CancellationToken);

        assigned.IsSuccess.ShouldBeTrue();
        assigned.Value!.Permissions.ShouldHaveSingleItem().ShouldBe("connections:write");
        stranger.IsSuccess.ShouldBeTrue();
        stranger.Value!.Permissions.ShouldBeEmpty();
        accessor.Current.ShouldBeSameAs(previous);
    }

    [Fact]
    public async Task ResolveRestoresCallerWhenGrantReadIsCancelled()
    {
        var previous = Mock.Of<IAuthenticationContext>();
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var grants = new Mock<IUserPermissionGrants>();
        grants.Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(async (Guid _, CancellationToken _) =>
        {
            await Task.Yield();
            accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
            throw new OperationCanceledException();
        });
        var userPermissions = new UserPermissions(grants.Object, accessor, NullLogger<UserPermissions>.Instance);

        await Should.ThrowAsync<OperationCanceledException>(() => userPermissions.Get(Guid.NewGuid().ToString(), null,
            null, TestContext.Current.CancellationToken));

        accessor.Current.ShouldBeSameAs(previous);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrgGrantReadFailureDoesNotReturnSuccessfulPermissions(bool nullRows)
    {
        var grants = new Mock<IUserPermissionGrants>();
        grants.Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserPermissionGrant>>.Success([]));
        var orgAccess = new Mock<IOrgAccessProvider>();
        orgAccess.Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nullRows ? GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success(null!)
                : GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Failure(Mock.Of<Fdw.Messages.IGenericMessage>()));
        var accessor = new AuthenticationContextAccessor();
        var userPermissions = new UserPermissions(grants.Object, accessor, NullLogger<UserPermissions>.Instance, orgAccess.Object);

        var result = await userPermissions.Get(Guid.NewGuid().ToString(), Guid.NewGuid(), Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        accessor.Current.ShouldBeNull();
        orgAccess.VerifyAll();
    }
}