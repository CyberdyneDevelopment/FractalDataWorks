using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for <see cref="OrganizationRoles"/> (FDW-1593): a role carries at least one permission the caller holds, a name is unique within
/// an organization, a built-in or held role is kept, and changing a role refreshes everyone who holds it.
/// </summary>
public sealed class OrganizationRolesTests
{
    private static readonly Guid Tenant = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = new("44444444-0000-0000-0000-000000000002");
    private static readonly Guid Jo = new("22222222-0000-0000-0000-000000000001");
    private static readonly Guid Sam = new("22222222-0000-0000-0000-000000000002");

    private static GenericMessage Fail(string reason) => new(reason);

    private sealed class Rig
    {
        public Mock<IOrganizationScope> Scope { get; } = new();
        public Mock<IOrganizationRoleConfigurationProvider> Roles { get; } = new();
        public Mock<TenantOrgAccessConfigurationProvider> Access { get; } =
            new(Mock.Of<IConfigurationGatewayProvider>(), NullLogger<TenantOrgAccessConfigurationProvider>.Instance);
        public Mock<IGrantRule> GrantRule { get; } = new();
        public List<IOrganizationRoleImplementationConfiguration> Stored { get; } = [];

        public Rig()
        {
            Scope.Setup(s => s.TenantOf(Finance, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(Tenant));
            Scope.Setup(s => s.TenantOf(Legal, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Failure(Fail("not acting in Legal")));
            GrantRule.Setup(g => g.CanDefine(It.IsAny<IReadOnlyList<string>>(), Tenant)).Returns(GenericResult.Success());
            Roles.Setup(r => r.Get(ConfigurationLevels.Children, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IReadOnlyList<IOrganizationRoleImplementationConfiguration>>.Success([.. Stored]));
            Roles.Setup(r => r.Save(It.IsAny<OrganizationRoleImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            Roles.Setup(r => r.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Access.Setup(a => a.Members(Tenant, Finance, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success([]));
            Access.Setup(a => a.Replace(It.IsAny<Guid>(), Tenant, Finance, It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
        }

        public OrganizationRoles Service() =>
            new(Scope.Object, Roles.Object, Access.Object, GrantRule.Object, NullLogger<OrganizationRoles>.Instance);

        public void Has(string name, bool builtIn = false, Guid? organization = null, params string[] permissions) =>
            Stored.Add(new OrganizationRoleImplementationConfiguration
            {
                Name = $"{organization ?? Finance}:{name}", OrganizationId = organization ?? Finance, RoleName = name, IsBuiltIn = builtIn,
                Permissions = [.. permissions.Select(p => new OrganizationRolePermissionConfiguration { PermissionName = p })],
            });

        public void NothingSaved() =>
            Roles.Verify(r => r.Save(It.IsAny<OrganizationRoleImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        public void HeldBy(string role, params Guid[] people) =>
            Access.Setup(a => a.Members(Tenant, Finance, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success(
                    [.. people.Select(p => new TenantOrgAccessConfiguration { UserId = p, TenantId = Tenant, OrgId = Finance, RoleName = role, PermissionName = "datasets:read" })]));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleIsDefinedUnderTheOrganizationsNameWithItsPermissions()
    {
        var rig = new Rig();

        var result = await rig.Service().Define(Finance, "Contracts clerk", "Uploads contracts", ["datasets:write", "datasets:read", "DATASETS:READ"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Permissions.ShouldBe(["datasets:write", "datasets:read"]);
        rig.Roles.Verify(r => r.Save(
            It.Is<OrganizationRoleImplementationConfiguration>(c => c.OrganizationId == Finance && c.RoleName == "Contracts clerk" && !c.IsBuiltIn && c.Permissions.Count == 2),
            "OrganizationRole", "OrganizationRole", $"{Finance}:Contracts clerk", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleWithNoPermissionIsRefusedBecauseItWouldAdmitNobody()
    {
        var rig = new Rig();

        var result = await rig.Service().Define(Finance, "Observer", null, [" ", ""], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        rig.NothingSaved();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleWithNoNameIsRefused()
    {
        var rig = new Rig();

        var result = await rig.Service().Define(Finance, "  ", null, ["datasets:read"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        rig.NothingSaved();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerCannotPutIntoARolePermissionsTheyLack()
    {
        var rig = new Rig();
        rig.GrantRule.Setup(g => g.CanDefine(It.IsAny<IReadOnlyList<string>>(), Tenant)).Returns(GenericResult.Failure(Fail("the caller does not hold datasets:delete")));

        var result = await rig.Service().Define(Finance, "Clerk", null, ["datasets:delete"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        rig.NothingSaved();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationTheCallerMayNotAdministerGetsNoRole()
    {
        var rig = new Rig();

        var result = await rig.Service().Define(Legal, "Clerk", null, ["datasets:read"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        rig.NothingSaved();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleNameIsUniqueWithinAnOrganizationButNotAcrossOrganizations()
    {
        var rig = new Rig();
        rig.Has("Clerk", permissions: ["datasets:read"]);
        rig.Has("Clerk", organization: Legal, permissions: ["datasets:read"]);

        var again = await rig.Service().Define(Finance, "clerk", null, ["datasets:read"], TestContext.Current.CancellationToken);

        again.IsSuccess.ShouldBeFalse();
        rig.NothingSaved();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleNameAnotherOrganizationHasIsFreeHere()
    {
        var rig = new Rig();
        rig.Has("Clerk", organization: Legal, permissions: ["datasets:read"]);

        var result = await rig.Service().Define(Finance, "Clerk", null, ["datasets:read"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ChangingARoleRefreshesEveryoneWhoHoldsItWithTheNewPermissions()
    {
        var rig = new Rig();
        rig.Has("Clerk", permissions: ["datasets:read"]);
        rig.HeldBy("Clerk", Jo, Sam, Jo);

        var result = await rig.Service().Redefine(Finance, "Clerk", "Reads and writes", ["datasets:read", "datasets:write"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        rig.Access.Verify(a => a.Replace(Jo, Tenant, Finance, "Clerk", It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { "datasets:read", "datasets:write" })), It.IsAny<CancellationToken>()), Times.Once);
        rig.Access.Verify(a => a.Replace(Sam, Tenant, Finance, "Clerk", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AHolderWhoCannotBeRefreshedFailsTheChange()
    {
        var rig = new Rig();
        rig.Has("Clerk", permissions: ["datasets:read"]);
        rig.HeldBy("Clerk", Jo);
        rig.Access.Setup(a => a.Replace(Jo, Tenant, Finance, "Clerk", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(Fail("store down")));

        var result = await rig.Service().Redefine(Finance, "Clerk", null, ["datasets:write"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ARoleThatDoesNotExistCannotBeChanged()
    {
        var rig = new Rig();

        var result = await rig.Service().Redefine(Finance, "Nobody", null, ["datasets:read"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        rig.NothingSaved();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ABuiltInRoleIsKeptAndAHeldRoleIsKept()
    {
        var rig = new Rig();
        rig.Has("Administrator", builtIn: true, permissions: ["organizations:read"]);
        rig.Has("Clerk", permissions: ["datasets:read"]);
        rig.HeldBy("Clerk", Jo);

        var builtIn = await rig.Service().Delete(Finance, "Administrator", TestContext.Current.CancellationToken);
        var held = await rig.Service().Delete(Finance, "Clerk", TestContext.Current.CancellationToken);

        builtIn.IsSuccess.ShouldBeFalse();
        held.IsSuccess.ShouldBeFalse();
        rig.Roles.Verify(r => r.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleNobodyHoldsIsDeletedByItsName()
    {
        var rig = new Rig();
        rig.Has("Clerk", permissions: ["datasets:read"]);

        var result = await rig.Service().Delete(Finance, "Clerk", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        rig.Roles.Verify(r => r.Delete($"{Finance}:Clerk", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task OnlyTheLiveRolesOfTheOrganizationAreListedByName()
    {
        var rig = new Rig();
        rig.Has("Member", true, null, "organizations:read");
        rig.Has("Clerk", false, null, "datasets:read");
        rig.Has("Clerk", false, Legal, "datasets:read");
        rig.Stored.Add(new OrganizationRoleImplementationConfiguration { Name = $"{Finance}:Gone", OrganizationId = Finance, RoleName = "Gone", IsDeleted = true });

        var result = await rig.Service().List(Finance, TestContext.Current.CancellationToken);

        result.Value!.Select(r => r.Name).ShouldBe(["Clerk", "Member"]);
        result.Value!.Single(r => r.Name == "Member").IsBuiltIn.ShouldBeTrue();
    }
}
