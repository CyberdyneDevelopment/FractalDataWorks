using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Calculations.Abstractions.CalculationSources;
using Fdw.Web.Calculations.Clients.Models;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Base endpoint for listing all available calculation types — the unified catalog (codified +
/// configured) surfaced through <see cref="ICalculationCatalogProvider"/>.
/// </summary>
public abstract class ListCalculationTypesEndpointBase : EndpointWithoutRequest<CalculationTypesResponse>
{
    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/calculations/types");
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
    public override async Task HandleAsync(CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        CalculationEndpointLog.ListingCalculationTypes(EndpointLogger);

        var catalog = Resolve<ICalculationCatalogProvider>();
        var result = await catalog.Get(ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            CalculationEndpointLog.ListCalculationTypesFailed(EndpointLogger);
            var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
            HttpContext.Response.StatusCode = statusCode;
            await HttpContext.Response.WriteAsJsonAsync(errorResponse, ct).ConfigureAwait(false);
            return;
        }

        var catalogItems = result.Value ?? [];
        var dtos = new CalculationTypePayload[catalogItems.Count];
        for (var i = 0; i < catalogItems.Count; i++)
        {
            var item = catalogItems[i];
            dtos[i] = new CalculationTypePayload
            {
                Name = item.Name,
                DisplayName = item.DisplayName,
                Description = item.Description,
                CalculationSource = item.CalculationSource,
                CalculationEntityId = item.CalculationEntityId,
                OperatorId = item.OperatorId
            };
        }

        CalculationEndpointLog.ListedCalculationTypes(EndpointLogger, dtos.Length);

        await Send.OkAsync(new CalculationTypesResponse { Types = dtos }, ct).ConfigureAwait(false);
    }
}
