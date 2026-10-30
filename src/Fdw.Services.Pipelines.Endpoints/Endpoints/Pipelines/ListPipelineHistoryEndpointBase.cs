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
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Endpoint to list pipeline execution history.
/// </summary>
public abstract class ListPipelineHistoryEndpointBase : Endpoint<ListPipelineHistoryRequest, PaginatedResponse<PipelineExecutionRecord>>
{
    /// <summary>Initializes a new instance of the <see cref="ListPipelineHistoryEndpointBase"/> class.</summary>
        private readonly IDataGatewayProvider _dataGateways;

    /// <summary>Initializes a new instance of the <see cref="ListPipelineHistoryEndpointBase"/> class.</summary>
    protected ListPipelineHistoryEndpointBase(ILogger logger, IDataGatewayProvider dataGateways)
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
        Get("/etl/history");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "List pipeline execution history";
            s.Description = "Returns a paginated list of pipeline execution history, newest first, with optional filtering by pipeline name and either success (false means the stored status Failed) or status (the stored status of the run: Running, Succeeded, Failed). Each run answers its stored status beside success.";
            s.ExampleRequest = new ListPipelineHistoryRequest { Page = 1, PageSize = 25, PipelineName = "NflDataSync" };
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListPipelineHistoryRequest req, CancellationToken ct)
    {
        var paging = PaginationQuery.ResolvePage(HttpContext, EndpointLogger, req.Page, req.PageSize);
        if (paging.IsFailure)
        {
            await Refuse(paging).ConfigureAwait(false);
            return;
        }

        var page = paging.Value!;

        if (req.Success is { } successAsked && !string.IsNullOrWhiteSpace(req.Status))
        {
            await Refuse(GenericResult.Failure(
                PipelineEndpointLog.HistorySuccessAndStatusBothGiven(EndpointLogger, successAsked, req.Status))).ConfigureAwait(false);
            return;
        }

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

        OnFetchingExecutionHistory(req.PipelineName);

        var filter = HistoryFilter(req);

        var rows = await PipelineExecutionHistory.Page(DataGateway, EndpointLogger, filter, page.Skip, page.Take, ct).ConfigureAwait(false);
        if (rows.IsFailure)
        {
            OnExecutionHistoryFetchFailed(rows.CurrentMessage);
            await Refuse(rows).ConfigureAwait(false);
            return;
        }

        var total = await PipelineExecutionHistory.Count(DataGateway, EndpointLogger, filter, ct).ConfigureAwait(false);
        if (total.IsFailure)
        {
            OnExecutionHistoryFetchFailed(total.CurrentMessage);
            await Refuse(total).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new PaginatedResponse<PipelineExecutionRecord>
        {
            Items = rows.Value!.Select(MapToExecutionRecord).ToList(),
            Page = req.Page,
            PageSize = page.Take,
            TotalCount = total.Value
        }, ct).ConfigureAwait(false);
    }

    // Success is the stored status Succeeded (true) or Failed (false); Status is the stored status as given.
    // The request is refused before this when both are given, so at most one of the two adds a condition.
    private static FilterExpression? HistoryFilter(ListPipelineHistoryRequest req)
    {
        var conditions = new List<IFilterNode>();

        if (!string.IsNullOrWhiteSpace(req.PipelineName))
        {
            conditions.Add(Equal("PipelineName", req.PipelineName));
        }

        if (req.Success is { } success)
        {
            conditions.Add(Equal("Status", success ? "Succeeded" : "Failed"));
        }

        if (!string.IsNullOrWhiteSpace(req.Status))
        {
            conditions.Add(Equal("Status", req.Status));
        }

        return conditions.Count switch
        {
            0 => null,
            1 => new FilterExpression { Root = conditions[0] },
            _ => new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = conditions } }
        };
    }

    private static FilterCondition Equal(string propertyName, string value) => new()
    {
        PropertyName = propertyName,
        Operator = FilterOperators.ByName("Equal"),
        Value = value
    };

    private Task<FastEndpoints.Void> Refuse(IGenericResult failure)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(failure, HttpContext);
        return Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode));
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
    protected virtual void OnFetchingExecutionHistory(string? pipelineName) { }

    /// <summary>Called when execution history fetch fails. Override for custom logging.</summary>
    protected virtual void OnExecutionHistoryFetchFailed(string? error) { }
}
