using System;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Supplies the local health monitor's own configuration.
/// </summary>
public sealed class LocalHealthMonitorConfigurationProvider
    : ImplementationConfigurationProvider<
          IHealthMonitorImplementationConfiguration,
          LocalHealthMonitorConfiguration,
          LocalHealthMonitorConfigurationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalHealthMonitorConfigurationProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="lazyGateway">The gateway this implementation's rows are read through.</param>
    /// <param name="dataStoreName">The connection the rows live in.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    public LocalHealthMonitorConfigurationProvider(
        ILogger<LocalHealthMonitorConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ServerConfiguration",
        string pathName = "settings")
        : base(logger ?? NullLogger<LocalHealthMonitorConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName,
               pathName)
    {
    }
}
