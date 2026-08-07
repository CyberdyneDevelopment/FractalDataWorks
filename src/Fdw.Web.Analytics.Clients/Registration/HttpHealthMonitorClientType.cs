using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Abstractions.Health.Monitoring.Logging;
using Fdw.Services.HealthChecks.Monitoring;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Analytics.Clients;

/// <summary>
/// Service type for the HTTP-proxy health monitor ("HttpClient") — used by hosts that are NOT the
/// health source (e.g. UI hosts) and query the API's health endpoints instead.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(HealthMonitorTypes), "HttpClient")]
public sealed class HttpHealthMonitorClientType
    : HealthMonitorTypeBase<IHealthMonitorService, IHttpHealthMonitorFactory, HealthMonitorConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHealthMonitorClientType"/> class.
    /// </summary>
    public HttpHealthMonitorClientType() : base(
        name: "HttpClient",
        sectionName: "HttpClient",
        displayName: "HTTP Health Monitor Client",
        description: "Queries the API host's health endpoints over HTTP instead of checking locally")
    {
        Configuration(builder =>
        {

            builder.Services.AddOptions<List<HealthMonitorConfiguration>>()
                .BindConfiguration("HealthMonitors");
            builder.Services.AddOptions<HealthMonitorSelectionOptions>()
                .BindConfiguration(HealthMonitorSelectionOptions.SectionName);

            // Why the client name is a literal rather than this option's Name: the registration is keyed by
            // the CLIENT registered here ("HealthMonitorClient"), not by the option's own Name ("HttpClient"),
            // so a host can point health monitoring at a different endpoint from its other API clients.
            builder.Services.AddApiHttpClient(builder.Configuration, "HealthMonitorClient");
    
                    return builder;
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            DefaultHealthMonitorProvider.Register(Name, sp => sp.GetRequiredService<HttpHealthMonitorFactory>());
            builder.Services.TryAddSingleton<HttpHealthMonitorFactory>();
            HealthMonitorConfigurationProvider.RegisterDomainConfiguration(builder.Services);
            return builder;
        });

    }

}
