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

namespace Fdw.Services.Etl.Projects.Clients.Registration;

/// <summary>
/// ServiceTypeOption for the Project API client.
/// </summary>
[ServiceTypeOption(typeof(ApiClientTypes), "ProjectClient")]
public sealed class ProjectClientType : ApiClientTypeBase<ProjectApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectClientType"/> class.
    /// </summary>
    public ProjectClientType() : base("ProjectClient", "Project API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return builder;
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            builder.Services.AddScoped<ProjectApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<ProjectApiClient>>() ?? NullLogger<ProjectApiClient>.Instance;
                return new ProjectApiClient(factory.CreateClient(Name), logger);
            });
            return builder;
        });
 }

}
