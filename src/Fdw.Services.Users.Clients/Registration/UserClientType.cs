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

namespace Fdw.Services.Users.Clients;

/// <summary>
/// ServiceTypeOption for the User API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "UserClient")]
public sealed class UserClientType : ApiClientTypeBase<UserApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserClientType"/> class.
    /// </summary>
    public UserClientType() : base("UserClient", "User API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return builder;
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            builder.Services.AddScoped<UserApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<UserApiClient>>() ?? NullLogger<UserApiClient>.Instance;
                return new UserApiClient(factory.CreateClient(Name), logger);
            });
            return builder;
        });
 }

}
