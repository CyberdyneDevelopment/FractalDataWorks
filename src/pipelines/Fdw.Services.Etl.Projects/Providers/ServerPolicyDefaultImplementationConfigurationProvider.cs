using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Providers;

/// <summary>Reads pipe.ServerPolicyDefaultConfiguration rows for the ServerPolicyDefault domain.</summary>
public sealed class ServerPolicyDefaultImplementationConfigurationProvider
    : ImplementationProviderBase<ServerPolicyDefaultConfiguration, IServerPolicyDefaultImplementationConfiguration>,
      IServerPolicyDefaultImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ServerPolicyDefaultImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayProvider">The configuration gateway provider.</param>
    /// <param name="dataStoreName">The data store holding the implementation table.</param>
    /// <param name="pathName">The path holding the implementation table.</param>
    /// <param name="containerName">The implementation table.</param>
    public ServerPolicyDefaultImplementationConfigurationProvider(
        ILogger<ServerPolicyDefaultImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
