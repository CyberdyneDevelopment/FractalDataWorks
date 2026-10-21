using Fdw.Services.Configuration;
using Fdw.Services.Connections.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections;

/// <summary>Supplies the ConnectionAssignment configuration (FDW-1581).</summary>
public sealed class ConnectionAssignmentConfigurationProvider
    : DomainConfigurationProviderBase<IConnectionAssignmentImplementationConfiguration>,
      IConnectionAssignmentConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionAssignmentConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public ConnectionAssignmentConfigurationProvider(
        ILogger<ConnectionAssignmentConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
