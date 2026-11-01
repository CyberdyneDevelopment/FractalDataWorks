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

namespace Fdw.Schema.Clients;

/// <summary>
/// Implementation for the Table API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[Implementation(typeof(ApiClientTypes), "TableClient")]
public sealed class TableClientType : ApiClientTypeBase<TableApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableClientType"/> class.
    /// </summary>
    public TableClientType() : base("TableClient", "Table API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<TableApiClient>(sp =>
            {
                var logger = sp.GetService<ILogger<TableApiClient>>() ?? NullLogger<TableApiClient>.Instance;
                return new TableApiClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
