using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Logging;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Services.Abstractions.Health;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Service type for the in-process health monitor ("Local") — used by hosts that ARE the health
/// source (e.g. the API host aggregates its own <c>IHealthCheckable</c> services).
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(HealthMonitorTypes), "Local")]
public sealed class LocalHealthMonitorType
    : HealthMonitorTypeBase<IHealthMonitorService, ILocalHealthMonitorFactory, LocalHealthMonitorConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalHealthMonitorType"/> class.
    /// </summary>
    public LocalHealthMonitorType() : base(
        name: "Local",
        sectionName: "Local",
        displayName: "In-Process Health Monitor",
        description: "Aggregates health from the host's own registered IHealthCheckable services")
    {
        Registration((builder, loggerFactory) =>
        {
            // DI registration only. Handing the implementation provider to the domain provider is
            // Initialization's job, once a container exists to resolve both out of.
            builder.Services.TryAddSingleton<ILocalHealthMonitorFactory>(
                sp => new LocalHealthMonitorFactory(
                    sp.GetRequiredService<IEnumerable<IHealthCheckable>>(),
                    sp,
                    sp.GetService<ILoggerFactory>()));
            builder.Services.TryAddSingleton<ILocalHealthMonitorProvider>(
                sp => new LocalHealthMonitorProvider(sp.GetRequiredService<ILocalHealthMonitorFactory>()));

            ServiceLogger.FactoryRegistrationDeferred(
                loggerFactory?.CreateLogger<LocalHealthMonitorType>()
                    ?? NullLogger<LocalHealthMonitorType>.Instance,
                nameof(LocalHealthMonitorType),
                Name,
                nameof(LocalHealthMonitorFactory));

            builder.Services.AddSingleton<ILocalHealthMonitorConfigurationProvider, LocalHealthMonitorConfigurationProvider>(sp => new LocalHealthMonitorConfigurationProvider(sp.GetRequiredService<ILogger<LocalHealthMonitorConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), HealthMonitorTypes.ConfigurationConnection));

            // Decorate the domain's own AddScoped<IHealthMonitorProvider> registration -- it already
            // exists in builder.Services by the time this runs, since HealthMonitorTypes' own
            // Registration body sets it up before running the option collect. Wrapping its factory
            // (rather than being handed the provider as a parameter) means this runs inside whichever
            // scope actually constructs the instance -- root at startup, a real request's own scope
            // for a request -- using that construction's own IServiceProvider.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(IHealthMonitorProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<IHealthMonitorProvider>(sp =>
            {
                var provider = (IHealthMonitorProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<ILocalHealthMonitorProvider>());
                if (!factoryResult.IsSuccess)
                {
                    var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<LocalHealthMonitorType>()
                        ?? NullLogger<LocalHealthMonitorType>.Instance;
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        logger, nameof(LocalHealthMonitorType), Name, nameof(ILocalHealthMonitorProvider), factoryResult.CurrentMessage);
                }

                return provider;
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // IHealthMonitorConfigurationProvider is Singleton, so root's Initialize call reaches the
        // same instance every later resolution sees -- unlike the domain SERVICE provider above,
        // this one is safe to populate once, here, unmodified.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<IHealthMonitorConfigurationProvider>()
                .Register(Name, services.GetRequiredService<ILocalHealthMonitorConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }
}
