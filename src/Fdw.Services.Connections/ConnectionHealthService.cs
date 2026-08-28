using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.Connections;

/// <summary>
/// Default implementation of <see cref="IConnectionHealthService"/> using <see cref="IDataGateway"/>
/// for persistence against <c>ops.ConnectionHealthCheck</c>.
/// </summary>
/// <remarks>
/// Why ConfigurationDb/conn, not OpsDb (FDW-623): health status is co-located with the connection domain
/// so the runtime never needs a second (OpsDb) connection just to record a probe — everything the
/// connection layer reads already comes from ConfigurationDb. The target table conn.ConnectionHealthCheck
/// is a plain, NON-versioned table, so writing a probe result is a single insert that never re-versions
/// the connection aggregate (the version-on-write churn this replaces).
/// </remarks>
public sealed class ConnectionHealthService : IConnectionHealthService
{
    private const string DataStoreName = "PlatformConfiguration";
    private const string PathName = "conn";
    private const string Container = "ConnectionHealthCheck";

    private readonly ILogger<ConnectionHealthService> _logger;
    private readonly IDataGateway _dataGateway;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionHealthService"/> class.
    /// </summary>
    /// <param name="dataGateway">The data gateway for health check history persistence.</param>
    /// <param name="logger">Optional logger; falls back to <see cref="NullLogger{T}"/>.</param>
    public ConnectionHealthService(IDataGateway dataGateway, ILogger<ConnectionHealthService>? logger = null)
    {
        _dataGateway = dataGateway ?? throw new ArgumentNullException(nameof(dataGateway));
        _logger = logger ?? NullLogger<ConnectionHealthService>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> RecordHealthCheck(
        Guid connectionId,
        string connectionName,
        bool isHealthy,
        int? responseTimeMs,
        string? errorMessage,
        CancellationToken cancellationToken = default)
    {
        ConnectionHealthServiceLog.RecordingHealthCheck(_logger, connectionName, connectionId, isHealthy);

        try
        {
            var insertRecord = new ConnectionHealthCheckInsertRecord
            {
                ConnectionId = connectionId,
                ConnectionName = connectionName,
                Status = isHealthy ? "Healthy" : "Unhealthy",
                IsHealthy = isHealthy,
                ResponseTimeMs = responseTimeMs,
                ErrorMessage = errorMessage
            };

            var command = CmdBuilders.Insert.Into<ConnectionHealthCheckInsertRecord>(Container)
                .DataStore(DataStoreName).Path(PathName)
                .Value(insertRecord);

            var insertResult = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!insertResult.IsSuccess)
            {
                ConnectionHealthServiceLog.RecordHealthCheckCommandFailed(_logger, connectionName);
                return insertResult;
            }

            if (isHealthy)
                ConnectionHealthServiceLog.HealthCheckRecorded(_logger, connectionName, isHealthy, responseTimeMs);
            else
                ConnectionHealthServiceLog.HealthCheckRecordedUnhealthy(_logger, connectionName, isHealthy, responseTimeMs);

            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                ConnectionHealthServiceLog.RecordHealthCheckFailed(_logger, connectionName, ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<ConnectionHealthCheckRecord>>> GetHistory(
        Guid connectionId,
        int count = 20,
        CancellationToken cancellationToken = default)
    {
        ConnectionHealthServiceLog.QueryingHistory(_logger, connectionId, count);

        try
        {
            var command = Query.From<ConnectionHealthCheckRecord>(DataStoreName, PathName, Container)
                .Where(r => r.ConnectionId).Equal(connectionId)
                .Build();

            var result = await _dataGateway.Execute<IEnumerable<ConnectionHealthCheckRecord>>(command, cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                ConnectionHealthServiceLog.QueryHistoryCommandFailed(_logger, connectionId);
                return result.ToNewResult<IReadOnlyList<ConnectionHealthCheckRecord>>();
            }

            var history = (result.Value ?? [])
                .OrderByDescending(r => r.CheckedAt)
                .Take(count)
                .ToList();

            ConnectionHealthServiceLog.HistoryRetrieved(_logger, connectionId, history.Count);
            return GenericResult<IReadOnlyList<ConnectionHealthCheckRecord>>.Success(history);
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<ConnectionHealthCheckRecord>>.Failure(
                ConnectionHealthServiceLog.QueryHistoryFailed(_logger, connectionId, ex.Message));
        }
    }

    /// <summary>
    /// Internal record for inserting into ops.ConnectionHealthCheck. Excludes CheckedAt/CheckedBy so
    /// their DB defaults apply.
    /// </summary>
    private sealed class ConnectionHealthCheckInsertRecord
    {
        public Guid ConnectionId { get; set; }
        public string ConnectionName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public int? ResponseTimeMs { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
