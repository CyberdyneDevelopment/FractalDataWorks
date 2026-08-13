using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Services.Abstractions.Health.Monitoring;
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
    : HealthMonitorTypeBase<IHealthMonitorService, ILocalHealthMonitorFactory, HealthMonitorConfiguration>
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
        // Why: health monitor configuration is HOST-TOPOLOGY (which implementation this host runs), so
        // it binds from the host's appsettings/environment rather than shared ConfigurationDb rows.
        Configuration(builder =>
        {

            builder.Services.AddOptions<List<HealthMonitorConfiguration>>()
                .BindConfiguration("HealthMonitors");
            builder.Services.AddOptions<HealthMonitorSelectionOptions>()
                .BindConfiguration(HealthMonitorSelectionOptions.SectionName);
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            DefaultHealthMonitorProvider.Register(Name, sp => sp.GetRequiredService<LocalHealthMonitorFactory>());

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
            // Why: RegisterFactory (below) requires the domain config provider to already be registered.
            // Idempotent TryAdd inside — every health monitor option calls it, first registration wins.
            HealthMonitorConfigurationProvider.RegisterDomainConfiguration(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }


}
