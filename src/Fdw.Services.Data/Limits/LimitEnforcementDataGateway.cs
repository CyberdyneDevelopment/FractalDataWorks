using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Limits;

/// <summary>
/// Decorator that enforces per-connection outbound limits before dispatching to the
/// inner <see cref="IDataGateway"/> (which is <c>DataGatewayService</c>, caching built in).
///
/// Limit kinds checked in order:
///   1. RateLimit (token bucket, in-memory singleton)
///   2. ConcurrencyLimit (SemaphoreSlim per connection, non-blocking)
///   3. MaxResultSize (cap on Paging.Take if command is IQueryCommand)
///   4. QueryTimeout (linked CancellationTokenSource wrapping the inner CT)
///   5. DailyBudget (in-memory counter checked before dispatch)
///
/// On exceeded: returns GenericResult.Failure with the appropriate log message.
/// NEVER throws. NEVER blocks (non-blocking semaphore TryEnter).
/// </summary>
internal sealed class LimitEnforcementDataGateway : IDataGateway
{
    private readonly IDataGateway _inner;
    private readonly IConnectionLimitResolver _limitResolver;
    private readonly ConnectionLimitCounterStore _counters;
    private readonly ILogger<LimitEnforcementDataGateway> _logger;

    private readonly ConcurrentDictionary<string, TokenBucket> _tokenBuckets =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _concurrencyGates =
        new(StringComparer.OrdinalIgnoreCase);

