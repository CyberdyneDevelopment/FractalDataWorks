using System;
using System.Collections.Generic;
using System.Threading;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Per-task inspector state for a single test execution.
/// All counter fields use <see cref="Interlocked"/> operations for thread safety.
/// </summary>
public sealed class TaskInspectorState
{
    private long _recordsIn;
    private long _recordsOut;
    private long _recordsDiscarded;
    private long _recordsHeld;
    private long _samplesDiscarded;

    private readonly List<IDictionary<string, object?>> _samples = new();

    /// <summary>Gets the synchronization object for sample buffer mutations.</summary>
    public object SamplesLock { get; } = new object();

    /// <summary>Gets or sets the total records received by this task node.</summary>
    public long RecordsIn
    {
        get => Interlocked.Read(ref _recordsIn);
        set => Interlocked.Exchange(ref _recordsIn, value);
    }

    /// <summary>Gets or sets the total records emitted on the data stream.</summary>
    public long RecordsOut
    {
        get => Interlocked.Read(ref _recordsOut);
        set => Interlocked.Exchange(ref _recordsOut, value);
    }

    /// <summary>Gets or sets the total records routed to a reject/error stream.</summary>
    public long RecordsDiscarded
    {
        get => Interlocked.Read(ref _recordsDiscarded);
        set => Interlocked.Exchange(ref _recordsDiscarded, value);
    }

    /// <summary>Gets or sets the current number of records being processed in the active batch.</summary>
    public long RecordsHeld
    {
        get => Interlocked.Read(ref _recordsHeld);
        set => Interlocked.Exchange(ref _recordsHeld, value);
    }

    /// <summary>Gets the count of samples evicted from the ring buffer due to the byte cap.</summary>
    public long SamplesDiscarded
    {
        get => Interlocked.Read(ref _samplesDiscarded);
        set => Interlocked.Exchange(ref _samplesDiscarded, value);
    }

    /// <summary>
    /// Gets or sets whether the sample ring buffer has reached its byte cap at least once.
    /// Used to trigger the amber alert in the inspector UI.
    /// </summary>
    public bool SampleBufferAtCapacity { get; set; }

    /// <summary>Gets the sample ring buffer (up to byte budget).</summary>
    public IReadOnlyList<IDictionary<string, object?>> Samples => _samples;

    /// <summary>Gets the byte budget in effect for this execution (for UI display).</summary>
    public long SampleBufferMaxBytes { get; set; }

    /// <summary>
    /// Adds <paramref name="value"/> to <see cref="RecordsIn"/> atomically.
    /// </summary>
    public void AddRecordsIn(long value) => Interlocked.Add(ref _recordsIn, value);

    /// <summary>
    /// Adds <paramref name="value"/> to <see cref="RecordsOut"/> atomically.
    /// </summary>
    public void AddRecordsOut(long value) => Interlocked.Add(ref _recordsOut, value);

    /// <summary>
    /// Adds <paramref name="value"/> to <see cref="RecordsDiscarded"/> atomically.
    /// </summary>
    public void AddRecordsDiscarded(long value) => Interlocked.Add(ref _recordsDiscarded, value);

    /// <summary>
    /// Adds <paramref name="delta"/> to <see cref="RecordsHeld"/> atomically.
    /// </summary>
    public void AddRecordsHeld(long delta) => Interlocked.Add(ref _recordsHeld, delta);

    /// <summary>
    /// Increments <see cref="SamplesDiscarded"/> atomically.
    /// </summary>
    public void IncrementSamplesDiscarded() => Interlocked.Increment(ref _samplesDiscarded);

    /// <summary>
    /// Evicts oldest records from the sample buffer until the byte budget allows
    /// <paramref name="estimatedBytes"/>, then adds <paramref name="record"/>.
    /// Must be called while holding <see cref="SamplesLock"/>.
    /// </summary>
    /// <param name="record">The record to add.</param>
    /// <param name="estimatedBytes">Pre-computed byte estimate for <paramref name="record"/>.</param>
    /// <param name="remainingBytes">Current remaining bytes in the shared bucket budget.</param>
    /// <param name="estimateBytes">Delegate to compute byte size of an existing record (used for evicted records).</param>
    /// <param name="reportByteDelta">Callback to adjust the shared bucket's used-bytes counter.</param>
    /// <returns>True if the budget was already at capacity before eviction began.</returns>
    public bool AddSampleRecord(
        IDictionary<string, object?> record,
        long estimatedBytes,
        long remainingBytes,
        Func<IDictionary<string, object?>, long> estimateBytes,
        Action<long> reportByteDelta)
    {
        var wasAtCap = remainingBytes < estimatedBytes;
        while (remainingBytes < estimatedBytes && _samples.Count > 0)
        {
            var oldest = _samples[0];
            var oldestBytes = estimateBytes(oldest);
            _samples.RemoveAt(0);
            IncrementSamplesDiscarded();
            reportByteDelta(-oldestBytes);
            remainingBytes += oldestBytes;
        }

        _samples.Add(record);
        reportByteDelta(estimatedBytes);
        return wasAtCap;
    }
}
