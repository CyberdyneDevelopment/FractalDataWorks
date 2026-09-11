using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling;

/// <summary>Supplies the Schedule implementation's own configuration.</summary>
public sealed class ScheduleImplementationConfigurationProvider
    : ImplementationProviderBase<ScheduleImplementationConfiguration, IScheduleImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    public ScheduleImplementationConfigurationProvider(
        ILogger<ScheduleImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "sched", "ScheduleImplementation")
    {
    }
}
