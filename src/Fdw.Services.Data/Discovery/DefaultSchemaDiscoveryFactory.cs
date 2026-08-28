using System;
using System.Collections.Concurrent;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions.Discovery;
using Fdw.Services.Data.Results;

namespace Fdw.Services.Data.Discovery;

/// <summary>
/// Default <see cref="ISchemaDiscoveryFactory"/> backed by a concurrent dictionary
/// keyed on the concrete connection runtime type. Connection-type packages register
/// their adapter via <see cref="Register"/> at startup.
/// </summary>
public sealed class DefaultSchemaDiscoveryFactory : ISchemaDiscoveryFactory
{
    private readonly ConcurrentDictionary<Type, ISchemaDiscoverer> _byConnectionType = new();

    /// <summary>
    /// Registers the schema discoverer for the supplied connection runtime type.
    /// Subsequent registrations for the same type overwrite the previous entry —
    /// idempotent for adapter swaps during DI re-registration.
    /// </summary>
    public void Register<TConnection>(ISchemaDiscoverer discoverer)
        where TConnection : IGenericConnection
    {
        if (discoverer is null) throw new ArgumentNullException(nameof(discoverer));
        _byConnectionType[typeof(TConnection)] = discoverer;
    }

    /// <summary>
    /// Registers a discoverer against an arbitrary connection type at runtime
    /// (for cases where the type isn't statically known to the registrar).
    /// </summary>
    public void Register(Type connectionType, ISchemaDiscoverer discoverer)
    {
        if (connectionType is null) throw new ArgumentNullException(nameof(connectionType));
        if (discoverer is null) throw new ArgumentNullException(nameof(discoverer));
        _byConnectionType[connectionType] = discoverer;
    }

    /// <inheritdoc />
    public IGenericResult<ISchemaDiscoverer> DiscovererFor(IGenericConnection connection)
    {
        if (connection is null)
            return GenericResult<ISchemaDiscoverer>.Failure(
                DataServiceResultCodes.ByName("DiscovererNotFound"));

        // Walk the runtime type hierarchy so derived connection classes resolve
        // against a base-type registration if the derived itself isn't registered.
        for (var t = connection.GetType(); t is not null; t = t.BaseType)
        {
            if (_byConnectionType.TryGetValue(t, out var discoverer))
                return GenericResult<ISchemaDiscoverer>.Success(discoverer);
        }

        // Try interfaces too — supports registration against IDataConnection or similar.
        foreach (var iface in connection.GetType().GetInterfaces())
        {
            if (_byConnectionType.TryGetValue(iface, out var discoverer))
                return GenericResult<ISchemaDiscoverer>.Success(discoverer);
        }

        return GenericResult<ISchemaDiscoverer>.Failure(
            DataServiceResultCodes.ByName("DiscovererNotFound"),
            ResultDetails.Create().With("ConnectionType", connection.GetType().Name));
    }
}
