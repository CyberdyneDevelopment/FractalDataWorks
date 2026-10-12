using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Fdw.Tests.Shared;

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
/// <typeparam name="TDomain">
/// The domain's record: <see cref="DomainConfiguration"/>, or the record a domain that lists fields
/// declares. The provider reads its rows as this type, so the fake has to answer for it.
/// </typeparam>
internal sealed class ConfigurationStore<TProvider, TContract, TDomain>
    where TProvider : DomainConfigurationProviderBase<TContract, TDomain>
    where TContract : IImplementationConfiguration
    where TDomain : DomainConfiguration, new()
{
    private readonly List<TDomain> _rows = [];
    private readonly Dictionary<Guid, TContract> _byRow = [];
    private IGenericMessage? _readFailure;

    private ConfigurationStore(
        TProvider provider,
        string implementationName,
        Mock<IConfigurationSupersedeParticipant<TContract>> implementations,
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
    public Mock<IConfigurationSupersedeParticipant<TContract>> Implementations { get; }

    /// <summary>Every configuration write the store received, in order, with the container it was addressed to.</summary>
    public List<(IDataCommand Command, DataStoreTarget Target)> Writes { get; } = [];

    /// <summary>The faked gateway underneath, for tests that assert on the domain row itself.</summary>
    public Mock<IConfigurationGateway> Gateway { get; }

    /// <summary>Every read of the domain table the store received, in order.</summary>
    public List<IDataCommand> DomainReads { get; } = [];

    /// <summary>A store, empty, whose rows are written under <paramref name="implementationName"/>.</summary>
    public static ConfigurationStore<TProvider, TContract, TDomain> Empty(
        Func<IConfigurationGatewayProvider, TProvider> create,
        string implementationName)
    {
        var gateway = new Mock<IConfigurationGateway>();
        GatewayWriteTransaction.StubOn(gateway);

        // A save resolves the domain container from the gateway's loaded store before it writes
        // (FDW-892: relationships come from the configured keys), so the store has to answer with a
        // path and a container. Any container will do: the implementation participant is faked, and
        // it is what reads the container's key.
        var container = new Mock<IDataContainer>();

        // A page read completes its ordering with the keys the container declares, so it declares one:
        // its own Id, a key that references no other container.
        var keyedField = new Mock<global::Fdw.Data.Abstractions.IDataField>();
        keyedField.SetupGet(f => f.Name).Returns("Id");
        var keyField = new Mock<IContainerKeyField>();
        keyField.SetupGet(k => k.LocalField).Returns(keyedField.Object);
        var key = new Mock<IContainerKey>();
        key.SetupGet(k => k.ReferencedContainer).Returns((IDataContainer?)null);
        key.SetupGet(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
        container.SetupGet(c => c.Keys).Returns(new List<IContainerKey> { key.Object });

        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Container(It.IsAny<string>())).Returns(GenericResult<IDataContainer>.Success(container.Object));
        var dataStore = new Mock<IDataStore>();
        dataStore.SetupGet(d => d.Name).Returns(ConfigurationStore.StoreName);
        dataStore.Setup(d => d.Path(It.IsAny<string>())).Returns(GenericResult<IDataNodePath>.Success(path.Object));
        gateway.SetupGet(g => g.DataStores).Returns([dataStore.Object]);
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways
            .Setup(p => p.Get(It.IsAny<string>()))
            .Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));

        // The provider is built before the gateway is armed: it only stores the reference at
        // construction and reads through it later, so nothing is asked of the gateway yet.
        var store = new ConfigurationStore<TProvider, TContract, TDomain>(
            create(gateways.Object),
            implementationName,
            new Mock<IConfigurationSupersedeParticipant<TContract>>(),
            gateway);

        // Read late, not at setup time: a test adds rows after the store is built, and the command
        // decides which of them come back.
        gateway
            .Setup(g => g.Execute<IEnumerable<TDomain>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback((IDataCommand command, DataStoreTarget _, CancellationToken __) => store.DomainReads.Add(command))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken __) =>
                store._readFailure is { } failure
                    ? GenericResult<IEnumerable<TDomain>>.Failure(failure)
                    : GenericResult<IEnumerable<TDomain>>.Success(store.Matching(command)));
        gateway
            .Setup(g => g.Execute<TDomain>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TDomain>.Success(new TDomain()));

        // Minting a domain row writes it and reads back its key values; saving onto an existing one
        // reads that row's key values. The implementation participant is faked, so any key values
        // stand in for the store's.
        // A retire reports the row it retired -- a new version of a domain row is only written once the
        // previous one is known to have been retired -- and a write reports the row it wrote. Every write
        // is recorded, in order, so a test can say what reached the store and in what sequence.
        gateway
            .Setup(g => g.Execute<ConfigurationWriteResult>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback((IDataCommand command, DataStoreTarget target, CancellationToken _) => store.Writes.Add((command, target)))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken __) =>
                GenericResult<ConfigurationWriteResult>.Success(
                    command is IConfigurationRetireCommand
                        ? new ConfigurationWriteResult([DomainKeys()], new Dictionary<string, object?>(StringComparer.Ordinal))
                        : new ConfigurationWriteResult([], DomainKeys())));
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success([DomainKeys()]));

        // A list read asks for each record whole; a single read asks for it to a depth.
        store.Implementations
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) => Task.FromResult(
                store._byRow.TryGetValue(id, out var item)
                    ? GenericResult<TContract>.Success(item)
                    : GenericResult<TContract>.Success(default!)));
        store.Implementations
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, int _, CancellationToken _) => Task.FromResult(
                store._byRow.TryGetValue(id, out var item)
                    ? GenericResult<TContract>.Success(item)
                    : GenericResult<TContract>.Success(default!)));
        store.SaveSucceeds();
        store.Implementations
            .Setup(p => p.Supersede(It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        store.Provider.Register(implementationName, store.Implementations.Object);
        return store;
    }

    private static Dictionary<string, object?> DomainKeys()
        => new(StringComparer.Ordinal) { ["RowId"] = 1 };

    /// <summary>A store holding exactly these rows.</summary>
    public static ConfigurationStore<TProvider, TContract, TDomain> Holding(
        Func<IConfigurationGatewayProvider, TProvider> create,
        string implementationName,
        params TContract[] items)
    {
        var store = Empty(create, implementationName);
        foreach (var item in items) store.Holds(item);
        return store;
    }

    /// <summary>A store whose rows cannot be read.</summary>
    public static ConfigurationStore<TProvider, TContract, TDomain> Unreadable(
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
        var row = new TDomain
        {
            Id = item.Id,
            Name = item.Name,
            Domain = typeof(TContract).Name,
            Implementation = ImplementationName,
        };

        // The fields a list shows are held on the domain row, copied from the record the way a save
        // copies them -- through the two types' mappers, by the properties the record declares beyond
        // the four every domain has.
        var recordMapper = PocoMapperCollection.ByName(typeof(TDomain).Name);
        var plainNames = PocoMapperCollection.ByName(nameof(DomainConfiguration)).GetPropertyNames();
        var listFields = recordMapper.GetPropertyNames().Where(name => !plainNames.Contains(name, StringComparer.Ordinal)).ToList();
        if (listFields.Count > 0)
        {
            var itemValues = PocoMapperCollection.ByName(item.GetType().Name).MapToParameters(item);
            foreach (var field in listFields)
                recordMapper.SetValue(row, field, itemValues[field]);
        }

        _rows.Add(row);
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
    private List<TDomain> Matching(IDataCommand command)
    {
        if (command is not IQueryCommand { Filter.Root: not null }) return _rows.ToList();

        var identity = ConfigurationStore.ConditionsOf(command)
            .Where(condition => condition.PropertyName is "Name" or "Id")
            .ToList();

        return _rows.Where(row => identity.TrueForAll(condition => Matches(row, condition))).ToList();
    }

    private static bool Matches(TDomain row, IFilterCondition condition)
        => condition.PropertyName switch
        {
            "Name" => string.Equals(row.Name, condition.Value as string, StringComparison.Ordinal),
            "Id" => condition.Value switch
            {
                Guid id => row.Id == id,
                // An In: the ids a read of several members names.
                IEnumerable<object?> ids => ids.Contains(row.Id),
                _ => false,
            },
            _ => true,
        };

    /// <summary>Makes every read of this store fail.</summary>
    public void CannotBeRead(IGenericMessage message) => _readFailure = message;

    /// <summary>Makes the implementation write succeed, handing back the record it was given.</summary>
    public void SaveSucceeds()
        => Implementations
            .Setup(p => p.Save(It.IsAny<TContract>(), It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TContract record, IDataContainer _, IReadOnlyDictionary<string, object?> _, ConfigurationWriteScope _, CancellationToken _) => GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], new Dictionary<string, object?>(StringComparer.Ordinal))));

    /// <summary>Makes the implementation write fail.</summary>
    public void SaveFails(IGenericMessage message)
        => Implementations
            .Setup(p => p.Save(It.IsAny<TContract>(), It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConfigurationWriteResult>.Failure(message));
}

