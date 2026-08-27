using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// Unit tests for <see cref="UserConfigurationProvider"/>.
///
/// Only <see cref="IConfigurationGateway"/> is faked. The real provider code runs
/// under test, including the Username column filter (not Name).
/// </summary>
public class UserConfigurationProviderTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    // Why: IFilterNode is a marker interface; IFilterCondition is a leaf; IFilterGroup is a
    // composite with child Nodes. Walk the tree recursively to gather all leaf conditions.
    private static List<IFilterCondition> CollectConditions(IFilterNode? node)
    {
        var results = new List<IFilterCondition>();
        if (node is null)
            return results;

        if (node is IFilterCondition leaf)
        {
            results.Add(leaf);
            return results;
        }

        if (node is FilterGroup group)
        {
            foreach (var child in group.Nodes)
                results.AddRange(CollectConditions(child));
        }

        return results;
    }

    private static UserConfigurationProvider MakeProvider(
        Mock<IConfigurationGateway>? gateway = null,
        params UserConfiguration[] storedRows)
    {

        var gw = gateway ?? new Mock<IConfigurationGateway>();

        if (gateway is null)
        {
            // Why: default setup returns all storedRows for any Execute call so individual tests
            // don't need to configure the mock unless they need specific behaviour.
            gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                    It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(storedRows));
        }

        return new UserConfigurationProvider(
            NullLogger<UserConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => gw.Object));
    }

    private static UserConfiguration User(
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

    // ── GetUser(Guid) ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetUserByIdReturnsSuccessWhenUserFound()
    {
        var stored = User();
        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        // Why: GetUser(Guid) calls base.Get(id) which executes a by-id query. Return the
        // stored row for that call.
        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(new[] { stored }));

        var provider = MakeProvider(gw);

        var result = await provider.GetUser(stored.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(stored.Id);
        result.Value.Username.ShouldBe(stored.Username);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetUserByIdReturnsNullValueWhenNotFound()
    {
        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(
              Enumerable.Empty<UserConfiguration>()));

        var provider = MakeProvider(gw);

        var result = await provider.GetUser(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Why: GetUser(Guid) wraps base.Get(id) which returns Success(null) when no row matches.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    // ── GetUser(string) — Username column, NOT Name ───────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetUserByUsernameQueriesUsernameColumn()
    {
        // Why: Regression guard for FDW-532-analogue — usr.Users has no [Name] column. The
        // query MUST filter on [Username]; using the inherited base.Get(string) would filter on
        // [Name] and return no rows against a real DB. This test captures the right command.
        var stored = User("bob");
        IDataCommand? capturedCommand = null;

        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => capturedCommand = cmd)
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(new[] { stored }));

        var provider = MakeProvider(gw);

        var result = await provider.GetUser("bob", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Username.ShouldBe("bob");

        // Why: Cast to QueryCommand<UserConfiguration> to inspect the filter tree. A future
        // regression that reverts GetUser(string) to the inherited base.Get(string) (which
        // filters on [Name] not [Username]) will fail here.
        capturedCommand.ShouldNotBeNull();
        var queryCmd = capturedCommand.ShouldBeOfType<QueryCommand<UserConfiguration>>();
        queryCmd.Filter.ShouldNotBeNull();

        // Walk the filter conditions and assert at least one targets "Username".
        var conditions = CollectConditions(queryCmd.Filter.Root);
        conditions.ShouldContain(
            c => string.Equals(c.PropertyName, "Username", StringComparison.Ordinal),
            "GetUser(string) must filter on [Username], not [Name]");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Users")]
    public async Task GetUserByUsernameReturnsNullValueWhenNoMatch()
    {
        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(
              Enumerable.Empty<UserConfiguration>()));

        var provider = MakeProvider(gw);

        var result = await provider.GetUser("nobody", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    // ── ResolveUser(string) — the shared id-or-name route resolver ────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task ResolveUserWithGuidQueriesByIdNotUsername()
    {
        // Why: THE regression guard for the role-revocation defect. RevokeUserRole and GetUserRoles
        // used to call GetUser(string) directly, so a client sending a Guid (which UserApiClient
        // does) made the provider hunt for a user whose USERNAME was "3f2a8c…". It never matched,
        // revoke 404'd, and the UI reported success — role grants became one-way. A Guid must
        // resolve by id; if this ever filters on [Username] again, that bug is back.
        var stored = User("bob");
        IDataCommand? capturedCommand = null;

        var gw = new Mock<IConfigurationGateway>();
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => capturedCommand = cmd)
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(new[] { stored }));

        var result = await MakeProvider(gw).ResolveUser(stored.Id.ToString(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(stored.Id);

        capturedCommand.ShouldNotBeNull();
        CollectConditions((capturedCommand as QueryCommand<UserConfiguration>)?.Filter?.Root)
            .ShouldNotContain(
                c => string.Equals(c.PropertyName, "Username", StringComparison.Ordinal),
                "a Guid must resolve by id — filtering on [Username] is the revocation bug");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task ResolveUserWithUsernameQueriesUsernameColumn()
    {
        var stored = User("bob");
        IDataCommand? capturedCommand = null;

        var gw = new Mock<IConfigurationGateway>();
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => capturedCommand = cmd)
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(new[] { stored }));

        var result = await MakeProvider(gw).ResolveUser("bob", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Username.ShouldBe("bob");

        capturedCommand.ShouldNotBeNull();
        CollectConditions((capturedCommand as QueryCommand<UserConfiguration>)?.Filter?.Root)
            .ShouldContain(
                c => string.Equals(c.PropertyName, "Username", StringComparison.Ordinal),
                "a non-Guid segment must resolve by [Username]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task ResolveUserFailsLoudWhenNoUserMatches()
    {
        // Why: ResolveUser must FAIL on a miss, not hand back Success(null). The endpoints branch on
        // IsSuccess; a null-valued success would let a caller treat "no such user" as a normal result.
        var gw = new Mock<IConfigurationGateway>();
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(
              Enumerable.Empty<UserConfiguration>()));

        var result = await MakeProvider(gw).ResolveUser("nobody", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── GetAllUsers ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetAllUsersReturnsAllStoredRows()
    {
        var users = new[] { User("alice"), User("bob"), User("carol") };

        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(users));

        var provider = MakeProvider(gw);

        var result = await provider.GetAllUsers(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(3);
        result.Value.Select(u => u.Username).ShouldBe(new[] { "alice", "bob", "carol" }, ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Users")]
    public async Task GetAllUsersReturnsEmptyListWhenNoUsers()
    {
        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(
              Enumerable.Empty<UserConfiguration>()));

        var provider = MakeProvider(gw);

        var result = await provider.GetAllUsers(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    // ── CreateUser ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task CreateUserReturnsNewGuidOnSuccess()
    {
        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        // Why: CreateUser issues three gateway calls in sequence:
        //   1. Execute<IEnumerable<UserConfiguration>> — by-username existence check (must be empty)
        //   2. Execute<IEnumerable<UserConfiguration>> — inside Save().Get(id) pre-insert check (empty)
        //   3. Execute<UserConfiguration>              — the actual ConfigurationSaveCommand insert
        // Moq matches on the result type; both IEnumerable<> calls share one setup.
        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(
              Enumerable.Empty<UserConfiguration>()));

        // Why: ImplementationConfigurationProviderBase.Save() executes the insert via Execute<TConfig>
        // (not Execute<int>). Return the record passed in so the save round-trips successfully.
        gw.Setup(g => g.Execute<UserConfiguration>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync((IDataCommand _, DataStoreTarget _, CancellationToken _) =>
              GenericResult<UserConfiguration>.Success(new UserConfiguration()));

        var provider = MakeProvider(gw);

        var result = await provider.CreateUser(
            "newuser", "new@example.com", Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task CreateUserFailsWhenUserAlreadyExists()
    {
        var existing = User("duplicate");
        var gw = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        // Why: simulate the by-username check returning an existing user so CreateUser aborts.
        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(new[] { existing }));

        var provider = MakeProvider(gw);

        var result = await provider.CreateUser(
            "duplicate", "dup@example.com", Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        // Why: no insert must have been attempted when the user already exists.
        gw.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
