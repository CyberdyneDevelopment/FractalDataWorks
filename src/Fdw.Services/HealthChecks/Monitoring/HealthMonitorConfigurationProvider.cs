using System;
using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// The health-monitor domain's configuration provider.
/// </summary>
/// <remarks>
/// Which monitor a host runs is per-host rather than shared, so this domain reads from the
/// <c>ServerConfiguration</c> connection — the store for boot-time and near-static server values —
/// while domains whose rows are shared across hosts read from <c>PlatformConfiguration</c>. The
/// mechanism is identical either way; only the connection differs.
/// </remarks>
public sealed class HealthMonitorConfigurationProvider
    : ServiceConfigurationProviderBase<
          HealthMonitorConfiguration,
          IHealthMonitorImplementationConfiguration,
          HealthMonitorConfigurationCommand>,
      IHealthMonitorConfigurationProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthMonitorConfigurationProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="lazyGateway">The gateway this domain's rows are read through.</param>
    /// <param name="dataStoreName">The connection the domain's rows live in.</param>
    /// <param name="pathName">The schema the domain's rows live in.</param>
    public HealthMonitorConfigurationProvider(
        ILogger<HealthMonitorConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ServerConfiguration",
        string pathName = "settings")
        : base(logger ?? NullLogger<HealthMonitorConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName,
               pathName)
    {
    }

    /// <inheritdoc />
    protected override HealthMonitorConfiguration Compose<T>(
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
