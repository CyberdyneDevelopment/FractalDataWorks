using System;
using System.Net.Http;
using Fdw.Collections;
// ISchemaProvider is in namespace Fdw.Schema.Clients (same as this file)
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Schema.Clients;

/// <summary>
/// ServiceTypeOption for the Schema API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "SchemaClient")]
public sealed class SchemaClientType : ApiClientTypeBase<SchemaApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaClientType"/> class.
    /// </summary>
    public SchemaClientType() : base("SchemaClient", "Schema API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            // TableWizardProvider [Inject]s ISchemaProvider — without this the schema wizard crashes.
            builder.Services.AddScoped<ISchemaProvider>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<SchemaApiClient>>() ?? NullLogger<SchemaApiClient>.Instance;
                return new SchemaApiClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
