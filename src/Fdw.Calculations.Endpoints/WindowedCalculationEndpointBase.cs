using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Calculations;
using Fdw.Services.Calculations.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Base endpoint for executing a windowed calculation.
/// Route: POST /calculations/windowed
/// </summary>
public abstract class WindowedCalculationEndpointBase : Endpoint<WindowedCalculationRequest, WindowedCalculationResponse>
{
    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/calculations/windowed");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("calculations:execute");
#endif
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (summary, tags, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(WindowedCalculationRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        if (string.IsNullOrWhiteSpace(req.WindowFunction))
        {
            CalculationEndpointLog.ValidationFailed(EndpointLogger, "WindowFunction is required");
            AddError("WindowFunction is required");
            await Send.ErrorsAsync(cancellation: ct).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.TargetField))
        {
            CalculationEndpointLog.ValidationFailed(EndpointLogger, "TargetField is required");
            AddError("TargetField is required");
            await Send.ErrorsAsync(cancellation: ct).ConfigureAwait(false);
            return;
        }

        CalculationEndpointLog.PreviewingCalculation(EndpointLogger, $"Windowed:{req.WindowFunction}");

        var spec = new WindowedCalculationSpec
        {
            PartitionByFields = req.PartitionByFields,
            OrderByFields = req.OrderByFields.Select(f => new WindowOrderField
            {
                FieldName = f.FieldName,
                Descending = f.Descending
            }).ToList(),
            TargetField = req.TargetField,
            WindowFunction = req.WindowFunction,
            OutputFieldName = req.OutputFieldName
        };

        var stopwatch = Stopwatch.StartNew();

        var result = await ExecuteWindowed(spec, req, ct).ConfigureAwait(false);

        stopwatch.Stop();

        if (result is null)
        {
            CalculationEndpointLog.ValidationFailed(EndpointLogger, "Windowed calculation returned null");
            AddError("Windowed calculation failed");
            await Send.ErrorsAsync(cancellation: ct).ConfigureAwait(false);
            return;
        }

        result.DurationMs = stopwatch.ElapsedMilliseconds;
        CalculationEndpointLog.CalculationPreviewCompleted(EndpointLogger, $"Windowed:{req.WindowFunction}");

        await Send.OkAsync(result, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the windowed calculation. Override in concrete endpoints to provide the actual implementation.
    /// </summary>
    protected abstract Task<WindowedCalculationResponse?> ExecuteWindowed(
        WindowedCalculationSpec spec,
        WindowedCalculationRequest request,
        CancellationToken cancellationToken);
}
