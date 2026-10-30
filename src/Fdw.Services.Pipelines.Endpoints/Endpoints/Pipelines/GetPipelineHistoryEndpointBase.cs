using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Data.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Fdw.Services.Pipelines;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Endpoint to get pipeline execution history for a specific pipeline.
/// </summary>
public abstract class GetPipelineHistoryEndpointBase : Endpoint<GetPipelineStatusRequest, List<PipelineExecutionRecord>>
{
    private const int LatestRunCount = 100;

    private readonly IDataGatewayProvider _dataGateways;

    /// <summary>Initializes a new instance of the <see cref="GetPipelineHistoryEndpointBase"/> class.</summary>
    protected GetPipelineHistoryEndpointBase(ILogger logger, IDataGatewayProvider dataGateways)
    {
        EndpointLogger = logger;
        _dataGateways = dataGateways;
    }


    /// <summary>Gets the data gateway.</summary>
    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one
    // would be a captive dependency. The provider is asked when a call is actually being made.
    protected IDataGateway DataGateway => _dataGateways.ByName("Main");

    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/etl/pipelines/{Name}/history");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get execution history for a pipeline";
            s.Description = "Returns the latest 100 runs of a specific pipeline, newest first, each with its stored status beside success.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GetPipelineStatusRequest req, CancellationToken ct)
    {
        
        // Fails here as well as in the Registration phase: registration proves it for a real host,
        // and this proves it for anything that reaches the endpoint without having run registration.
        if (string.IsNullOrWhiteSpace(PipelineServiceTypes.OperationalConnection))
        {
            await Send.ResultAsync(TypedResults.Json(new
                {
                    Error = "The Pipelines operational connection is not configured",
                    Details = "The host must set PipelineServiceTypes.OperationalConnection before this endpoint can read execution history."
                }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        OnFetchingExecutionHistory(req.Name);

        var rows = await PipelineExecutionHistory.Page(
            DataGateway,
            EndpointLogger,
            new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "PipelineName",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = req.Name
                }
            },
            0,
            LatestRunCount,
            ct).ConfigureAwait(false);

        if (rows.IsFailure)
        {
            OnExecutionHistoryFetchFailed(rows.CurrentMessage);
            var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(rows, HttpContext);
            await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(rows.Value!.Select(MapToExecutionRecord).ToList(), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a database record to an execution record DTO.
    /// </summary>
    protected virtual PipelineExecutionRecord MapToExecutionRecord(PipelineExecutionDbRecord dbRecord) => new()
    {
        ExecutionId = dbRecord.Id,
        PipelineName = dbRecord.PipelineName,
        StartedAt = dbRecord.StartedAt?.UtcDateTime,
        CompletedAt = dbRecord.CompletedAt?.UtcDateTime,
        Status = dbRecord.Status,
        Success = string.Equals(dbRecord.Status, "Succeeded", StringComparison.OrdinalIgnoreCase),
        ErrorMessage = dbRecord.ErrorMessage,
        RecordsExtracted = dbRecord.RecordsExtracted,
        RecordsTransformed = dbRecord.RecordsTransformed,
        RecordsLoaded = dbRecord.RecordsLoaded,
        RecordsFailed = dbRecord.RecordsFailed,
        TotalDurationMs = dbRecord.DurationMs ?? 0,
        ExecutedBy = dbRecord.ExecutedBy
    };

    /// <summary>Called when fetching execution history. Override for custom logging.</summary>
    protected virtual void OnFetchingExecutionHistory(string pipelineName) { }

    /// <summary>Called when execution history fetch fails. Override for custom logging.</summary>
    protected virtual void OnExecutionHistoryFetchFailed(string? error) { }
}
