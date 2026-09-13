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

        // Called once per HealthMonitorTypes AddScoped construction, with THAT construction's own
        // serviceProvider — so whichever scope actually builds domainProvider (root at startup, a
        // real request's own scope for a request) is the same scope this closure resolves against.
        Registration((serviceProvider, domainProvider, domainConfigurationProvider, logger) =>
        {
            if (domainConfigurationProvider is not null)
                domainConfigurationProvider.Register(Name, serviceProvider.GetRequiredService<ILocalHealthMonitorConfigurationProvider>());

            if (domainProvider is null)
                return GenericResult.Success();

            var factoryResult = domainProvider.Register(Name, () => serviceProvider.GetRequiredService<ILocalHealthMonitorProvider>());
            if (!factoryResult.IsSuccess)
            {
                ServiceTypeLog.OptionFactoryRegistrationFailed(
                    logger, nameof(LocalHealthMonitorType), Name, nameof(ILocalHealthMonitorProvider), factoryResult.CurrentMessage);
                return factoryResult;
            }

            ServiceTypeLog.OptionFactoryRegistered(logger, nameof(LocalHealthMonitorType), Name, nameof(ILocalHealthMonitorProvider));
            return factoryResult;
        });
    }
}
