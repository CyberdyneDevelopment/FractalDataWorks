using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for <see cref="OrganizationMembers"/> (FDW-1593): a person holds one role in an organization and exactly its permissions, a
/// caller can give only a role whose permissions they hold, and Jo can be administrator in Finance and member in Legal.
/// </summary>
public sealed class OrganizationMembersTests
{
    private static readonly Guid Tenant = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = new("44444444-0000-0000-0000-000000000002");
    private static readonly Guid Jo = new("22222222-0000-0000-0000-000000000001");
    private static readonly Guid Sam = new("22222222-0000-0000-0000-000000000002");

    private static GenericMessage Fail(string reason) => new(reason);

    private static TenantOrgAccessConfiguration Row(Guid user, Guid org, string role, string permission) =>
        new() { UserId = user, TenantId = Tenant, OrgId = org, RoleName = role, PermissionName = permission };

    private sealed class Rig
    {
        public Mock<IOrganizationScope> Scope { get; } = new();
        public Mock<IOrganizationRoles> Roles { get; } = new();
        public Mock<TenantOrgAccessConfigurationProvider> Access { get; } =
            new(Mock.Of<IConfigurationGatewayProvider>(), NullLogger<TenantOrgAccessConfigurationProvider>.Instance);
        public Mock<IGrantRule> GrantRule { get; } = new();

        public Rig()
        {
            Scope.Setup(s => s.TenantOf(Finance, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(Tenant));
            Scope.Setup(s => s.TenantOf(Legal, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Failure(Fail("not acting in Legal")));
            Roles.Setup(r => r.Get(Finance, "Clerk", It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<OrganizationRoleView>.Success(new OrganizationRoleView("Clerk", null, false, ["datasets:read", "datasets:write"])));
            Roles.Setup(r => r.Get(Finance, "Nobody", It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<OrganizationRoleView>.Failure(Fail("no such role")));
            GrantRule.Setup(g => g.CanDefine(It.IsAny<IReadOnlyList<string>>(), Tenant)).Returns(GenericResult.Success());
            Access.Setup(a => a.Get(It.IsAny<Guid>(), Tenant, Finance, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success([]));
            Access.Setup(a => a.Replace(It.IsAny<Guid>(), Tenant, Finance, It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            Access.Setup(a => a.Clear(It.IsAny<Guid>(), Tenant, Finance, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        }

        public OrganizationMembers Service() =>
            new(Scope.Object, Roles.Object, Access.Object, GrantRule.Object, NullLogger<OrganizationMembers>.Instance);

        public void Holds(Guid user, string role, params string[] permissions) =>
            Access.Setup(a => a.Get(user, Tenant, Finance, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success([.. permissions.Select(p => Row(user, Finance, role, p))]));

        public void NothingWritten()
        {
            Access.Verify(a => a.Replace(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
            Access.Verify(a => a.Clear(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonGivenARoleHoldsExactlyItsPermissionsAndTheOldRoleIsReported()
    {
        var rig = new Rig();
        rig.Holds(Jo, "Observer", "organizations:read");

        var result = await rig.Service().Give(Finance, Jo, "Clerk", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.PreviousRole.ShouldBe("Observer");
        result.Value.Role.ShouldBe("Clerk");
        rig.Access.Verify(a => a.Replace(Jo, Tenant, Finance, "Clerk",
            It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { "datasets:read", "datasets:write" })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonNotYetInTheOrganizationHasNoPreviousRole()
    {
        var rig = new Rig();

        var result = await rig.Service().Give(Finance, Sam, "Clerk", TestContext.Current.CancellationToken);

        result.Value!.PreviousRole.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleTheCallerDoesNotHoldTheRightsOfIsNotGivenAndNothingChanges()
    {
        var rig = new Rig();
        rig.GrantRule.Setup(g => g.CanDefine(It.IsAny<IReadOnlyList<string>>(), Tenant)).Returns(GenericResult.Failure(Fail("the caller does not hold datasets:write")));

        var result = await rig.Service().Give(Finance, Jo, "Clerk", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        rig.NothingWritten();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleThatDoesNotExistAndAnOrganizationTheCallerMayNotAdministerChangeNothing()
    {
        var rig = new Rig();

        var noRole = await rig.Service().Give(Finance, Jo, "Nobody", TestContext.Current.CancellationToken);
        var legal = await rig.Service().Give(Legal, Jo, "Clerk", TestContext.Current.CancellationToken);

        noRole.IsSuccess.ShouldBeFalse();
        legal.IsSuccess.ShouldBeFalse();
        rig.NothingWritten();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonRemovedLosesEveryRowAndTheirRoleIsReported()
    {
        var rig = new Rig();
        rig.Holds(Jo, "Clerk", "datasets:read", "datasets:write");

        var result = await rig.Service().Remove(Finance, Jo, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Clerk");
        rig.Access.Verify(a => a.Clear(Jo, Tenant, Finance, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task APersonWhoIsNotInTheOrganizationCannotBeRemoved()
    {
        var rig = new Rig();

        var result = await rig.Service().Remove(Finance, Sam, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        rig.NothingWritten();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task EachPersonIsListedOnceWithTheirRole()
    {
        var rig = new Rig();
        rig.Access.Setup(a => a.Members(Tenant, Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success(
                [Row(Jo, Finance, "Clerk", "datasets:read"), Row(Jo, Finance, "Clerk", "datasets:write"), Row(Sam, Finance, "Observer", "organizations:read")]));

        var result = await rig.Service().List(Finance, TestContext.Current.CancellationToken);

        result.Value!.ShouldBe([new OrganizationMember(Jo, "Clerk"), new OrganizationMember(Sam, "Observer")]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task JoCanBeAdministratorInOneOrganizationAndMemberInAnotherBecauseRowsAreKeyedByOrganization()
    {
        var rig = new Rig();
        rig.Access.Setup(a => a.Get(Jo, Tenant, Legal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success([Row(Jo, Legal, "Member", "organizations:read")]));
        rig.Holds(Jo, "Administrator", "organizations:read", "organizations/members:write");

        var finance = await rig.Service().Remove(Finance, Jo, TestContext.Current.CancellationToken);

        finance.Value.ShouldBe("Administrator");
        rig.Access.Verify(a => a.Clear(Jo, Tenant, Legal, It.IsAny<CancellationToken>()), Times.Never);
    }
}
