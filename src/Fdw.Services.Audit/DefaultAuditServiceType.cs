using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Audit.Abstractions;
using Fdw.Services.Audit.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Results;

namespace Fdw.Services.Audit;

/// <summary>
/// Default audit service type that registers audit services
/// with the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(AuditServiceTypes), "Default")]
public sealed class DefaultAuditServiceType : AuditServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAuditServiceType"/> class.
    /// </summary>
    public DefaultAuditServiceType()
        : base(
            "Default",
            "Audit:Default",
            "Default Audit Services",
            "Default audit trail service using DataGateway persistence")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddScoped<IAuditService, AuditService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