/// <summary>Convenience constructors for the stores these tests use.</summary>
/// <remarks>
/// Each names the implementation production actually writes under for that domain, so a save in a
/// test reaches the same registered provider it would reach in the running system.
/// </remarks>
internal static class ConfigurationStore
{
    /// <summary>The configuration store every provider here reads and writes.</summary>
    public const string StoreName = "PlatformConfiguration";

    /// <summary>The implementation name the Users domain writes under.</summary>
    public const string UsersImplementation = "Users";

    /// <summary>The implementation name the UserTenants domain writes under.</summary>
    public const string UserTenantsImplementation = "UserTenants";

    /// <summary>The implementation name the UsersService domain writes under.</summary>
    public const string UsersServiceImplementation = "UsersService";

    /// <summary>The user-to-tenant memberships this store holds.</summary>
    public static ConfigurationStore<UserTenantConfigurationProvider, IUserTenantImplementationConfiguration, DomainConfiguration> UserTenants(
        params IUserTenantImplementationConfiguration[] items)
        => ConfigurationStore<UserTenantConfigurationProvider, IUserTenantImplementationConfiguration, DomainConfiguration>.Holding(
            NewUserTenantProvider, UserTenantsImplementation, items);

    /// <summary>A user-tenant store whose rows cannot be read.</summary>
    public static ConfigurationStore<UserTenantConfigurationProvider, IUserTenantImplementationConfiguration, DomainConfiguration> UnreadableUserTenants(
        string reason)
        => ConfigurationStore<UserTenantConfigurationProvider, IUserTenantImplementationConfiguration, DomainConfiguration>.Unreadable(
            NewUserTenantProvider, UserTenantsImplementation, reason);

