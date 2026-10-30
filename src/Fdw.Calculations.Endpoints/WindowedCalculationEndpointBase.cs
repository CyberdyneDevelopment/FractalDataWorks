using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Calculations;
using Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Base endpoint for executing a windowed calculation.
/// Route: POST /calculations/windowed
/// </summary>
public abstract class WindowedCalculationEndpointBase : Endpoint<WindowedCalculationRequest, WindowedCalculationResponse>
{
    /// <summary>Initializes a new instance of the <see cref="WindowedCalculationEndpointBase"/> class.</summary>
    protected WindowedCalculationEndpointBase(ILogger logger)
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
        
        var missing = MissingField(req);
        if (missing is not null)
        {
            CalculationEndpointLog.ValidationFailed(EndpointLogger, $"{missing} is required");
            AddError($"{missing} is required");
            await Send.ErrorsAsync(cancellation: ct).ConfigureAwait(false);
            return;
        }

        if (WindowedCalculationTypes.ByName(req.WindowFunction).Id == 0)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(
                GenericResult.Failure(CalculationEndpointLog.WindowFunctionNotOffered(
                    EndpointLogger, req.WindowFunction, WindowedCalculationTypes.OfferedNames)),
                HttpContext)).ConfigureAwait(false);
            return;
        }

        CalculationEndpointLog.PreviewingCalculation(EndpointLogger, $"Windowed:{req.WindowFunction}");

        var stopwatch = Stopwatch.StartNew();

        var executed = await ExecuteWindowed(ToSpec(req), req, ct).ConfigureAwait(false);

        stopwatch.Stop();

        if (!executed.IsSuccess || executed.Value is not { } result)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(
                executed.ToNewResult<WindowedCalculationResponse>(), HttpContext)).ConfigureAwait(false);
            return;
        }

        result.DurationMs = stopwatch.ElapsedMilliseconds;
        CalculationEndpointLog.CalculationPreviewCompleted(EndpointLogger, $"Windowed:{req.WindowFunction}");

        await Send.OkAsync(result, ct).ConfigureAwait(false);
    }

    private static string? MissingField(WindowedCalculationRequest req)
        => string.IsNullOrWhiteSpace(req.CalculationName) ? nameof(req.CalculationName)
            : string.IsNullOrWhiteSpace(req.WindowFunction) ? nameof(req.WindowFunction)
            : string.IsNullOrWhiteSpace(req.TargetField) ? nameof(req.TargetField)
            : string.IsNullOrWhiteSpace(req.OutputFieldName) ? nameof(req.OutputFieldName)
            : null;

    private static WindowedCalculationSpec ToSpec(WindowedCalculationRequest req) => new()
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

    /// <summary>
    /// Executes the windowed calculation with the spec the request states: the function, target field, partition and order
    /// fields and output field are the request's, and the calculation supplies its inputs. Override in concrete endpoints to
    /// provide the actual implementation. A failed result is answered with its own status and structured error body.
    /// </summary>
    protected abstract Task<IGenericResult<WindowedCalculationResponse>> ExecuteWindowed(
        WindowedCalculationSpec spec,
        WindowedCalculationRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Builds the response for a windowed run from the result JSON the calculation's entity type returned, stating the partition and
    /// row counts that JSON holds so a caller does not have to parse <c>ResultJson</c> for them.
    /// </summary>
    /// <param name="calculationName">The calculation that was executed.</param>
    /// <param name="spec">The window the request asked for.</param>
    /// <param name="resultJson">The JSON the calculation returned, carrying <c>PartitionCount</c>, <c>RowCount</c> and <c>Rows</c>.</param>
    /// <returns>The response, or an internal failure when the JSON does not state both counts; a count is never defaulted.</returns>
    protected IGenericResult<WindowedCalculationResponse> ToResponse(
        string calculationName, WindowedCalculationSpec spec, string resultJson)
    {
        if (!TryReadCounts(resultJson, out var partitionCount, out var rowCount))
            return GenericResult<WindowedCalculationResponse>.Failure(
                CalculationEndpointLog.WindowedResultCountsMissing(EndpointLogger, calculationName));

        return GenericResult<WindowedCalculationResponse>.Success(new WindowedCalculationResponse
        {
            CalculationName = calculationName,
            WindowFunction = spec.WindowFunction,
            ResultField = spec.OutputFieldName,
            PartitionCount = partitionCount,
            RowCount = rowCount,
            ResultJson = resultJson
        });
    }

    private static bool TryReadCounts(string resultJson, out int partitionCount, out int rowCount)
    {
        partitionCount = 0;
        rowCount = 0;
        try
        {
            using var document = JsonDocument.Parse(resultJson);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("PartitionCount", out var partitions) && partitions.TryGetInt32(out partitionCount)
                && document.RootElement.TryGetProperty("RowCount", out var rows) && rows.TryGetInt32(out rowCount);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Builds the parameters of the calculation context a windowed run executes in: the spec the request states, and the most rows
    /// the run may read from a data set input, resolved as <c>/data</c> resolves its cap for the caller's tenant and role.
    /// </summary>
    /// <param name="spec">The spec the request states.</param>
    /// <returns>The parameters, or the failure when the row cap cannot be resolved.</returns>
    protected IGenericResult<IReadOnlyDictionary<string, object?>> ContextParameters(WindowedCalculationSpec spec)
    {
        var rowCap = PaginationQuery.ResolveCalculatedRowCap(HttpContext, EndpointLogger);
        if (rowCap.IsFailure)
            return rowCap.ToNewResult<IReadOnlyDictionary<string, object?>>();

        return GenericResult<IReadOnlyDictionary<string, object?>>.Success(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [WindowedCalculationSpec.ParameterName] = spec,
                [CalculationDataSetRead.RowCapParameterName] = rowCap.Value
            });
    }
}
