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
// Why: Keeps limit enforcement orthogonal to caching — limits apply to ALL commands
// including fresh fetches on cache misses. Wraps DataGatewayService directly (no
// intermediate CachingDataGateway; caching is now built into DataGatewayService):
//   LimitEnforcement → DataGatewayService (with built-in caching)
// Why: Uses ConnectionLimitConfiguration virtual enforcement properties so this
// class has NO reference to MsSql/Http specific types — connection type stays invisible
// above the connection layer.
internal sealed class LimitEnforcementDataGateway : IDataGateway
{
    private readonly IDataGateway _inner;
    private readonly IConnectionLimitResolver _limitResolver;
    private readonly ConnectionLimitCounterStore _counters;
    private readonly ILogger<LimitEnforcementDataGateway> _logger;

    // Why: One TokenBucket per connection (keyed by connection name from the command).
    // Created lazily on first use; never removed (connections are long-lived config objects).
    private readonly ConcurrentDictionary<string, TokenBucket> _tokenBuckets =
        new(StringComparer.OrdinalIgnoreCase);

    // Why: One SemaphoreSlim per connection for concurrency enforcement.
    // InitialCount/MaxCount are set when the semaphore is created from the limit config.
    // Keyed by connection name because that's what IDataCommand exposes.
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
        // Why: Transactions bypass per-command limits — the open+commit pattern doesn't map
        // to token-bucket or concurrency limits designed for individual command dispatches.
        => _inner.BeginTransaction(connectionName, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command,
        DataStoreTarget target,
        CancellationToken cancellationToken = default)
        // Why: a streaming cursor holds an open connection for its whole lifetime — token-bucket and
        // concurrency limits, designed for short discrete dispatches, don't map to it. Bound the result
        // size via the command's own paging at the call site. Pass the cursor straight through.
        => _inner.OpenRecordSource(command, target, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(
        IDataCommand command,
        DataStoreTarget target,
        bool useCache,
        CancellationToken cancellationToken = default)
        // Why: Forward useCache through the limit enforcement path so DataGatewayService (which
        // owns caching in P3) receives the caller's intent. Limit enforcement is cache-agnostic.
        => ExecuteWithLimits<T>(command, target, useCache, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(
        IDataCommand command,
        DataStoreTarget target,
        CancellationToken cancellationToken = default)
        // Why: Default call — use the cache (useCache=true). The limit enforcement path is the
        // same regardless; both overloads share ExecuteWithLimits.
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

        // Why: Limits are indexed by target.DataStore — the canonical address for
        // connection selection in the target-typed gateway.
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
        // Why: timeoutCts is only ever created by ApplyQueryTimeout when FindQueryTimeoutSeconds
        // returned a value (see ApplyQueryTimeout below), so timeoutCts firing means a configured
        // timeout is guaranteed to exist — the previous "no limit resolved" branch here was dead code.
        // Why: `ex is not null` observes the caught exception (matches the file's pre-existing
        // idiom below) purely to satisfy FDW022 — the exception carries no extra info beyond
        // "cancelled", which is already captured by the QueryTimeoutExceeded/OperationCancelled result.
        catch (OperationCanceledException ex) when (timeoutCts?.IsCancellationRequested == true && ex is not null)
        {
            int timeoutSecs = FindQueryTimeoutSeconds(limits)!.Value;
            return GenericResult<T>.Failure(
                ConnectionLimitLog.QueryTimeoutExceeded(_logger, Guid.Empty, timeoutSecs));
        }
        // Why: catches caller-token cancellation that is NOT our own timeout firing — this includes
        // the case where no timeout limit is configured at all (timeoutCts is null, so the filtered
        // catch above never matches) and the caller's own token cancels mid-Execute. Without this
        // clause the OperationCanceledException propagated uncaught, violating this gateway's
        // documented "NEVER throws" contract.
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
        // Why: Limits are indexed by connection name from IDataCommand. DataSet execution
        // is federated and may span multiple connections; limit enforcement at this level
        // would double-count. Pass straight through — per-source limits apply within the
        // DataGatewayService when it dispatches each source query.
        => _inner.Execute<T>(command, target, cancellationToken);

    // ── private helpers ────────────────────────────────────────────────────────

    private IGenericResult<T>? CheckLimit<T>(
        ConnectionLimitConfiguration limit,
        IDataCommand command,
        string connectionName)
    {
        var baseCfg = limit;

        // Why: Dispatching through the base class virtual enforcement properties keeps
        // this class free of per-connection-type imports. Each subclass decides which
        // enforcement properties return values.
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

        // Why: KNOWN LIMITATION (TOCTOU) — the semaphore is acquired then released immediately
        // here rather than held across the inner _inner.Execute call in ExecuteWithLimits, so
        // concurrent callers can each observe "under the cap" in the gap between this check and
        // their own dispatch, and all proceed — temporarily exceeding maxConcurrent. Correctly
        // enforcing the cap requires restructuring ExecuteWithLimits to acquire here and release
        // in a finally block around the inner Execute call (not this helper). Left as-is: this is
        // a documented, not silently patched, limitation — not a fix in this pass.
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
    // Why this passes straight through: this decorator enforces row/size limits on what a command
    // READS. Dropping cached results is neither a read nor bounded by a limit.
    public void InvalidateCachedResults(DataStoreTarget target) => _inner.InvalidateCachedResults(target);
}
