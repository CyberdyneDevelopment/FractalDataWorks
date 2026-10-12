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
/// API client for audit trail endpoints.
/// </summary>
public class AuditApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditApiClient"/> class.
    /// </summary>
    public AuditApiClient(HttpClient httpClient, ILogger<AuditApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets audit records with optional filtering.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of audit records.</returns>
    public Task<IGenericResult<IReadOnlyList<AuditRecordPayload>>> GetAuditRecords(AuditFilterRequest filter, CancellationToken ct = default)
    {
        var path = $"audit?skip={filter.Skip}&take={filter.Take}";
        if (!string.IsNullOrEmpty(filter.EntityType))
        {
            path += $"&entityType={Uri.EscapeDataString(filter.EntityType)}";
        }

        if (!string.IsNullOrEmpty(filter.UserName))
        {
            path += $"&userName={Uri.EscapeDataString(filter.UserName)}";
        }

        if (!string.IsNullOrEmpty(filter.Action))
        {
            path += $"&action={Uri.EscapeDataString(filter.Action)}";
        }

        if (filter.From.HasValue)
        {
            path += $"&from={filter.From.Value:O}";
        }

        if (filter.To.HasValue)
        {
            path += $"&to={filter.To.Value:O}";
        }

        return GetList<AuditRecordPayload>(path, ct);
    }
}
