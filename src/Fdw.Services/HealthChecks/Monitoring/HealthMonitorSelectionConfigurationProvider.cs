using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>Reads which health monitor row this host reports to.</summary>
public class HealthMonitorSelectionConfigurationProvider
    : ImplementationConfigurationProviderBase<
          HealthMonitorSelectionConfiguration,
          HealthMonitorSelectionConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="HealthMonitorSelectionConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the store this row lives on.</param>
    /// <param name="dataStoreName">The store the row lives in.</param>
    /// <param name="pathName">The path the row lives under.</param>
    public HealthMonitorSelectionConfigurationProvider(
        ILogger<HealthMonitorSelectionConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "settings")
        : base(logger ?? NullLogger<HealthMonitorSelectionConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
    }
}
