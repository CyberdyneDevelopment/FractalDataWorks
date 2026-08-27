using System;
using System.Collections.Generic;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
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
// Why: SchedulerConfiguration is loaded from ConfigurationDb at runtime via Lazy<IConfigurationGateway>,
// not through BindConfiguration("Schedulers:..."). The empty IOptionsMonitor passed to the base class
// means the provider's gateway-backed query path is the only source.
public class SchedulerConfigurationProvider : ImplementationConfigurationProviderBase<SchedulerConfiguration, SchedulerConfigurationCommand>
{

    /// <summary>Initializes a new instance of the <see cref="SchedulerConfigurationProvider"/> class.</summary>
    public SchedulerConfigurationProvider(
        ILogger<SchedulerConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sched")
        : base(logger ?? NullLogger<SchedulerConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
    }
}
