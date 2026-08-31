using System;
using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Abstractions;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Default implementation of <see cref="IHealthMonitorProvider"/>. Resolves the configured
/// <see cref="IHealthMonitorService"/> by row name: loads the <c>settings.HealthMonitor</c> row via
/// the registered <c>IDomainConfigurationProvider</c>, then dispatches to the
/// factory registered for the row's <c>ServiceOptionType</c> ("Local", "HttpClient", …).
/// </summary>
public sealed class HealthMonitorProvider
    : PlatformServiceProviderBase<IHealthMonitorService, IHealthMonitorImplementationConfiguration, IServiceFactory<IHealthMonitorService>, IHealthMonitorConfigurationProvider>,
      IHealthMonitorProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthMonitorProvider"/> class.
    /// </summary>
    /// <param name="services">The container this provider resolves factories from.</param>
    /// <param name="logger">The logger instance (NullLogger fallback per FDW convention).</param>
    public HealthMonitorProvider(
        IServiceProvider services,
        ILogger<HealthMonitorProvider>? logger = null)
        : base(services, logger is null
            ? NullLogger<PlatformServiceProviderBase<IHealthMonitorService, IHealthMonitorImplementationConfiguration, IServiceFactory<IHealthMonitorService>, IHealthMonitorConfigurationProvider>>.Instance
            : logger)
    {
    }
}
