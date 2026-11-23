using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Abstractions;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Runs DDL on a connection through the connection's type (FDW-3617), as <see cref="SchemaInformationService"/> runs discovery: the
/// connection is found by name, its type is read off its configuration, and a type that offers DDL execution is handed the live connection.
/// </summary>
/// <remarks>
/// Why nothing is assumed: a connection with no configuration, a type that is not registered and a type that offers no DDL execution are
/// each refused by name, before the connection is opened, and no other kind's way of running DDL stands in for one.
/// </remarks>
public sealed class SchemaDdlService : ISchemaDdlService
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly IConnectionConfigurationProvider _configProvider;
    private readonly ILogger<SchemaDdlService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SchemaDdlService"/>.
    /// </summary>
    /// <param name="connectionProvider">Opens the connection the DDL runs on.</param>
    /// <param name="configProvider">Reads the connection's configuration.</param>
    /// <param name="logger">The logger.</param>
    public SchemaDdlService(
        IConnectionProvider connectionProvider,
        IConnectionConfigurationProvider configProvider,
        ILogger<SchemaDdlService>? logger = null)
    {
        _connectionProvider = connectionProvider;
        _configProvider = configProvider;
        _logger = logger ?? NullLogger<SchemaDdlService>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Execute(
        string connectionName,
        IReadOnlyList<IDdlCommand> commands,
        CancellationToken cancellationToken = default)
    {
        var configured = await _configProvider.Get(connectionName, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, cancellationToken).ConfigureAwait(false);
        if (!configured.IsSuccess || configured.Value is not { } connection)
            return GenericResult.Failure(SchemaInformationLog.ConnectionConfigNotFound(_logger, connectionName));

        var connectionType = ConnectionTypes.ByName(connection.Implementation);
        if (ReferenceEquals(connectionType, ConnectionTypes.NotFound))
            return GenericResult.Failure(SchemaInformationLog.ConnectionTypeUnknown(_logger, connection.Name, connection.Implementation));

        if (connectionType is not IDdlExecution execution)
        {
            return GenericResult.Failure(
                ConnectionsResultCodes.ByName("DdlExecutionNotOffered"),
                ResultDetails.Create("connectionType", connection.Implementation));
        }

        var live = await _connectionProvider.Get(connection.Name, cancellationToken).ConfigureAwait(false);
        if (!live.IsSuccess || live.Value is null)
        {
            return ConnectionFailures.Caused<bool>(
                live,
                live.CurrentMessage is { } buildError
                    ? SchemaInformationLog.ConnectionBuildFailed(_logger, connection.Name, buildError)
                    : SchemaInformationLog.ConnectionBuildFailedNoDetails(_logger, connection.Name));
        }

        return await execution.ExecuteDdl(live.Value, commands, cancellationToken).ConfigureAwait(false);
    }
}
