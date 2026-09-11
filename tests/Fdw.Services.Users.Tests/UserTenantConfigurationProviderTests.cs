using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Tests.TestSupport;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// Unit tests for reading and writing user-to-tenant memberships through
/// <see cref="UserTenantConfigurationProvider"/>.
/// </summary>
/// <remarks>
/// The provider is REAL; only the configuration store beneath it is faked. The specialized verbs
/// this suite was written against — <c>GetUserTenants</c>, <c>GetDefaultTenant</c> and
/// <c>GrantTenantAccess</c> — no longer exist: a caller states its own predicate through
/// <c>Find</c> and writes through <c>Save</c>. These tests pin the predicates and the save a caller
/// must now write, which is where that behaviour lives.
/// </remarks>
public class UserTenantConfigurationProviderTests
{
    private const string UserTenantDomain = "UserTenants";

    private static UserTenantImplementationConfiguration Membership(
        Guid userId,
        Guid tenantId,
        bool isDefault = false)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = $"{userId}:{tenantId}",
            UserId = userId,
            TenantId = tenantId,
            IsDefault = isDefault,
            IsCurrent = true,
            IsDeleted = false,
        };

    /// <summary>Every membership this user holds.</summary>
    private static Func<IUserTenantImplementationConfiguration, bool> ForUser(Guid userId)
        => membership => membership.UserId == userId;

    /// <summary>The one membership this user holds by default.</summary>
    private static Func<IUserTenantImplementationConfiguration, bool> DefaultForUser(Guid userId)
        => membership => membership.UserId == userId && membership.IsDefault;

    // ── Reading a user's tenants ──────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task FindByUserReturnsTenantsForUser()
    {
        var userId = Guid.NewGuid();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var other = Guid.NewGuid();

        var store = ConfigurationStore.UserTenants(
            Membership(userId, tenantA),
            Membership(userId, tenantB),
            Membership(other, Guid.NewGuid()));

        var result = await store.Provider.Find(ForUser(userId), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(2);
        var tenants = result.Value.Select(m => m.TenantId).ToList();
        tenants.ShouldContain(tenantA);
        tenants.ShouldContain(tenantB);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Users")]
    public async Task FindByUserReturnsEmptyWhenNoMemberships()
    {
        var store = ConfigurationStore.UserTenants();

        var result = await store.Provider.Find(ForUser(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task FindByUserFailsLoudWhenTheStoreCannotBeRead()
    {
        // Why this matters: "this user belongs to no tenant" and "the store could not be read" are
        // opposite facts, and an empty list reported for the second denies access on bad evidence.
        var store = ConfigurationStore.UnreadableUserTenants("tenant store unavailable");

        var result = await store.Provider.Find(ForUser(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── Reading the default tenant ────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task FindDefaultReturnsTheDefaultMembership()
    {
        var userId = Guid.NewGuid();
        var defaultTenantId = Guid.NewGuid();

        var store = ConfigurationStore.UserTenants(
            Membership(userId, Guid.NewGuid()),
            Membership(userId, defaultTenantId, isDefault: true));

        var result = await store.Provider.Find(DefaultForUser(userId), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
        result.Value[0].TenantId.ShouldBe(defaultTenantId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task FindDefaultReturnsEmptyWhenNoDefaultRow()
    {
        var userId = Guid.NewGuid();

        // A membership exists, but none of them is the default one.
        var store = ConfigurationStore.UserTenants(Membership(userId, Guid.NewGuid()));

        var result = await store.Provider.Find(DefaultForUser(userId), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    // ── Granting access ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task SaveWritesTheMembershipToItsImplementationRow()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var store = ConfigurationStore.UserTenants();
        var membership = Membership(userId, tenantId, isDefault: true);

        var result = await store.Provider.Save(
            membership,
            UserTenantDomain,
            store.ImplementationName,
            membership.Name,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        store.Implementations.Verify(
            p => p.Save(It.IsAny<IUserTenantImplementationConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task SaveFailsLoudWhenNothingIsRegisteredForTheImplementationItNames()
    {
        // Why: a domain row naming an implementation nothing is registered for is the record that
        // fails to compose on the next read, so the write must refuse rather than half-land.
        var userId = Guid.NewGuid();
        var store = ConfigurationStore.UserTenants();
        var membership = Membership(userId, Guid.NewGuid());

        var result = await store.Provider.Save(
            membership,
            UserTenantDomain,
            "NoSuchImplementation",
            membership.Name,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        store.Implementations.Verify(
            p => p.Save(It.IsAny<IUserTenantImplementationConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
