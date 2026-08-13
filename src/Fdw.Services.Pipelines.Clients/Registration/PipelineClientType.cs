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
/// ServiceTypeOption for the Pipeline API client.
/// </summary>
/// <remarks>
/// Registered with <see cref="ApiClientTypes"/> via a <c>[ModuleInitializer]</c> generated in
/// the entry point project by <c>Fdw.Registration.SourceGenerators</c>.
/// </remarks>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "PipelineClient")]
public sealed class PipelineClientType : ApiClientTypeBase<IPipelineClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineClientType"/> class.
    /// </summary>
    public PipelineClientType() : base("PipelineClient", "Pipeline API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<IPipelineClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<PipelineHttpClient>>() ?? NullLogger<PipelineHttpClient>.Instance;
                return new PipelineHttpClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
