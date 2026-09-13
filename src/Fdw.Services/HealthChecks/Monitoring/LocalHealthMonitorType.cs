using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<IHealthMonitorConfigurationProvider>()
                .Register(Name, services.GetRequiredService<ILocalHealthMonitorConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });

    }

    /// <inheritdoc/>
    /// <remarks>
    /// Why this replaces what used to be here in <c>Initialization</c>: that callback resolved
    /// <see cref="IHealthMonitorProvider"/> from the ROOT container once, at startup, and this
    /// domain provider is registered <c>AddScoped</c> — so root's copy is one instance among many
    /// a real request never sees, and a real request's own instance never had this call reach it.
    /// This method is instead called once per construction, by <c>HealthMonitorTypes</c>'s own
    /// factory, and handed THAT construction's <paramref name="serviceProvider"/> — the same
    /// scope root or a request actually used to build <paramref name="domainProvider"/> itself.
    /// </remarks>
    public override IGenericResult RegisterImplementationProvider(IHealthMonitorProvider domainProvider, IServiceProvider serviceProvider, ILogger logger)
    {
        var factoryResult = domainProvider.Register(Name, () => serviceProvider.GetRequiredService<ILocalHealthMonitorProvider>());
        if (!factoryResult.IsSuccess)
        {
            ServiceTypeLog.OptionFactoryRegistrationFailed(
                logger, nameof(LocalHealthMonitorType), Name, nameof(ILocalHealthMonitorProvider), factoryResult.CurrentMessage);
            return factoryResult;
        }

        ServiceTypeLog.OptionFactoryRegistered(logger, nameof(LocalHealthMonitorType), Name, nameof(ILocalHealthMonitorProvider));
        return factoryResult;
    }
}
