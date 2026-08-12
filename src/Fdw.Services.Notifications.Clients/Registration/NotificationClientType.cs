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

namespace Fdw.Services.Notifications.Clients;

/// <summary>
/// ServiceTypeOption for the Notification API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "NotificationClient")]
public sealed class NotificationClientType : ApiClientTypeBase<NotificationApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationClientType"/> class.
    /// </summary>
    public NotificationClientType() : base("NotificationClient", "Notification API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<NotificationApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<NotificationApiClient>>() ?? NullLogger<NotificationApiClient>.Instance;
                return new NotificationApiClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
