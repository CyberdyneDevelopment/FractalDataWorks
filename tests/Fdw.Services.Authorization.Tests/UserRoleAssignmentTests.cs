using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for <see cref="UserRoleAssignment"/>: giving a role to a person in a tenant writes one grant, leaves a role already held alone,
/// keeps a grant in a second tenant apart from the first, and reports what could not be read or saved (FDW-1578).
/// </summary>
public sealed class UserRoleAssignmentTests
{
    private static readonly Guid User = new("22222222-0000-0000-0000-000000000001");
    private static readonly Guid Role = new("33333333-0000-0000-0000-000000000001");
    private static readonly Guid TenantA = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid TenantB = new("11111111-0000-0000-0000-000000000002");

    private static Mock<IUserRoleConfigurationProvider> Provider(IReadOnlyList<UserRoleImplementationConfiguration> held)
    {
        var provider = new Mock<IUserRoleConfigurationProvider>();
        provider.Setup(p => p.Find(It.IsAny<Func<UserRoleImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<UserRoleImplementationConfiguration, bool> predicate, CancellationToken _) =>
                Task.FromResult(GenericResult<IReadOnlyList<UserRoleImplementationConfiguration>>.Success(held.Where(predicate).ToArray())));
        provider.Setup(p => p.Save(It.IsAny<UserRoleImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        return provider;
    }

    private static Mock<IRoleConfigurationProvider> Roles(string name, RoleImplementationConfiguration? role)
    {
        var roles = new Mock<IRoleConfigurationProvider>();
        roles.Setup(r => r.Get(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoleImplementationConfiguration>.Success(role!));
        return roles;
    }

    private static UserRoleAssignment Assignment(Mock<IUserRoleConfigurationProvider> provider, RoleImplementationConfiguration? role = null) =>
        new(Roles("Admin", role ?? new RoleImplementationConfiguration { Id = Role, Name = "Admin" }).Object, provider.Object,
            NullLogger<UserRoleAssignment>.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AssigningARoleThePersonLacksWritesOneGrantInTheTenant()
    {
        var provider = Provider([]);

        var result = await Assignment(provider).Assign(User, "Admin", TenantA, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        provider.Verify(p => p.Save(
            It.Is<UserRoleImplementationConfiguration>(g => g.UserId == User.ToString() && g.RoleId == Role && g.TenantId == TenantA),
            "UserRole", "UserRole", $"{User}:Admin:{TenantA}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleAlreadyHeldInTheTenantIsLeftAloneAndWritesNothing()
    {
        var provider = Provider([new UserRoleImplementationConfiguration { UserId = User.ToString().ToUpperInvariant(), RoleId = Role, TenantId = TenantA }]);

        var result = await Assignment(provider).Assign(User, "Admin", TenantA, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        provider.Verify(p => p.Save(It.IsAny<UserRoleImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleHeldInAnotherTenantDoesNotStandInForThisOneAndGetsItsOwnName()
    {
        var provider = Provider([new UserRoleImplementationConfiguration { UserId = User.ToString(), RoleId = Role, TenantId = TenantA }]);

        var result = await Assignment(provider).Assign(User, "Admin", TenantB, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        provider.Verify(p => p.Save(
            It.Is<UserRoleImplementationConfiguration>(g => g.TenantId == TenantB),
            "UserRole", "UserRole", $"{User}:Admin:{TenantB}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleThatDoesNotExistIsRefusedAndNothingIsWritten()
    {
        var provider = Provider([]);
        var assignment = new UserRoleAssignment(Roles("Nobody", null).Object, provider.Object, NullLogger<UserRoleAssignment>.Instance);

        var result = await assignment.Assign(User, "Nobody", TenantA, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        provider.Verify(p => p.Save(It.IsAny<UserRoleImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AFailedReadOfTheHeldRolesIsReportedAndNothingIsWritten()
    {
        var provider = new Mock<IUserRoleConfigurationProvider>();
        provider.Setup(p => p.Find(It.IsAny<Func<UserRoleImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleImplementationConfiguration>>.Failure(new GenericMessage("the store is down")));

        var result = await Assignment(provider).Assign(User, "Admin", TenantA, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        provider.Verify(p => p.Save(It.IsAny<UserRoleImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AFailedSaveIsReported()
    {
        var provider = Provider([]);
        provider.Setup(p => p.Save(It.IsAny<UserRoleImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage("the index refused it")));

        var result = await Assignment(provider).Assign(User, "Admin", TenantA, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
