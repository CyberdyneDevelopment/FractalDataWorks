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
/// <c>ServiceOptionType</c> that row names, and hands the request to the implementation provider
/// registered under it — <c>MsSql</c> to <c>conn.MsSqlConnection</c>, <c>Sqlite</c> to
/// <c>conn.SqliteConnection</c>. What comes back is that implementation's own configuration.
/// </remarks>
public class ConnectionConfigurationProvider
    : ServiceConfigurationProviderBase<
          ConnectionConfiguration,
          IConnectionImplementationConfiguration,
          ConnectionConfigurationCommand>,
      IConnectionConfigurationProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionConfigurationProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="lazyGateway">The gateway this domain's rows are read through.</param>
    /// <param name="dataStoreName">The connection the domain's rows live in.</param>
    /// <param name="pathName">The schema the domain's rows live in.</param>
    public ConnectionConfigurationProvider(
        ILogger<ConnectionConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "PlatformConfiguration",
        string pathName = "conn")
        : base(logger ?? NullLogger<ConnectionConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName,
               pathName)
    {
    }

    /// <inheritdoc />
    protected override ConnectionConfiguration Compose<T>(
        string serviceOptionType,
        string name,
        T implementationConfiguration)
        => new()
        {
            Name = name,
            ServiceOptionType = serviceOptionType,
            Configuration = implementationConfiguration,
        };
}
