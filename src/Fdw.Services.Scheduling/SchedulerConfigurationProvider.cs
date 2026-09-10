using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling;

/// <summary>Supplies the Scheduler domain configuration.</summary>
public sealed class SchedulerConfigurationProvider
    : DomainConfigurationProviderBase<ISchedulerImplementationConfiguration>,
      ISchedulerConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="SchedulerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public SchedulerConfigurationProvider(
        ILogger<SchedulerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "sched", "Scheduler")
    {
    }
}
