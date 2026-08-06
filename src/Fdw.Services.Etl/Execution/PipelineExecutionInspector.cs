using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Singleton implementation of <see cref="IPipelineExecutionInspector"/>.
/// All state is ephemeral (lost on restart). Production executions never use this class.
/// </summary>
public sealed class PipelineExecutionInspector : IPipelineExecutionInspector
{
    private readonly ILogger<PipelineExecutionInspector> _logger;

    // Why: Two-level ConcurrentDictionary: outer key=executionId, inner key=taskId|edgeKey.
    // Thread-safe without a global write lock on the hot path (record tracking per batch).
    private readonly ConcurrentDictionary<Guid, ExecutionInspectorBucket> _buckets = new();

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineExecutionInspector"/>.
    /// </summary>
    public PipelineExecutionInspector(ILogger<PipelineExecutionInspector>? logger = null)
    {
        _logger = logger ?? NullLogger<PipelineExecutionInspector>.Instance;
    }

    /// <inheritdoc/>
    public void RegisterExecution(Guid executionId, PipelineExecutionOptions options)
    {
        _buckets[executionId] = new ExecutionInspectorBucket(options.SampleBufferMaxBytes);
    }

    /// <inheritdoc/>
    public void UnregisterExecution(Guid executionId)
    {
        _buckets.TryRemove(executionId, out _);
    }

    /// <inheritdoc/>
    public bool IsTestExecution(Guid executionId) => _buckets.ContainsKey(executionId);

    /// <inheritdoc/>
    public void RecordTaskIn(Guid executionId, Guid taskId, int count)
    {
        if (!_buckets.TryGetValue(executionId, out var bucket)) return;
        bucket.GetOrAddTask(taskId).AddRecordsIn(count);
    }

    /// <inheritdoc/>
    public void RecordTaskOut(Guid executionId, Guid taskId, int count)
    {
        if (!_buckets.TryGetValue(executionId, out var bucket)) return;
        bucket.GetOrAddTask(taskId).AddRecordsOut(count);
    }

    /// <inheritdoc/>
    public void RecordTaskDiscarded(Guid executionId, Guid taskId, int count)
    {
        if (!_buckets.TryGetValue(executionId, out var bucket)) return;
        bucket.GetOrAddTask(taskId).AddRecordsDiscarded(count);
    }

    /// <inheritdoc/>
    public void RecordTaskHeld(Guid executionId, Guid taskId, int delta)
    {
        if (!_buckets.TryGetValue(executionId, out var bucket)) return;
        bucket.GetOrAddTask(taskId).AddRecordsHeld(delta);
    }

    /// <inheritdoc/>
    public void AddTaskSamples(Guid executionId, Guid taskId, IEnumerable<IDictionary<string, object?>> records)
    {
        if (!_buckets.TryGetValue(executionId, out var bucket)) return;
        var state = bucket.GetOrAddTask(taskId);
        lock (state.SamplesLock)
        {
            foreach (var record in records)
            {
                var estimatedBytes = EstimateRecordBytes(record);
                var wasAtCap = state.AddSampleRecord(
                    record,
                    estimatedBytes,
                    bucket.RemainingBytes,
                    EstimateRecordBytes,
                    delta => Interlocked.Add(ref bucket.UsedBytes, delta));

                if (!state.SampleBufferAtCapacity && wasAtCap)
                    state.SampleBufferAtCapacity = true;
            }
        }
    }

    /// <inheritdoc/>
    public void RecordEdgeFlow(Guid executionId, Guid sourceTaskId, Guid targetTaskId, int count)
    {
        if (!_buckets.TryGetValue(executionId, out var bucket)) return;
        bucket.GetOrAddEdge(sourceTaskId, targetTaskId).AddRecordsFlowed(count);
    }

    /// <inheritdoc/>
    public void AddEdgeSamples(Guid executionId, Guid sourceTaskId, Guid targetTaskId, IEnumerable<IDictionary<string, object?>> records)
    {
        if (!_buckets.TryGetValue(executionId, out var bucket)) return;
        var state = bucket.GetOrAddEdge(sourceTaskId, targetTaskId);
        lock (state.SamplesLock)
        {
            foreach (var record in records)
            {
                var estimatedBytes = EstimateRecordBytes(record);
                var wasAtCap = state.AddSampleRecord(
                    record,
                    estimatedBytes,
                    bucket.RemainingBytes,
                    EstimateRecordBytes,
                    delta => Interlocked.Add(ref bucket.UsedBytes, delta));

                if (!state.SampleBufferAtCapacity && wasAtCap)
                    state.SampleBufferAtCapacity = true;
            }
        }
    }

    /// <inheritdoc/>
    public TaskInspectorState? GetTaskState(Guid executionId, Guid taskId)
    {
        if (!_buckets.TryGetValue(executionId, out var bucket)) return null;
        bucket.Tasks.TryGetValue(taskId, out var state);
        return state;
    }

    /// <inheritdoc/>
    public EdgeInspectorState? GetEdgeState(Guid executionId, Guid sourceTaskId, Guid targetTaskId)
    {
        if (!_buckets.TryGetValue(executionId, out var bucket)) return null;
        var key = BuildEdgeKey(sourceTaskId, targetTaskId);
        bucket.Edges.TryGetValue(key, out var state);
        return state;
    }

    // Why: JSON serialization provides a reasonable byte estimate without reflection.
    // Approximate — actual CLR object size is larger — but sufficient for ring-buffer
    // eviction purposes since we're bounding transport-visible data size.
    private static long EstimateRecordBytes(IDictionary<string, object?> record)
    {
        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(record).Length;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            // Why: Fallback to a fixed estimate when serialization fails (e.g., circular refs).
            // ex is observed via the when clause; unexpected exceptions propagate.
            return 512;
        }
    }

    private static string BuildEdgeKey(Guid sourceTaskId, Guid targetTaskId)
        => $"{sourceTaskId:N}_{targetTaskId:N}";

    /// <summary>
    /// Per-execution container holding task/edge state and the shared byte budget counter.
    /// </summary>
    private sealed class ExecutionInspectorBucket
    {
        private readonly long _maxBytes;

        public ExecutionInspectorBucket(long maxBytes)
        {
            _maxBytes = maxBytes;
        }

        // Why: Exposed as a field (not property) so Interlocked.Add(ref bucket.UsedBytes, delta)
        // can be called from the inspector methods above. This is a private nested class, so
        // CA1051 (visible instance fields) does not apply.
        public long UsedBytes;

        public long RemainingBytes => _maxBytes <= 0
            ? long.MaxValue
            : Math.Max(0, _maxBytes - Interlocked.Read(ref UsedBytes));

        public ConcurrentDictionary<Guid, TaskInspectorState> Tasks { get; } = new();

        // Why: StringComparer.Ordinal used on edge key dictionary per MA0002 requirement.
        public ConcurrentDictionary<string, EdgeInspectorState> Edges { get; } = new(StringComparer.Ordinal);

        public TaskInspectorState GetOrAddTask(Guid taskId)
            => Tasks.GetOrAdd(taskId, _ => new TaskInspectorState { SampleBufferMaxBytes = _maxBytes });

        public EdgeInspectorState GetOrAddEdge(Guid sourceTaskId, Guid targetTaskId)
        {
            var key = $"{sourceTaskId:N}_{targetTaskId:N}";
            return Edges.GetOrAdd(key, _ => new EdgeInspectorState { SampleBufferMaxBytes = _maxBytes });
        }
    }
}
