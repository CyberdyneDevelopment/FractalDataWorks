using Fdw.Commands.Data.Abstractions;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Tests.TestSupport;
using Fdw.Services.Configuration;
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
            p => p.Save(It.IsAny<IUserImplementationConfiguration>(), It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()),
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
            .Setup(p => p.Save(It.IsAny<IUserImplementationConfiguration>(), It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback((IUserImplementationConfiguration record, IDataContainer _, IReadOnlyDictionary<string, object?> _, ConfigurationWriteScope _, CancellationToken _) => written = record)
            .ReturnsAsync((IUserImplementationConfiguration record, IDataContainer _, IReadOnlyDictionary<string, object?> _, ConfigurationWriteScope _, CancellationToken _) => GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], new Dictionary<string, object?>(StringComparer.Ordinal))));

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
            p => p.Save(It.IsAny<IUserImplementationConfiguration>(), It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── A listed field that changes: a new version of the domain row ──────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task ReadingAUserStampsTheDomainRowsListFieldsOntoIt()
    {
        // Why: the implementation table no longer holds Email, IsActive or CreatedAt, so a read that did
        // not copy them from the domain row would return a user without them.
        var held = User("alice", "alice@example.com");
        var store = ConfigurationStore.Users(held);

        var read = await store.Provider.Get("alice", TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        read.Value!.Email.ShouldBe("alice@example.com");
        read.Value.IsActive.ShouldBeTrue();
        read.Value.CreatedAt.ShouldBe(held.CreatedAt);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task SavingAnUnchangedUserWritesNoNewVersionOfTheDomainRow()
    {
        var store = ConfigurationStore.Users(User("alice", "alice@example.com"));
        var loaded = (await store.Provider.Get("alice", TestContext.Current.CancellationToken)).Value!;

        var result = await store.Provider.Save(loaded, UsersDomain, store.ImplementationName, "alice", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        store.Writes.ShouldNotContain(write => write.Command is IConfigurationRetireCommand);
        store.Implementations.Verify(
            p => p.Supersede(It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task ChangingAListedFieldSupersedesTheImplementationThenTheDomainRowThenWritesBothAgain()
    {
        var store = ConfigurationStore.Users(User("alice", "alice@example.com"));
        var order = new List<string>();
        store.Implementations
            .Setup(p => p.Supersede(It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("implementation superseded"))
            .ReturnsAsync(GenericResult.Success());
        store.Implementations
            .Setup(p => p.Save(It.IsAny<IUserImplementationConfiguration>(), It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("implementation written"))
            .ReturnsAsync(GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], new Dictionary<string, object?>(StringComparer.Ordinal))));

        var loaded = (await store.Provider.Get("alice", TestContext.Current.CancellationToken)).Value!;
        loaded.Email = "alice@new.example.com";
        var result = await store.Provider.Save(loaded, UsersDomain, store.ImplementationName, "alice", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();

        // The old domain row is superseded, not deleted, and the new one is inserted -- never a Create,
        // whose field values would also select the row it retires.
        var domainWrites = store.Writes.Where(write => write.Target.Container == "Users").Select(write => write.Command).ToList();
        domainWrites.Count.ShouldBe(2);
        domainWrites[0].ShouldBeAssignableTo<IConfigurationRetireCommand>().MarkDeleted.ShouldBeFalse();
        var inserted = domainWrites[1].ShouldBeAssignableTo<IConfigurationSaveCommand>();
        inserted.ReplacesCurrent.ShouldBeFalse();
        inserted.ConfigurationType.ShouldBe(typeof(UserDomainConfiguration));
        ((UserDomainConfiguration)((IDataCommandWithInput)inserted).InputData!).Email.ShouldBe("alice@new.example.com");

        // The columns the record does not map -- where tenancy lives -- are carried from the old row.
        inserted.FieldValues.ShouldContainKey("RowId");
        inserted.FieldValues.ShouldNotContainKey("Email");

        order.ShouldBe(["implementation superseded", "implementation written"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task SavingALoadedUserUnderANewNameRenamesItsDomainRowInPlaceInsteadOfMintingASecondUser()
    {
        var store = ConfigurationStore.Users(User("alice", "alice@example.com"));
        var loaded = (await store.Provider.Get("alice", TestContext.Current.CancellationToken)).Value!;
        var id = loaded.Id;

        var result = await store.Provider.Save(loaded, UsersDomain, store.ImplementationName, "alicia", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        loaded.Id.ShouldBe(id);

        // The domain row is updated where it is: a row's Id is unique across its versions in the domain tables, so a new version
        // of it under the same Id would be refused. Nothing is retired and nothing is inserted into the domain table.
        store.Updates.Count.ShouldBe(1);
        store.Writes.Where(write => write.Target.Container == "Users").ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task SavingOneUserUnderTheNameOfAnotherIsRefusedAndWritesNothing()
    {
        var store = ConfigurationStore.Users(User("alice", "alice@example.com"), User("bob", "bob@example.com"));
        var loaded = (await store.Provider.Get("alice", TestContext.Current.CancellationToken)).Value!;

        var result = await store.Provider.Save(loaded, UsersDomain, store.ImplementationName, "bob", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("SERVICES-41061");
        store.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task SavingANewRecordUnderAnExistingNameStillSavesIntoThatUser()
    {
        var store = ConfigurationStore.Users(User("alice", "alice@example.com"));
        var fresh = User("alice", "alice@new.example.com");

        var result = await store.Provider.Save(fresh, UsersDomain, store.ImplementationName, "alice", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    // ── Reading several members by id (FDW-1003) ──────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetByIdsReturnsTheDomainRecordsOfExactlyThoseIdsInOneRead()
    {
        // Why: one domain names another's members (a data store its connection) from the owning domain's
        // table, in one query for the whole page, never one read per member and never a join across domains.
        var alice = User("alice", "alice@example.com");
        var bob = User("bob", "bob@example.com");
        var carol = User("carol", "carol@example.com");
        var store = ConfigurationStore.Users(alice, bob, carol);

        var read = await store.Provider.Get(new[] { alice.Id, carol.Id }, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        read.Value!.Select(record => record.Name).OrderBy(name => name, StringComparer.Ordinal).ShouldBe(["alice", "carol"]);
        read.Value!.Single(record => record.Name == "carol").Email.ShouldBe("carol@example.com");
        store.DomainReads.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetByIdsAsksTheStoreNothingWhenNoIdsAreNamed()
    {
        var store = ConfigurationStore.Users(User("alice", "alice@example.com"));

        var read = await store.Provider.Get(Array.Empty<Guid>(), TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBeEmpty();
        store.DomainReads.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetByIdsAsksEachIdOnce()
    {
        var alice = User("alice", "alice@example.com");
        var bob = User("bob", "bob@example.com");
        var store = ConfigurationStore.Users(alice, bob);

        var read = await store.Provider.Get(new[] { alice.Id, alice.Id, bob.Id }, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        read.Value!.Count.ShouldBe(2);
        var asked = ConfigurationStore.ConditionsOf(store.DomainReads.Single()).Single(condition => condition.PropertyName == "Id");
        asked.Value.ShouldBeAssignableTo<IReadOnlyCollection<object?>>()!.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetByIdsLeavesOutAnIdTheStoreDoesNotHold()
    {
        // Why: a member row-level security hides is absent from the store's answer, which is an absence
        // and not a failure -- the caller names what it is shown.
        var alice = User("alice", "alice@example.com");
        var store = ConfigurationStore.Users(alice, User("bob", "bob@example.com"));

        var read = await store.Provider.Get(new[] { alice.Id, Guid.NewGuid() }, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        read.Value!.ShouldHaveSingleItem().Name.ShouldBe("alice");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetByIdsFailsLoudWhenMoreIdsAreNamedThanOneQueryMayCarry()
    {
        // Why: a statement carries one parameter per id and SQL Server allows 2,100, so the ceiling is a
        // stated limit that fails, never a silent truncation that answers with fewer members.
        var store = ConfigurationStore.Users(User("alice", "alice@example.com"));
        var tooMany = Enumerable.Range(0, 1001).Select(_ => Guid.NewGuid()).ToList();

        var read = await store.Provider.Get(tooMany, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeFalse();
        store.DomainReads.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetByIdsFailsLoudWhenTheStoreCannotBeRead()
    {
        var store = ConfigurationStore.UnreadableUsers("user store unavailable");

        var read = await store.Provider.Get(new[] { Guid.NewGuid() }, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task ANewVersionOfTheDomainRowDropsTheCachedReadsOfTheDomainContainerOnCommit()
    {
        // Why: the child-row cache is tagged by container (FDW-983). A save that supersedes and inserts a
        // domain row writes the domain container, so its tag must be dropped or the next read would return
        // the old version's list fields.
        var store = ConfigurationStore.Users(User("alice", "alice@example.com"));
        var loaded = (await store.Provider.Get("alice", TestContext.Current.CancellationToken)).Value!;
        loaded.IsActive = false;

        var result = await store.Provider.Save(loaded, UsersDomain, store.ImplementationName, "alice", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        store.Gateway.Verify(
            g => g.InvalidateCachedResults(It.Is<DataStoreTarget>(target => target.Container == "Users")),
            Times.AtLeastOnce);
    }
}
