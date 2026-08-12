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

namespace Fdw.Services.Authorization.Clients;

/// <summary>
/// ServiceTypeOption for the Role API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "RoleClient")]
public sealed class RoleClientType : ApiClientTypeBase<RoleApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoleClientType"/> class.
    /// </summary>
    public RoleClientType() : base("RoleClient", "Role API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<RoleApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<RoleApiClient>>() ?? NullLogger<RoleApiClient>.Instance;
                return new RoleApiClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
