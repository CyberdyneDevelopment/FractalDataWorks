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
/// ServiceTypeOption for the Pipeline Job API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "PipelineJobClient")]
public sealed class PipelineJobClientType : ApiClientTypeBase<IPipelineJobClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineJobClientType"/> class.
    /// </summary>
    public PipelineJobClientType() : base("PipelineJobClient", "Pipeline Job API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<IPipelineJobClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<PipelineJobHttpClient>>() ?? NullLogger<PipelineJobHttpClient>.Instance;
                return new PipelineJobHttpClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