    /// <summary>The conditions a read command filters on, the version predicates among them.</summary>
    /// <param name="command">A command a store received.</param>
    /// <returns>Every condition in the command's filter, however grouped; none when it has no filter.</returns>
    public static IReadOnlyList<IFilterCondition> ConditionsOf(IDataCommand command)
        => command is IQueryCommand { Filter.Root: { } root } ? Conditions(root).ToList() : [];

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

    /// <summary>The users this store holds.</summary>
    public static ConfigurationStore<UserConfigurationProvider, IUserImplementationConfiguration, UserDomainConfiguration> Users(
        params IUserImplementationConfiguration[] items)
        => ConfigurationStore<UserConfigurationProvider, IUserImplementationConfiguration, UserDomainConfiguration>.Holding(
            NewUserProvider, UsersImplementation, items);

    /// <summary>A user store whose rows cannot be read.</summary>
    public static ConfigurationStore<UserConfigurationProvider, IUserImplementationConfiguration, UserDomainConfiguration> UnreadableUsers(
        string reason)
        => ConfigurationStore<UserConfigurationProvider, IUserImplementationConfiguration, UserDomainConfiguration>.Unreadable(
            NewUserProvider, UsersImplementation, reason);

    /// <summary>The users-service configuration this store holds.</summary>
    public static ConfigurationStore<UsersServiceConfigurationProvider, IUsersServiceImplementationConfiguration, DomainConfiguration> UsersService(
        params IUsersServiceImplementationConfiguration[] items)
        => ConfigurationStore<UsersServiceConfigurationProvider, IUsersServiceImplementationConfiguration, DomainConfiguration>.Holding(
            gateways => new UsersServiceConfigurationProvider(
                NullLogger<UsersServiceConfigurationProvider>.Instance, gateways, StoreName, "usr", "UsersService"),
            UsersServiceImplementation,
            items);

    private static UserConfigurationProvider NewUserProvider(IConfigurationGatewayProvider gateways)
        => new(NullLogger<UserConfigurationProvider>.Instance, gateways, StoreName, "usr", "Users");

    private static UserTenantConfigurationProvider NewUserTenantProvider(IConfigurationGatewayProvider gateways)
        => new(NullLogger<UserTenantConfigurationProvider>.Instance, gateways, StoreName, "tenant", "UserTenants");
}
