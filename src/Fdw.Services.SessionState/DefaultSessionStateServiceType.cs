using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Results;

namespace Fdw.Services.SessionState;

/// <summary>
/// Default session state service type that registers <see cref="ISessionStateService"/>
/// with DataGateway-backed persistence.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(SessionStateTypes), "Default")]
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
            "Default session state service using DataGateway persistence")
    {
        Registration((builder, loggerFactory) =>
        {

            builder.Services.TryAddSingleton<SessionStateConfigurationProvider>();
            builder.Services.TryAddScoped<ISessionStateService, SessionStateService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
