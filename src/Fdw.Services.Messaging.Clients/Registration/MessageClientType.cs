using System;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Services.Messaging.Clients;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Messaging.Clients.Registration;

/// <summary>
/// Implementation for the Message API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[Implementation(typeof(ApiClientTypes), "MessageClient")]
public sealed class MessageClientType : ApiClientTypeBase<MessageApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageClientType"/> class.
    /// </summary>
    public MessageClientType() : base("MessageClient", "Message API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<MessageApiClient>(sp =>
            {
                var logger = sp.GetService<ILogger<MessageApiClient>>() ?? NullLogger<MessageApiClient>.Instance;
                return new MessageApiClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
