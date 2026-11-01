using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Calculations.Abstractions.CalculationTypeOptions;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.Calculations.Clients.Models;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Extensions;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Base endpoint for executing a calculation on provided values.
/// </summary>
public abstract class ExecuteCalculationEndpointBase : Endpoint<ExecuteCalculationRequest, ExecuteCalculationResponse>
{
    private readonly IDataSetResolver _resolver;

    /// <summary>Initializes a new instance of the <see cref="ExecuteCalculationEndpointBase"/> class.</summary>
    protected ExecuteCalculationEndpointBase(
        ILogger logger,
        IDataSetResolver resolver)
    {
        EndpointLogger = logger;
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/calculations/execute");
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
    public override async Task HandleAsync(ExecuteCalculationRequest req, CancellationToken ct)
    {
        // The name is resolved as /data resolves it, so the name of a view is found as well as the name of a data set.
        if (req.DataSetName.Length > 0)
        {
            var resolved = await _resolver.Resolve(req.DataSetName, ct).ConfigureAwait(false);
            if (resolved.IsFailure)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(
                    resolved.ToNewResult<ExecuteCalculationResponse>(), HttpContext)).ConfigureAwait(false);
                return;
            }

            if (resolved.Value is null)
            {
                await HttpContext.WriteNotFound("DataSet", req.DataSetName, ct).ConfigureAwait(false);
                return;
            }
        }

        IReadOnlyList<decimal> values = req.Values;
        if (values.Count == 0)
        {
            var projected = await ProjectFromDataSet(req, ct).ConfigureAwait(false);
            if (!projected.IsSuccess || projected.Value is not { } read)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(
                    projected.ToNewResult<ExecuteCalculationResponse>(), HttpContext)).ConfigureAwait(false);
                return;
            }

            values = read;
        }

        // Validator guarantees CalculationTypes.ByName(...) returns a real option.
        var calculationType = CalculationTypes.ByName(req.CalculationType);
        var dataSetName = GetDataSetName(req);
        CalculationEndpointLog.ExecutingCalculation(EndpointLogger, dataSetName, req.CalculationType);

        var sw = Stopwatch.StartNew();

        try
        {
            var rows = ToDataRows(values);
            var result = calculationType.Calculate(rows, "value");
            sw.Stop();

            CalculationEndpointLog.CalculationExecuted(EndpointLogger, dataSetName, req.CalculationType, sw.ElapsedMilliseconds);

            var response = new ExecuteCalculationResponse
            {
                CalculationType = req.CalculationType,
                Result = result,
                InputCount = values.Count,
                ExecutedAt = DateTimeOffset.UtcNow
            };

            await OnCalculationExecuted(req, response, ct).ConfigureAwait(false);
            await Send.OkAsync(response, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            sw.Stop();
            CalculationEndpointLog.CalculationFailed(EndpointLogger, ex, dataSetName, req.CalculationType);
            AddError("An internal error occurred during calculation execution.");
            await Send.ErrorsAsync(cancellation: ct).ConfigureAwait(false);
        }
    }

    // The values of a data set's field, read as the caller through the data set row reader, up to the cap /data uses for calculated reads.
    // A row with no value in the field is skipped, as an aggregate skips it; a value that is not a number is refused.
    private async Task<IGenericResult<IReadOnlyList<decimal>>> ProjectFromDataSet(ExecuteCalculationRequest req, CancellationToken ct)
    {
        var rowCap = PaginationQuery.ResolveCalculatedRowCap(HttpContext, EndpointLogger);
        if (rowCap.IsFailure)
            return rowCap.ToNewResult<IReadOnlyList<decimal>>();

        var services = HttpContext.RequestServices;
        if (services.GetService(typeof(IDataSetRowReader)) is not IDataSetRowReader reader
            || services.GetService(typeof(IDataGatewayProvider)) is not IDataGatewayProvider gateways)
            return GenericResult<IReadOnlyList<decimal>>.Failure(CalculationEndpointLog.DataSetReaderUnavailable(EndpointLogger));

        var read = await reader.Read(gateways.ByName("Main"), req.DataSetName, rowCap.Value, ct).ConfigureAwait(false);
        if (!read.IsSuccess || read.Value is not { } rows)
            return read.ToNewResult<IReadOnlyList<decimal>>();

        return FieldValues(rows, req);
    }

    // The numbers in the request's field of the rows. Every row of one read carries the same columns, so the column is found once.
    private IGenericResult<IReadOnlyList<decimal>> FieldValues(IReadOnlyList<Dictionary<string, object?>> rows, ExecuteCalculationRequest req)
    {
        var values = new List<decimal>(rows.Count);
        if (rows.Count == 0)
            return GenericResult<IReadOnlyList<decimal>>.Success(values);

        var column = rows[0].Keys.FirstOrDefault(key => string.Equals(key, req.FieldName, StringComparison.OrdinalIgnoreCase));
        if (column is null)
            return GenericResult<IReadOnlyList<decimal>>.Failure(
                CalculationEndpointLog.FieldNotInDataSet(EndpointLogger, req.FieldName, req.DataSetName));

        foreach (var row in rows)
        {
            if (row[column] is not { } raw)
                continue;

            if (!decimal.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                return GenericResult<IReadOnlyList<decimal>>.Failure(
                    CalculationEndpointLog.FieldValueNotNumber(EndpointLogger, req.FieldName, req.DataSetName));

            values.Add(number);
        }

        return GenericResult<IReadOnlyList<decimal>>.Success(values);
    }

    /// <summary>
    /// Gets the data set name from the request for logging purposes.
    /// Override to extract from custom request properties.
    /// </summary>
    protected virtual string GetDataSetName(ExecuteCalculationRequest req) =>
        req.DataSetName.Length > 0 ? req.DataSetName : "inline";

    /// <summary>
    /// Called after a calculation has been successfully executed.
    /// Override to implement caching, auditing, or other post-processing.
    /// </summary>
    protected virtual Task OnCalculationExecuted(ExecuteCalculationRequest req, ExecuteCalculationResponse response, CancellationToken ct) =>
        Task.CompletedTask;

    private static List<IDataRow> ToDataRows(IReadOnlyList<decimal> values)
    {
        var rows = new List<IDataRow>(values.Count);
        foreach (var v in values)
            rows.Add(DataRow.SingleField("value", v));
        return rows;
    }
}
