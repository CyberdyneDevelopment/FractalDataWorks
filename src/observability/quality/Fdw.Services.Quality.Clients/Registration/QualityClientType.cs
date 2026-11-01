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

namespace Fdw.Services.Quality.Clients;

/// <summary>
/// Implementation for the Quality API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[Implementation(typeof(ApiClientTypes), "QualityClient")]
public sealed class QualityClientType : ApiClientTypeBase<QualityApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualityClientType"/> class.
    /// </summary>
    public QualityClientType() : base("QualityClient", "Quality API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<QualityApiClient>(sp =>
            {
                var logger = sp.GetService<ILogger<QualityApiClient>>() ?? NullLogger<QualityApiClient>.Instance;
                return new QualityApiClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
