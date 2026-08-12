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

namespace Fdw.Operations.Clients;

/// <summary>
/// ServiceTypeOption for the Lineage API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "LineageClient")]
public sealed class LineageClientType : ApiClientTypeBase<LineageApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineageClientType"/> class.
    /// </summary>
    public LineageClientType() : base("LineageClient", "Lineage API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<LineageApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<LineageApiClient>>() ?? NullLogger<LineageApiClient>.Instance;
                return new LineageApiClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
