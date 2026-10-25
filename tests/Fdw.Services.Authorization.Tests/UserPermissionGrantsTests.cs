using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions.Caching;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Tests.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// FDW-2688: what a person's roles grant is read from the <c>UserPermissionGrant</c> view by the person's id. Only that person's rows are read,
/// however many people there are.
/// </summary>
public sealed class UserPermissionGrantsTests
{
    private static readonly Guid Person = Guid.Parse("0a1b2c3d-0000-0000-0000-00000000000a");
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Admin = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid Viewer = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private static Dictionary<string, object?> Row(string user, Guid role, string roleName, string? permission, DateTimeOffset? expires = null, bool tenantScoped = false)
        => InMemoryConfigurationStore.Row(
            ("UserId", user), ("AssignmentTenantId", Tenant), ("ExpiresAt", expires), ("RoleId", role), ("RoleName", roleName),
            ("IsTenantScoped", tenantScoped), ("RoleTenantId", Tenant), ("PermissionName", permission));

    private static UserPermissionGrants Reader(InMemoryConfigurationStore store)
        => new(store.Gateways, "PlatformConfiguration", "authz", NullLogger<UserPermissionGrants>.Instance);

    [Fact]
    public async Task APersonsGrantsAreTheRowsThatNameThemWithTheirRoleAssignmentAndPermission()
    {
        var expires = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var store = new InMemoryConfigurationStore().Holds("UserPermissionGrant",
        [
            Row(Person.ToString("D"), Admin, "Admin", "roles:write"),
            Row(Person.ToString("D"), Admin, "Admin", "users:read"),
            Row(Person.ToString("D"), Viewer, "Viewer", null, expires),
            Row(Guid.NewGuid().ToString("D"), Admin, "Admin", "users:read"),
        ]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.IsSuccess.ShouldBeTrue(held.CurrentMessage);
        held.Value!.ToArray().ShouldBe(new[]
        {
            new UserPermissionGrant(Tenant, null, Admin, "Admin", false, Tenant, "roles:write"),
            new UserPermissionGrant(Tenant, null, Admin, "Admin", false, Tenant, "users:read"),
            new UserPermissionGrant(Tenant, expires, Viewer, "Viewer", false, Tenant, null),
        });
    }

    [Fact]
    public async Task TheRowsReadForOnePersonDoNotGrowWithTheNumberOfPeople()
    {
        var others = Enumerable.Range(0, 5000).Select(_ => Row(Guid.NewGuid().ToString("D"), Admin, "Admin", "users:read")).ToList();
        var store = new InMemoryConfigurationStore().Holds("UserPermissionGrant",
            [Row(Person.ToString("D"), Viewer, "Viewer", "users:read"), .. others]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.Count.ShouldBe(1);
        store.RowsReturned.ShouldBe(1);
        store.Queries.ToArray().ShouldBe(new[] { "UserPermissionGrant" });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AGrantIsFoundWhicheverWayTheIdWasSpelled(bool upper)
    {
        var spelled = upper ? Person.ToString("D").ToUpperInvariant() : Person.ToString("D").ToLowerInvariant();
        var store = new InMemoryConfigurationStore().Holds("UserPermissionGrant", [Row(spelled, Admin, "Admin", "users:read")]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.ShouldHaveSingleItem().RoleId.ShouldBe(Admin);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheCachedReadIsDroppedByAWriteToAnyTableTheViewIsMadeOf()
    {
        var store = new InMemoryConfigurationStore().Holds("UserPermissionGrant", [Row(Person.ToString("D"), Admin, "Admin", "users:read")]);

        await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        // Giving or removing a role writes UserRole and UserRoleConfiguration; changing what a role holds writes the rest.
        var tags = (string[])store.Commands.ShouldHaveSingleItem().Metadata[CachePolicy.CacheInvalidationTagsKey];
        tags.ShouldBe(
            new[]
            {
                "authz.UserPermissionGrant", "authz.UserRole", "authz.UserRoleConfiguration", "authz.Role", "authz.RoleConfiguration",
                "authz.RolePermission", "authz.RolePermissionConfiguration", "authz.Permission", "authz.PermissionConfiguration",
            },
            ignoreOrder: true);
    }

    [Fact]
    public async Task AServiceIdentityIsNotAPersonAndMatchesNobody()
    {
        var store = new InMemoryConfigurationStore().Holds("UserPermissionGrant", [Row("fdw.scheduler", Admin, "Admin", "users:read")]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.ShouldBeEmpty();
    }

    [Fact]
    public async Task APersonWhoseRolesGrantMoreRowsThanOneReadCarriesFailsTheReadInsteadOfCuttingTheirPermissionsShort()
    {
        var many = Enumerable.Range(0, UserPermissionGrants.MaxRows + 1).Select(key => Row(Person.ToString("D"), Admin, "Admin", "p" + key)).ToList();
        var store = new InMemoryConfigurationStore().Holds("UserPermissionGrant", many);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task ARowWithoutARoleFailsTheReadInsteadOfBeingSkipped()
    {
        var withoutRole = InMemoryConfigurationStore.Row(
            ("UserId", Person.ToString("D")), ("AssignmentTenantId", Tenant), ("ExpiresAt", null), ("RoleName", "Admin"),
            ("IsTenantScoped", false), ("RoleTenantId", Tenant), ("PermissionName", "users:read"));
        var store = new InMemoryConfigurationStore().Holds("UserPermissionGrant", [withoutRole]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.IsFailure.ShouldBeTrue();
    }
}
