using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Tests.TestSupport;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// Unit tests for <see cref="UserConfigurationProvider"/>.
/// </summary>
/// <remarks>
/// The provider is REAL; only the configuration store beneath it is faked. The specialized verbs
/// this suite was written against — <c>GetUser</c>, <c>ResolveUser</c>, <c>GetAllUsers</c> and
/// <c>CreateUser</c> — no longer exist; a user is read by id or by name through <c>Get</c>, listed
/// through <c>Get()</c>, and written through <c>Save</c>.
/// <para>
/// A read hands back <see cref="IUserImplementationConfiguration"/>, which carries <c>Name</c> and
/// not the concrete record's <c>Username</c> alias — the user's name IS the domain row's name, which
/// is the whole point of the split.
/// </para>
/// <para>
/// The fake store honours the identity filter the read command carries, so a miss is stated as a
/// store that holds OTHER users — not as an empty store, which would pass whether or not the
/// provider filtered at all.
/// </para>
/// </remarks>
public class UserConfigurationProviderTests
{
    private const string UsersDomain = "Users";

    private static UserImplementationConfiguration User(
        string username = "alice",
        string email = "alice@example.com")
        => new()
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            IsActive = true,
            IsCurrent = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    // ── Reading one user ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetByIdComposesTheDomainRowWithItsImplementationRow()
    {
        var alice = User("alice", "alice@example.com");
        var store = ConfigurationStore.Users(alice, User("bob", "bob@example.com"));

        var result = await store.Provider.Get(alice.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        // The name, domain and implementation are stamped onto the record from the domain row —
        // they are the output of a read, never persisted on the implementation row.
        result.Value.Name.ShouldBe("alice");
        result.Value.Email.ShouldBe("alice@example.com");
        result.Value.Implementation.ShouldBe("Users");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetByIdReportsAMissAsSuccessCarryingNull()
    {
        // Users exist — just not this one. An empty store would pass even if the id were ignored.
        var store = ConfigurationStore.Users(User("alice"), User("bob"));

        var result = await store.Provider.Get(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetByNameReturnsTheUserUnderThatName()
    {
        // Why by name at all: the user's name IS the domain row's name, so a username lookup is the
        // ordinary domain read — there is no separate username column above the store any more.
        var store = ConfigurationStore.Users(User("alice"), User("bob", "bob@example.com"));

        var result = await store.Provider.Get("bob", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("bob");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Users")]
    public async Task GetByNameReportsAMissAsSuccessCarryingNull()
    {
        // Why this shape matters: a miss is NOT a failure. A caller that must 404 checks for a null
        // value; one that treated the absence as an error would report a broken store instead of an
        // unknown user.
        var store = ConfigurationStore.Users(User("alice"), User("bob"));

        var result = await store.Provider.Get("nobody", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    // ── Reading every user ────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetReturnsAllStoredRows()
    {
        var store = ConfigurationStore.Users(User("alice"), User("bob"), User("carol"));

        var result = await store.Provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(3);
        result.Value.Select(user => user.Name).ShouldBe(new[] { "alice", "bob", "carol" }, ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Users")]
    public async Task GetReturnsEmptyListWhenNoUsers()
    {
        var store = ConfigurationStore.Users();

        var result = await store.Provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetFailsLoudWhenTheStoreCannotBeRead()
    {
        // Why: "there are no users" and "the user store could not be read" are opposite facts, and an
        // empty list reported for the second is a silent outage.
        var store = ConfigurationStore.UnreadableUsers("user store unavailable");

        var result = await store.Provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── Writing a user ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task SaveWritesTheUserToItsImplementationRow()
    {
        var store = ConfigurationStore.Users();
        var user = User("newuser", "new@example.com");

        var result = await store.Provider.Save(
            user,
            UsersDomain,
            store.ImplementationName,
            user.Name,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        store.Implementations.Verify(
            p => p.Save(It.IsAny<IUserImplementationConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task SaveStampsTheDomainKeyOntoTheRecordItWrites()
    {
        // Why the four arguments: name finds the domain row, domain and implementation mint one when
        // it is absent, and the resulting domain Id is what gets stamped onto the implementation.
        // The record cannot supply that key — it IS the thing being keyed.
        var store = ConfigurationStore.Users();
        var user = User("newuser");
        IUserImplementationConfiguration? written = null;
        store.Implementations
            .Setup(p => p.Save(It.IsAny<IUserImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .Callback((IUserImplementationConfiguration record, CancellationToken _) => written = record)
            .ReturnsAsync((IUserImplementationConfiguration record, CancellationToken _) =>
                GenericResult<IUserImplementationConfiguration>.Success(record));

        var result = await store.Provider.Save(
            user,
            UsersDomain,
            store.ImplementationName,
            "newuser",
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        written.ShouldNotBeNull();
        written.Name.ShouldBe("newuser");
        written.Domain.ShouldBe(UsersDomain);
        written.Implementation.ShouldBe("Users");
        written.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task SaveFailsLoudWhenNothingIsRegisteredForTheImplementationItNames()
    {
        // Why: a domain row naming an implementation nothing is registered for is the record that
        // fails to compose on the next read, so the write must refuse rather than half-land.
        var store = ConfigurationStore.Users();
        var user = User("newuser");

        var result = await store.Provider.Save(
            user,
            UsersDomain,
            "NoSuchImplementation",
            user.Name,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        store.Implementations.Verify(
            p => p.Save(It.IsAny<IUserImplementationConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
