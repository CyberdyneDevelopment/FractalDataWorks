using System;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Clients;

/// <summary>
/// ServiceTypeOption for the Schedule API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "ScheduleClient")]
public sealed class ScheduleClientType : ApiClientTypeBase<IScheduleClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleClientType"/> class.
    /// </summary>
    public ScheduleClientType() : base("ScheduleClient", "Schedule API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<IScheduleClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<ScheduleHttpClient>>() ?? NullLogger<ScheduleHttpClient>.Instance;
                return new ScheduleHttpClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
