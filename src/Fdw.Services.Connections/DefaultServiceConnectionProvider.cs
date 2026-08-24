using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Logging;
using Fdw.Services.Results;
using Fdw.ServiceTypes;

namespace Fdw.Services.Connections;

/// <summary>
/// Default implementation of <see cref="IServiceConnectionProvider"/>.
/// Manages framework-internal connections (e.g., ConfigurationDb) that are pre-registered
/// at bootstrap time rather than loaded from a database-backed configuration source.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="DefaultConnectionProvider"/>, this provider does not use IOptionsMonitor
/// because framework connections are stable for the process lifetime — they are established
/// once during bootstrap before the configuration system is fully initialised.
/// </para>
/// <para>
/// Connections are registered via <see cref="Register(string, IGenericConnection)"/> and
/// cached in a thread-safe <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// Attempting to register the same name twice emits a Warning and is a no-op.
/// </para>
/// </remarks>
public sealed class DefaultServiceConnectionProvider : IServiceConnectionProvider, IDisposable
{
    private readonly ILogger<DefaultServiceConnectionProvider> _logger;
    private readonly ConcurrentDictionary<string, IGenericConnection> _registry =
        new(StringComparer.OrdinalIgnoreCase);
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultServiceConnectionProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public DefaultServiceConnectionProvider(ILogger<DefaultServiceConnectionProvider>? logger = null)
    {
        _logger = logger ?? NullLogger<DefaultServiceConnectionProvider>.Instance;
    }

    /// <summary>
    /// Registers a framework connection under the specified name.
    /// If the name is already registered this call is a no-op (emits a Warning).
    /// </summary>
    /// <param name="name">The logical name for the connection (case-insensitive).</param>
    /// <param name="connection">The pre-created connection instance.</param>
    public void Register(string name, IGenericConnection connection)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        if (connection is null)
            throw new ArgumentNullException(nameof(connection));

        ServiceConnectionProviderLog.Registering(_logger, name);

        if (!_registry.TryAdd(name, connection))
        {
            ServiceConnectionProviderLog.AlreadyRegistered(_logger, name);
            return;
        }

        ServiceConnectionProviderLog.Registered(_logger, name);
    }

    /// <summary>
    /// Gets a service connection by name.
    /// Returns <see cref="IGenericResult{T}"/> Failure if the name is not registered.
    /// </summary>
    /// <param name="name">The connection name (case-insensitive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the connection, or a failure message.</returns>
    public Task<IGenericResult<IGenericConnection>> Get(string name, CancellationToken cancellationToken = default)
    {
        if (_registry.TryGetValue(name, out var connection))
        {
            ServiceConnectionProviderLog.CacheHit(_logger, name);
            return Task.FromResult(GenericResult<IGenericConnection>.Success(connection));
        }

        ServiceConnectionProviderLog.CacheMiss(_logger, name);
        return Task.FromResult(GenericResult<IGenericConnection>.Failure(
            ServiceConnectionProviderLog.ConnectionNotFound(_logger, name)));
    }

    /// <summary>
    /// Gets a service connection by GUID.
    /// Framework connections are name-keyed — GUID lookup is not supported.
    /// Always returns a failure result.
    /// </summary>
    /// <param name="id">The connection GUID (not used for framework connections).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A failure result indicating GUID lookup is not supported.</returns>
    public Task<IGenericResult<IGenericConnection>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GenericResult<IGenericConnection>.Failure(
            ServiceConnectionProviderLog.ConnectionNotFound(_logger, id.ToString())));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<IGenericConnection>>> Get(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IGenericConnection> connections = _registry.Values.ToList();
        return Task.FromResult(GenericResult<IReadOnlyList<IGenericConnection>>.Success(connections));
    }

    /// <summary>
    /// Gets a pre-registered framework connection matching the supplied configuration.
    /// </summary>
    // Why: this provider holds pre-created connections keyed by name — it has no factories,
    // so it cannot BUILD from a configuration. Name lookup in the registry is its only
    // resolution mechanism (in-memory objects, no database read). Unregistered → fail loud.
    public Task<IGenericResult<IGenericConnection>> Get(IGenericConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration is null || string.IsNullOrWhiteSpace(configuration.Name))
        {
            return Task.FromResult(GenericResult<IGenericConnection>.Failure(
                ServiceConnectionProviderLog.ConnectionNotFound(_logger, "(null configuration)")));
        }

        return Get(configuration.Name, cancellationToken);
    }

    /// <inheritdoc />
    async Task<IGenericResult<T>> IPlatformServiceProvider.Get<T>(string name, CancellationToken cancellationToken)
    {
        var result = await Get(name, cancellationToken).ConfigureAwait(false);
        return CastResult<T>(result);
    }

    /// <inheritdoc />
    async Task<IGenericResult<T>> IPlatformServiceProvider.Get<T>(Guid id, CancellationToken cancellationToken)
    {
        var result = await Get(id, cancellationToken).ConfigureAwait(false);
        return CastResult<T>(result);
    }

    /// <inheritdoc />
    async Task<IGenericResult<IReadOnlyList<T>>> IPlatformServiceProvider.Get<T>(CancellationToken cancellationToken)
    {
        var result = await Get(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<IReadOnlyList<T>>();
        var typed = result.Value?.OfType<T>().ToList() ?? [];
        return GenericResult<IReadOnlyList<T>>.Success(typed);
    }

    /// <inheritdoc />
    public void Evict(string name) { }

    /// <inheritdoc />
    public void Evict(Guid id) { }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var entry in _registry)
        {
            if (entry.Value is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
#pragma warning disable CA1031 // Do not catch general exception types — must not throw during disposal
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    // Why: Disposal must not propagate exceptions; log the failure so it is not silently discarded.
                    ServiceConnectionProviderLog.DisposeConnectionFailed(_logger, ex, entry.Key);
                }
            }
        }

        _registry.Clear();
    }

    private static IGenericResult<T> CastResult<T>(IGenericResult<IGenericConnection> result)
    {
        if (!result.IsSuccess)
            return result.ToNewResult<T>();

        if (result.Value is T typed)
            return result.ToNewResult(typed);

        var expectedType = typeof(T).Name;
        var actualType = result.Value?.GetType().Name ?? "null";
        return GenericResult<T>.Failure(
            ServicesResultCodes.ByName("ServiceCastFailed"),
            ResultDetails.Create("ExpectedType", expectedType, "ActualType", actualType));
    }
}
