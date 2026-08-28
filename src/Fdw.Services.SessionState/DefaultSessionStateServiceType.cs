using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Collections;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Results;

namespace Fdw.Services.SessionState;

/// <summary>
/// Default session state service type that registers <see cref="ISessionStateService"/>
/// with DataGateway-backed persistence and the Blazor circuit handler.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(SessionStateTypes), "Default")]
public sealed class DefaultSessionStateServiceType : SessionStateServiceTypeBase<IGenericService, ISessionStateServiceFactory>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSessionStateServiceType"/> class.
    /// </summary>
    public DefaultSessionStateServiceType()
        : base(
            "Default",
            "SessionState:Default",
            "Default Session State",
            "Default session state service using DataGateway persistence with Blazor circuit handler")
    {
        Registration((builder, loggerFactory) =>
        {

            builder.Services.TryAddSingleton<SessionStateConfigurationProvider>();
            builder.Services.TryAddScoped<ISessionStateService, SessionStateService>();

            // Only register CircuitHandler for Blazor Server hosts.
            // API hosts don't have AuthenticationStateProvider registered,
            // which SessionStateCircuitHandler depends on.
            if (builder.Services.Any(d => d.ServiceType == typeof(AuthenticationStateProvider)))
            {
                builder.Services.AddScoped<CircuitHandler, SessionStateCircuitHandler>();
            }
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
