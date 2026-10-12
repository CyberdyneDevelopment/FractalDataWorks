using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>Supplies the HealthMonitor configuration.</summary>
public sealed class HealthMonitorConfigurationProvider
    : DomainConfigurationProviderBase<IHealthMonitorImplementationConfiguration>,
      IHealthMonitorConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="HealthMonitorConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public HealthMonitorConfigurationProvider(
        ILogger<HealthMonitorConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
