using System;
using System.Collections.Generic;
using System.Threading;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Per-edge inspector state for a single test execution.
/// All counter fields use <see cref="Interlocked"/> operations for thread safety.
/// </summary>
public sealed class EdgeInspectorState
{
    private long _recordsFlowed;
    private long _samplesDiscarded;

    // Why: Private list with a public lock object so the inspector (different assembly)
    // can synchronize access via lock(state.SamplesLock) while still using the public
    // AddSampleRecord / Samples members. The list itself never escapes the class boundary.
    private readonly List<IDictionary<string, object?>> _samples = new();

    /// <summary>Gets the synchronization object for sample buffer mutations.</summary>
    public object SamplesLock { get; } = new object();

    /// <summary>Gets the total records that have flowed across this edge.</summary>
    public long RecordsFlowed
    {
        get => Interlocked.Read(ref _recordsFlowed);
        set => Interlocked.Exchange(ref _recordsFlowed, value);
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

    /// <summary>Adds <paramref name="value"/> to <see cref="RecordsFlowed"/> atomically.</summary>
    public void AddRecordsFlowed(long value) => Interlocked.Add(ref _recordsFlowed, value);

    /// <summary>Increments <see cref="SamplesDiscarded"/> atomically.</summary>
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
    // Why: Mirrors TaskInspectorState.AddSampleRecord — eviction stays inside the state class
    // so the inspector never needs cross-assembly access to the private _samples list.
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
