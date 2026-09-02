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

namespace Fdw.Services.Authentication.Clients;

/// <summary>
/// ServiceTypeOption for the Authentication API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "AuthenticationClient")]
public sealed class AuthenticationClientType : ApiClientTypeBase<AuthenticationApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationClientType"/> class.
    /// </summary>
    public AuthenticationClientType() : base("AuthenticationClient", "Authentication API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<AuthenticationApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<AuthenticationApiClient>>() ?? NullLogger<AuthenticationApiClient>.Instance;
                return new AuthenticationApiClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
