using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Abstractions.Health.Monitoring.Logging;
using Fdw.Services.HealthChecks.Monitoring;
using Fdw.Services.Logging;
using Fdw.ServiceTypes.Logging;
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

            // Decorate the domain's own AddScoped<IHealthMonitorProvider> registration -- already in
            // builder.Services by the time this runs, since HealthMonitorTypes' own Registration body
            // sets it up before running the option collect.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(IHealthMonitorProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<IHealthMonitorProvider>(sp =>
            {
                var provider = (IHealthMonitorProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IHttpHealthMonitorProvider>());
                if (!factoryResult.IsSuccess)
                {
                    var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<HttpHealthMonitorClientType>()
                        ?? NullLogger<HttpHealthMonitorClientType>.Instance;
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        logger, nameof(HttpHealthMonitorClientType), Name, nameof(IHttpHealthMonitorProvider), factoryResult.CurrentMessage);
                }

                return provider;
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
