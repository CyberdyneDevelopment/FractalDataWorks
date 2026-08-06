using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Data.Abstractions.Visualization;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Base endpoint for listing all available visualization types.
/// </summary>
public abstract class ListVisualizationTypesEndpointBase : EndpointWithoutRequest<VisualizationTypeListResponse>
{
    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/visualization-types");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("authenticated");
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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        CalculationEndpointLog.ListingCalculationTypes(EndpointLogger);

        var types = VisualizationTypes.All()
            .Where(t => !string.Equals(t.Name, "_Empty", StringComparison.Ordinal))
            .Select(t => new VisualizationTypeItem
            {
                Name = t.Name,
                DisplayName = t.DisplayName,
                Icon = t.Icon
            })
            .ToList();

        return Send.OkAsync(new VisualizationTypeListResponse { Types = types }, ct);
    }
}
