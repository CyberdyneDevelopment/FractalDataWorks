using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling;

/// <summary>
/// Default <see cref="SchedulerTypes"/> TypeOption. Registers <see cref="DefaultSchedulingFactory"/>
/// and the gateway-backed <see cref="SchedulerConfigurationProvider"/>; no IConfiguration binding.
/// </summary>
// Why: SchedulerTypes was an empty [ServiceTypeCollection] before this type existed. The provider had
// no factory or configuration provider to register, so AddSchedulers() effectively wired nothing —
// IFrameworkSchedulingService could not be resolved at runtime. SchedulerConfiguration is read from
// ConfigurationDb (sched.Scheduler) via IConfigurationGateway; nothing binds from IConfiguration.
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(SchedulerTypes), "Default")]
public sealed class DefaultSchedulerType
    : SchedulerTypeBase<IFrameworkSchedulingService, SchedulerConfiguration, ISchedulingFactory<IFrameworkSchedulingService, SchedulerConfiguration>>
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
        // Why Initialize and not Register: this wiring needs a LIVE container (it resolves the
        // domain provider and its typed-body providers), and Register runs while the container
        // is still being built. Initialize runs after Build() with a real IServiceProvider.
        Initialization((services, loggerFactory) =>
        {
            var provider = services.GetRequiredService<IFdwServiceProvider<IFrameworkSchedulingService, SchedulerConfiguration>>();

            var factory = services.GetRequiredService<ISchedulingFactory<IFrameworkSchedulingService, SchedulerConfiguration>>();
            var factoryRegResult = provider.Register(Name, factory);

            var configProvider = services.GetRequiredService<SchedulerConfigurationProvider>();
            var configRegResult = provider.Register(Name, configProvider);

            // Why one combined guard: each of these leaves schedulerProvider.Get("DefaultScheduler")
            // unable to resolve sched.Scheduler rows at runtime, and Initialize has no result to return,
            // so every failure takes the same exit. Separate identical branches would just be noise.
            var parentRegResult = provider.RegisterParentProvider(configProvider);
            if (!factoryRegResult.IsSuccess || !configRegResult.IsSuccess || !parentRegResult.IsSuccess)
            {
                return services;
            }
    
            return services;
        });

        Configuration(builder =>
        {

            ScheduleConfigurationProvider.RegisterDomainConfiguration(builder.Services, builder.Configuration);
    
                    return builder;
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            // Why Scoped: the factory requires IDataGateway (scoped) via constructor injection.
            // SchedulerTypes' generated IFdwServiceProvider<IFrameworkSchedulingService,
            // SchedulerConfiguration> is itself Scoped and resolves this factory via RegisterFactory
            // inside its own per-scope resolver, so a Scoped factory here is lifetime-consistent.
            builder.Services.TryAddScoped<ISchedulingFactory<IFrameworkSchedulingService, SchedulerConfiguration>, DefaultSchedulingFactory>();
            builder.Services.TryAddScoped<DefaultSchedulingFactory>(sp =>
                (DefaultSchedulingFactory)sp.GetRequiredService<ISchedulingFactory<IFrameworkSchedulingService, SchedulerConfiguration>>());

            SchedulerConfigurationProvider.RegisterDomainConfiguration(builder.Services);
            return builder;
        });

    }

}
