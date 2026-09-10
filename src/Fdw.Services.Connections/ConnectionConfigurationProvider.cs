using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections;

/// <summary>Supplies the configured connections.</summary>
public sealed class ConnectionConfigurationProvider
    : DomainConfigurationProviderBase<IConnectionImplementationConfiguration>,
      IConnectionConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ConnectionConfigurationProvider(
        ILogger<ConnectionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "conn", "Connection")
    {
    }
}
