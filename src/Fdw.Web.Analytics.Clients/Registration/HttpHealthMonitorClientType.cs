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
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Called once per HealthMonitorTypes AddScoped construction, with THAT construction's own
        // serviceProvider — so whichever scope actually builds domainProvider (root at startup, a
        // real request's own scope for a request) is the same scope this closure resolves against.
        // This option has no configuration row of its own — it queries the API host's health
        // endpoints over HTTP rather than reading a store — so only the runtime provider needs
        // registering, not a configuration source.
        Registration((serviceProvider, domainProvider, domainConfigurationProvider, logger) =>
        {
            if (domainProvider is null)
                return GenericResult.Success();

            var factoryResult = domainProvider.Register(Name, () => serviceProvider.GetRequiredService<IHttpHealthMonitorProvider>());
            if (!factoryResult.IsSuccess)
            {
                ServiceTypeLog.OptionFactoryRegistrationFailed(
                    logger, nameof(HttpHealthMonitorClientType), Name, nameof(IHttpHealthMonitorProvider), factoryResult.CurrentMessage);
                return factoryResult;
            }

            ServiceTypeLog.OptionFactoryRegistered(logger, nameof(HttpHealthMonitorClientType), Name, nameof(IHttpHealthMonitorProvider));
            return factoryResult;
        });

    }

}
