using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// In-memory inspector for test-mode executions. Retains per-task and per-edge counters
/// plus a sample ring buffer of recently processed records.
/// Production executions NEVER use this inspector — no PII leakage to non-test runs.
/// </summary>
public interface IPipelineExecutionInspector
{
    /// <summary>
    /// Initializes state for a new test execution. Must be called before any record-tracking calls.
    /// </summary>
    /// <param name="executionId">The test execution ID.</param>
    /// <param name="options">Execution options supplying the byte budget for sample buffers.</param>
    void RegisterExecution(Guid executionId, PipelineExecutionOptions options);

    /// <summary>
    /// Removes all state for a completed test execution, releasing memory.
    /// </summary>
    /// <param name="executionId">The test execution ID to remove.</param>
    void UnregisterExecution(Guid executionId);

    /// <summary>
    /// Records that a batch of records was received by a task node.
    /// </summary>
    void RecordTaskIn(Guid executionId, Guid taskId, int count);

    /// <summary>
    /// Records that a batch of records was emitted from a task node on the data stream.
    /// </summary>
    void RecordTaskOut(Guid executionId, Guid taskId, int count);

    /// <summary>
    /// Records that a batch of records was rejected/discarded by a task node.
    /// </summary>
    void RecordTaskDiscarded(Guid executionId, Guid taskId, int count);

    /// <summary>
    /// Records a delta to the count of records currently held in a task node's processing window.
    /// Pass a negative value to decrement when the batch completes.
    /// </summary>
    void RecordTaskHeld(Guid executionId, Guid taskId, int delta);

    /// <summary>
    /// Adds records to the task's sample ring buffer, evicting the oldest entries when the
    /// execution-level byte budget is exceeded. Increments <see cref="TaskInspectorState.SamplesDiscarded"/>
    /// for each evicted record.
    /// </summary>
    void AddTaskSamples(Guid executionId, Guid taskId, IEnumerable<IDictionary<string, object?>> records);

    /// <summary>
    /// Records that a batch of records flowed across an edge.
    /// </summary>
    void RecordEdgeFlow(Guid executionId, Guid sourceTaskId, Guid targetTaskId, int count);

    /// <summary>
    /// Adds records to the edge's sample ring buffer, evicting oldest entries when the
    /// execution-level byte budget is exceeded. Increments <see cref="EdgeInspectorState.SamplesDiscarded"/>
    /// for each evicted record.
    /// </summary>
    void AddEdgeSamples(Guid executionId, Guid sourceTaskId, Guid targetTaskId, IEnumerable<IDictionary<string, object?>> records);

    /// <summary>
    /// Returns the current inspector state for a task node, or null if the execution is not
    /// registered or is not a test execution.
    /// </summary>
    TaskInspectorState? GetTaskState(Guid executionId, Guid taskId);

    /// <summary>
    /// Returns the current inspector state for an edge, or null if the execution is not
    /// registered or is not a test execution.
    /// </summary>
    EdgeInspectorState? GetEdgeState(Guid executionId, Guid sourceTaskId, Guid targetTaskId);

    /// <summary>
    /// Returns true if the given execution ID is a registered test execution (not production).
    /// Endpoints should return 404 when this returns false.
    /// </summary>
    bool IsTestExecution(Guid executionId);
}
