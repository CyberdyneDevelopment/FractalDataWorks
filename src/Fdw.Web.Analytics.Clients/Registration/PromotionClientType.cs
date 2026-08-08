using System;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Web.Analytics.Clients.ApiClients;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Web.Analytics.Clients;

/// <summary>
/// ServiceTypeOption for the Promotion API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "PromotionClient")]
public sealed class PromotionClientType : ApiClientTypeBase<PromotionApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionClientType"/> class.
    /// </summary>
    public PromotionClientType() : base("PromotionClient", "Promotion API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            builder.Services.AddScoped<PromotionApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<PromotionApiClient>>() ?? NullLogger<PromotionApiClient>.Instance;
                return new PromotionApiClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
