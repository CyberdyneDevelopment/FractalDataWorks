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
        // Why Initialize and not Register: this wiring needs a LIVE container (it resolves the
        // domain provider and its typed-body providers), and Register runs while the container
        // is still being built. Initialize runs after Build() with a real IServiceProvider.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<ISchedulerServiceProvider>();
            var log = loggerFactory?.CreateLogger<DefaultSchedulerType>()
                ?? NullLogger<DefaultSchedulerType>.Instance;

            var factory = services.GetRequiredService<ISchedulingFactory<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>>();
            var factoryRegResult = provider.Register(Name, factory);

            // Why this is guarded: a factory that failed to register leaves
            // schedulerProvider.Get("DefaultScheduler") unable to resolve at runtime, and Initialize
            // has no result to return, so the failure is knowable only much later.
            if (!factoryRegResult.IsSuccess)
            {
                // Why this exit is logged at Error and the success path at Trace: the exit returns
                // SUCCESS to the host. Without a line here the scheduler simply does not come up and
                // the host starts as though it had — the failure is knowable only at the first
                // Get("DefaultScheduler"), long after anything can point back to this method. The
                // reason names what refused.
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

            // Why Scoped: the factory requires IDataGateway (scoped) via constructor injection.
            // SchedulerTypes' generated IPlatformServiceProvider<IFrameworkSchedulingService,
            // inside its own per-scope resolver, so a Scoped factory here is lifetime-consistent.
            builder.Services.TryAddScoped<ISchedulingFactory<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>, DefaultSchedulingFactory>();
            builder.Services.TryAddScoped<DefaultSchedulingFactory>(sp =>
                (DefaultSchedulingFactory)sp.GetRequiredService<ISchedulingFactory<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
