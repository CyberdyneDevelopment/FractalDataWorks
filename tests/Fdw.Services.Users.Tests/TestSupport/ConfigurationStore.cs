using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Users.Tests.TestSupport;

/// <summary>
/// A REAL domain configuration provider over a faked configuration store.
/// </summary>
/// <remarks>
/// Why not a mock of the provider: these providers are sealed and their reads are not virtual, so
/// nothing can stand in for one. The store is supplied the way production supplies it — domain rows
/// from the gateway, each dispatched to the implementation provider registered for the row's
/// implementation — and a test states only what the store HOLDS.
/// <para>
/// The fake honours the identity filter each read command carries — <c>Name</c> and <c>Id</c> — so
/// a test states "no match" by holding OTHER records rather than by holding none, and a read that
/// ignored the key would fail here instead of passing on an empty store.
/// </para>
/// <para>
/// It deliberately does NOT honour the version predicate (<c>IsCurrent</c>/<c>IsDeleted</c>): these
/// rows carry no version history, so filtering on it would drop every one of them. The consequence
/// is a real gap — no test built on this harness can express "the row exists but is superseded or
/// soft-deleted", which is the one part of the read contract it cannot exercise.
/// </para>
/// <para>
/// A row's id IS the record's id — that is what the domain provider stamps back onto the
/// implementation on every save, so the fake would misreport identity if it minted its own.
/// </para>
/// <para>
/// <see cref="Implementations"/> is exposed so a write test can assert what reached the
/// implementation row, which is where a save actually lands.
/// </para>
/// </remarks>
/// <typeparam name="TProvider">The domain provider under test.</typeparam>
/// <typeparam name="TContract">The domain's implementation contract.</typeparam>
internal sealed class ConfigurationStore<TProvider, TContract>
    where TProvider : DomainConfigurationProviderBase<TContract>
    where TContract : IImplementationConfiguration
{
    private readonly List<DomainConfiguration> _rows = [];
    private readonly Dictionary<Guid, TContract> _byRow = [];
    private IGenericMessage? _readFailure;

    private ConfigurationStore(
        TProvider provider,
        string implementationName,
        Mock<IImplementationConfigurationProvider<TContract>> implementations,
        Mock<IConfigurationGateway> gateway)
    {
        Provider = provider;
        ImplementationName = implementationName;
        Implementations = implementations;
        Gateway = gateway;
    }

    /// <summary>The real provider, reading and writing through the faked store.</summary>
    public TProvider Provider { get; }

    /// <summary>The implementation every row in this store is written under.</summary>
    public string ImplementationName { get; }

    /// <summary>The implementation provider the domain dispatches to.</summary>
    public Mock<IImplementationConfigurationProvider<TContract>> Implementations { get; }

    /// <summary>The faked gateway underneath, for tests that assert on the domain row itself.</summary>
    public Mock<IConfigurationGateway> Gateway { get; }

    /// <summary>A store, empty, whose rows are written under <paramref name="implementationName"/>.</summary>
    public static ConfigurationStore<TProvider, TContract> Empty(
        Func<IConfigurationGatewayProvider, TProvider> create,
        string implementationName)
    {
        var gateway = new Mock<IConfigurationGateway>();
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways
            .Setup(p => p.Get(It.IsAny<string>()))
            .Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));

        // The provider is built before the gateway is armed: it only stores the reference at
        // construction and reads through it later, so nothing is asked of the gateway yet.
        var store = new ConfigurationStore<TProvider, TContract>(
            create(gateways.Object),
            implementationName,
            new Mock<IImplementationConfigurationProvider<TContract>>(),
            gateway);

        // Read late, not at setup time: a test adds rows after the store is built, and the command
        // decides which of them come back.
        gateway
            .Setup(g => g.Execute<IEnumerable<DomainConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken __) =>
                store._readFailure is { } failure
                    ? GenericResult<IEnumerable<DomainConfiguration>>.Failure(failure)
                    : GenericResult<IEnumerable<DomainConfiguration>>.Success(store.Matching(command)));
        gateway
            .Setup(g => g.Execute<DomainConfiguration>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DomainConfiguration>.Success(new DomainConfiguration()));

        store.Implementations
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) => Task.FromResult(
                store._byRow.TryGetValue(id, out var item)
                    ? GenericResult<TContract>.Success(item)
                    : GenericResult<TContract>.Success(default!)));
        store.SaveSucceeds();

        store.Provider.Register(implementationName, store.Implementations.Object);
        return store;
    }

    /// <summary>A store holding exactly these rows.</summary>
    public static ConfigurationStore<TProvider, TContract> Holding(
        Func<IConfigurationGatewayProvider, TProvider> create,
        string implementationName,
        params TContract[] items)
    {
        var store = Empty(create, implementationName);
        foreach (var item in items) store.Holds(item);
        return store;
    }

    /// <summary>A store whose rows cannot be read.</summary>
    public static ConfigurationStore<TProvider, TContract> Unreadable(
        Func<IConfigurationGatewayProvider, TProvider> create,
        string implementationName,
        string reason)
    {
        var store = Empty(create, implementationName);
        store.CannotBeRead(new GenericMessage(reason));
        return store;
    }

    /// <summary>Adds a record, and the domain row that names it, to what this store holds.</summary>
    public void Holds(TContract item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // One row per id: a store cannot hold the same configuration twice, and appending a second
        // row for an id already held would return one record twice from a single read.
        _byRow[item.Id] = item;
        _rows.RemoveAll(row => row.Id == item.Id);
        _rows.Add(new DomainConfiguration
        {
            Id = item.Id,
            Name = item.Name,
            Domain = typeof(TContract).Name,
            Implementation = ImplementationName,
        });
    }

    /// <summary>
    /// The rows this command asks for.
    /// </summary>
    /// <remarks>
    /// The read commands filter on the identity columns — <c>Get(name)</c> on <c>Name</c>,
    /// <c>Get(id)</c> on <c>Id</c>, <c>List</c> on neither — alongside the version predicate. Only
    /// the identity conditions are honoured here: these rows model no version history, so filtering
    /// on <c>IsCurrent</c>/<c>IsDeleted</c> would drop every row. Honouring identity is what lets a
    /// test say "the store holds users, but not THIS one" rather than only "the store is empty".
    /// </remarks>
    private List<DomainConfiguration> Matching(IDataCommand command)
    {
        if (command is not IQueryCommand { Filter.Root: { } root }) return _rows.ToList();

        var identity = Conditions(root)
            .Where(condition => condition.PropertyName is "Name" or "Id")
            .ToList();

        return _rows.Where(row => identity.TrueForAll(condition => Matches(row, condition))).ToList();
    }

    private static IEnumerable<IFilterCondition> Conditions(IFilterNode node)
    {
        switch (node)
        {
            case IFilterCondition condition:
                yield return condition;
                break;
            case FilterGroup group:
                foreach (var child in group.Nodes)
                    foreach (var condition in Conditions(child))
                        yield return condition;
                break;
            default:
                break;
        }
    }

    private static bool Matches(DomainConfiguration row, IFilterCondition condition)
        => condition.PropertyName switch
        {
            "Name" => string.Equals(row.Name, condition.Value as string, StringComparison.Ordinal),
            "Id" => condition.Value is Guid id && row.Id == id,
            _ => true,
        };

    /// <summary>Makes every read of this store fail.</summary>
    public void CannotBeRead(IGenericMessage message) => _readFailure = message;

    /// <summary>Makes the implementation write succeed, handing back the record it was given.</summary>
    public void SaveSucceeds()
        => Implementations
            .Setup(p => p.Save(It.IsAny<TContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TContract record, CancellationToken _) => GenericResult<TContract>.Success(record));

    /// <summary>Makes the implementation write fail.</summary>
    public void SaveFails(IGenericMessage message)
        => Implementations
            .Setup(p => p.Save(It.IsAny<TContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TContract>.Failure(message));
}

