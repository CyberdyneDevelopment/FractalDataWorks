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
/// <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/>.</summary>
public class ScheduleConfigurationProvider : DefaultConfigurationProvider<ScheduleConfiguration, ScheduleConfigurationCommand>
{
    /// <summary>
    /// Registers the ScheduleConfigurationProvider and interface forwardings with DI, targeting this
    /// domain's own default location. To override, call <c>SetConfiguration</c> on the resolved singleton.
    /// </summary>
    public static void RegisterDomainConfiguration(IServiceCollection services, IConfiguration configuration)
    {

        services.TryAddSingleton<ScheduleConfigurationProvider>(sp =>
            new ScheduleConfigurationProvider(
                sp.GetService<ILogger<ScheduleConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
        services.TryAddSingleton<DefaultConfigurationProvider<ScheduleConfiguration, ScheduleConfigurationCommand>>(
            sp => sp.GetRequiredService<ScheduleConfigurationProvider>());
        services.TryAddSingleton<IServiceConfigurationProvider<ScheduleConfiguration>>(sp =>
            sp.GetRequiredService<ScheduleConfigurationProvider>());
    }

    /// <summary>Initializes a new instance of the <see cref="ScheduleConfigurationProvider"/> class.</summary>
    public ScheduleConfigurationProvider(
        ILogger<ScheduleConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sched",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<ScheduleConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
