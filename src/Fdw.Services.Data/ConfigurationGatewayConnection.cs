using System;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Marker singleton wrapping the <c>Lazy&lt;IDataConnection&gt;</c> that <see cref="ConfigurationGateway"/>
/// uses for its dedicated ConfigurationDb connection. Storage-specific Hosting layers
/// (e.g. <c>Hosting.MsSql</c>) register this with a factory that builds the connection from
/// the typed <c>MsSqlConnectionConfiguration</c> via <c>IMsSqlConnectionFactory</c> — bypassing
/// <c>IDataConnectionProvider</c>/<c>DefaultServiceProvider</c> entirely.
/// </summary>
/// <remarks>
/// Why this marker exists: ConfigurationGateway used to inject <c>IDataConnectionProvider</c> and
/// look the connection up by name at runtime. That triggered <c>DefaultServiceProvider</c> →
/// typed-config <c>Get(Guid id)</c> → gateway.Execute → same connection lookup → re-entrance
/// crash. By having the storage-specific Hosting layer build the connection ahead of time and
/// expose it via this marker, the gateway never touches the provider chain at runtime.
/// </remarks>
public sealed class ConfigurationGatewayConnection
{
    /// <summary>Gets the lazy-evaluated IDataConnection for ConfigurationDb.</summary>
    public Lazy<IDataConnection> Lazy { get; }

    /// <summary>Initializes a new instance of <see cref="ConfigurationGatewayConnection"/>.</summary>
    /// <param name="lazy">Lazy-evaluated IDataConnection.</param>
    public ConfigurationGatewayConnection(Lazy<IDataConnection> lazy)
    {
        Lazy = lazy ?? throw new ArgumentNullException(nameof(lazy));
    }
}
