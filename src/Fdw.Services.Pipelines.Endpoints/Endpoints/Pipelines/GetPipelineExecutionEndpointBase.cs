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
using Fdw.Web.RestEndpoints.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Fdw.Services.Pipelines;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Endpoint to get a specific pipeline execution record.
/// </summary>
public abstract class GetPipelineExecutionEndpointBase : Endpoint<GetPipelineExecutionRequest, PipelineExecutionRecord?>
{
    private readonly IDataGateway _dataGateway;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPipelineExecutionEndpointBase"/> class.
    /// </summary>
    protected GetPipelineExecutionEndpointBase(IDataGateway dataGateway)
    {
        _dataGateway = dataGateway;
    }

    /// <summary>Gets the data gateway.</summary>
    protected IDataGateway DataGateway => _dataGateway;

    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/etl/history/{ExecutionId}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get pipeline execution details";
            s.Description = "Returns detailed information about a specific pipeline execution by its execution ID.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GetPipelineExecutionRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        // Fails here as well as in the Registration phase: registration proves it for a real host,
        // and this proves it for anything that reaches the endpoint without having run registration.
        if (string.IsNullOrWhiteSpace(PipelineServiceTypes.OperationalConnection))
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new
                {
                    Error = "The Pipelines operational connection is not configured",
                    Details = "The host must set PipelineServiceTypes.OperationalConnection before this endpoint can read execution history."
                }, ct).ConfigureAwait(false);
            return;
        }

        OnFetchingExecutionRecord(req.ExecutionId);

        var command = new QueryCommand<PipelineExecutionDbRecord>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "Id",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = req.ExecutionId
                }
            }
        };

        var result = await _dataGateway.Execute<IEnumerable<PipelineExecutionDbRecord>>(
            command, new DataStoreTarget(PipelineServiceTypes.OperationalConnection, "etl", "PipelineExecution"), ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            OnExecutionHistoryFetchFailed(result.CurrentMessage ?? "Unknown error");
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to fetch execution record", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        var dbRecord = result.Value?.FirstOrDefault();
        if (dbRecord == null)
        {
            OnExecutionRecordNotFound(req.ExecutionId);
            await HttpContext.WriteNotFound("PipelineExecution", req.ExecutionId.ToString(), ct).ConfigureAwait(false);
            return;
        }

        var record = MapToExecutionRecord(dbRecord);

        await Send.OkAsync(record, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a database record to an execution record DTO.
    /// </summary>
    protected virtual PipelineExecutionRecord MapToExecutionRecord(PipelineExecutionDbRecord dbRecord) => new()
    {
        ExecutionId = dbRecord.Id,
        PipelineName = dbRecord.PipelineName,
        StartedAt = dbRecord.StartedAt,
        CompletedAt = dbRecord.CompletedAt,
        Success = string.Equals(dbRecord.Status, "Succeeded", StringComparison.OrdinalIgnoreCase),
        ErrorMessage = dbRecord.ErrorMessage,
        RecordsExtracted = dbRecord.RecordsExtracted,
        RecordsTransformed = dbRecord.RecordsTransformed,
        RecordsLoaded = dbRecord.RecordsLoaded,
        RecordsFailed = dbRecord.RecordsFailed,
        TotalDurationMs = dbRecord.DurationMs ?? 0,
        ExecutedBy = dbRecord.ExecutedBy
    };

    /// <summary>Called when fetching execution record. Override for custom logging.</summary>
    protected virtual void OnFetchingExecutionRecord(Guid executionId) { }

    /// <summary>Called when execution history fetch fails. Override for custom logging.</summary>
    protected virtual void OnExecutionHistoryFetchFailed(string error) { }

    /// <summary>Called when execution record is not found. Override for custom logging.</summary>
    protected virtual void OnExecutionRecordNotFound(Guid executionId) { }
}
