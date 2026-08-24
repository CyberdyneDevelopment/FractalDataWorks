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
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IPlatformServiceProvider<IFrameworkSchedulingService, SchedulerConfiguration>>();
            var log = loggerFactory?.CreateLogger<DefaultSchedulerType>()
                ?? NullLogger<DefaultSchedulerType>.Instance;

            var factory = services.GetRequiredService<ISchedulingFactory<IFrameworkSchedulingService, SchedulerConfiguration>>();
            var factoryRegResult = provider.Register(Name, factory);

            var configProvider = services.GetRequiredService<SchedulerConfigurationProvider>();
            var configRegResult = provider.Register(Name, configProvider);

            // Why one combined guard: each of these leaves schedulerProvider.Get("DefaultScheduler")
            // unable to resolve sched.Scheduler rows at runtime, and Initialize has no result to return,
            // so every failure takes the same exit. Separate identical branches would just be noise.
            var parentRegResult = provider.Register(configProvider);
            if (!factoryRegResult.IsSuccess || !configRegResult.IsSuccess || !parentRegResult.IsSuccess)
            {
                // Why this exit is logged at Error and the success path at Trace: the exit returns
                // SUCCESS to the host. Without a line here the scheduler simply does not come up and
                // the host starts as though it had — the failure is knowable only at the first
                // Get("DefaultScheduler"), long after anything can point back to this method. The
                // three results are reported together because they take one exit; the reason names
                // whichever of them actually refused.
                ServiceTypeLog.OptionFactoryRegistrationFailed(
                    log,
                    nameof(DefaultSchedulerType),
                    Name,
                    nameof(DefaultSchedulingFactory),
                    // Why the first FAILING result rather than the first non-null message: a result
                    // that succeeded can still carry a message, so coalescing on messages would
                    // report a success line as the reason the registration failed.
                    (!factoryRegResult.IsSuccess ? factoryRegResult
                        : !configRegResult.IsSuccess ? configRegResult
                        : parentRegResult).CurrentMessage);
                return GenericResult<IHost>.Success(host);
            }

            ServiceTypeLog.OptionFactoryRegistered(log, nameof(DefaultSchedulerType), Name, nameof(DefaultSchedulingFactory));

            return GenericResult<IHost>.Success(host);
        });

        Registration((builder, loggerFactory) =>
        {

            // Why Scoped: the factory requires IDataGateway (scoped) via constructor injection.
            // SchedulerTypes' generated IPlatformServiceProvider<IFrameworkSchedulingService,
            // SchedulerConfiguration> is itself Scoped and resolves this factory via RegisterFactory
            // inside its own per-scope resolver, so a Scoped factory here is lifetime-consistent.
            builder.Services.TryAddScoped<ISchedulingFactory<IFrameworkSchedulingService, SchedulerConfiguration>, DefaultSchedulingFactory>();
            builder.Services.TryAddScoped<DefaultSchedulingFactory>(sp =>
                (DefaultSchedulingFactory)sp.GetRequiredService<ISchedulingFactory<IFrameworkSchedulingService, SchedulerConfiguration>>());

            // Why both providers register here rather than behind a static on each provider: this option
            // is the only consumer of either, so a shared entry point would be a forwarding indirection.
            // The generated Initialize() resolves IServiceConfigurationProvider<T> via GetService to link
            // the parent — a nullable lookup, so a missing registration fails silently rather than loudly.
            builder.Services.TryAddSingleton<SchedulerConfigurationProvider>(sp =>
                new SchedulerConfigurationProvider(
                    sp.GetService<ILogger<SchedulerConfigurationProvider>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
            builder.Services.TryAddSingleton<DefaultConfigurationProvider<SchedulerConfiguration, SchedulerConfigurationCommand>>(
                sp => sp.GetRequiredService<SchedulerConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<SchedulerConfiguration>>(
                sp => sp.GetRequiredService<SchedulerConfigurationProvider>());

            builder.Services.TryAddSingleton<ScheduleConfigurationProvider>(sp =>
                new ScheduleConfigurationProvider(
                    sp.GetService<ILogger<ScheduleConfigurationProvider>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
            builder.Services.TryAddSingleton<DefaultConfigurationProvider<ScheduleConfiguration, ScheduleConfigurationCommand>>(
                sp => sp.GetRequiredService<ScheduleConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<ScheduleConfiguration>>(
                sp => sp.GetRequiredService<ScheduleConfigurationProvider>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
