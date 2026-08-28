using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Service type for the in-process health monitor ("Local") — used by hosts that ARE the health
/// source (e.g. the API host aggregates its own <c>IHealthCheckable</c> services).
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(HealthMonitorTypes), "Local")]
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
            HealthMonitorProvider.Register(Name, sp => sp.GetRequiredService<LocalHealthMonitorFactory>());

            ServiceLogger.FactoryRegistrationDeferred(
                loggerFactory?.CreateLogger<LocalHealthMonitorType>()
                    ?? NullLogger<LocalHealthMonitorType>.Instance,
                nameof(LocalHealthMonitorType),
                Name,
                nameof(LocalHealthMonitorFactory));

            builder.Services.TryAddSingleton<LocalHealthMonitorFactory>();
            builder.Services.TryAddSingleton<LocalHealthMonitorConfigurationProvider>(sp =>
                new LocalHealthMonitorConfigurationProvider(
                    sp.GetService<ILogger<LocalHealthMonitorConfigurationProvider>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    HealthMonitorTypes.ConfigurationConnection));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<IHealthMonitorConfigurationProvider>()
                .Register(Name, services.GetRequiredService<LocalHealthMonitorConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });

    }


}
