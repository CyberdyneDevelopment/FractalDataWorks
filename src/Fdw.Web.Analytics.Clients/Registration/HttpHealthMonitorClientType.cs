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
using Fdw.Services.Logging;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.Clients.Abstractions.Registration;

namespace Fdw.Web.Analytics.Clients;

/// <summary>
/// Service type for the HTTP-proxy health monitor ("HttpClient") — used by hosts that are NOT the
/// health source (e.g. UI hosts) and query the API's health endpoints instead.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(HealthMonitorTypes), "HttpClient")]
public sealed class HttpHealthMonitorClientType
    : HealthMonitorTypeBase<IHealthMonitorService, IHttpHealthMonitorFactory, IHealthMonitorImplementationConfiguration>
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

            builder.Services.AddApiHttpClient("HealthMonitorClient");
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {

            ServiceLogger.FactoryRegistrationDeferred(
                loggerFactory?.CreateLogger<HttpHealthMonitorClientType>()
                    ?? NullLogger<HttpHealthMonitorClientType>.Instance,
                nameof(HttpHealthMonitorClientType),
                Name,
                nameof(HttpHealthMonitorFactory));

            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IHttpHealthMonitorFactory), typeof(HttpHealthMonitorFactory), ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IHttpHealthMonitorProvider), typeof(HttpHealthMonitorProvider), ServiceLifetime.Singleton));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Why Initialize: this wiring needs a LIVE container, and Register runs while the container
        // is still being built. This option has no configuration row of its own — it queries the
        // API host's health endpoints over HTTP rather than reading a store — so only the runtime
        // provider needs registering, not a configuration source.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var registered = services.GetRequiredService<IHealthMonitorProvider>()
                .Register(Name, () => services.GetRequiredService<IHttpHealthMonitorProvider>());
            return registered.IsSuccess
                ? GenericResult<IHost>.Success(host)
                : registered.ToNewResult<IHost>();
        });

    }

}
