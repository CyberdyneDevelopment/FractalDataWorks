using System;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Web.Calculations.Clients.ApiClients;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Calculations.Clients;

/// <summary>
/// ServiceTypeOption for the Calculation API client.
/// </summary>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "CalculationClient")]
public sealed class CalculationClientType : ApiClientTypeBase<CalculationApiClient>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationClientType"/> class.
    /// </summary>
    public CalculationClientType() : base("CalculationClient", "Calculation API Client") {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(builder.Configuration, Name);
                    return builder;
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            // Why: register as both concrete and interface so [Inject] ICalculationApiClient resolves
            // in headless Blazor components without requiring them to depend on the concrete type.
            builder.Services.AddScoped<CalculationApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<CalculationApiClient>>() ?? NullLogger<CalculationApiClient>.Instance;
                return new CalculationApiClient(factory.CreateClient(Name), logger);
            });
            builder.Services.AddScoped<ICalculationApiClient>(sp => sp.GetRequiredService<CalculationApiClient>());
            return builder;
        });
 }

}
