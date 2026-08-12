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

namespace Fdw.Services.Settings.Clients;

/// <summary>
/// ServiceTypeOption for the Settings API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "SettingsClient")]
public sealed class SettingsClientType : ApiClientTypeBase<SettingsApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsClientType"/> class.
    /// </summary>
    public SettingsClientType() : base("SettingsClient", "Settings API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<SettingsApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<SettingsApiClient>>() ?? NullLogger<SettingsApiClient>.Instance;
                return new SettingsApiClient(factory.CreateClient(Name), logger);
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
 }

}
