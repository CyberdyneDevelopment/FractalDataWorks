using System;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Web.Analytics.Clients.ApiClients;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Web.Analytics.Clients;

/// <summary>
/// ServiceTypeOption for the Profile API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "ProfileClient")]
public sealed class ProfileClientType : ApiClientTypeBase<ProfileApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileClientType"/> class.
    /// </summary>
    public ProfileClientType() : base("ProfileClient", "Profile API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<ProfileApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<ProfileApiClient>>() ?? NullLogger<ProfileApiClient>.Instance;
                return new ProfileApiClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
