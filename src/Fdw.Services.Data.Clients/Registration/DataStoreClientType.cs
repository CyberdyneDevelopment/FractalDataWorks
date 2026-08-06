using System;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Clients;

/// <summary>
/// ServiceTypeOption for the DataStore API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "DataStoreClient")]
public sealed class DataStoreClientType : ApiClientTypeBase<DataStoreApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreClientType"/> class.
    /// </summary>
    public DataStoreClientType() : base("DataStoreClient", "DataStore API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return builder;
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            builder.Services.AddScoped<DataStoreApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<DataStoreApiClient>>() ?? NullLogger<DataStoreApiClient>.Instance;
                return new DataStoreApiClient(factory.CreateClient(Name), logger);
            });
            return builder;
        });
 }

}
