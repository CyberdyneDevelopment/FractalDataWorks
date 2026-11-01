namespace Fdw.Schema.Clients;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Schema.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for physical database table management endpoints.
/// </summary>
public sealed class TableApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableApiClient"/> class.
    /// </summary>
    public TableApiClient(HttpClient httpClient, ILogger<TableApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Generates DDL for a new physical table.
    /// </summary>
    /// <param name="request">The table creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the DDL generation results.</returns>
    public Task<IGenericResult<DdlResponse>> GenerateDdl(CreateTableRequest request, CancellationToken ct = default)
        => Post<CreateTableRequest, DdlResponse>(
            $"connections/{Uri.EscapeDataString(request.ConnectionName)}/generate-ddl", request, ct);

    /// <summary>
    /// Executes a DDL script.
    /// </summary>
    /// <param name="request">The DDL execution request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the DDL execution result.</returns>
    public Task<IGenericResult<ExecuteDdlResponse>> ExecuteDdl(ExecuteDdlRequestPayload request, CancellationToken ct = default)
        => Post<ExecuteDdlRequestPayload, ExecuteDdlResponse>(
            $"connections/{Uri.EscapeDataString(request.ConnectionName)}/execute-ddl", request, ct);
}
