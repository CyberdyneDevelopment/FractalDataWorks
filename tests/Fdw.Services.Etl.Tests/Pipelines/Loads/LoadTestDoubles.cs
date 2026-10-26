using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// The doubles the load executor tests run against: data sets, data stores and containers that resolve to a connection,
/// transactional connections that record what ran on them, and connection types that list capabilities.
/// </summary>
internal static class LoadTestDoubles
{
    /// <summary>A container of a data store: its name, kind, table path and the connection of its store.</summary>
    internal static IDataContainer Container(
        string name, Guid connectionId, string containerType = "Table", string schema = "dbo", string? database = null)
    {
        var store = new Mock<IDataStore>();
        store.SetupGet(s => s.ConnectionId).Returns(connectionId);
        var path = new Mock<IDataNodePath>();
        path.SetupGet(p => p.Store).Returns(store.Object);
        var type = new Mock<IContainerType>();
        type.SetupGet(t => t.Name).Returns(containerType);
        var tablePath = new Mock<IPath>();
        var database_ = tablePath.As<IDatabasePath>();
        database_.SetupGet(p => p.ObjectName).Returns(name);
        database_.SetupGet(p => p.Schema).Returns(schema);
        database_.SetupGet(p => p.Database).Returns(database);

        var container = new Mock<IDataContainer>();
        container.SetupGet(c => c.Name).Returns(name);
        container.SetupGet(c => c.Parent).Returns(path.Object);
        container.SetupGet(c => c.ContainerType).Returns(type.Object);
        container.SetupGet(c => c.Path).Returns(tablePath.Object);
        return container.Object;
    }

    /// <summary>A data set provider that knows the named data sets, each with its sources.</summary>
    internal static Mock<IDataSetConfigurationProvider> DataSetProviderOf(params (string Name, DataSetSourceConfiguration[] Sources)[] sets)
    {
        var provider = new Mock<IDataSetConfigurationProvider>();

        // A data set the test did not define is a failure, as it is from the real provider, never a null result.
        provider
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Failure(new Fdw.Messages.GenericMessage("data set not found")));
        foreach (var (name, sources) in sets)
        {
            var configuration = new Mock<IDataSetImplementationConfiguration>();
            configuration.SetupGet(c => c.Sources).Returns(sources.ToList());
            provider
                .Setup(p => p.Get(name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(configuration.Object));
        }

        return provider;
    }

    /// <summary>A source of a data set: a data store, path and container.</summary>
    internal static DataSetSourceConfiguration Source(string store, string path, string container) =>
        new() { DataStoreName = store, PathValue = path, ContainerName = container };

    /// <summary>A data store provider that knows the given containers.</summary>
    internal static Mock<IDataStoreProvider> DataStoreProviderOf(params (string Store, string Path, string Container, IDataContainer Value)[] containers)
    {
        var provider = new Mock<IDataStoreProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Failure(new Fdw.Messages.GenericMessage("container not found")));
        foreach (var (store, path, name, value) in containers)
        {
            provider
                .Setup(p => p.Get(store, path, name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IDataContainer>.Success(value));
        }

        return provider;
    }

    /// <summary>A connection provider that hands out the given connections by id.</summary>
    internal static Mock<IConnectionProvider> ConnectionProviderOf(params (Guid Id, IGenericConnection Connection)[] connections)
    {
        var provider = new Mock<IConnectionProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Failure(new Fdw.Messages.GenericMessage("connection not found")));
        foreach (var (id, connection) in connections)
        {
            provider
                .Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IGenericConnection>.Success(connection));
        }

        return provider;
    }

    /// <summary>A connection type that lists the named command capabilities.</summary>
    internal static IConnectionType ConnectionType(string[] capabilities)
    {
        var type = new Mock<IConnectionType>();
        type.SetupGet(t => t.SupportedCommands).Returns(capabilities.Select(name => (ICommandCapabilityType)CommandCapabilityTypes.ByName(name)).ToList());
        return type.Object;
    }

    /// <summary>The capabilities a full SQL-like connection type lists: its modes, not its data commands.</summary>
    internal static readonly string[] AllCapabilities =
        ["SoftDelete", "UnitOfWork"];

    /// <summary>A connection type resolver that answers per connection id.</summary>
    internal sealed class TypeResolver(Dictionary<Guid, IConnectionType> types) : IConnectionTypeResolver
    {
        public Task<IGenericResult<IConnectionType>> ForConnection(Guid connectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<IConnectionType>>(GenericResult<IConnectionType>.Success(types[connectionId]));
    }

    /// <summary>A transactional connection that appends what happens on it to a shared event list.</summary>
    internal sealed class RecordingConnection
    {
        private readonly List<string> _events;
        private readonly string _name;

        /// <summary>Initializes a new instance of the <see cref="RecordingConnection"/> class.</summary>
        /// <param name="name">The connection's name in the events.</param>
        /// <param name="events">Receives "begin:name", "exec:name:Verb", "commit:name" and "rollback:name" in order.</param>
        /// <param name="failVerb">The verb of the command whose execution fails, or <see langword="null"/> when none does.</param>
        internal RecordingConnection(string name, List<string> events, string? failVerb = null)
        {
            _name = name;
            _events = events;
            Commands = [];

            var transaction = new Mock<IDataConnectionTransaction>();
            transaction
                .Setup(t => t.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDataCommand command, IDataContainer container, CancellationToken token) =>
                {
                    _events.Add($"exec:{_name}:{command.CommandType}");
                    Commands.Add(command);
                    return string.Equals(command.CommandType, failVerb, StringComparison.Ordinal)
                        ? GenericResult<int>.Failure(new Fdw.Messages.GenericMessage($"{command.CommandType} failed"))
                        : GenericResult<int>.Success(1);
                });
            transaction
                .Setup(t => t.Commit(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    _events.Add($"commit:{_name}");
                    return GenericResult.Success();
                });
            transaction
                .Setup(t => t.Rollback(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    _events.Add($"rollback:{_name}");
                    return GenericResult.Success();
                });

            var connection = new Mock<ITransactionalDataConnection>();
            connection
                .Setup(c => c.BeginTransaction(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    _events.Add($"begin:{_name}");
                    return GenericResult<IDataConnectionTransaction>.Success(transaction.Object);
                });
            Connection = connection.Object;
        }

        /// <summary>Gets the connection.</summary>
        internal ITransactionalDataConnection Connection { get; }

        /// <summary>Gets the commands executed on it, in order.</summary>
        internal List<IDataCommand> Commands { get; }
    }
}
