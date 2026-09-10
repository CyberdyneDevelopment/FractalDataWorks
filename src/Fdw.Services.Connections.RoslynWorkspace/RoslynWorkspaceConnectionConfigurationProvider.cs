using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>Supplies the RoslynWorkspaceConnection configuration.</summary>
public sealed class RoslynWorkspaceConnectionConfigurationProvider
    : ImplementationProviderBase<RoslynWorkspaceConnectionConfiguration, IConnectionImplementationConfiguration>,
      IRoslynWorkspaceConnectionConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="RoslynWorkspaceConnectionConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public RoslynWorkspaceConnectionConfigurationProvider(
        ILogger<RoslynWorkspaceConnectionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "conn", "RoslynWorkspaceConnection")
    {
    }
}
