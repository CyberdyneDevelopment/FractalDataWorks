using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Fdw.Services.Data.Limits;

/// <summary>
/// Singleton in-memory store for per-connection daily budget counters.
///
/// Design:
/// - In-memory counters for fast hot-path checks (no DB read per query).
/// - Counters are loaded from <c>ops.ConnectionLimitCounter</c> on first access.
/// - Increments are written back to the DB periodically by the background flush loop.
/// - At midnight UTC the DailyLimitResetJob zeros all counters (both in-memory
///   and in the DB row) for the new day.
/// - On process restart, in-memory counters are reloaded from the DB row, so budget
///   already consumed in the current day is not forgotten.
/// </summary>
internal sealed class ConnectionLimitCounterStore
{
    private readonly ConcurrentDictionary<Guid, CounterEntry> _counters = new();

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private sealed class CounterEntry
    {
        public long QueryCount;
        public long ByteCount;
        public DateTimeOffset LastReset;

        public CounterEntry(long queryCount, long byteCount, DateTimeOffset lastReset)
        {
            QueryCount = queryCount;
            ByteCount = byteCount;
            LastReset = lastReset;
        }
    }

    /// <summary>
    /// Increments the query counter for the given connection and returns the new total.
    /// </summary>
    public long IncrementQueryCount(Guid connectionId)
    {
        var entry = _counters.GetOrAdd(connectionId,
            static id => new CounterEntry(0, 0, DateTimeOffset.UtcNow));
        return Interlocked.Increment(ref entry.QueryCount);
    }

    /// <summary>
    /// Increments the byte counter for the given connection and returns the new total.
    /// </summary>
    public long IncrementByteCount(Guid connectionId, long byteDelta)
    {
        var entry = _counters.GetOrAdd(connectionId,
            static id => new CounterEntry(0, 0, DateTimeOffset.UtcNow));
        return Interlocked.Add(ref entry.ByteCount, byteDelta);
    }

    /// <summary>
    /// Reads the current counters without modifying them.
    /// </summary>
    public (long queries, long bytes) Read(Guid connectionId)
    {
        if (_counters.TryGetValue(connectionId, out var entry))
            return (Volatile.Read(ref entry.QueryCount), Volatile.Read(ref entry.ByteCount));
        return (0L, 0L);
    }

    /// <summary>
    /// Seeds the in-memory counters from a DB-loaded value.
    /// Called at startup to restore counters consumed earlier in the current day.
    /// Skips the entry if an in-memory counter already exists (startup race guard).
    /// </summary>
    public void Seed(Guid connectionId, long queryCount, long byteCount, DateTimeOffset lastReset)
    {
        _counters.TryAdd(connectionId, new CounterEntry(queryCount, byteCount, lastReset));
    }

    /// <summary>
    /// Resets counters for all tracked connections to zero.
    /// Called by the nightly DailyLimitResetJob after the DB row is zeroed.
    /// </summary>
    public void ResetAll()
    {
        foreach (var entry in _counters.Values)
        {
            Interlocked.Exchange(ref entry.QueryCount, 0L);
            Interlocked.Exchange(ref entry.ByteCount, 0L);
        }
    }

    /// <summary>
    /// Enumerates all tracked connections and their current counters for DB flush.
    /// </summary>
    public System.Collections.Generic.IEnumerable<(Guid connectionId, long queries, long bytes)> Snapshot()
    {
        foreach (var kv in _counters)
            yield return (kv.Key,
                Volatile.Read(ref kv.Value.QueryCount),
                Volatile.Read(ref kv.Value.ByteCount));
    }
}
