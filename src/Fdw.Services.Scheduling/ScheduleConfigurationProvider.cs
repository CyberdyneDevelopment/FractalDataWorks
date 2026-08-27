using System;
using System.Collections.Generic;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Scheduling;

/// <summary>Configuration provider for schedule configurations. Thin wrapper over
/// <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/>.</summary>
public class ScheduleConfigurationProvider : ImplementationConfigurationProviderBase<ScheduleConfiguration, ScheduleConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleConfigurationProvider"/> class.</summary>
    public ScheduleConfigurationProvider(
        ILogger<ScheduleConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "sched")
        : base(logger ?? NullLogger<ScheduleConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }
}
