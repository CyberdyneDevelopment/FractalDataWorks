using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.EtlGateway;

/// <summary>The ETL gateway (FDW-1479). See <see cref="IEtlGateway"/>.</summary>
public sealed class EtlGatewayService : IEtlGateway
{
    private readonly IDomainConfigurationProvider<IDataStoreImplementationConfiguration> _dataStores;
    private readonly IDataConnectionProvider _connections;
    private readonly IDataStoreProvider _dataStoreProvider;
    private readonly IAuthenticationContextAccessor? _authentication;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="EtlGatewayService"/> class.</summary>
    /// <param name="dataStores">The data store configurations, read by name.</param>
    /// <param name="connections">Supplies the connection a data store reaches its data by.</param>
    /// <param name="dataStoreProvider">Says whether a data store is declared by the host. Required: a guard that can be left out is no guard.</param>
    /// <param name="authentication">Supplies the caller whose roles are checked.</param>
    /// <param name="logger">The logger.</param>
    public EtlGatewayService(
        IDomainConfigurationProvider<IDataStoreImplementationConfiguration> dataStores,
        IDataConnectionProvider connections,
        IDataStoreProvider dataStoreProvider,
        IAuthenticationContextAccessor? authentication = null,
        ILogger<EtlGatewayService>? logger = null)
    {
        _dataStores = dataStores ?? throw new ArgumentNullException(nameof(dataStores));
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _dataStoreProvider = dataStoreProvider ?? throw new ArgumentNullException(nameof(dataStoreProvider));
        _authentication = authentication;
        _logger = logger ?? NullLogger<EtlGatewayService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<T>> Execute<T>(IScriptedDataCommand<T> command, string dataStore, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(dataStore))
        {
            return ScriptRefusal<T>("the data store's name is required");
        }

        var problem = command.FirstProblem();
        if (problem is not null)
        {
            return GenericResult<T>.Failure(EtlGatewayLog.ScriptedCommandInvalid(_logger, dataStore, command.CommandType, problem.Message));
        }

        // Why a role and not the system context: a script is arbitrary text, so it is run on behalf of a person or a run that
        // holds the data engineer role, never on the platform's own authority.
        if (!MayRunScripts())
        {
            return GenericResult<T>.Failure(EtlGatewayLog.RoleRequired(_logger, dataStore, command.CommandType, ScriptRoles.DataEngineer));
        }

        // Why a host store is refused: it holds the configuration, identity and operations the host runs on, and a script is
        // arbitrary text. A script runs against a store the data store configuration holds, never one the host declares.
        var declaration = await HostDeclaration(dataStore, cancellationToken).ConfigureAwait(false);
        if (declaration.IsFailure)
        {
            return GenericResult<T>.Failure(EtlGatewayLog.StoreLookupFailed(_logger, dataStore, command.CommandType, declaration.CurrentMessage));
        }

        if (declaration.Value)
        {
            return GenericResult<T>.Failure(EtlGatewayLog.HostStoreRefused(_logger, dataStore, command.CommandType));
        }

        var scripted = await Resolve(dataStore, cancellationToken).ConfigureAwait(false);
        if (scripted.IsFailure || scripted.Value.Connection is null)
        {
            return scripted.ToNewResult<T>();
        }

        EtlGatewayLog.Running(_logger, command.CommandType, dataStore);
        var stopwatch = Stopwatch.StartNew();
        var result = await scripted.Value.Connection.Execute(command, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        if (result.IsSuccess)
        {
            EtlGatewayLog.Completed(_logger, command.CommandType, dataStore, stopwatch.Elapsed.TotalMilliseconds);
        }
        else
        {
            EtlGatewayLog.Failed(_logger, command.CommandType, dataStore, result.CurrentMessage);
        }

        // A failure the store's connection returned names that connection, so a run that stops on it can say which connection it came from (FDW-4334, as FDW-4279 does in the data gateways).
        return result.IsSuccess ? result : ConnectionFailures.RaisedBy<T>(result, scripted.Value.ConnectionId);
    }

    // The store's connection, wrapped by the script extension this host references for it (a connection that takes scripts itself
    // needs no wrapper). A host that references no extension for that kind of connection finds none and refuses.
    private async Task<IGenericResult<(Guid ConnectionId, IScriptedDataConnection? Connection)>> Resolve(string dataStore, CancellationToken cancellationToken)
    {
        // The store's row alone: ConnectionId is a field of it, and reading it whole would compose everything under it.
        var store = await _dataStores.Get(dataStore, ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (store.IsFailure || store.Value is null)
        {
            return GenericResult<(Guid, IScriptedDataConnection?)>.Failure(EtlGatewayLog.StoreNotResolved(_logger, dataStore, store.NoValueReason().Message));
        }

        if (store.Value.ConnectionId == Guid.Empty)
        {
            return GenericResult<(Guid, IScriptedDataConnection?)>.Failure(EtlGatewayLog.StoreNotResolved(_logger, dataStore, "it names no connection"));
        }

        var connection = await _connections.Get(store.Value.ConnectionId, cancellationToken).ConfigureAwait(false);
        if (connection.IsFailure || connection.Value is null)
        {
            // The connection provider refused or could not give this connection, so it raised the failure (FDW-4334).
            return ConnectionFailures.RaisedBy<(Guid, IScriptedDataConnection?)>(
                ConnectionFailures.Caused<(Guid, IScriptedDataConnection?)>(
                    connection, EtlGatewayLog.StoreNotResolved(_logger, dataStore, connection.NoValueReason().Message)),
                store.Value.ConnectionId);
        }

        var scripted = connection.Value as IScriptedDataConnection;
        if (scripted is null)
            scripted = ScriptedConnectionTypes.All().Select(type => type.Adapt(connection.Value)).FirstOrDefault(adapted => adapted is not null);
        return scripted is null
            ? GenericResult<(Guid, IScriptedDataConnection?)>.Failure(EtlGatewayLog.NotSupported(_logger, dataStore, nameof(IScriptedDataCommand)))
            : GenericResult<(Guid, IScriptedDataConnection?)>.Success((store.Value.ConnectionId, scripted));
    }

    // Whether the host declared the store. A store that does not exist is not the host's (Resolve refuses it next); a lookup that broke
    // says nothing, so it is a failure and the script is refused, never read as "not the host's" (FDW-4281).
    private async Task<IGenericResult<bool>> HostDeclaration(string dataStore, CancellationToken cancellationToken)
    {
        var store = await _dataStoreProvider.GetReference(dataStore, cancellationToken).ConfigureAwait(false);
        if (store.IsFailure)
        {
            return FailureCategory.IsMissing(store)
                ? GenericResult<bool>.Success(false)
                : store.ToNewResult<bool>();
        }

        return store.Value is { } found
            ? GenericResult<bool>.Success(found.DeclaredByHost)
            : GenericResult<bool>.Failure(EtlGatewayLog.StoreNotResolved(_logger, dataStore, "the data store provider answered with no store"));
    }

    private bool MayRunScripts() =>
        _authentication?.Current is { } caller && caller.Roles.Contains(ScriptRoles.DataEngineer, StringComparer.OrdinalIgnoreCase);

    private IGenericResult<T> ScriptRefusal<T>(string problem) =>
        GenericResult<T>.Failure(EtlGatewayLog.ScriptedCommandInvalid(_logger, string.Empty, nameof(IScriptedDataCommand), problem));
}
