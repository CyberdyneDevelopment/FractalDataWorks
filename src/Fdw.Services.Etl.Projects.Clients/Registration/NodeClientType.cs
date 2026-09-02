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

namespace Fdw.Services.Etl.Projects.Clients.Registration;

/// <summary>
/// ServiceTypeOption for the Node API client.
/// </summary>
[ServiceTypeOption(typeof(ApiClientTypes), "NodeClient")]
public sealed class NodeClientType : ApiClientTypeBase<NodeApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeClientType"/> class.
    /// </summary>
    public NodeClientType() : base("NodeClient", "Node API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<NodeApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<NodeApiClient>>() ?? NullLogger<NodeApiClient>.Instance;
                return new NodeApiClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
