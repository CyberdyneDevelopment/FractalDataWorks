using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Data.Tests.Search;
using Fdw.Services.Users.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// FDW-2676: the tenants of one person are read by the person's id. Only that person's memberships and their domain rows are read, however many
/// people there are, and a membership counts only while its domain row is the current one and not deleted.
/// </summary>
public sealed class UserTenantsHeldTests
{
    private static readonly Guid Person = Guid.Parse("0a1b2c3d-0000-0000-0000-00000000000a");
    private static readonly Guid Acme = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Globex = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    private static Dictionary<string, object?> Membership(int key, int domain, Guid user, Guid tenant, bool deleted = false, bool isDefault = false, Guid? organization = null)
        => InMemoryConfigurationStore.Row(("RowId", key), ("UserTenantsRowId", domain), ("UserId", user), ("TenantId", tenant), ("IsDefault", isDefault), ("DefaultOrganizationId", organization), ("IsDeleted", deleted));

    private static Dictionary<string, object?> Domain(int key, bool current = true)
        => InMemoryConfigurationStore.Row(("RowId", key), ("IsCurrent", current));

    private static UserTenantsHeld Reader(InMemoryConfigurationStore store) => new(store.Gateways, "PlatformConfiguration", "tenant", NullLogger<UserTenantsHeld>.Instance);

    [Fact]
    public async Task APersonsTenantsAreTheMembershipsThatNameThem()
    {
        var store = new InMemoryConfigurationStore()
            .Holds("UserTenantsConfiguration", [Membership(1, 11, Person, Acme, isDefault: true), Membership(2, 12, Person, Globex), Membership(3, 13, Guid.NewGuid(), Acme)])
            .Holds("UserTenants", [Domain(11), Domain(12), Domain(13)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.IsSuccess.ShouldBeTrue(held.CurrentMessage);
        held.Value!.ToArray().ShouldBe(new[] { new HeldTenant(Acme, true, null), new HeldTenant(Globex, false, null) });
    }

    [Fact]
    public async Task TheDepartmentAPersonMarkedAsDefaultIsReadWithTheMembership()
    {
        var finance = Guid.Parse("44444444-0000-0000-0000-000000000001");
        var store = new InMemoryConfigurationStore()
            .Holds("UserTenantsConfiguration", [Membership(1, 11, Person, Acme, isDefault: true, organization: finance), Membership(2, 12, Person, Globex)])
            .Holds("UserTenants", [Domain(11), Domain(12)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.IsSuccess.ShouldBeTrue(held.CurrentMessage);
        held.Value!.ToArray().ShouldBe(new[] { new HeldTenant(Acme, true, finance), new HeldTenant(Globex, false, null) });
    }

    [Fact]
    public async Task TheRowsReadToResolveOnePersonDoNotGrowWithTheNumberOfPeople()
    {
        var others = Enumerable.Range(100, 5000).Select(key => Membership(key, key, Guid.NewGuid(), Acme)).ToList();
        var store = new InMemoryConfigurationStore()
            .Holds("UserTenantsConfiguration", [Membership(1, 11, Person, Acme), Membership(2, 12, Person, Globex), .. others])
            .Holds("UserTenants", [Domain(11), Domain(12), .. others.Select(row => Domain(Convert.ToInt32(row["UserTenantsRowId"])))]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.Count.ShouldBe(2);
        store.RowsReturned.ShouldBe(4);
        store.Queries.ToArray().ShouldBe(new[] { "UserTenantsConfiguration", "UserTenants" });
    }

    [Fact]
    public async Task AMembershipWhoseDomainRowIsNoLongerCurrentIsLeftOut()
    {
        var store = new InMemoryConfigurationStore()
            .Holds("UserTenantsConfiguration", [Membership(1, 11, Person, Acme), Membership(2, 12, Person, Globex)])
            .Holds("UserTenants", [Domain(11, current: false), Domain(12)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.ToArray().ShouldBe(new[] { new HeldTenant(Globex, false, null) });
    }

    [Fact]
    public async Task ADeletedMembershipIsNotRead()
    {
        var store = new InMemoryConfigurationStore()
            .Holds("UserTenantsConfiguration", [Membership(1, 11, Person, Acme, deleted: true)])
            .Holds("UserTenants", [Domain(11)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.ShouldBeEmpty();
    }

    [Fact]
    public async Task APersonWithNoMembershipsReadsOnlyTheMembershipsTable()
    {
        var store = new InMemoryConfigurationStore()
            .Holds("UserTenantsConfiguration", [Membership(1, 11, Guid.NewGuid(), Acme)])
            .Holds("UserTenants", [Domain(11)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.Value!.ShouldBeEmpty();
        store.Queries.ToArray().ShouldBe(new[] { "UserTenantsConfiguration" });
    }

    [Fact]
    public async Task APersonWithMoreMembershipsThanOneReadCarriesFailsTheReadInsteadOfCuttingTheirTenantsShort()
    {
        var many = Enumerable.Range(1, UserTenantsHeld.MaxMemberships + 1).Select(key => Membership(key, key, Person, Guid.NewGuid())).ToList();
        var store = new InMemoryConfigurationStore().Holds("UserTenantsConfiguration", many).Holds("UserTenants", []);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.IsFailure.ShouldBeTrue();
        store.Queries.ToArray().ShouldBe(new[] { "UserTenantsConfiguration" });
    }

    [Fact]
    public async Task AMembershipWithoutATenantFailsTheReadInsteadOfBeingSkipped()
    {
        var withoutTenant = InMemoryConfigurationStore.Row(("RowId", 1), ("UserTenantsRowId", 11), ("UserId", Person));
        var store = new InMemoryConfigurationStore().Holds("UserTenantsConfiguration", [withoutTenant]).Holds("UserTenants", [Domain(11)]);

        var held = await Reader(store).Get(Person, TestContext.Current.CancellationToken);

        held.IsFailure.ShouldBeTrue();
    }
}
