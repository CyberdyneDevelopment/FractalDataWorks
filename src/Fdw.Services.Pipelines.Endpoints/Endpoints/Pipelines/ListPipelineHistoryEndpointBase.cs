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
using Fdw.Web.Endpoints.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Fdw.Services.Pipelines;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Endpoint to list pipeline execution history.
/// </summary>
public abstract class ListPipelineHistoryEndpointBase : Endpoint<ListPipelineHistoryRequest, PaginatedResponse<PipelineExecutionRecord>>
{
    private readonly IDataGatewayProvider _dataGateways;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListPipelineHistoryEndpointBase"/> class.
    /// </summary>
    protected ListPipelineHistoryEndpointBase(IDataGatewayProvider dataGateways)
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
        Get("/etl/history");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "List pipeline execution history";
            s.Description = "Returns a paginated list of pipeline execution history with optional filtering by pipeline name and success status.";
            s.ExampleRequest = new ListPipelineHistoryRequest { Page = 1, PageSize = 25, PipelineName = "NflDataSync" };
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListPipelineHistoryRequest req, CancellationToken ct)
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

        OnFetchingExecutionHistory(req.PipelineName ?? "all pipelines");

        // Build filter conditions
        var conditions = new List<IFilterNode>();

        if (!string.IsNullOrWhiteSpace(req.PipelineName))
        {
            conditions.Add(new FilterCondition
            {
                PropertyName = "PipelineName",
                Operator = FilterOperators.ByName("Equal"),
                Value = req.PipelineName
            });
        }

        if (req.Success.HasValue)
        {
            // Map Success bool to Status string
            var status = req.Success.Value ? "Succeeded" : "Failed";
            conditions.Add(new FilterCondition
            {
                PropertyName = "Status",
                Operator = FilterOperators.ByName("Equal"),
                Value = status
            });
        }

        FilterExpression? filter = null;
        if (conditions.Count == 1)
        {
            filter = new FilterExpression { Root = conditions[0] };
        }
        else if (conditions.Count > 1)
        {
            filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes = conditions
                }
            };
        }

        var command = new QueryCommand<PipelineExecutionDbRecord>
        {
            Filter = filter
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

        var allRecords = result.Value?.ToList() ?? [];
        var totalCount = allRecords.Count;

        // Apply sorting and pagination in-memory
        var sorted = allRecords.OrderByDescending(r => r.StartedAt);
        var items = sorted
            .Skip(req.Skip)
            .Take(req.ValidatedPageSize)
            .Select(MapToExecutionRecord)
            .ToList();

        await Send.OkAsync(new PaginatedResponse<PipelineExecutionRecord>
        {
            Items = items,
            Page = req.ValidatedPage,
            PageSize = req.ValidatedPageSize,
            TotalCount = totalCount
        }, ct).ConfigureAwait(false);
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
