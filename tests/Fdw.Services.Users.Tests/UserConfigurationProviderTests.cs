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
using Fdw.Services.Data;

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
            gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                    It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(storedRows));
        }

        return new UserConfigurationProvider(
            NullLogger<UserConfigurationProvider>.Instance,
            GatewayProviderFor(gw.Object),
            "PlatformConfiguration", "usr");
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
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

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
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(
              Enumerable.Empty<UserConfiguration>()));

        var provider = MakeProvider(gw);

        var result = await provider.GetUser(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    // ── GetUser(string) — Username column, NOT Name ───────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Users")]
    public async Task GetUserByUsernameQueriesUsernameColumn()
    {
        var stored = User("bob");
        IDataCommand? capturedCommand = null;

        var gw = new Mock<IConfigurationGateway>();
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
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(
              Enumerable.Empty<UserConfiguration>()));

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
        gw.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gw.Setup(g => g.Execute<IEnumerable<UserConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(GenericResult<IEnumerable<UserConfiguration>>.Success(new[] { existing }));

        var provider = MakeProvider(gw);

        var result = await provider.CreateUser(
            "duplicate", "dup@example.com", Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        gw.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IConfigurationGatewayProvider GatewayProviderFor(IConfigurationGateway gateway)
        => new AnyConnectionGateways(gateway);

    private sealed class AnyConnectionGateways : IConfigurationGatewayProvider
    {
        private readonly IConfigurationGateway _gateway;

        public AnyConnectionGateways(IConfigurationGateway gateway) => _gateway = gateway;

        public IGenericResult<IConfigurationGateway> Get(string connectionName)
            => GenericResult<IConfigurationGateway>.Success(_gateway);

        public IGenericResult Register(IConfigurationGateway gateway) => GenericResult.Success();
    }

}
