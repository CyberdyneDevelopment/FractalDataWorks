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
using Fdw.Results;

namespace Fdw.Services.SecretManagers.Clients;

/// <summary>
/// Implementation for the Secret Manager API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[Implementation(typeof(ApiClientTypes), "SecretManagerClient")]
public sealed class SecretManagerClientType : ApiClientTypeBase<SecretManagerApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerClientType"/> class.
    /// </summary>
    public SecretManagerClientType() : base("SecretManagerClient", "Secret Manager API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<SecretManagerApiClient>(sp =>
            {
                var logger = sp.GetService<ILogger<SecretManagerApiClient>>() ?? NullLogger<SecretManagerApiClient>.Instance;
                return new SecretManagerApiClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
