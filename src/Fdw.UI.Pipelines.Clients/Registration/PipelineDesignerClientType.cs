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

namespace Fdw.UI.Pipelines.Clients;

/// <summary>
/// ServiceTypeOption for the Pipeline Designer API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "PipelineDesignerClient")]
public sealed class PipelineDesignerClientType : ApiClientTypeBase<IPipelineDesignerClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineDesignerClientType"/> class.
    /// </summary>
    public PipelineDesignerClientType() : base("PipelineDesignerClient", "Pipeline Designer API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return builder;
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            builder.Services.AddScoped<IPipelineDesignerClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<PipelineDesignerApiClient>>() ?? NullLogger<PipelineDesignerApiClient>.Instance;
                return new PipelineDesignerApiClient(factory.CreateClient(Name), logger);
            });
            return builder;
        });
 }

}
