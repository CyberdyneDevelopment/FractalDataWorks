using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling;

/// <summary>Supplies the Schedule configuration.</summary>
public sealed class ScheduleConfigurationProvider
    : DomainConfigurationProviderBase<IScheduleImplementationConfiguration>,
      IScheduleConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public ScheduleConfigurationProvider(
        ILogger<ScheduleConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "sched", "Schedule")
    {
    }
}
