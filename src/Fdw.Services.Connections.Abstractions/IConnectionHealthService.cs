using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Service for recording and retrieving connection health check history.
/// Health checks are persisted to the ops.ConnectionHealthCheck table.
/// </summary>
public interface IConnectionHealthService
{
    /// <summary>
    /// Records a health check result for the specified connection.
    /// </summary>
    /// <param name="connectionId">The logical Id of the connection.</param>
    /// <param name="connectionName">The display name of the connection.</param>
    /// <param name="isHealthy">Whether the connection test succeeded.</param>
    /// <param name="responseTimeMs">Optional elapsed time in milliseconds.</param>
    /// <param name="errorMessage">Optional error message when the check failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult> RecordHealthCheck(
        Guid connectionId,
        string connectionName,
        bool isHealthy,
        int? responseTimeMs,
        string? errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves recent health check records for a connection, ordered by most recent first.
    /// </summary>
    /// <param name="connectionId">The logical Id of the connection.</param>
    /// <param name="count">Maximum number of records to return. Defaults to 20.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<ConnectionHealthCheckRecord>>> GetHistory(
        Guid connectionId,
        int count = 20,
        CancellationToken cancellationToken = default);
}
