using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.SessionState.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SessionState;

/// <summary>
/// Default implementation of <see cref="ISessionStateService"/> using
/// <see cref="SessionStateConfigurationProvider"/> for database-backed session state persistence
/// with JSON serialization.
/// </summary>
/// <remarks>
/// All ConfigurationDb access is delegated to <see cref="SessionStateConfigurationProvider"/>,
/// which is the sole owner of the gateway for the SessionState domain. This service contains
/// only orchestration logic (serialize → upsert, query → deserialize) with no direct gateway calls.
/// </remarks>
public sealed class SessionStateService : ISessionStateService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // Why: SessionStateConfigurationProvider is the domain-owned gateway path for all
    // SessionState data. Services inject the provider, never IConfigurationGateway directly.
    private readonly SessionStateConfigurationProvider _provider;
    private readonly ILogger<SessionStateService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionStateService"/> class.
    /// </summary>
    /// <param name="provider">Domain configuration provider owning all SessionState gateway access.</param>
    /// <param name="logger">The logger instance.</param>
    public SessionStateService(
        SessionStateConfigurationProvider provider,
        ILogger<SessionStateService>? logger = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger ?? NullLogger<SessionStateService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> SaveState<T>(
        string userId,
        string key,
        T value,
        CancellationToken cancellationToken = default)
    {
        SessionStateLog.TraceSaveStateEntry(_logger);

        if (!TryParseUserId(userId, out var userGuid))
            return GenericResult<bool>.Failure(SessionStateLog.SaveStateFailed(_logger, userId, key, "Invalid userId — expected GUID"));

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, SerializerOptions);
            var now = DateTimeOffset.UtcNow;

            var existingResult = await _provider.GetRecord(userGuid, key, cancellationToken).ConfigureAwait(false);

            if (!existingResult.IsSuccess)
            {
                return GenericResult<bool>.Failure(
                    SessionStateLog.SaveStateFailed(_logger, userId, key, existingResult.CurrentMessage ?? "Failed to query existing record"));
            }

            IGenericResult operationResult;

            if (existingResult.Value is not null)
            {
                operationResult = await _provider.Update(userGuid, key,
                    new SessionStateUpdateRecord { StateValue = serializedValue, UpdatedAt = now },
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                operationResult = await _provider.Insert(
                    new SessionStateRecord
                    {
                        Id = Guid.CreateVersion7(),
                        UserId = userGuid,
                        StateKey = key,
                        StateValue = serializedValue,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    cancellationToken).ConfigureAwait(false);
            }

            if (!operationResult.IsSuccess)
            {
                return GenericResult<bool>.Failure(
                    SessionStateLog.SaveStateFailed(_logger, userId, key,
                        operationResult.CurrentMessage ?? "Write command failed"));
            }

            SessionStateLog.StateSaved(_logger, userId, key);
            return GenericResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                SessionStateLog.SaveStateFailed(_logger, userId, key, ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<T?>> GetState<T>(
        string userId,
        string key,
        CancellationToken cancellationToken = default)
    {
        SessionStateLog.TraceGetStateEntry(_logger);

        if (!TryParseUserId(userId, out var userGuid))
            return GenericResult<T?>.Failure(SessionStateLog.GetStateFailed(_logger, userId, key, "Invalid userId — expected GUID"));

        try
        {
            var recordResult = await _provider.GetRecord(userGuid, key, cancellationToken).ConfigureAwait(false);

            if (!recordResult.IsSuccess)
            {
                return GenericResult<T?>.Failure(
                    SessionStateLog.GetStateFailed(_logger, userId, key,
                        recordResult.CurrentMessage ?? "Failed to query record"));
            }

            if (recordResult.Value is null)
            {
                SessionStateLog.StateNotFound(_logger, userId, key);
                return GenericResult<T?>.Success(default);
            }

            try
            {
                var value = JsonSerializer.Deserialize<T>(recordResult.Value.StateValue, SerializerOptions);
                SessionStateLog.StateRetrieved(_logger, userId, key);
                return GenericResult<T?>.Success(value);
            }
            catch (JsonException ex)
            {
                return GenericResult<T?>.Failure(
                    SessionStateLog.DeserializationFailed(_logger, userId, key, ex.Message));
            }
        }
        catch (Exception ex)
        {
            return GenericResult<T?>.Failure(
                SessionStateLog.GetStateFailed(_logger, userId, key, ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> DeleteState(
        string userId,
        string key,
        CancellationToken cancellationToken = default)
    {
        SessionStateLog.TraceDeleteStateEntry(_logger);

        if (!TryParseUserId(userId, out var userGuid))
            return GenericResult<bool>.Failure(SessionStateLog.DeleteStateFailed(_logger, userId, key, "Invalid userId — expected GUID"));

        try
        {
            var result = await _provider.Delete(userGuid, key, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return GenericResult<bool>.Failure(
                    SessionStateLog.DeleteStateFailed(_logger, userId, key,
                        result.CurrentMessage ?? "Delete command failed"));
            }

            SessionStateLog.StateDeleted(_logger, userId, key);
            return GenericResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                SessionStateLog.DeleteStateFailed(_logger, userId, key, ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<string>>> GetAllKeys(
        string userId,
        CancellationToken cancellationToken = default)
    {
        SessionStateLog.TraceGetAllKeysEntry(_logger);

        if (!TryParseUserId(userId, out var userGuid))
            return GenericResult<IReadOnlyList<string>>.Failure(SessionStateLog.GetAllKeysFailed(_logger, userId, "Invalid userId — expected GUID"));

        try
        {
            var result = await _provider.GetAllRecords(userGuid, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return GenericResult<IReadOnlyList<string>>.Failure(
                    SessionStateLog.GetAllKeysFailed(_logger, userId,
                        result.CurrentMessage ?? "Query command failed"));
            }

            var keys = result.Value!.Select(r => r.StateKey).ToList();
            SessionStateLog.AllKeysRetrieved(_logger, userId, keys.Count);
            return GenericResult<IReadOnlyList<string>>.Success(keys);
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<string>>.Failure(
                SessionStateLog.GetAllKeysFailed(_logger, userId, ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> ClearAll(
        string userId,
        CancellationToken cancellationToken = default)
    {
        SessionStateLog.TraceClearAllEntry(_logger);

        if (!TryParseUserId(userId, out var userGuid))
            return GenericResult<bool>.Failure(SessionStateLog.ClearAllFailed(_logger, userId, "Invalid userId — expected GUID"));

        try
        {
            var result = await _provider.DeleteAll(userGuid, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return GenericResult<bool>.Failure(
                    SessionStateLog.ClearAllFailed(_logger, userId,
                        result.CurrentMessage ?? "Delete command failed"));
            }

            SessionStateLog.AllStateCleared(_logger, userId);
            return GenericResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                SessionStateLog.ClearAllFailed(_logger, userId, ex.Message));
        }
    }

    // Why: The interface accepts string userId (from JWT sub claim). The DDL column is
    // UNIQUEIDENTIFIER. Parse once and pass the Guid to all provider calls.
    private static bool TryParseUserId(string userId, out Guid parsed)
        => Guid.TryParse(userId, out parsed);
}
