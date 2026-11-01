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
/// Tests for <see cref="OrganizationBootstrap"/> (FDW-1758): a new organization gets a copy of another's roles, the grant rule judges every
/// role before any is written, the first administrator holds exactly the permissions of the role, and a clear removes the people and roles.
/// </summary>
public sealed class OrganizationBootstrapTests
{
    private static readonly Guid Tenant = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = new("44444444-0000-0000-0000-000000000009");
    private static readonly Guid Jo = new("22222222-0000-0000-0000-000000000001");

    private static GenericMessage Fail(string reason) => new(reason);

    private sealed class Rig
    {
        public Mock<IOrganizationRoleConfigurationProvider> Roles { get; } = new();
        public Mock<TenantOrgAccessConfigurationProvider> Access { get; } =
            new(Mock.Of<IConfigurationGatewayProvider>(), NullLogger<TenantOrgAccessConfigurationProvider>.Instance);
        public Mock<IGrantRule> GrantRule { get; } = new();
        public List<IOrganizationRoleImplementationConfiguration> Stored { get; } = [];

        public Rig()
        {
            GrantRule.Setup(g => g.CanDefine(It.IsAny<IReadOnlyList<string>>(), Tenant)).Returns(GenericResult.Success());
            Roles.Setup(r => r.Get(ConfigurationLevels.Children, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IReadOnlyList<IOrganizationRoleImplementationConfiguration>>.Success([.. Stored]));
            Roles.Setup(r => r.Save(It.IsAny<OrganizationRoleImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            Roles.Setup(r => r.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Access.Setup(a => a.Replace(It.IsAny<Guid>(), Tenant, It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            Access.Setup(a => a.Clear(It.IsAny<Guid>(), Tenant, It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Access.Setup(a => a.Members(Tenant, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success([]));
        }

        public OrganizationBootstrap Service() => new(Roles.Object, Access.Object, GrantRule.Object, NullLogger<OrganizationBootstrap>.Instance);

        public void Has(Guid organization, string name, bool builtIn, params string[] permissions)
            => Stored.Add(new OrganizationRoleImplementationConfiguration
            {
                Name = $"{organization}:{name}",
                OrganizationId = organization,
                RoleName = name,
                Description = name + " role",
                IsBuiltIn = builtIn,
                Permissions = [.. permissions.Select(permission => new OrganizationRolePermissionConfiguration { PermissionName = permission })],
            });

        public void NothingSaved()
            => Roles.Verify(r => r.Save(It.IsAny<OrganizationRoleImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task EveryRoleOfTheOrganizationToCopyIsSavedForTheNewOneWithTheSamePermissions()
    {
        var rig = new Rig();
        rig.Has(Finance, "Administrator", true, "organizations:read", "organizations:write");
        rig.Has(Finance, "Member", true, "organizations:read");

        var result = await rig.Service().CopyRoles(Tenant, Finance, Legal, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Select(role => role.Name).ShouldBe(["Administrator", "Member"], ignoreOrder: true);
        rig.Roles.Verify(r => r.Save(
            It.Is<OrganizationRoleImplementationConfiguration>(c => c.OrganizationId == Legal && c.RoleName == "Administrator" && c.IsBuiltIn
                && c.Permissions.Select(p => p.PermissionName).SequenceEqual(new[] { "organizations:read", "organizations:write" })),
            "OrganizationRole", "OrganizationRole", $"{Legal}:Administrator", It.IsAny<CancellationToken>()), Times.Once);
        rig.Roles.Verify(r => r.Save(It.Is<OrganizationRoleImplementationConfiguration>(c => c.RoleName == "Member"), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleTheCallerDoesNotHoldCopiesNoRoleAtAll()
    {
        var rig = new Rig();
        rig.Has(Finance, "Member", true, "organizations:read");
        rig.Has(Finance, "Administrator", true, "organizations:write");
        rig.GrantRule.Setup(g => g.CanDefine(It.Is<IReadOnlyList<string>>(p => p.Contains("organizations:write")), Tenant))
            .Returns(GenericResult.Failure(Fail("the caller does not hold organizations:write")));

        var result = await rig.Service().CopyRoles(Tenant, Finance, Legal, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.NothingSaved();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationWithNoRolesCannotBeCopied()
    {
        var rig = new Rig();

        var result = await rig.Service().CopyRoles(Tenant, Finance, Legal, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.NothingSaved();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task RolesFromAnotherOrganizationsAreNotCopied()
    {
        var rig = new Rig();
        rig.Has(Finance, "Member", true, "organizations:read");
        rig.Has(Guid.NewGuid(), "Secret", false, "organizations:write");

        var result = await rig.Service().CopyRoles(Tenant, Finance, Legal, TestContext.Current.CancellationToken);

        result.Value!.Select(role => role.Name).ShouldBe(["Member"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheFirstAdministratorHoldsExactlyThePermissionsOfTheRole()
    {
        var rig = new Rig();
        rig.Has(Legal, "Administrator", true, "organizations:read", "organizations:write");

        var result = await rig.Service().GiveRole(Tenant, Legal, Jo, "administrator", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Role.ShouldBe("Administrator");
        rig.Access.Verify(a => a.Replace(Jo, Tenant, Legal, "Administrator",
            It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { "organizations:read", "organizations:write" })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARoleTheOrganizationDoesNotHaveIsNotGiven()
    {
        var rig = new Rig();
        rig.Has(Legal, "Member", true, "organizations:read");

        var result = await rig.Service().GiveRole(Tenant, Legal, Jo, "Administrator", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.Access.Verify(a => a.Replace(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleTheCallerDoesNotHoldIsNotGiven()
    {
        var rig = new Rig();
        rig.Has(Legal, "Administrator", true, "organizations:write");
        rig.GrantRule.Setup(g => g.CanDefine(It.IsAny<IReadOnlyList<string>>(), Tenant)).Returns(GenericResult.Failure(Fail("the caller does not hold organizations:write")));

        var result = await rig.Service().GiveRole(Tenant, Legal, Jo, "Administrator", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.Access.Verify(a => a.Replace(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ClearRemovesEveryMemberAndEveryRoleOfTheOrganization()
    {
        var rig = new Rig();
        rig.Has(Legal, "Administrator", true, "organizations:write");
        rig.Has(Legal, "Member", true, "organizations:read");
        rig.Access.Setup(a => a.Members(Tenant, Legal, It.IsAny<CancellationToken>())).ReturnsAsync(
            GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success(
            [
                new TenantOrgAccessConfiguration { UserId = Jo, TenantId = Tenant, OrgId = Legal, RoleName = "Administrator", PermissionName = "organizations:write" },
            ]));

        var result = await rig.Service().Clear(Tenant, Legal, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        rig.Access.Verify(a => a.Clear(Jo, Tenant, Legal, It.IsAny<CancellationToken>()), Times.Once);
        rig.Roles.Verify(r => r.Delete($"{Legal}:Administrator", It.IsAny<CancellationToken>()), Times.Once);
        rig.Roles.Verify(r => r.Delete($"{Legal}:Member", It.IsAny<CancellationToken>()), Times.Once);
    }
}