    public LimitEnforcementDataGateway(
        IDataGateway inner,
        IConnectionLimitResolver limitResolver,
        ConnectionLimitCounterStore counters,
        ILoggerFactory? loggerFactory)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _limitResolver = limitResolver ?? throw new ArgumentNullException(nameof(limitResolver));
        _counters = counters ?? throw new ArgumentNullException(nameof(counters));
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<LimitEnforcementDataGateway>();
    }

    /// <inheritdoc />
    public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(
        string connectionName,
        CancellationToken cancellationToken = default)
        => _inner.BeginTransaction(connectionName, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command,
        DataStoreTarget target,
        CancellationToken cancellationToken = default)
        => _inner.OpenRecordSource(command, target, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(
        IDataCommand command,
        DataStoreTarget target,
        bool useCache,
        CancellationToken cancellationToken = default)
        => ExecuteWithLimits<T>(command, target, useCache, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(
        IDataCommand command,
        DataStoreTarget target,
        CancellationToken cancellationToken = default)
        => ExecuteWithLimits<T>(command, target, useCache: true, cancellationToken);

    private async Task<IGenericResult<T>> ExecuteWithLimits<T>(
        IDataCommand command,
        DataStoreTarget target,
        bool useCache,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return GenericResult<T>.Failure(
                ConnectionLimitLog.OperationCancelled(_logger, Guid.Empty));

        var connectionName = target.DataStore;

        var limitsResult = _limitResolver.Resolve(connectionName, cancellationToken);
        if (!limitsResult.IsSuccess)
        {
            ConnectionLimitLog.LimitResolutionFailed(
                _logger, Guid.Empty, limitsResult.CurrentMessage ?? "unknown");
            return await _inner.Execute<T>(command, target, useCache, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<ConnectionLimitConfiguration> limits = limitsResult.Value
            ?? (IReadOnlyList<ConnectionLimitConfiguration>)[];

        if (limits.Count == 0)
        {
            ConnectionLimitLog.NoLimitsConfigured(_logger, Guid.Empty);
            return await _inner.Execute<T>(command, target, useCache, cancellationToken).ConfigureAwait(false);
        }

        foreach (var limit in limits)
        {
            var checkResult = CheckLimit<T>(limit, command, connectionName);
            if (checkResult is not null)
                return checkResult;
        }

        var effectiveCt = ApplyQueryTimeout(limits, cancellationToken, out var timeoutCts);

        try
        {
            var result = await _inner.Execute<T>(command, target, useCache, effectiveCt).ConfigureAwait(false);

            if (result.IsSuccess)
                IncrementDailyCounters(limits);

            return result;
        }
        catch (OperationCanceledException ex) when (timeoutCts?.IsCancellationRequested == true && ex is not null)
        {
            int timeoutSecs = FindQueryTimeoutSeconds(limits)!.Value;
            return GenericResult<T>.Failure(
                ConnectionLimitLog.QueryTimeoutExceeded(_logger, Guid.Empty, timeoutSecs));
        }
        catch (OperationCanceledException ex) when (ex is not null)
        {
            return GenericResult<T>.Failure(
                ConnectionLimitLog.OperationCancelled(_logger, Guid.Empty));
        }
        finally
        {
            timeoutCts?.Dispose();
        }
    }

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(
        IDataCommand command,
        DataSetTarget target,
        CancellationToken cancellationToken = default)
        => _inner.Execute<T>(command, target, cancellationToken);

    // ── private helpers ────────────────────────────────────────────────────────

    private IGenericResult<T>? CheckLimit<T>(
        ConnectionLimitConfiguration limit,
        IDataCommand command,
        string connectionName)
    {
        var baseCfg = limit;

        if (baseCfg.EnforceMaxPerSecond.HasValue)
        {
            var rateResult = CheckRateLimit<T>(baseCfg, connectionName);
            if (rateResult is not null)
                return rateResult;
        }

        if (baseCfg.EnforceMaxConcurrent.HasValue)
        {
            var concurrencyResult = CheckConcurrency<T>(baseCfg, connectionName);
            if (concurrencyResult is not null)
                return concurrencyResult;
        }

        if (baseCfg.EnforceMaxRows.HasValue)
        {
            var sizeResult = CheckMaxResultSize<T>(baseCfg, command);
            if (sizeResult is not null)
                return sizeResult;
        }

        if (baseCfg.EnforceMaxQueriesPerDay.HasValue || baseCfg.EnforceMaxBytesPerDay.HasValue)
        {
            var budgetResult = CheckDailyBudget<T>(baseCfg);
            if (budgetResult is not null)
                return budgetResult;
        }

        return null;
    }

    private IGenericResult<T>? CheckRateLimit<T>(ConnectionLimitConfiguration limit, string connectionName)
    {
        int maxPerSecond = limit.EnforceMaxPerSecond!.Value;
        double burst = limit.EnforceBurstSize ?? maxPerSecond;
        var bucket = _tokenBuckets.GetOrAdd(connectionName,
            _ => new TokenBucket(maxPerSecond, burst));

        if (bucket.TryConsume())
            return null;

        double rate = maxPerSecond - bucket.CurrentTokens;
        return GenericResult<T>.Failure(
            ConnectionLimitLog.RateExceeded(_logger, limit.ConnectionConfigurationId, rate, maxPerSecond));
    }

    private IGenericResult<T>? CheckConcurrency<T>(ConnectionLimitConfiguration limit, string connectionName)
    {
        int maxConcurrent = limit.EnforceMaxConcurrent!.Value;
        var semaphore = _concurrencyGates.GetOrAdd(connectionName,
            _ => new SemaphoreSlim(maxConcurrent, maxConcurrent));

        if (!semaphore.Wait(0))
        {
            return GenericResult<T>.Failure(
                ConnectionLimitLog.ConcurrencyBlocked(
                    _logger,
                    limit.ConnectionConfigurationId,
                    maxConcurrent - semaphore.CurrentCount,
                    maxConcurrent));
        }

        semaphore.Release();
        return null;
    }

    private IGenericResult<T>? CheckMaxResultSize<T>(ConnectionLimitConfiguration limit, IDataCommand command)
    {
        int maxRows = limit.EnforceMaxRows!.Value;
        if (command is IQueryCommand qc && qc.Paging is not null)
        {
            int requested = qc.Paging.Take ?? int.MaxValue;
            if (requested > maxRows)
                return GenericResult<T>.Failure(
                    ConnectionLimitLog.MaxResultSizeExceeded(
                        _logger,
                        limit.ConnectionConfigurationId,
                        requested,
                        maxRows));
        }

        return null;
    }

    private IGenericResult<T>? CheckDailyBudget<T>(ConnectionLimitConfiguration limit)
    {
        var (queries, bytes) = _counters.Read(limit.ConnectionConfigurationId);

        if (limit.EnforceMaxQueriesPerDay.HasValue && queries >= limit.EnforceMaxQueriesPerDay.Value)
            return GenericResult<T>.Failure(
                ConnectionLimitLog.DailyQueryBudgetExhausted(
                    _logger,
                    limit.ConnectionConfigurationId,
                    queries,
                    limit.EnforceMaxQueriesPerDay.Value));

        if (limit.EnforceMaxBytesPerDay.HasValue && bytes >= limit.EnforceMaxBytesPerDay.Value)
            return GenericResult<T>.Failure(
                ConnectionLimitLog.DailyByteBudgetExhausted(
                    _logger,
                    limit.ConnectionConfigurationId,
                    bytes,
                    limit.EnforceMaxBytesPerDay.Value));

        return null;
    }

    private static CancellationToken ApplyQueryTimeout(
        IReadOnlyList<ConnectionLimitConfiguration> limits,
        CancellationToken callerCt,
        out CancellationTokenSource? timeoutCts)
    {
        int? timeoutSeconds = FindQueryTimeoutSeconds(limits);
        if (timeoutSeconds is null)
        {
            timeoutCts = null;
            return callerCt;
        }

        timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds.Value));
        return CancellationTokenSource.CreateLinkedTokenSource(callerCt, timeoutCts.Token).Token;
    }

    private static int? FindQueryTimeoutSeconds(IReadOnlyList<ConnectionLimitConfiguration> limits)
    {
        foreach (var limit in limits)
        {
            if (limit.EnforceTimeoutSeconds.HasValue)
                return limit.EnforceTimeoutSeconds.Value;
        }

        return null;
    }

    private void IncrementDailyCounters(IReadOnlyList<ConnectionLimitConfiguration> limits)
    {
        foreach (var limit in limits)
        {
            if (limit.EnforceMaxQueriesPerDay.HasValue || limit.EnforceMaxBytesPerDay.HasValue)
                _counters.IncrementQueryCount(limit.ConnectionConfigurationId);
        }
    }

    /// <inheritdoc/>
    public void InvalidateCachedResults(DataStoreTarget target) => _inner.InvalidateCachedResults(target);
}
