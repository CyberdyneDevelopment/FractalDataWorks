namespace Fdw.Schema.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Schema.Clients.Models;
using Fdw.Services.Data.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for schema discovery and data preview endpoints.
/// Implements <see cref="ISchemaProvider"/> with HTTP-backed operations
/// scoped to a connection context.
/// </summary>
public sealed class SchemaApiClient : ApiClientBase, ISchemaProvider
{
    private string? _currentConnection;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaApiClient"/> class.
    /// </summary>
    public SchemaApiClient(HttpClient httpClient, ILogger<SchemaApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc />
    public string? CurrentConnection => _currentConnection;

    /// <inheritdoc />
    public void SetConnection(string connectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);
        _currentConnection = connectionName;
    }

    /// <inheritdoc />
    public Task<IGenericResult<SchemaDiscoveryResponse>> DiscoverSchema(CancellationToken ct = default)
    {
        var connectionName = GetRequiredConnection();
        return Get<SchemaDiscoveryResponse>(
            $"connections/{Uri.EscapeDataString(connectionName)}/schema", ct);
    }

    /// <inheritdoc />
    public Task<IGenericResult<ImportSchemaResponse>> ImportSchema(ImportSchemaRequestPayload request, CancellationToken ct = default)
    {
        var connectionName = GetRequiredConnection();
        return Post<ImportSchemaRequestPayload, ImportSchemaResponse>(
            $"connections/{Uri.EscapeDataString(connectionName)}/import-schema", request, ct);
    }

    /// <inheritdoc />
    public Task<IGenericResult<SyncSchemaResponse>> SyncSchema(bool applyChanges = false, CancellationToken ct = default)
    {
        var connectionName = GetRequiredConnection();
        return Post<SyncSchemaRequest, SyncSchemaResponse>(
            $"connections/{Uri.EscapeDataString(connectionName)}/sync-schema",
            new SyncSchemaRequest { ConnectionName = connectionName, ApplyChanges = applyChanges },
            ct);
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<SchemaCapableConnectionPayload>>> GetCapableConnections(CancellationToken ct = default)
        => GetList<SchemaCapableConnectionPayload>("connections/schema-capable", ct);

    /// <inheritdoc />
    public Task<IGenericResult<DataPreviewResponsePayload>> PreviewData(SchemaPreviewRequest request, CancellationToken ct = default)
        => Post<SchemaPreviewRequest, DataPreviewResponsePayload>("schema/preview", request, ct);

    /// <inheritdoc />
    public Task<IGenericResult<ExecuteDdlResponse>> ExecuteDdl(string ddl, CancellationToken ct = default)
    {
        var connectionName = GetRequiredConnection();
        return Post<ExecuteDdlRequestPayload, ExecuteDdlResponse>(
            $"connections/{Uri.EscapeDataString(connectionName)}/execute-ddl",
            new ExecuteDdlRequestPayload { ConnectionName = connectionName, Ddl = ddl },
            ct);
    }

    /// <summary>
    /// Saves (replaces) field mappings for a DataSet source.
    /// </summary>
    /// <param name="request">The save request containing DataSet name, source name, and mappings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the persisted mapping DTOs.</returns>
    public Task<IGenericResult<List<FieldMappingResponsePayload>>> SaveSourceMappings(
        SaveSourceMappingsPayload request,
        CancellationToken ct = default)
        => Patch<SaveSourceMappingsPayload, List<FieldMappingResponsePayload>>(
            $"datasets/{Uri.EscapeDataString(request.DataSetName)}/sources/{Uri.EscapeDataString(request.SourceName)}/mappings",
            request,
            ct);

    private string GetRequiredConnection()
    {
        if (string.IsNullOrWhiteSpace(_currentConnection))
        {
            throw new InvalidOperationException(
                "No connection has been set. Call SetConnection before invoking connection-scoped operations.");
        }

        return _currentConnection;
    }
}
