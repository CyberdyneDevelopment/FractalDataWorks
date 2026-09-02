using System;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Web.Analytics.Clients.ApiClients;
using Fdw.Web.Analytics.Clients.Services;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Web.Analytics.Clients;

/// <summary>
/// ServiceTypeOption for the Analytics API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "AnalyticsClient")]
public sealed class AnalyticsClientType : ApiClientTypeBase<AnalyticsApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsClientType"/> class.
    /// </summary>
    public AnalyticsClientType() : base("AnalyticsClient", "Analytics API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<AnalyticsApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<AnalyticsApiClient>>() ?? NullLogger<AnalyticsApiClient>.Instance;
                return new AnalyticsApiClient(factory.CreateClient(Name), logger);
            });

            builder.Services.TryAddSingleton<IAnalyticsService, AnalyticsService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
