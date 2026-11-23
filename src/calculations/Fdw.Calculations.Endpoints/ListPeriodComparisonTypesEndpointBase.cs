using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;
using Fdw.Web.Calculations.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Base endpoint for listing all available period comparison types.
/// </summary>
public abstract class ListPeriodComparisonTypesEndpointBase : EndpointWithoutRequest<PeriodComparisonTypesResponse>
{
    /// <summary>Initializes a new instance of the <see cref="ListPeriodComparisonTypesEndpointBase"/> class.</summary>
    protected ListPeriodComparisonTypesEndpointBase(ILogger logger)
    {
        EndpointLogger = logger;
    }

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/calculations/period-comparisons");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("calculations:read");
#endif
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (summary, tags, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override Task HandleAsync(CancellationToken ct)
    {
        
        CalculationEndpointLog.ListingPeriodComparisonTypes(EndpointLogger);

        var types = PeriodComparisonTypes.All();
        var dtos = new PeriodComparisonTypePayload[types.Count];

        int i = 0;
        foreach (var t in types)
        {
            dtos[i++] = new PeriodComparisonTypePayload
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description
            };
        }

        CalculationEndpointLog.ListedPeriodComparisonTypes(EndpointLogger, dtos.Length);

        return Send.OkAsync(new PeriodComparisonTypesResponse { Types = dtos }, ct);
    }
}