/// <summary>Convenience constructors for the stores these tests use.</summary>
/// <remarks>
/// Each names the implementation production actually writes under for that domain, so a save in a
/// test reaches the same registered provider it would reach in the running system.
/// </remarks>
internal static class ConfigurationStore
{
    /// <summary>The implementation name the Users domain writes under.</summary>
    public const string UsersImplementation = "Users";

    /// <summary>The implementation name the UserTenants domain writes under.</summary>
    public const string UserTenantsImplementation = "UserTenants";

    /// <summary>The implementation name the UsersService domain writes under.</summary>
    public const string UsersServiceImplementation = "UsersService";

    /// <summary>The user-to-tenant memberships this store holds.</summary>
    public static ConfigurationStore<UserTenantConfigurationProvider, IUserTenantImplementationConfiguration> UserTenants(
        params IUserTenantImplementationConfiguration[] items)
        => ConfigurationStore<UserTenantConfigurationProvider, IUserTenantImplementationConfiguration>.Holding(
            NewUserTenantProvider, UserTenantsImplementation, items);

    /// <summary>A user-tenant store whose rows cannot be read.</summary>
    public static ConfigurationStore<UserTenantConfigurationProvider, IUserTenantImplementationConfiguration> UnreadableUserTenants(
        string reason)
        => ConfigurationStore<UserTenantConfigurationProvider, IUserTenantImplementationConfiguration>.Unreadable(
            NewUserTenantProvider, UserTenantsImplementation, reason);

    /// <summary>The users this store holds.</summary>
    public static ConfigurationStore<UserConfigurationProvider, IUserImplementationConfiguration> Users(
        params IUserImplementationConfiguration[] items)
        => ConfigurationStore<UserConfigurationProvider, IUserImplementationConfiguration>.Holding(
            NewUserProvider, UsersImplementation, items);

    /// <summary>A user store whose rows cannot be read.</summary>
    public static ConfigurationStore<UserConfigurationProvider, IUserImplementationConfiguration> UnreadableUsers(
        string reason)
        => ConfigurationStore<UserConfigurationProvider, IUserImplementationConfiguration>.Unreadable(
            NewUserProvider, UsersImplementation, reason);

    /// <summary>The users-service configuration this store holds.</summary>
    public static ConfigurationStore<UsersServiceConfigurationProvider, IUsersServiceImplementationConfiguration> UsersService(
        params IUsersServiceImplementationConfiguration[] items)
        => ConfigurationStore<UsersServiceConfigurationProvider, IUsersServiceImplementationConfiguration>.Holding(
            gateways => new UsersServiceConfigurationProvider(
                NullLogger<UsersServiceConfigurationProvider>.Instance, gateways, "PlatformConfiguration"),
            UsersServiceImplementation,
            items);

    private static UserConfigurationProvider NewUserProvider(IConfigurationGatewayProvider gateways)
        => new(NullLogger<UserConfigurationProvider>.Instance, gateways, "PlatformConfiguration");

    private static UserTenantConfigurationProvider NewUserTenantProvider(IConfigurationGatewayProvider gateways)
        => new(NullLogger<UserTenantConfigurationProvider>.Instance, gateways, "PlatformConfiguration");
}
