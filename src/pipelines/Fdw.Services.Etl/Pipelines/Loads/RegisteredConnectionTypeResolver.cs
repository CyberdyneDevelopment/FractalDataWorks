using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ConnectionTypes = Fdw.Services.Connections.ConnectionTypes;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Finds a connection's type the way the capabilities endpoint does: the connection's configuration names its
/// implementation, and the registered connection types answer for it (FDW-1109).
/// </summary>
public sealed class RegisteredConnectionTypeResolver : IConnectionTypeResolver
{
    private const string Owner = "the load's connection capability check";

    private readonly IConnectionConfigurationProvider? _configurations;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisteredConnectionTypeResolver"/> class.
    /// </summary>
    /// <param name="configurations">The connection configuration provider, or <see langword="null"/> when the pipeline was built without one.</param>
    /// <param name="logger">The logger.</param>
    public RegisteredConnectionTypeResolver(IConnectionConfigurationProvider? configurations, ILogger? logger = null)
    {
        _configurations = configurations;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IConnectionType>> ForConnection(Guid connectionId, CancellationToken cancellationToken = default)
    {
        if (_configurations is null)
            return GenericResult<IConnectionType>.Failure(PipelineRunLog.LoadServiceMissing(_logger, Owner, "connection configuration provider"));

        var configuration = await _configurations.Get(connectionId, cancellationToken).ConfigureAwait(false);
        if (configuration.IsFailure)
            return configuration.ToNewResult<IConnectionType>();

        var type = ConnectionTypes.ByName(configuration.Value!.Implementation);
        return ReferenceEquals(type, ConnectionTypes.NotFound)
            ? GenericResult<IConnectionType>.Failure(PipelineRunLog.LoadTargetUnusable(
                _logger, Owner, configuration.Value.Name, $"its connection type '{configuration.Value.Implementation}' is not registered"))
            : GenericResult<IConnectionType>.Success(type);
    }
}
