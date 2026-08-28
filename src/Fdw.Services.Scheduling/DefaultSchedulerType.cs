using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Commands;
using Fdw.Services.Scheduling.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Scheduling;

/// <summary>
/// Default <see cref="SchedulerTypes"/> TypeOption. Registers <see cref="DefaultSchedulingFactory"/>
/// and the gateway-backed <see cref="SchedulerConfigurationProvider"/>; no IConfiguration binding.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(SchedulerTypes), "Default")]
public sealed class DefaultSchedulerType
    : SchedulerTypeBase<IFrameworkSchedulingService, ISchedulerImplementationConfiguration, ISchedulingFactory<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>>
{
    /// <summary>Initializes a new instance of the <see cref="DefaultSchedulerType"/> class.</summary>
    public DefaultSchedulerType() : base(
        name: "Default",
        schedulingEngine: "Default",
        jobExecutorType: typeof(DefaultSchedulingService),
        triggerType: typeof(SchedulerConfiguration),
        supportsRecurring: true,
        supportsDelayed: false,
        defaultContainerName: "Scheduler")
    {
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<ISchedulerServiceProvider>();
            var log = loggerFactory?.CreateLogger<DefaultSchedulerType>()
                ?? NullLogger<DefaultSchedulerType>.Instance;

            var factory = services.GetRequiredService<ISchedulingFactory<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>>();
            var factoryRegResult = provider.Register(Name, factory);

            if (!factoryRegResult.IsSuccess)
            {
                ServiceTypeLog.OptionFactoryRegistrationFailed(
                    log,
                    nameof(DefaultSchedulerType),
                    Name,
                    nameof(DefaultSchedulingFactory),
                    factoryRegResult.CurrentMessage);
                return GenericResult<IHost>.Success(host);
            }

            ServiceTypeLog.OptionFactoryRegistered(log, nameof(DefaultSchedulerType), Name, nameof(DefaultSchedulingFactory));

            return GenericResult<IHost>.Success(host);
        });

        Registration((builder, loggerFactory) =>
        {

            builder.Services.TryAddScoped<ISchedulingFactory<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>, DefaultSchedulingFactory>();
            builder.Services.TryAddScoped<DefaultSchedulingFactory>(sp =>
                (DefaultSchedulingFactory)sp.GetRequiredService<ISchedulingFactory<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
