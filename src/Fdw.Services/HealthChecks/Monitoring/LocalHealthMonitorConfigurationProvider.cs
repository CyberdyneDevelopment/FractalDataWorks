using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>Supplies the LocalHealthMonitor configuration.</summary>
public sealed class LocalHealthMonitorConfigurationProvider
    : ImplementationProviderBase<LocalHealthMonitorConfiguration, IHealthMonitorImplementationConfiguration>,
      ILocalHealthMonitorConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="LocalHealthMonitorConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public LocalHealthMonitorConfigurationProvider(
        ILogger<LocalHealthMonitorConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "hlth", "LocalHealthMonitor")
    {
    }
}
