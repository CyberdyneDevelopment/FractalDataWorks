using System;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Operations.Clients;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Operations.Clients.Registration;

/// <summary>
/// ServiceTypeOption for the Notification Preferences API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "NotificationPreferencesClient")]
public sealed class NotificationPreferencesClientType : ApiClientTypeBase<NotificationPreferencesApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationPreferencesClientType"/> class.
    /// </summary>
    public NotificationPreferencesClientType() : base("NotificationPreferencesClient", "Notification Preferences API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<NotificationPreferencesApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<NotificationPreferencesApiClient>>() ?? NullLogger<NotificationPreferencesApiClient>.Instance;
                return new NotificationPreferencesApiClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
