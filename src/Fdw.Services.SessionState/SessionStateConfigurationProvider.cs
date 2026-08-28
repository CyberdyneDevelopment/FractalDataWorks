using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SessionState.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.SessionState;

/// <summary>
/// Owns all ConfigurationDb gateway access for the SessionState domain.
/// Services in the SessionState domain inject this provider — never IConfigurationGateway directly.
/// </summary>
/// <remarks>
/// SessionState uses upsert semantics (update-if-exists, insert-if-new) rather than the
/// version-on-write pattern used by named configurations. The provider exposes typed methods
/// for each operation so gateway details stay entirely in this class.
/// </remarks>
public class SessionStateConfigurationProvider
{
    private const string DataStoreName = "ConfigurationDb";
    private const string PathName = "settings";
    private const string ContainerName = "SessionState";

    private readonly IConfigurationGateway _gateway;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionStateConfigurationProvider"/> class.
    /// </summary>
    /// <param name="gateway">The configuration gateway.</param>
    /// <param name="logger">Optional logger instance.</param>
    public SessionStateConfigurationProvider(
        IConfigurationGateway gateway,
        ILogger<SessionStateConfigurationProvider>? logger = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _logger = logger ?? NullLogger<SessionStateConfigurationProvider>.Instance;
    }

    /// <summary>
    /// Queries for an existing session state record by user and key.
    /// Returns null if not found (not a failure condition).
    /// </summary>
    /// <param name="userId">The parsed user GUID.</param>
    /// <param name="key">The state key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching record, or null if not found.</returns>
    public virtual async Task<IGenericResult<SessionStateRecord?>> GetRecord(
        Guid userId, string key, CancellationToken cancellationToken = default)
    {
        SessionStateConfigurationProviderLog.GetRecordTrace(_logger, userId, key);

        var command = Query.From<SessionStateRecord>(DataStoreName, PathName, ContainerName)
            .Where(r => r.UserId).Equal(userId)
            .Where(r => r.StateKey).Equal(key)
            .Build();

        var result = await _gateway.Execute<IEnumerable<SessionStateRecord>>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<SessionStateRecord?>.Failure(
                SessionStateConfigurationProviderLog.GetRecordFailed(_logger, userId, key,
                    new InvalidOperationException(result.CurrentMessage)));
        }

        return GenericResult<SessionStateRecord?>.Success(result.Value?.FirstOrDefault());
    }

    /// <summary>
    /// Queries all session state keys for a user.
    /// </summary>
    /// <param name="userId">The parsed user GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All records for the user (caller extracts keys).</returns>
    public virtual async Task<IGenericResult<IReadOnlyList<SessionStateRecord>>> GetAllRecords(
        Guid userId, CancellationToken cancellationToken = default)
    {
        SessionStateConfigurationProviderLog.GetKeysTrace(_logger, userId);

        var command = Query.From<SessionStateRecord>(DataStoreName, PathName, ContainerName)
            .Where(r => r.UserId).Equal(userId)
            .Build();

        var result = await _gateway.Execute<IEnumerable<SessionStateRecord>>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<IReadOnlyList<SessionStateRecord>>.Failure(
                SessionStateConfigurationProviderLog.GetKeysFailed(_logger, userId,
                    new InvalidOperationException(result.CurrentMessage)));
        }

        return GenericResult<IReadOnlyList<SessionStateRecord>>.Success(
            result.Value?.ToList() ?? []);
    }

    /// <summary>
    /// Inserts a new session state record.
    /// </summary>
    /// <param name="record">The record to insert (Id must be set by caller).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or a failure result.</returns>
    public virtual async Task<IGenericResult> Insert(
        SessionStateRecord record, CancellationToken cancellationToken = default)
    {
        SessionStateConfigurationProviderLog.UpsertTrace(_logger, record.UserId, record.StateKey);

        var command = CmdBuilders.Insert.Into<SessionStateRecord>(ContainerName)
            .DataStore(DataStoreName).Path(PathName)
            .Value(record);

        var result = await _gateway.Execute<int>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult.Failure(
                SessionStateConfigurationProviderLog.InsertFailed(_logger, record.UserId, record.StateKey,
                    new InvalidOperationException(result.CurrentMessage)));
        }

        SessionStateConfigurationProviderLog.UpsertSaved(_logger, record.UserId, record.StateKey);
        return GenericResult.Success();
    }

    /// <summary>
    /// Updates an existing session state record's value and timestamp.
    /// </summary>
    /// <param name="userId">The user GUID to match.</param>
    /// <param name="key">The state key to match.</param>
    /// <param name="update">The partial record containing the updated fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or a failure result.</returns>
    public virtual async Task<IGenericResult> Update(
        Guid userId, string key, SessionStateUpdateRecord update,
        CancellationToken cancellationToken = default)
    {
        SessionStateConfigurationProviderLog.UpsertTrace(_logger, userId, key);

        var command = CmdBuilders.Update.In<SessionStateUpdateRecord>(ContainerName)
            .DataStore(DataStoreName).Path(PathName)
            .Where(nameof(SessionStateRecord.UserId), userId)
            .Where(nameof(SessionStateRecord.StateKey), key)
            .Value(update);

        var result = await _gateway.Execute<int>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult.Failure(
                SessionStateConfigurationProviderLog.UpdateFailed(_logger, userId, key,
                    new InvalidOperationException(result.CurrentMessage)));
        }

        SessionStateConfigurationProviderLog.UpsertSaved(_logger, userId, key);
        return GenericResult.Success();
    }

    /// <summary>
    /// Deletes a single session state record by user and key.
    /// </summary>
    /// <param name="userId">The user GUID to match.</param>
    /// <param name="key">The state key to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or a failure result.</returns>
    public virtual async Task<IGenericResult> Delete(
        Guid userId, string key, CancellationToken cancellationToken = default)
    {
        SessionStateConfigurationProviderLog.DeleteTrace(_logger, userId, key);

        var command = CmdBuilders.Delete.From(ContainerName)
            .DataStore(DataStoreName).Path(PathName)
            .Where(nameof(SessionStateRecord.UserId), userId)
            .Where(nameof(SessionStateRecord.StateKey), key)
            .Build();

        var result = await _gateway.Execute<int>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult.Failure(
                SessionStateConfigurationProviderLog.DeleteFailed(_logger, userId, key,
                    new InvalidOperationException(result.CurrentMessage)));
        }

        SessionStateConfigurationProviderLog.DeleteDone(_logger, userId, key);
        return GenericResult.Success();
    }

    /// <summary>
    /// Deletes all session state records for a user.
    /// </summary>
    /// <param name="userId">The user GUID whose state to clear.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or a failure result.</returns>
    public virtual async Task<IGenericResult> DeleteAll(
        Guid userId, CancellationToken cancellationToken = default)
    {
        SessionStateConfigurationProviderLog.ClearAllTrace(_logger, userId);

        var command = CmdBuilders.Delete.From(ContainerName)
            .DataStore(DataStoreName).Path(PathName)
            .Where(nameof(SessionStateRecord.UserId), userId)
            .Build();

        var result = await _gateway.Execute<int>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult.Failure(
                SessionStateConfigurationProviderLog.ClearAllFailed(_logger, userId,
                    new InvalidOperationException(result.CurrentMessage)));
        }

        SessionStateConfigurationProviderLog.ClearAllDone(_logger, userId);
        return GenericResult.Success();
    }
}
