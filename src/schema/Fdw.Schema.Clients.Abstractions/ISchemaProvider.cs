using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Schema.Clients.Models;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Schema.Clients;

/// <summary>
/// Provides schema operations scoped to a connection context.
/// Call <see cref="SetConnection"/> to establish the target connection
/// before invoking connection-scoped operations.
/// </summary>
public interface ISchemaProvider
{
    /// <summary>
    /// Gets the currently selected connection name, or <c>null</c> if no connection is set.
    /// </summary>
    string? CurrentConnection { get; }

    /// <summary>
    /// Sets the active connection context for subsequent operations.
    /// </summary>
    /// <param name="connectionName">The connection name.</param>
    void SetConnection(string connectionName);

    /// <summary>
    /// Discovers the schema for the current connection.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the schema discovery response.</returns>
    Task<IGenericResult<SchemaDiscoveryResponse>> DiscoverSchema(CancellationToken ct = default);

    /// <summary>
    /// Imports schema from the current connection into DataStore configuration.
    /// </summary>
    /// <param name="request">The import request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the import response.</returns>
    Task<IGenericResult<ImportSchemaResponse>> ImportSchema(ImportSchemaRequestPayload request, CancellationToken ct = default);

    /// <summary>
    /// Synchronizes schema for the current connection, detecting drift.
    /// </summary>
    /// <param name="applyChanges">Whether to apply detected changes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the sync response.</returns>
    Task<IGenericResult<SyncSchemaResponse>> SyncSchema(bool applyChanges = false, CancellationToken ct = default);

    /// <summary>
    /// Gets a list of connections that support schema discovery.
    /// This operation does not require a connection context.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of capable connections.</returns>
    Task<IGenericResult<IReadOnlyList<SchemaCapableConnectionPayload>>> GetCapableConnections(CancellationToken ct = default);

    /// <summary>
    /// Previews data from a table or DataSet.
    /// </summary>
    /// <param name="request">The preview request parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the data preview result.</returns>
    Task<IGenericResult<DataPreviewResponsePayload>> PreviewData(SchemaPreviewRequest request, CancellationToken ct = default);

    /// <summary>
    /// Executes DDL against the current connection.
    /// </summary>
    /// <param name="ddl">The DDL script to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the DDL execution result.</returns>
    Task<IGenericResult<ExecuteDdlResponse>> ExecuteDdl(string ddl, CancellationToken ct = default);
}
