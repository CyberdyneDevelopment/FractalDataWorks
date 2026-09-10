using System;
using System.Collections.Generic;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Scheduling;

/// <summary>
/// Configuration provider for SchedulerConfiguration rows in sched.Scheduler.
/// Reads through IConfigurationGateway — no IConfiguration binding section.
/// </summary>
public class SchedulerConfigurationProvider
    : ImplementationConfigurationProviderBase<SchedulerConfiguration, ISchedulerImplementationConfiguration, SchedulerConfigurationCommand>,
      ISchedulerConfigurationProvider
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;


    /// <summary>Initializes a new instance of the <see cref="SchedulerConfigurationProvider"/> class.</summary>
    public SchedulerConfigurationProvider(
        ILogger<SchedulerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "sched")
        : base(logger ?? NullLogger<SchedulerConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }
}
