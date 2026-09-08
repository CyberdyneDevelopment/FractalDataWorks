using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Logging;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Logging;
using Fdw.ServiceTypes;

namespace Fdw.Services.Connections;

/// <summary>
/// Resolves connections by name or id, and exposes them as data connections.
/// </summary>
/// <remarks>
/// The domain configuration provider supplies the implementation configuration; the factory registered
/// for that ServiceOptionType builds the connection. Nothing is cached — the connection wraps a driver
/// that pools underneath, so a cache above it would add a disposal lifecycle and a staleness dance for
/// nothing.
/// </remarks>
public sealed class ConnectionProvider
    : PlatformServiceProviderBase<
          IGenericConnection,
          IConnectionImplementationConfiguration,
          IServiceFactory<IGenericConnection>,
          IConnectionConfigurationProvider>,
      IConnectionProvider,
      IDataConnectionProvider
{
    private readonly ILogger<ConnectionProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionProvider"/> class.
    /// </summary>
    /// <param name="services">The container this provider resolves factories from.</param>
    /// <param name="logger">The logger instance.</param>
    public ConnectionProvider(
        IServiceProvider services,
        ILogger<ConnectionProvider> logger)
        : base(services, logger ?? NullLogger<ConnectionProvider>.Instance)
    {
        _logger = logger ?? NullLogger<ConnectionProvider>.Instance;
    }

    // IDataConnectionProvider — every Get, cast to IDataConnection

    async Task<IGenericResult<IDataConnection>> IDataConnectionProvider.Get(string name, CancellationToken cancellationToken)
        => Cast<IDataConnection>(await Get(name, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IDataConnection>> IDataConnectionProvider.Get(IGenericConfiguration configuration, CancellationToken cancellationToken)
        => Cast<IDataConnection>(await Get(configuration, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IDataConnection>> IDataConnectionProvider.Get(Guid id, CancellationToken cancellationToken)
        => Cast<IDataConnection>(await Get(id, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IReadOnlyList<IDataConnection>>> IDataConnectionProvider.Get(CancellationToken cancellationToken)
    {
        var result = await Get(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<IReadOnlyList<IDataConnection>>();
        var typed = result.Value?.OfType<IDataConnection>().ToList() ?? [];
        return GenericResult<IReadOnlyList<IDataConnection>>.Success(typed);
    }

    async Task<IGenericResult<T>> IDataConnectionProvider.Get<T>(string name, CancellationToken cancellationToken)
        => Cast<T>(await Get(name, cancellationToken).ConfigureAwait(false));
}
