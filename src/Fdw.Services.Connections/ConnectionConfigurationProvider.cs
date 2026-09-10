using System;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Commands;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// The connection domain's configuration provider.
/// </summary>
/// <remarks>
/// It reads <c>conn.Connection</c> to find a configured connection by name or id, takes the
/// <c>Implementation</c> that row names, and hands the request to the implementation provider
/// registered under it — <c>MsSql</c> to <c>conn.MsSqlConnection</c>, <c>Sqlite</c> to
/// <c>conn.SqliteConnection</c>. What comes back is that implementation's own configuration.
/// </remarks>
public class ConnectionConfigurationProvider
    : ImplementationConfigurationProviderBase<ConnectionConfiguration, IConnectionImplementationConfiguration, ConnectionConfigurationCommand>,
      IConnectionConfigurationProvider
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionConfigurationProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the named connection.</param>
    /// <param name="dataStoreName">The connection the domain's rows live in.</param>
    /// <param name="pathName">The schema the domain's rows live in.</param>
    public ConnectionConfigurationProvider(
        ILogger<ConnectionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "conn")
        : base(logger ?? NullLogger<ConnectionConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
    }
}
