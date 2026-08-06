using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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
    
                    return builder;
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            DefaultHealthMonitorProvider.RegisterFactory(Name, sp => sp.GetRequiredService<LocalHealthMonitorFactory>());
            builder.Services.TryAddSingleton<LocalHealthMonitorFactory>();
            // Why: RegisterFactory (below) requires the domain config provider to already be registered.
            // Idempotent TryAdd inside — every health monitor option calls it, first registration wins.
            HealthMonitorConfigurationProvider.RegisterDomainConfiguration(builder.Services);
            return builder;
        });

    }


}
