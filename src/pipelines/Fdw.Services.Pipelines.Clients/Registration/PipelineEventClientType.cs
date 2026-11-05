using System;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Clients;

/// <summary>
/// Implementation for the Pipeline Event API client: how a host that serves no hub hands pipeline events to the one that does (FDW-3773).
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[Implementation(typeof(ApiClientTypes), "PipelineEventClient")]
public sealed class PipelineEventClientType : ApiClientTypeBase<IPipelineEventClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineEventClientType"/> class.
    /// </summary>
    public PipelineEventClientType() : base("PipelineEventClient", "Pipeline Event API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<IPipelineEventClient>(sp =>
            {
                var logger = sp.GetService<ILogger<PipelineEventHttpClient>>() ?? NullLogger<PipelineEventHttpClient>.Instance;
                return new PipelineEventHttpClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
