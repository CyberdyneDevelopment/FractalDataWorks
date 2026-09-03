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

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Endpoint to get pipeline execution history for a specific pipeline.
/// </summary>
public abstract class GetPipelineHistoryEndpointBase : Endpoint<GetPipelineStatusRequest, List<PipelineExecutionRecord>>
{
    private readonly IDataGatewayProvider _dataGateways;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPipelineHistoryEndpointBase"/> class.
    /// </summary>
    protected GetPipelineHistoryEndpointBase(IDataGatewayProvider dataGateways)
    {
        _dataGateways = dataGateways;
    }

    /// <summary>Gets the data gateway.</summary>
    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one
    // would be a captive dependency. The provider is asked when a call is actually being made.
    protected IDataGateway DataGateway => _dataGateways.ByName("Main");

    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

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
            s.Description = "Returns the execution history for a specific pipeline.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GetPipelineStatusRequest req, CancellationToken ct)
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

        OnFetchingExecutionHistory(req.Name);

        var command = new QueryCommand<PipelineExecutionDbRecord>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "PipelineName",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = req.Name
                }
            }
        };

        var result = await DataGateway.Execute<IEnumerable<PipelineExecutionDbRecord>>(
            command, new DataStoreTarget(PipelineServiceTypes.OperationalConnection, "etl", "PipelineExecution"), ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            OnExecutionHistoryFetchFailed(result.CurrentMessage ?? "Unknown error");
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to fetch execution history", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        // Apply sorting and take top 100 in-memory
        var records = result.Value!
            .OrderByDescending(r => r.StartedAt)
            .Take(100)
            .Select(MapToExecutionRecord)
            .ToList();

        await Send.OkAsync(records, ct).ConfigureAwait(false);
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

    /// <summary>Called when fetching execution history. Override for custom logging.</summary>
    protected virtual void OnFetchingExecutionHistory(string pipelineName) { }

    /// <summary>Called when execution history fetch fails. Override for custom logging.</summary>
    protected virtual void OnExecutionHistoryFetchFailed(string error) { }
}
