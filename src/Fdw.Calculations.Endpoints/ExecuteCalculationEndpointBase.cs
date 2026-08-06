using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Calculations.Abstractions.CalculationTypeOptions;
using Fdw.Calculations.Endpoints.Validators;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Commands.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.Calculations.Clients.Models;
using Fdw.Web.RestEndpoints.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Base endpoint for executing a calculation on provided values.
/// </summary>
public abstract class ExecuteCalculationEndpointBase : Endpoint<ExecuteCalculationRequest, ExecuteCalculationResponse>
{
    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        // Why: shape validation (CalculationType, inline-Values OR DataSetName+FieldName) lives in
        // ExecuteCalculationRequestValidator. DataSet *existence* is a resource-state concern: a
        // typo should surface as 404, not 400, so the lookup stays at the endpoint boundary.
        if (req.DataSetName.Length > 0 && !await DataSetLookup.Exists(Resolve<IConfigurationGateway>(), req.DataSetName, ct).ConfigureAwait(false))
        {
            await HttpContext.WriteNotFound("DataSet", req.DataSetName, ct).ConfigureAwait(false);
            return;
        }

        var values = req.Values.Count > 0
            ? req.Values
            : await ProjectFromDataSet(req.DataSetName, req.FieldName, ct).ConfigureAwait(false);

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

    /// <summary>
    /// Gets the data set name from the request for logging purposes.
    /// Override to extract from custom request properties.
    /// </summary>
    protected virtual string GetDataSetName(ExecuteCalculationRequest req) =>
        req.DataSetName.Length > 0 ? req.DataSetName : "inline";

    private async Task<IReadOnlyList<decimal>> ProjectFromDataSet(string dataSetName, string fieldName, CancellationToken ct)
    {
        if (dataSetName.Length == 0 || fieldName.Length == 0) return Array.Empty<decimal>();

        var gateway = TryResolve<IDataGateway>();
        if (gateway is null) return Array.Empty<decimal>();

        // Why: Addressing moved off IDataCommand onto DataStoreTarget. The DataStore name is not
        // present in ExecuteCalculationRequest — the endpoint must be subclassed to supply it.
        // Passing DataSetName as both DataStore and Container preserves the pre-existing behaviour
        // of querying by logical name only; the gateway will fail if no matching store is configured.
        var cmd = new QueryCommand<Dictionary<string, object?>>();
        var dataResult = await gateway.Execute<IEnumerable<Dictionary<string, object?>>>(
            cmd, new DataStoreTarget(dataSetName, null, dataSetName), ct).ConfigureAwait(false);
        if (!dataResult.IsSuccess || dataResult.Value is null) return Array.Empty<decimal>();

        var projected = new List<decimal>();
        foreach (var row in dataResult.Value)
        {
            if (row.TryGetValue(fieldName, out var raw) && raw is not null
                && decimal.TryParse(raw.ToString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                projected.Add(parsed);
            }
        }
        return projected;
    }

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
