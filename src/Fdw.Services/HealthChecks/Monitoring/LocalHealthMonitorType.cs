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

            // Why this line exists at all: the call above writes into a STATIC registry that nothing
            // else narrates. Until the provider drains it — much later, in another scope — a
            // registration that never happened and one that happened and was then discarded are
            // byte-identical in the log, because both are silence. This says which type registered
            // what, for which option, at the moment it happened.
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
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    HealthMonitorTypes.ConfigurationConnection));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Why Initialize: registering into the domain provider needs a live container, and Register
        // runs while it is still being built.
        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<IHealthMonitorConfigurationProvider>()
                .Register(Name, services.GetRequiredService<LocalHealthMonitorConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });

    }


}
