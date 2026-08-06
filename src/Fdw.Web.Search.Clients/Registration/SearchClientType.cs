using System;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Fdw.Web.Search.Clients.ApiClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Search.Clients;

/// <summary>
/// ServiceTypeOption for the Search API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "SearchClient")]
public sealed class SearchClientType : ApiClientTypeBase<SearchApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchClientType"/> class.
    /// </summary>
    public SearchClientType() : base("SearchClient", "Search API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return builder;
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            builder.Services.AddScoped<SearchApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<SearchApiClient>>() ?? NullLogger<SearchApiClient>.Instance;
                return new SearchApiClient(factory.CreateClient(Name), logger);
            });
            return builder;
        });
 }

}
