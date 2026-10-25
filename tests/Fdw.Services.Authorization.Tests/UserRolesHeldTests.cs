using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Tests.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// FDW-2668: the roles of one person are read by the person's id. Only that person's assignments and their domain rows are read, however many people
/// there are, and an assignment counts only while its domain row is the current one and not deleted.
/// </summary>
public sealed class UserRolesHeldTests
{
    private static readonly Guid Person = Guid.Parse("0a1b2c3d-0000-0000-0000-00000000000a");
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Admin = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid Viewer = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private static Dictionary<string, object?> Assignment(int key, int domain, string user, Guid role, DateTimeOffset? expires = null, bool deleted = false)
        => InMemoryConfigurationStore.Row(
            ("RowId", key), ("UserRoleRowId", domain), ("UserId", user), ("RoleId", role), ("TenantId", Tenant), ("ExpiresAt", expires), ("IsDeleted", deleted));

    private static Dictionary<string, object?> Domain(int key, bool current = true)
        => InMemoryConfigurationStore.Row(("RowId", key), ("IsCurrent", current));

    private static UserRolesHeld Reader(InMemoryConfigurationStore store) => new(store.Gateways, "PlatformConfiguration", "authz", NullLogger<UserRolesHeld>.Instance);

    [Fact]
    public async Task APersonsRolesAreTheAssignmentsThatNameThemWithTheirRoleTenantAndExpiry()
    {
        var expires = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var store = new InMemoryConfigurationStore()
            .Holds("UserRoleConfiguration",
            [
                Assignment(1, 11, Person.ToString("D"), Admin),
                Assignment(2, 12, Person.ToString("D"), Viewer, expires),
                Assignment(3, 13, Guid.NewGuid().ToString("D"), Admin),
            ])
            .Holds("UserRole", [Domain(11), Domain(12), Domain(13)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.IsSuccess.ShouldBeTrue(held.CurrentMessage);
        held.Value!.ToArray().ShouldBe(new[] { new HeldRole(Admin, Tenant, null), new HeldRole(Viewer, Tenant, expires) });
    }

    [Fact]
    public async Task TheRowsReadToResolveOnePersonDoNotGrowWithTheNumberOfPeople()
    {
        var others = Enumerable.Range(100, 5000).Select(key => Assignment(key, key, Guid.NewGuid().ToString("D"), Admin)).ToList();
        var store = new InMemoryConfigurationStore()
            .Holds("UserRoleConfiguration", [Assignment(1, 11, Person.ToString("D"), Admin), Assignment(2, 12, Person.ToString("D"), Viewer), .. others])
            .Holds("UserRole", [Domain(11), Domain(12), .. others.Select(row => Domain(Convert.ToInt32(row["UserRoleRowId"])))]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.Count.ShouldBe(2);
        // The person's two assignments, then their two domain rows: nothing of the 5000 others.
        store.RowsReturned.ShouldBe(4);
        store.Queries.ToArray().ShouldBe(new[] { "UserRoleConfiguration", "UserRole" });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnAssignmentIsFoundWhicheverWayTheIdWasSpelled(bool upper)
    {
        var spelled = upper ? Person.ToString("D").ToUpperInvariant() : Person.ToString("D").ToLowerInvariant();
        var store = new InMemoryConfigurationStore()
            .Holds("UserRoleConfiguration", [Assignment(1, 11, spelled, Admin)])
            .Holds("UserRole", [Domain(11)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.ShouldHaveSingleItem().RoleId.ShouldBe(Admin);
    }

    [Fact]
    public async Task AServiceIdentityIsNotAPersonAndMatchesNobody()
    {
        var store = new InMemoryConfigurationStore()
            .Holds("UserRoleConfiguration", [Assignment(1, 11, "fdw.scheduler", Admin)])
            .Holds("UserRole", [Domain(11)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnAssignmentWhoseDomainRowIsNoLongerCurrentIsLeftOut()
    {
        var store = new InMemoryConfigurationStore()
            .Holds("UserRoleConfiguration", [Assignment(1, 11, Person.ToString("D"), Admin), Assignment(2, 12, Person.ToString("D"), Viewer)])
            .Holds("UserRole", [Domain(11, current: false), Domain(12)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.ShouldHaveSingleItem().RoleId.ShouldBe(Viewer);
    }

    [Fact]
    public async Task ADeletedAssignmentIsNotRead()
    {
        var store = new InMemoryConfigurationStore()
            .Holds("UserRoleConfiguration", [Assignment(1, 11, Person.ToString("D"), Admin, deleted: true)])
            .Holds("UserRole", [Domain(11)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.ShouldBeEmpty();
    }

    [Fact]
    public async Task APersonWithNoAssignmentsReadsOnlyTheAssignmentsTable()
    {
        var store = new InMemoryConfigurationStore()
            .Holds("UserRoleConfiguration", [Assignment(1, 11, Guid.NewGuid().ToString("D"), Admin)])
            .Holds("UserRole", [Domain(11)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.ShouldBeEmpty();
        store.Queries.ToArray().ShouldBe(new[] { "UserRoleConfiguration" });
    }

    [Fact]
    public async Task APersonWithMoreAssignmentsThanOneReadCarriesFailsTheReadInsteadOfCuttingTheirRolesShort()
    {
        var many = Enumerable.Range(1, UserRolesHeld.MaxAssignments + 1).Select(key => Assignment(key, key, Person.ToString("D"), Admin)).ToList();
        var store = new InMemoryConfigurationStore().Holds("UserRoleConfiguration", many).Holds("UserRole", []);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.IsFailure.ShouldBeTrue();
        store.Queries.ToArray().ShouldBe(new[] { "UserRoleConfiguration" });
    }

    [Fact]
    public async Task AnAssignmentWithoutARoleFailsTheReadInsteadOfBeingSkipped()
    {
        var withoutRole = InMemoryConfigurationStore.Row(("RowId", 1), ("UserRoleRowId", 11), ("UserId", Person.ToString("D")), ("TenantId", Tenant), ("ExpiresAt", null));
        var store = new InMemoryConfigurationStore().Holds("UserRoleConfiguration", [withoutRole]).Holds("UserRole", [Domain(11)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.IsFailure.ShouldBeTrue();
    }
}
