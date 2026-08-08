using System;
using Fdw.Collections;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Services.ExternalIdentityProviders.Clients;

/// <summary>
/// ServiceTypeOption for the external identity provider login-discovery API client.
/// </summary>
/// <remarks>
/// Registered with NO bearer-token handler: discovery is consumed pre-user-login (no user token exists),
/// and the endpoint is anonymous under DEVELOP. In production the endpoint is gated by
/// <c>identityproviders:read</c> and this client must attach the UI's own service (client-credentials)
/// token — a dedicated outbound service-token handler is the follow-up for that; see the endpoint's
/// ConfigureEndpoint policy seam.
/// </remarks>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "ExternalIdentityProviderClient")]
public sealed class ExternalIdentityProviderClientType : ApiClientTypeBase<ExternalIdentityProviderApiClient>
{
    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProviderClientType"/> class.</summary>
    public ExternalIdentityProviderClientType() : base("ExternalIdentityProviderClient", "External Identity Provider Discovery API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            builder.Services.AddScoped<ExternalIdentityProviderApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
                var logger = sp.GetService<ILogger<ExternalIdentityProviderApiClient>>() ?? NullLogger<ExternalIdentityProviderApiClient>.Instance;
                return new ExternalIdentityProviderApiClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
