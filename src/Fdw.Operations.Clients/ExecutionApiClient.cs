namespace Fdw.Operations.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.Results;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for execution history and audit trail endpoints.
/// </summary>
public class ExecutionApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionApiClient"/> class.
    /// </summary>
    public ExecutionApiClient(HttpClient httpClient, ILogger<ExecutionApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Lists execution history entries with optional filtering.
    /// </summary>
    /// <param name="itemType">Optional filter by item type (e.g., Pipeline, Schedule).</param>
    /// <param name="state">Optional filter by execution state.</param>
    /// <param name="skip">Number of entries to skip.</param>
    /// <param name="take">Number of entries to take.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of execution summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<ExecutionSummaryPayload>>> List(
        string? itemType = null,
        string? state = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        var url = $"executions?skip={skip}&take={take}";
        if (!string.IsNullOrEmpty(itemType))
        {
            url += $"&itemType={Uri.EscapeDataString(itemType)}";
        }

        if (!string.IsNullOrEmpty(state))
        {
            url += $"&state={Uri.EscapeDataString(state)}";
        }

        return GetList<ExecutionSummaryPayload>(url, ct);
    }

    /// <summary>
    /// Gets a specific execution by ID.
    /// </summary>
    /// <param name="id">The execution ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the execution summary.</returns>
    public virtual Task<IGenericResult<ExecutionSummaryPayload>> Get(Guid id, CancellationToken ct = default)
        => Get<ExecutionSummaryPayload>($"executions/{id:D}", ct);

    /// <summary>
    /// Gets child executions for a given parent execution.
    /// </summary>
    /// <param name="parentId">The parent execution ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of child execution summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<ExecutionSummaryPayload>>> GetChildren(Guid parentId, CancellationToken ct = default)
        => GetList<ExecutionSummaryPayload>($"executions/{parentId:D}/children", ct);
}
